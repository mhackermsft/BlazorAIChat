param location string = resourceGroup().location
param appServicePlanName string = 'BlazorOpenAIChatPlan'
param appServiceName string = 'BlazorOpenAIChat'
param openAIServiceName string = 'BlazorOpenAIService'
param gpt4oDeploymentName string = 'gpt-4o'
param adaDeploymentName string = 'text-embedding-ada-002'
param useExistingAppServicePlan bool = false
param useExistingOpenAIService bool = false
param useExistingGpt4oDeployment bool = false
param useExistingAdaDeployment bool = false
param gitHubRepoUrl string = 'https://github.com/mhackermsft/BlazorAIChat'

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = if (!useExistingAppServicePlan) {
  name: appServicePlanName
  location: location
  sku: {
    name: 'F1'
    tier: 'Free'
  }
}

resource appService 'Microsoft.Web/sites@2022-03-01' = {
  name: appServiceName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureOpenAIChatCompletion:Endpoint'
          value: openAIService.properties.endpoint
        }
        {
          name: 'AzureOpenAIChatCompletion:ApiKey'
          value: openAIService.properties.apiKey
        }
        {
          name: 'AzureOpenAIChatCompletion:Model'
          value: gpt4oDeploymentName
        }
        {
          name: 'AzureOpenAIChatCompletion:SupportsImages'
          value: 'true'
        }
        {
          name: 'AzureOpenAIEmbedding:Model'
          value: adaDeploymentName
        }
        {
          name: 'RequireEasyAuth'
          value: 'false'
        }
      ]
    }
  }
}

resource openAIService 'Microsoft.CognitiveServices/accounts@2022-12-01' = if (!useExistingOpenAIService) {
  name: openAIServiceName
  location: location
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    apiProperties: {
      apiType: 'OpenAI'
    }
  }
}

resource existingOpenAIService 'Microsoft.CognitiveServices/accounts@2022-12-01' existing = if (useExistingOpenAIService) {
  name: openAIServiceName
}

resource gpt4oDeployment 'Microsoft.CognitiveServices/accounts/deployments@2022-12-01' = if (!useExistingGpt4oDeployment) {
  parent: openAIService
  name: gpt4oDeploymentName
  properties: {
    model: 'gpt-4o'
  }
}

resource adaDeployment 'Microsoft.CognitiveServices/accounts/deployments@2022-12-01' = if (!useExistingAdaDeployment) {
  parent: openAIService
  name: adaDeploymentName
  properties: {
    model: 'text-embedding-ada-002'
  }
}

// Configure deployment from GitHub
resource sourceControl 'Microsoft.Web/sites/sourcecontrols@2021-02-01' = {
  name: '${appServiceName}/web'
  properties: {
    repoUrl: gitHubRepoUrl
    branch: 'main'
    isManualIntegration: false
    deploymentRollbackEnabled: true
    isMercurial: false
  }
}
