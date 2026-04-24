using './main.bicep'

// Resource name prefix — used for all Azure resource names.
// Must be lowercase alphanumeric + hyphens, globally unique for SQL/Key Vault.
// Example: "recipes-prod-abc123" (add a short random suffix to avoid name conflicts)
param prefix = 'recipes-prod'

// Azure region — default picks up the resource group location.
// param location = 'westeurope'

// SQL admin password — store in Azure DevOps / GitHub Actions secrets, never commit.
// Set via: az deployment group create ... --parameters sqlAdminPassword=<value>
param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD')

// Anthropic API key — stored in Key Vault after first deploy.
// Set via: az deployment group create ... --parameters anthropicApiKey=<value>
param anthropicApiKey = readEnvironmentVariable('ANTHROPIC_API_KEY')
