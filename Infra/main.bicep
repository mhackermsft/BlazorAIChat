param uniqueName string = uniqueString(resourceGroup().id)
param sku string = 'F1'
param location string = resourceGroup().location
param repositoryUrl string = 'https://github.com/mhackermsft/BlazorAIChat'
param branch string = 'master'
param openAiServiceName string = toLower('BlazorAIChatOpenAI-${uniqueName}')
param aiSkuName string = 'S0'
param aiChatModelName string = 'gpt-4o'
param aiChatModelVersion string = '2024-05-13'
param aiChatModelCapacity int = 80
param aiChatModelSupportsImages bool = true
param aiEmbedModelName string = 'text-embedding-ada-002'
param aiEmbedModelVersion string = '2'
param aiEmbedModelCapacity int = 120
param requireEasyAuth bool = true

var appServicePlanName = toLower('BlazorAIChatPlan-${uniqueName}')
var webSiteName = toLower('BlazorAIChat-${uniqueName}')


resource appServicePlan 'Microsoft.Web/serverfarms@2020-06-01' = {
  name: appServicePlanName
  location: location
  properties: {
    reserved: false
  }
  sku: {
    name: sku
  }
  kind: 'app'
}

resource appService 'Microsoft.Web/sites@2020-06-01' = {
  name: webSiteName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      windowsFxVersion: 'DOTNETCORE|8.0'
      appSettings: [
        {
          name: 'AzureOpenAIChatCompletion__Endpoint'
          value: openAiService.properties.endpoint
        }
        {
          name: 'AzureOpenAIChatCompletion__ApiKey'
          value: openAiService.listKeys().key1
        }
        {
          name: 'AzureOpenAIChatCompletion__Model'
          value: toLower('${aiChatModelName}-${uniqueName}')
        }
        {
          name: 'AzureOpenAIChatCompletion__SupportsImages'
          value: aiChatModelSupportsImages ? 'true' : 'false'
        }
        {
          name: 'AzureOpenAIEmbedding__Model'
          value: toLower('${aiEmbedModelName}-${uniqueName}')
        }
        {
          name:'SystemMessage'
          value:'You are a helpful AI assistant. Respond in a friendly and professional tone.'
        }
        {
          name: 'RequireEasyAuth'
          value: requireEasyAuth ? 'true' : 'false'
        }
        {
          name: 'ConnectionStrings__PostgreSQL'
          value: ''
        }
        {
          name: 'ConnectionStrings__ConfigDatabase'
          value: 'Data Source=ConfigDatabase.db'
        }
        {
          name: 'DocumentIntelligence__Endpoint'
          value: ''
        }
        {
          name: 'DocumentIntelligence__ApiKey'
          value: ''
        }
        {
          name: 'AzureAISearch__Endpoint'
          value: ''
        }
        {
          name: 'AzureAISearch__ApiKey'
          value: ''
        }
      ]
    }
  }
  dependsOn: [
    appServicePlan
    openAiService
  ]
}

resource gitsource 'Microsoft.Web/sites/sourcecontrols@2022-03-01' = {
  parent: appService
  name: 'web'
  properties: {
    repoUrl: repositoryUrl
    branch: branch
    isManualIntegration: true
  }
  dependsOn: [
    appService
  ]
}


// Create the Azure OpenAI Service
resource openAiService 'Microsoft.CognitiveServices/accounts@2021-04-30' = {
  name: openAiServiceName
  location: location
  kind: 'OpenAI'
  sku: {
    name: aiSkuName
  }
  properties: {
    apiProperties: {
      enableOpenAI: true
    }
  }

}


// Deploy the chat model
resource openAiChat 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAiService
  name: toLower('${aiChatModelName}-${uniqueName}')
  properties: {
    model: {
      format: 'OpenAI'
      name: aiChatModelName
      version: aiChatModelVersion
    }
  }
  sku: {
    name: 'standard'
    capacity: aiChatModelCapacity
  }
  dependsOn:[
    openAiService
  ]
}

// Deploy the embed model
resource openAiEmbed 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAiService
  name: toLower('${aiEmbedModelName}-${uniqueName}')
  properties: {
    model: {
      format: 'OpenAI'
      name: aiEmbedModelName
      version: aiEmbedModelVersion
    }
  }
  sku: {
    name: 'standard'
    capacity: aiEmbedModelCapacity
  }
  dependsOn:[
    openAiChat
  ]
}

