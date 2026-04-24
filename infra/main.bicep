targetScope = 'resourceGroup'

@description('Short prefix for all resource names, e.g. "recipes".')
param prefix string

@description('Azure region for all resources.')
param location string = resourceGroup().location

@secure()
@description('SQL Server administrator password.')
param sqlAdminPassword string

@secure()
@description('Anthropic API key stored in Key Vault.')
param anthropicApiKey string

var tags = {
  project: 'RecipesApp'
  environment: 'production'
}

// Key Vault name must be globally unique, max 24 chars, alphanumeric + hyphens
var kvPrefix = length(prefix) > 20 ? substring(prefix, 0, 20) : prefix

// ── Monitoring ────────────────────────────────────────────────────────────────

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    prefix: prefix
    tags: tags
  }
}

// ── SQL ───────────────────────────────────────────────────────────────────────

module sql 'modules/sql.bicep' = {
  name: 'sql'
  params: {
    location: location
    prefix: prefix
    tags: tags
    adminPassword: sqlAdminPassword
  }
}

// ── Static Web Apps ───────────────────────────────────────────────────────────

module swaReact 'modules/static-web-app.bicep' = {
  name: 'swa-react'
  params: {
    location: location
    name: '${prefix}-react'
    tags: tags
  }
}

module swaAngular 'modules/static-web-app.bicep' = {
  name: 'swa-angular'
  params: {
    location: location
    name: '${prefix}-angular'
    tags: tags
  }
}

// ── Key Vault ─────────────────────────────────────────────────────────────────

module keyVault 'modules/key-vault.bicep' = {
  name: 'key-vault'
  params: {
    location: location
    prefix: kvPrefix
    tags: tags
    anthropicApiKey: anthropicApiKey
    sqlConnectionString: sql.outputs.connectionString
  }
}

// ── App Service ───────────────────────────────────────────────────────────────

module appService 'modules/app-service.bicep' = {
  name: 'app-service'
  params: {
    location: location
    prefix: prefix
    tags: tags
    keyVaultName: keyVault.outputs.keyVaultName
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    corsOrigins: [
      'https://${swaReact.outputs.hostname}'
      'https://${swaAngular.outputs.hostname}'
    ]
  }
}

// ── Key Vault RBAC: grant App Service managed identity Secrets User ───────────

var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

// Reference the deployed Key Vault so we can scope the role assignment to it
resource kvExisting 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVault.outputs.keyVaultName
}

resource kvRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: kvExisting
  name: guid(keyVault.outputs.keyVaultName, appService.outputs.principalId, keyVaultSecretsUserRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: appService.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────

output apiUrl string = 'https://${appService.outputs.webAppHostname}'
output reactAppUrl string = 'https://${swaReact.outputs.hostname}'
output angularAppUrl string = 'https://${swaAngular.outputs.hostname}'
output reactDeploymentToken string = swaReact.outputs.deploymentToken
output angularDeploymentToken string = swaAngular.outputs.deploymentToken
