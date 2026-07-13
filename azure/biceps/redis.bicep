param location string
param redisName string

resource redisCache 'Microsoft.Cache/redis@2023-08-01' = {
  name: redisName
  location: location
  properties: {
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: 0 // C0 Basic is the cheapest tier
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
  }
}

output connectionString string = '${redisCache.properties.hostName},abortConnect=false,ssl=true,password=${redisCache.listKeys().primaryKey}'
