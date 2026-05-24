param location string
param prefix string
param tags object = {}

resource cognitiveAccount 'Microsoft.CognitiveServices/accounts@2024-04-01-preview' = {
  name: '${prefix}-docint'
  location: location
  tags: tags
  kind: 'FormRecognizer'
  sku: {
    name: 'F0' // Free tier: 500 pages/month
  }
  properties: {
    customSubDomainName: '${prefix}-docint'
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
  }
}

output endpoint string = cognitiveAccount.properties.endpoint
output name string = cognitiveAccount.name
#disable-next-line outputs-should-not-contain-secrets
output apiKey string = cognitiveAccount.listKeys().key1
