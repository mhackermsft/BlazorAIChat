# Blazor Azure OpenAI Chat Demo
##### BlazorAIChat: AI-Powered Chat Application

## Overview
This is a sample .NET 8 Blazor Interactive Server application for chatting with Azure OpenAI Models. Users may upload TXT, DOCX, XLSX, PPTX or PDF documents to a knowledge base for the AI to use when responding. If configured, it can also upload images for those AI models that support images in chat.

## Components
This solution utilizes several open source libraries to help with document ingestion and chat display. These projects include:
* MarkdownSharp
* Semantic Kernel
* Semantic Memory

## Features
- **AI Integration**: Utilizes advanced AI models to provide intelligent responses.
- **Data Interaction**: Enables users to chat with their data using Azure OpenAI models.
- **Deployment Flexibility**: Can be operated locally or hosted on Azure App Service.
- **Authentication**: Supports EasyAuth authentication when hosted on Azure.
- **Document Upload**: Allows users to upload TXT, DOCX, XLSX, PPTX, or PDF documents into the knowledge base. When using Azure App Service with EasyAuth, uploaded knowledge is associated exclusively with the user.
- **Image Analysis**: Supports image uploads for querying, compatible with models like GPT-4. When using models that don't support images and you have Azure Document Intelligence configured, it will OCR uploaded images and store the results as knowledge.
- **Streaming Responses**: Provides streaming chat results with the option to stop responses.
- **Data Management**: Offers the ability to clear chat history and delete data stored in the user's knowledge base.
- **Retry Handling**: Automatically pauses and retries calls to Azure OpenAI if it exceeds the API rate limit.


## Chat Over Documents (RAG)
Retrieval-augmented generation (RAG) is a technique that combines information retrieval with generative models to produce more accurate and contextually relevant responses by incorporating external knowledge sources. 

Retrieval-augmented generation (RAG) is essential for AI chat because it enhances the accuracy and relevance of responses by integrating real-time information from external sources. This allows the AI to provide up-to-date and contextually appropriate answers, especially for queries that require specific or current knowledge beyond the AI’s training data.

RAG also helps address the limited context window of large language models by only sending relevant knowledge to the model.

This demo utilizes a basic form of RAG that extracts the text from the uploaded documents, splits the content at paragraphs, and then generate embeddings for each paragraph. The results are stored by default on the web server filesystem. The original source document is not stored in the original format.

When a user chats with the solution, a semantic search is completed across the stored paragraphs and the 10 most related paragraphs are returned to the large language model as knowledge so it can attempt to answer the user's question.

For more accurate document ingestion, processing, and semantic search it is recommended to use a solution that uses the following services as part of the RAG process:
* Azure AI Search
* Azure Document Intelligence

Note: If deployed on an Azure App Service with EasyAuth enabled, the uploaded documents become knowledge for only the user who uploaded the document. It does not share the knowledge with other users of the solution.  If you are not using EasyAuth, you are running local, or deployed the app on another .NET web host, all of the users will be considered guests and all of the knowledge uploaded will be shared.

## Requirements
- **Azure Subscription**: Must include at least one Azure OpenAI chat model and one Azure OpenAI embedding model deployed. Optionally you can use Azure PostgreSQL for the knowledge storage. 
- **Deployment Options**: 
  - **Local**: Can be run locally.
  - **Azure App Service**: Can be published to an Azure App Service. If deployed on Azure App Service, EasyAuth can be enabled for authentication.


## Configuration
The appsettings.json file has a few configuration parameters that must be set for the app to properly work:

  ```
  "AzureOpenAIChatCompletion": {
    "Endpoint": "",
    "ApiKey": "",
    "Model": "",
    "SupportsImages": false
  },
  "AzureOpenAIEmbedding": {
    "Model": ""
  },
  "RequireEasyAuth": true,
  "SystemMessage" : "You are a helpful AI assistant. Respond in a friendly and professional tone.",
  "ConnectionStrings": {
    "PostgreSQL": "",
    "ConfigDatabase": "Data Source=ConfigDatabase.db"
  },
  "DocumentIntelligence": {
  "Endpoint": "",
  "ApiKey": ""
},
"AzureAISearch": {
  "Endpoint": "",
  "ApiKey": ""
}
  ```

- **AzureOpenAIChatCompletion Configuration**: 
  - Include your Azure OpenAI endpoint URL, API Key, and the name of the deployed chat model you intend to use.
  - If the model supports images, set `SupportsImages` to `true`.

- **AzureOpenAIEmbedding Configuration**: 
  - Specify the deployed embedding model you plan to use.
  - Both the chat and embedding models are assumed to be accessed through the same Azure OpenAI endpoint and API key.

- **PostgreSQL (optional)**:
If you would like to use PostgreSQL in place of files for knowledge storage, you must manually deploy a PostgreSQL instance and configure the connection string. Note: You must enable the pgvector extension to use PostgreSQL.

- **Azure Document Intelligence (optional)**
You may choose to optionally manually deploy Azure Document Intelligence which can be used to OCR uploaded images. This is not needed or enabled if your AI model supports images.

- **Azure AI Search (optional)**
This is an optional knowledge store that if configured will replace both the file and PostreSQL storage.

- **EasyAuth Configuration**: 
  - If utilizing EasyAuth with Azure App Service, it is recommended to set `RequireEasyAuth` to `true` to ensure that users are fully authenticated and not recognized as guests. This setting is set to true by default.

This solution has been tested with the `gpt-4o` chat model and the `text-embedding-ada-002` model. Other models can be integrated and tested as needed.


## Deployment

### Manual Deployment

- **Azure OpenAI Service Setup**: Manually create your Azure OpenAI Service and deploy both a chat model and an embedding model.
- **Repository Cloning**: Clone this repository and open it in Visual Studio 2022.
- **Configuration**: Update the `appsettings.json` file with the appropriate values.
- **Running the Application**: You can run the application locally through Visual Studio or publish it to an Azure App Service or another .NET web host.

### Automatic Deployment to Azure

To deploy the application to Azure, you can use the button below. This process will create an Azure App Service Plan, an Azure App Service, and an Azure OpenAI Service with two models deployed. It will also deploy the website to the Azure App Service. 

**Important**: Please read and understand all the information in this section before pressing the "Deploy to Azure" button. **For protection, the default value for RequireEasyAuth is set to true.**


[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fmhackermsft%2FBlazorAIChat%2Fmaster%2FInfra%2Fazuredeploy.json)

#### Important Azure Deployment Notes  
The button above will deploy Azure services that will be billed against your Azure subscription. The deployment template allows you to choose region, web app SKU, AI Chat model, AI embed model, along with OpenAI capacity size.  These options will impact the cost of the deployed solutions. Since the automatic deployment uses the Azure App Service build feature, it will use a significant amount of local web storage to complete the build process. After the web site has been deployed you can open the App Service console and run the command `dotnet nuget locals all --clear` to clear the local Nuget package store which will reclaim over 800MB of storage. After running that command you may be able to scale down to the Azure App Service Plan free SKU. If you do not configure Azure AI Search or PostgreSQL, the web server's file storage is used for knowledge storage. You may need to scale up your App Service Plan SKU to support the additional amount of storage needed for your knowledge store.

**Warning:** If you do not enable EasyAuth, your website will be available **on the public internet** by default. This means that **other people can connect and use the website**. You will **incur Azure OpenAI charges** for all usage of your website. If you upload documents, that **knowledge will be accessible to all users**. 

If you want to protect the website it is highly recommended that you set `Require Easy Auth` to true during the deployment and then configure EasyAuth authentication on the App Service once the deployment completes. Once EasyAuth is configured, each user will be required to login and they will have their own knowledge base which is not shared with other users.

All of the settings noted above in the appsettings.json file can be configured in the Azure Portal by going to the Azure App Service environment settings.

TLDR; If users see a green tag with the word guest located in the top left of the app, all of the uploaded knowledge is shared among the users.

### AI Model Capacity
When deploying to Azure, you can set various deployment properties, including the AI model capacity. This capacity represents the quota assigned to the model deployment.

- 1 unit = 1,000 Tokens per Minute (TPM)
- 10 units = 10,000 Tokens per Minute (TPM)

Select a value that meets your token and request requirements while staying within the available capacity for the model.

Read more about Azure OpenAI Service quota here: https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/quota?tabs=rest

### Costs
The cost to operate this demo application in your subscription will depend upon a few factors:
- **App Service Plan size** - The deployment script by default uses the B1 tier. You can, however, adjust this to increase performance and features.
- **Azure OpenAI Service** - Two Azure OpenAI Models are required in order for this demo to function properly. The recommended models are `gpt-4o` and `text-embedding-ada-002`. The chat models are priced based on the number of input and output tokens. The embedding model is priced based on the number of tokens.

You can learn more about the cost for Azure App Service and Azure OpenAI models at the links below.

- **Azure App Service** - https://azure.microsoft.com/en-us/pricing/details/app-service/windows/
- **Azure OpenAI** - https://azure.microsoft.com/en-us/pricing/details/cognitive-services/openai-service/

## Authentication
* If running locally, outside of Azure, or without EasyAuth, the app will show the user as a guest.
* If running on an Azure App Service with EasyAuth configured, the app will show the logged in username.

See the following link for details about configuring EasyAuth in Azure App Service: https://learn.microsoft.com/en-us/azure/app-service/overview-authentication-authorization

## Authorization
This solution allows you to require users to request access to the application if EasyAuth has been configured. If authorization is enabled, new users will need to click on the request access button and then wait for an administrator to approve the request. For a new deployment, the first user who requests access will automatically be approved and made the administrator. Administrators will use the admin menu to view the users and those that are waiting for approvals. Administrators may configure the application to only allow approved users to have access for a set number of days. Once a user's access expires, an administrator can edit the user's account and modify the access approved date to extend a user's access.

If authorization is not enabled, then any authenticated user will be able to utilize the application and there will not be a concept of administrators.  NOTE: The default deployment is setup so that if EasyAuth is enabled, authorization will also be enabled. If an admin chooses to turn off authorization, there is currently no way to turn it back on via the application.

## Knowledge Storage
All uploaded knowledge is stored by default in a SQLite database locally on the application host. The file is called memory.sqlite and is NOT encrypted. It is important to protect this file if any sensitive content is uploaded to the solution. 

Optionally you can configure the demo to utilize an Azure PostgreSQL instance as the knowledge store.

Users can use the clear button in the application to delete all of their uploaded knowledge. They currently cannot choose specific pieces of knowledge to delete.

An administrator may choose to delete the memory.sqlite file from the host to clear out all knowledge for all users.

## Impact of Azure OpenAI Capacity Settings

This demonstration application is designed for low-volume use and does not include retry logic for Azure OpenAI calls. If a request exceeds the allocated Azure OpenAI quota for the chat or embedding model, a notification will appear at the top of the application. 

To address this issue, please ensure that your Azure OpenAI models are configured with the appropriate quota to accommodate the volume of tokens and requests being submitted.

For more information on managing your Azure OpenAI service quotas, please visit this link: https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/quota?tabs=rest

## Known Limitation
Word, Excel or PowerPoint documents that have data classification or DRM enabled cannot be uploaded. You will receive a file corruption error. Only upload documents that are not protected.

## Disclaimer
This code is for demonstration purposes only. It has not been evaluated or reviewed for production purposes. Please utilize caution and do your own due diligence before using this code. I am not responsible for any issues you experience or damages caused by the use or misuse of this code.

## Credits
Thank you to the contributors of the Azure-Samples / cosmosdb-chatgpt project. The UI for this demo application was based upon some of their great work. You can check out that project here: https://github.com/Azure-Samples/cosmosdb-chatgpt
