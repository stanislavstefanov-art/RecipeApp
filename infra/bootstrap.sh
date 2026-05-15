#!/usr/bin/env bash
# One-time bootstrap for RecipesApp Azure deployment.
#
# Creates the resource group, an Azure AD application with GitHub OIDC federated
# credentials, the service principal, and a Contributor role assignment scoped to the
# resource group. Generates a JWT signing key and writes it to a gitignored file.
#
# Idempotent — re-running detects existing resources and skips them.
#
# After this script completes, set the printed GitHub repository variables and
# secrets, then push a change to main. No further manual `az` commands are needed.
#
# Usage:
#   infra/bootstrap.sh \
#     --resource-group RecipesApp \
#     --location westeurope \
#     --prefix recipes-prod-abc123 \
#     --github-repo your-org/RecipesApp

set -euo pipefail

# ── Parse args ────────────────────────────────────────────────────────────────

RESOURCE_GROUP=""
LOCATION=""
PREFIX=""
GITHUB_REPO=""

while [[ $# -gt 0 ]]; do
    case $1 in
        --resource-group) RESOURCE_GROUP="$2"; shift 2 ;;
        --location)       LOCATION="$2";       shift 2 ;;
        --prefix)         PREFIX="$2";         shift 2 ;;
        --github-repo)    GITHUB_REPO="$2";    shift 2 ;;
        -h|--help)
            sed -n '3,17p' "$0"
            exit 0
            ;;
        *) echo "Unknown argument: $1" >&2; exit 1 ;;
    esac
done

for var in RESOURCE_GROUP LOCATION PREFIX GITHUB_REPO; do
    if [[ -z "${!var}" ]]; then
        echo "Missing required argument: --${var,,}" >&2
        exit 1
    fi
done

if [[ ! "$GITHUB_REPO" =~ ^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$ ]]; then
    echo "GitHubRepo must be in 'owner/repo' format. Got: '$GITHUB_REPO'" >&2
    exit 1
fi

# ── Verify dependencies ───────────────────────────────────────────────────────

for cmd in az jq openssl; do
    if ! command -v "$cmd" >/dev/null 2>&1; then
        echo "$cmd not found. Please install it and retry." >&2
        exit 1
    fi
done

if ! ACCOUNT_JSON=$(az account show --output json 2>/dev/null); then
    echo "Not logged in to Azure. Run 'az login' first." >&2
    exit 1
fi

SUBSCRIPTION_ID=$(echo "$ACCOUNT_JSON" | jq -r .id)
SUBSCRIPTION_NAME=$(echo "$ACCOUNT_JSON" | jq -r .name)
TENANT_ID=$(echo "$ACCOUNT_JSON" | jq -r .tenantId)

cyan() { printf '\033[36m%s\033[0m\n' "$1"; }
yellow() { printf '\033[33m%s\033[0m\n' "$1"; }
green() { printf '\033[32m%s\033[0m\n' "$1"; }

echo
cyan "Subscription : $SUBSCRIPTION_NAME ($SUBSCRIPTION_ID)"
cyan "Tenant       : $TENANT_ID"
cyan "GitHub repo  : $GITHUB_REPO"
echo

# ── Resource group ────────────────────────────────────────────────────────────

yellow "▶ Resource group: $RESOURCE_GROUP ($LOCATION)"
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none
RG_ID=$(az group show --name "$RESOURCE_GROUP" --query id --output tsv)
echo "  ready"

# ── AD app + service principal ────────────────────────────────────────────────

APP_DISPLAY_NAME="github-actions-$RESOURCE_GROUP"

yellow "▶ Azure AD app: $APP_DISPLAY_NAME"
APP_ID=$(az ad app list --display-name "$APP_DISPLAY_NAME" --query "[0].appId" --output tsv)
if [[ -z "$APP_ID" ]]; then
    APP_ID=$(az ad app create --display-name "$APP_DISPLAY_NAME" --query appId --output tsv)
    echo "  created (appId: $APP_ID)"
else
    echo "  exists (appId: $APP_ID)"
fi

SP_ID=$(az ad sp list --filter "appId eq '$APP_ID'" --query "[0].id" --output tsv)
if [[ -z "$SP_ID" ]]; then
    az ad sp create --id "$APP_ID" --output none
    echo "  service principal: created"
else
    echo "  service principal: exists"
fi

# ── Role assignments ──────────────────────────────────────────────────────────

assert_role_assignment() {
    local role="$1"
    local count
    count=$(az role assignment list \
        --assignee "$APP_ID" \
        --scope "$RG_ID" \
        --role "$role" \
        --output json | jq 'length')

    if [[ "$count" -eq 0 ]]; then
        az role assignment create \
            --assignee "$APP_ID" \
            --role "$role" \
            --scope "$RG_ID" \
            --output none
        echo "  $role: assigned"
    else
        echo "  $role: already assigned"
    fi
}

yellow "▶ Role assignments on $RESOURCE_GROUP"
assert_role_assignment "Contributor"
assert_role_assignment "Key Vault Secrets User"
# Needed so the SP can create the Microsoft.Authorization/roleAssignments resources
# declared in infra/main.bicep (grants App Service + MCP Server access to Key Vault).
assert_role_assignment "User Access Administrator"

# ── Federated credentials ─────────────────────────────────────────────────────

yellow "▶ Federated credentials"
EXISTING_CREDS=$(az ad app federated-credential list --id "$APP_ID" --output json)

create_fed_cred() {
    local name="$1"
    local subject="$2"
    local description="$3"

    if echo "$EXISTING_CREDS" | jq -e --arg n "$name" '.[] | select(.name == $n)' >/dev/null; then
        echo "  $name: exists"
        return
    fi

    local body
    body=$(jq -n \
        --arg name "$name" \
        --arg subject "$subject" \
        --arg description "$description" \
        '{
            name: $name,
            issuer: "https://token.actions.githubusercontent.com",
            subject: $subject,
            description: $description,
            audiences: ["api://AzureADTokenExchange"]
        }')

    echo "$body" | az ad app federated-credential create \
        --id "$APP_ID" \
        --parameters @- \
        --output none
    echo "  $name: created"
}

create_fed_cred "github-main" "repo:${GITHUB_REPO}:ref:refs/heads/main"  "Push to main branch"
create_fed_cred "github-pr"   "repo:${GITHUB_REPO}:pull_request"          "Pull request validation"

# ── Generate secrets ──────────────────────────────────────────────────────────

JWT_SIGNING_KEY=$(openssl rand -base64 32)
MCP_SERVER_TOKEN=$(openssl rand -base64 32)

REPO_ROOT=$(cd "$(dirname "$0")/.." && pwd)
OUTPUT_FILE="$REPO_ROOT/.bootstrap-output.txt"
printf 'JWT_SIGNING_KEY=%s\nMCP_SERVER_TOKEN=%s' "$JWT_SIGNING_KEY" "$MCP_SERVER_TOKEN" > "$OUTPUT_FILE"

# ── Summary ───────────────────────────────────────────────────────────────────

cat <<EOF

$(green '════════════════════════════════════════════════════════════════════════')
$(green '  Bootstrap complete')
$(green '════════════════════════════════════════════════════════════════════════')

$(green 'Next steps:')

1. In your GitHub repo (Settings → Secrets and variables → Actions),
   set these as Variables:

   AZURE_CLIENT_ID         = $APP_ID
   AZURE_TENANT_ID         = $TENANT_ID
   AZURE_SUBSCRIPTION_ID   = $SUBSCRIPTION_ID
   AZURE_RESOURCE_GROUP    = $RESOURCE_GROUP
   AZURE_PREFIX            = $PREFIX

2. Set these as Secrets (same screen, Secrets tab):

   SQL_ADMIN_PASSWORD      = <choose a strong password — no '@' character>
   ANTHROPIC_API_KEY       = sk-ant-...
   JWT_SIGNING_KEY         = (see $OUTPUT_FILE)
   MCP_SERVER_TOKEN        = (see $OUTPUT_FILE)

3. Push a commit to main. Once the deploy workflows are wired up (later
   bundles in CICD-1), they will pick up the OIDC trust automatically.

After the first infra deploy, populate these additional repo variables
from the workflow run summary:

   API_HOSTNAME            (e.g. ${PREFIX}-api.azurewebsites.net)
   KEY_VAULT_NAME          (e.g. ${PREFIX}-kv — exact value in run summary)

To delete this OIDC trust later:
   az ad app delete --id $APP_ID

EOF
