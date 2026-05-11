param location string
param prefix string
param tags object = {}
param keyVaultName string
param appInsightsConnectionString string
param recipesApiHostname string
// Shared with the API app — Azure allows only one F1 Linux plan per subscription per region
param appServicePlanId string

resource mcpApp 'Microsoft.Web/sites@2023-12-01' = {
  name: '${prefix}-mcp'
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
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
