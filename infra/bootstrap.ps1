#Requires -Version 7.0
<#
.SYNOPSIS
  One-time bootstrap for RecipesApp Azure deployment.

.DESCRIPTION
  Creates the resource group, an Azure AD application with GitHub OIDC federated
  credentials, the service principal, and a Contributor role assignment scoped to the
  resource group. Generates a JWT signing key and writes it to a gitignored file.

  Idempotent — re-running detects existing resources and skips them.

  After this script completes, set the printed GitHub repository variables and secrets,
  then push a change to main. No further manual `az` commands are needed.

.PARAMETER ResourceGroup
  Azure resource group name. Created if it doesn't exist.

.PARAMETER Location
  Azure region for the resource group (e.g. westeurope).

.PARAMETER Prefix
  Resource name prefix used by Bicep (e.g. recipes-prod-abc123). Must be globally
  unique for SQL Server / Key Vault.

.PARAMETER GitHubRepo
  GitHub repository in 'owner/repo' format.

.EXAMPLE
  pwsh infra/bootstrap.ps1 `
    -ResourceGroup RecipesApp `
    -Location westeurope `
    -Prefix recipes-prod-abc123 `
    -GitHubRepo your-org/RecipesApp
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ResourceGroup,

    [Parameter(Mandatory)]
    [string]$Location,

    [Parameter(Mandatory)]
    [string]$Prefix,

    [Parameter(Mandatory)]
    [string]$GitHubRepo
)

$ErrorActionPreference = 'Stop'

# ── Validate inputs ───────────────────────────────────────────────────────────

if ($GitHubRepo -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$') {
    throw "GitHubRepo must be in 'owner/repo' format. Got: '$GitHubRepo'"
}

# ── Verify az CLI + login ─────────────────────────────────────────────────────

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "az CLI not found. Install: https://learn.microsoft.com/cli/azure/install-azure-cli"
}

$accountJson = az account show --output json 2>$null
if (-not $accountJson) {
    throw "Not logged in to Azure. Run 'az login' first."
}
$account = $accountJson | ConvertFrom-Json
$subscriptionId = $account.id
$tenantId = $account.tenantId

Write-Host ""
Write-Host "Subscription : $($account.name) ($subscriptionId)" -ForegroundColor Cyan
Write-Host "Tenant       : $tenantId" -ForegroundColor Cyan
Write-Host "GitHub repo  : $GitHubRepo" -ForegroundColor Cyan
Write-Host ""

# ── Resource providers ────────────────────────────────────────────────────────
# Fresh subscriptions only have a handful of providers registered. Bicep
# deployments fail with MissingSubscriptionRegistration if these aren't
# registered up-front. Registration is asynchronous but registering is a no-op
# once done.

$providers = @(
    'Microsoft.Web'                # App Service, Static Web Apps
    'Microsoft.Sql'                # Azure SQL
    'Microsoft.KeyVault'           # Key Vault
    'Microsoft.Insights'           # Application Insights
    'Microsoft.OperationalInsights' # Log Analytics workspace
    'Microsoft.Storage'            # Blob Storage (recipe images)
)

Write-Host "▶ Resource providers" -ForegroundColor Yellow
foreach ($ns in $providers) {
    $state = az provider show --namespace $ns --query registrationState --output tsv 2>$null
    if ($state -eq 'Registered') {
        Write-Host "  $ns`: already registered"
    } else {
        az provider register --namespace $ns --output none
        Write-Host "  $ns`: registration requested"
    }
}

# ── Resource group ────────────────────────────────────────────────────────────

Write-Host "▶ Resource group: $ResourceGroup ($Location)" -ForegroundColor Yellow
az group create --name $ResourceGroup --location $Location --output none
$rgId = az group show --name $ResourceGroup --query id --output tsv
Write-Host "  ready"

# ── AD app + service principal ────────────────────────────────────────────────

$appDisplayName = "github-actions-$ResourceGroup"

Write-Host "▶ Azure AD app: $appDisplayName" -ForegroundColor Yellow
$existingAppJson = az ad app list --display-name $appDisplayName --query "[0]" --output json
$existingApp = if ($existingAppJson -and $existingAppJson -ne 'null') {
    $existingAppJson | ConvertFrom-Json
} else {
    $null
}

if ($existingApp) {
    $appId = $existingApp.appId
    Write-Host "  exists (appId: $appId)"
} else {
    $appId = az ad app create --display-name $appDisplayName --query appId --output tsv
    Write-Host "  created (appId: $appId)"
}

$existingSpJson = az ad sp list --filter "appId eq '$appId'" --query "[0].id" --output tsv
if ($existingSpJson) {
    Write-Host "  service principal: exists"
} else {
    az ad sp create --id $appId --output none
    Write-Host "  service principal: created"
}

# ── Role assignments ──────────────────────────────────────────────────────────

function Assert-RoleAssignment {
    param([string]$Role)

    $existing = az role assignment list `
        --assignee $appId `
        --scope $rgId `
        --role $Role `
        --output json | ConvertFrom-Json

    if ($existing.Count -eq 0) {
        az role assignment create `
            --assignee $appId `
            --role $Role `
            --scope $rgId `
            --output none
        Write-Host "  $Role`: assigned"
    } else {
        Write-Host "  $Role`: already assigned"
    }
}

Write-Host "▶ Role assignments on $ResourceGroup" -ForegroundColor Yellow
Assert-RoleAssignment -Role "Contributor"
Assert-RoleAssignment -Role "Key Vault Secrets User"
# Needed so the SP can create the Microsoft.Authorization/roleAssignments resources
# declared in infra/main.bicep (grants App Service + MCP Server access to Key Vault).
Assert-RoleAssignment -Role "User Access Administrator"

# ── Federated credentials ─────────────────────────────────────────────────────

$federatedCreds = @(
    @{
        Name        = "github-main"
        Subject     = "repo:${GitHubRepo}:ref:refs/heads/main"
        Description = "Push to main branch"
    },
    @{
        Name        = "github-pr"
        Subject     = "repo:${GitHubRepo}:pull_request"
        Description = "Pull request validation"
    }
)

Write-Host "▶ Federated credentials" -ForegroundColor Yellow
$existingCreds = az ad app federated-credential list --id $appId --output json | ConvertFrom-Json
$existingNames = @($existingCreds | ForEach-Object { $_.name })

foreach ($cred in $federatedCreds) {
    if ($existingNames -contains $cred.Name) {
        Write-Host "  $($cred.Name): exists"
        continue
    }

    $body = @{
        name        = $cred.Name
        issuer      = "https://token.actions.githubusercontent.com"
        subject     = $cred.Subject
        description = $cred.Description
        audiences   = @("api://AzureADTokenExchange")
    } | ConvertTo-Json -Compress

    $tempFile = New-TemporaryFile
    try {
        [System.IO.File]::WriteAllText($tempFile.FullName, $body, [System.Text.UTF8Encoding]::new($false))
        az ad app federated-credential create `
            --id $appId `
            --parameters "@$($tempFile.FullName)" `
            --output none
    } finally {
        Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
    }
    Write-Host "  $($cred.Name): created"
}

# ── Generate secrets ──────────────────────────────────────────────────────────

$keyBytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($keyBytes)
$jwtSigningKey = [Convert]::ToBase64String($keyBytes)

$tokenBytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($tokenBytes)
$mcpServerToken = [Convert]::ToBase64String($tokenBytes)

$repoRoot = Split-Path -Parent $PSScriptRoot
$outputFile = Join-Path $repoRoot ".bootstrap-output.txt"
Set-Content -Path $outputFile -Value "JWT_SIGNING_KEY=$jwtSigningKey`nMCP_SERVER_TOKEN=$mcpServerToken" -Encoding utf8 -NoNewline

# ── Summary ───────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  Bootstrap complete" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Green
Write-Host ""
Write-Host "1. In your GitHub repo (Settings → Secrets and variables → Actions),"
Write-Host "   set these as Variables:"
Write-Host ""
Write-Host "   AZURE_CLIENT_ID         = $appId"
Write-Host "   AZURE_TENANT_ID         = $tenantId"
Write-Host "   AZURE_SUBSCRIPTION_ID   = $subscriptionId"
Write-Host "   AZURE_RESOURCE_GROUP    = $ResourceGroup"
Write-Host "   AZURE_PREFIX            = $Prefix"
Write-Host ""
Write-Host "2. Set these as Secrets (same screen, Secrets tab):"
Write-Host ""
Write-Host "   SQL_ADMIN_PASSWORD      = <choose a strong password — no '@' character>"
Write-Host "   ANTHROPIC_API_KEY       = sk-ant-..."
Write-Host "   JWT_SIGNING_KEY         = (see $outputFile)"
Write-Host "   MCP_SERVER_TOKEN        = (see $outputFile)"
Write-Host ""
Write-Host "3. Push a commit to main. Once the deploy workflows are wired up (later"
Write-Host "   bundles in CICD-1), they will pick up the OIDC trust automatically."
Write-Host ""
Write-Host "After the first infra deploy, populate these additional repo variables"
Write-Host "from the workflow run summary:"
Write-Host ""
Write-Host "   API_HOSTNAME            (e.g. $Prefix-api.azurewebsites.net)"
Write-Host "   KEY_VAULT_NAME          (e.g. $Prefix-kv — exact value in run summary)"
Write-Host ""
Write-Host "To delete this OIDC trust later:"
Write-Host "   az ad app delete --id $appId"
Write-Host ""
