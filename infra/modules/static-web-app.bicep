param location string
param name string
param tags object = {}

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {}
}

output hostname string = staticWebApp.properties.defaultHostname
output deploymentToken string = listSecrets(staticWebApp.id, staticWebApp.apiVersion).properties.apiKey
