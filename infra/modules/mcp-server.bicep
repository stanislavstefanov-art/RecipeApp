param location string
param prefix string
param tags object = {}
param keyVaultName string
param appInsightsConnectionString string
param recipesApiHostname string

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${prefix}-mcp-plan'
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

resource mcpApp 'Microsoft.Web/sites@2023-12-01' = {
  name: '${prefix}-mcp'
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
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'RECIPES_API_BASE_URL'
          value: 'https://${recipesApiHostname}'
        }
        {
          name: 'MCP_SERVER_TOKEN'
          value: '@Microsoft.KeyVault(VaultName=${keyVaultName};SecretName=MCP-SERVER-TOKEN)'
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'ASPNETCORE_URLS'
          value: 'http://+:8080'
        }
      ]
    }
  }
}

output mcpAppName string = mcpApp.name
output mcpAppHostname string = mcpApp.properties.defaultHostName
output principalId string = mcpApp.identity.principalId
