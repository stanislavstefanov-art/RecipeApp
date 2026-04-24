param location string
param prefix string
param tags object = {}
@secure()
param adminPassword string

var adminLogin = '${prefix}admin'

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: '${prefix}-sql'
  location: location
  tags: tags
  properties: {
    administratorLogin: adminLogin
    administratorLoginPassword: adminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource database 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: 'RecipesDb'
  location: location
  tags: tags
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1
  }
  properties: {
    autoPauseDelay: 60
    minCapacity: json('0.5')
    zoneRedundant: false
  }
}

output serverFqdn string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = database.name
output adminLogin string = adminLogin
output connectionString string = 'Server=${sqlServer.properties.fullyQualifiedDomainName};Database=${database.name};User ID=${adminLogin};Password=${adminPassword};TrustServerCertificate=False;Encrypt=True;MultipleActiveResultSets=true'
