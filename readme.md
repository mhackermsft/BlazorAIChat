# Blazor Azure OpenAI Chat Demo

## Overview
This is a sample .NET 8 Blazor Interactive Server application for chatting with Azure OpenAI Models. Users may upload TXT, DOCX or PDF documents as a knowledge base for the AI to use when responding.

## Components
This solution utilizes several open source libraries to help with document ingestion and chat display. These projects include:
* PDFPig
* OpenXML
* MarkdownSharp
* Semantic Kernel

## Features
* Chat with your data using Azure OpenAI models
* Can be run locally or hosted on Azure App Service
* If run in Azure, it can use EasyAuth authentication.
* User can upload TXT, DOCX or PDF documents into the knowledge base. If running on Azure App Service with EasyAuth, the knowledge uploaded is associated only to the user who uploaded the content.
* Streaming chat results with the ability to stop the response.
* Ability to clear chat and/or delete the data stored in the user's knowledge base.

## Requirements
* Azure Subscription with at least 1 Azure OpenAI chat model and 1 Azure OpenAI embedding model deployed.
* Can be run locally or published to an Azure App Service. If deployed on Azure App Service, you can enable EasyAuth.

## Configuration
The appsettings.json file has a few configuration parameters that must be set for the app to properly work:

  ```
  "AzureOpenAIChatCompletion": {
    "Endpoint": "",
    "ApiKey": "",
    "Model": ""
  },
  "AzureOpenAIEmbedding": {
    "Model": ""
  }
  ```

* Under the AzureOpenAIChatCompletion section include your Azure OpenAI endpoint URL, API Key, and the name of the deployed chat model you want to use.
* Under the AzureOpenAIEmbedding section include the deployed embedding model you want to use. It is assumed that both the chat and embedding models are accessed through the same Azure OpenAI endpoint and API key.

This solution has been tested with the gpt-4o chat model and the text-embedding-ada-002 model. You should be able to plug in other models to test them out.

## Deployment
* You need to manually create your Azure OpenAI Service and deploy a chat and embedding model.
* Clone this repo, open in Visual Studio 2022.
* Update the appsettings.json file with the proper values
* You can run the app locally through Visual Studio or you can publish the application to an Azure App Service or other .NET web host.

## Authentication
* If running locally, outside of Azure, or without EasyAuth, the app will show the user as a guest.
* If running on an Azure App Service with EasyAuth configured, the app will show the logged in username.

See the following link for details about configuring EasyAuth in Azure App Service: https://learn.microsoft.com/en-us/azure/app-service/overview-authentication-authorization

### Disclaimer
This code is for demonstration purposes only. It has not been evaluated or reviewed for production purposes. Please utilize caution and do your own due diligence before using this code. I am not responsible for any issues you experience or damages caused by the use or misuse of this code.