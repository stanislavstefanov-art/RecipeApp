param location string
param prefix string
param tags object = {}
@secure()
param anthropicApiKey string
@secure()
param sqlConnectionString string

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${prefix}-kv'
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenant().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Enabled'
  }
}

resource secretAnthropicKey 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'ANTHROPIC-API-KEY'
  properties: {
    value: anthropicApiKey
  }
}

resource secretSqlConnectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'ConnectionStrings--RecipesDb'
  properties: {
    value: sqlConnectionString
  }
}

output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output anthropicApiKeySecretUri string = secretAnthropicKey.properties.secretUri
output sqlConnectionStringSecretUri string = secretSqlConnectionString.properties.secretUri
