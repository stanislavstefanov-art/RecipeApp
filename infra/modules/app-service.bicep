param location string
param prefix string
param tags object = {}
param keyVaultName string
param appInsightsConnectionString string
param corsOrigins array = []

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${prefix}-plan'
  location: location
  tags: tags
  sku: {
    name: 'F1'
    tier: 'Free'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: '${prefix}-api'
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: false
      ftpsState: 'Disabled'
      cors: {
        allowedOrigins: corsOrigins
        supportCredentials: false
      }
      appSettings: [
        // Runtime
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsightsConnectionString }

        // Secrets — Key Vault references
        { name: 'Claude__ApiKey', value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=ANTHROPIC-API-KEY)' }
        { name: 'ConnectionStrings__RecipesDb', value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=ConnectionStrings--RecipesDb)' }
        { name: 'Jwt__SigningKey', value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=Jwt--SigningKey)' }

        // JWT (non-secret)
        { name: 'Jwt__Issuer', value: 'RecipesApp' }
        { name: 'Jwt__Audience', value: 'RecipesApp' }
        { name: 'Jwt__LifetimeDays', value: '7' }

        // Database / seeder
        { name: 'Database__Provider', value: 'SqlServer' }
        { name: 'Seed__Enabled', value: 'false' }

        // Blob storage
        { name: 'BlobStorage__Provider', value: 'AzureBlob' }
        { name: 'BlobStorage__ConnectionString', value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=BlobStorage--ConnectionString)' }

        // AI providers — live Claude in production
        { name: 'RecipeImport__Provider', value: 'Claude' }
        { name: 'MealPlanSuggestion__Provider', value: 'Claude' }
        { name: 'IngredientSubstitution__Provider', value: 'Claude' }
        { name: 'RecipeCritique__Provider', value: 'Claude' }
        { name: 'RecipeScaling__Provider', value: 'Claude' }
        { name: 'RecipeBatchAnalysis__Provider', value: 'Claude' }
        { name: 'RecipeDraftReview__Provider', value: 'Claude' }
        { name: 'ExpenseInsight__Provider', value: 'Claude' }
      ]
    }
  }
}

output appServicePlanId string = appServicePlan.id
output webAppName string = webApp.name
output webAppHostname string = webApp.properties.defaultHostName
output principalId string = webApp.identity.principalId
