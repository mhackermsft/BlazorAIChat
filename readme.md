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

## Chat Over Documents (RAG)
Retrieval-augmented generation (RAG) is a technique that combines information retrieval with generative models to produce more accurate and contextually relevant responses by incorporating external knowledge sources. 

Retrieval-augmented generation (RAG) is essential for AI chat because it enhances the accuracy and relevance of responses by integrating real-time information from external sources. This allows the AI to provide up-to-date and contextually appropriate answers, especially for queries that require specific or current knowledge beyond the AI’s training data.

RAG also helps address the limited context window of large language models by only sending relevant knowledge to the model.

This demo utilizes a basic form of RAG that extracts the text from the uploaded documents, splits the content at paragraphs, and then generate embeddings for each paragraph. The results are stored in a SQLite database. The original source document is not stored in the original format.

When a user chats with the solution, a semantic search is completed across the stored paragraphs and the 10 most related paragraphs are returned to the large language model as knowledge so it can attempt to answer the user's question.

For more accurate document ingestion, processing, and semantic search it is recommended to use a solution that uses the following services as part of the RAG process:
* Azure AI Search
* Azure Document Intelligence

Note: If deployed on an Azure App Service with EasyAuth enabled, the uploaded documents become knowledge for only the user who uploaded the document. It does not share the knowledge with other users of the solution.  If you are not using EasyAuth, you are running local, or deployed the app on another .NET web host, all of the users will be considered guests and all of the knowledge uploaded will be shared.

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

## Knowledge Storage
All uploaded knowledge is stored in a SQLite database locally on the application host. The file is called memory.sqlite and is NOT encrypted. It is important to protect this file if any sensitive content is uploaded to the solution. 

Users can use the clear button in the application to delete all of their uploaded knowledge. They currently cannot choose specific pieces of knowledge to delete.

An administrator may choose to delete the memory.sqlite file from the host to clear out all knowledge for all users.

### Disclaimer
This code is for demonstration purposes only. It has not been evaluated or reviewed for production purposes. Please utilize caution and do your own due diligence before using this code. I am not responsible for any issues you experience or damages caused by the use or misuse of this code.