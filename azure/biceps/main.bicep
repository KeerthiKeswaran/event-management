param location string = resourceGroup().location
param appName string
param environmentName string

@secure()
param pgAdminPassword string
param pgAdminLogin string = 'eventadmin'

var uniqueSuffix = uniqueString(resourceGroup().id)

// 1. Storage Account (For Assets/Blobs)
module storage 'storage.bicep' = {
  name: 'storageDeploy'
  params: {
    location: location
    storageAccountName: take('st${appName}${uniqueSuffix}', 24)
  }
}

// 2. Azure Container Registry (ACR)
module acr 'acr.bicep' = {
  name: 'acrDeploy'
  params: {
    location: location
    acrName: 'cr${appName}${uniqueSuffix}'
  }
}

// 3. PostgreSQL Flexible Server
module postgres 'postgres.bicep' = {
  name: 'postgresDeploy'
  params: {
    location: location
    serverName: 'pg-${appName}-${environmentName}'
    adminLogin: pgAdminLogin
    adminPassword: pgAdminPassword
  }
}

// 4. Azure Cache for Redis
module redis 'redis.bicep' = {
  name: 'redisDeploy'
  params: {
    location: 'eastus'
    redisName: 'redis-${appName}-${environmentName}'
  }
}

// 5. Azure Container Apps (Environment + App)
module containerApp 'containerapp.bicep' = {
  name: 'containerAppDeploy'
  params: {
    location: location
    environmentName: 'cae-${appName}-${environmentName}'
    containerAppName: 'ca-${appName}-${environmentName}'
    acrLoginServer: acr.outputs.loginServer
    acrUsername: acr.outputs.adminUsername
    acrPassword: acr.outputs.adminPassword
    pgConnectionString: postgres.outputs.connectionString
    storageConnectionString: storage.outputs.connectionString
    redisConnectionString: redis.outputs.connectionString
  }
}
