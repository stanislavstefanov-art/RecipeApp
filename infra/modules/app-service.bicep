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
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'Claude__ApiKey'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=ANTHROPIC-API-KEY)'
        }
        {
          name: 'ConnectionStrings__RecipesDb'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=ConnectionStrings--RecipesDb)'
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
      ]
    }
  }
}

output webAppName string = webApp.name
output webAppHostname string = webApp.properties.defaultHostName
output principalId string = webApp.identity.principalId
