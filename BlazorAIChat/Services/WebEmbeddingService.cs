using BlazorAIChat.Models;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory;
using System.Net.Http;
using Amazon.Runtime;
using Microsoft.KernelMemory.AI.OpenAI;
using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using System;

namespace BlazorAIChat.Services
{
    public enum WebEmbeddingServiceStatusEnum
    {
        Idle,
        Embedding
    }

    public class WebEmbeddingService
    {
        #pragma warning disable SKEXP0010, SKEXP0001, SKEXP0020, KMEXP00
        System.Timers.Timer embeddingTimer = new(600000);  //10 minutes
        
        private AppSettings settings = new AppSettings();
        private MemoryServerless? kernelMemory = null;
        private HttpClient? httpClient;
        private ITextTokenizer? textTokenizer;
        private readonly AIChatDBContext dbContext;

        public WebEmbeddingServiceStatusEnum Status { get; set; } = WebEmbeddingServiceStatusEnum.Idle;
        public int CrawlFrequencyHours { get; set; } = 72;

        public EventHandler<WebEmbeddingServiceStatusEnum>? OnEmbedServiceStateChanged;
        public EventHandler<CrawlerStatus>? OnURLStateChanged;
        public EventHandler<string>? OnStatusUpdate;

        public WebEmbeddingService(IOptions<AppSettings> appSettings, IHttpClientFactory httpClientFactory)
        {
            embeddingTimer.AutoReset = true;
            embeddingTimer.Elapsed += EmbeddingTimer_Elapsed;
            embeddingTimer.Start();

            dbContext = new AIChatDBContext(appSettings);

            settings = appSettings.Value;
            CrawlFrequencyHours = settings.Webcrawling.RecrawlIntervalHours;

            //setup HttpClient
            httpClient = httpClientFactory.CreateClient("retryHttpClient");

            //Setup the Tokenizer to use
            textTokenizer = new GPT4Tokenizer();

            //Set file directory for storing knowledge if PostgreSQL or Azure AI Search is not used
            var knnDirectory = "KNN";

            //Setup the memory store
            var kernelMemoryBuilder = new KernelMemoryBuilder()
            .WithAzureOpenAITextEmbeddingGeneration(new AzureOpenAIConfig
            {
                APIType = AzureOpenAIConfig.APITypes.EmbeddingGeneration,
                Endpoint = settings.AzureOpenAIChatCompletion.Endpoint,
                Deployment = settings.AzureOpenAIEmbedding.Model,
                Auth = AzureOpenAIConfig.AuthTypes.APIKey,
                APIKey = settings.AzureOpenAIChatCompletion.ApiKey,
                MaxTokenTotal = settings.AzureOpenAIEmbedding.MaxInputTokens,
                MaxRetries = 3
            },
                httpClient: httpClient)
            .WithAzureOpenAITextGeneration(new AzureOpenAIConfig
            {
                APIType = AzureOpenAIConfig.APITypes.ChatCompletion,
                Endpoint = settings.AzureOpenAIChatCompletion.Endpoint,
                Deployment = settings.AzureOpenAIChatCompletion.Model,
                Auth = AzureOpenAIConfig.AuthTypes.APIKey,
                APIKey = settings.AzureOpenAIChatCompletion.ApiKey,
                MaxTokenTotal = settings.AzureOpenAIChatCompletion.MaxInputTokens,
                MaxRetries = 3
            }, httpClient: httpClient, textTokenizer: textTokenizer);


            //If Azure AI Search is configured, we use that for storage
            if (settings.UsesAzureAISearch)
            {
                kernelMemoryBuilder = kernelMemoryBuilder.WithAzureAISearchMemoryDb(new AzureAISearchConfig()
                {
                    Endpoint = settings.AzureAISearch.Endpoint,
                    APIKey = settings.AzureAISearch.ApiKey,
                    Auth = AzureAISearchConfig.AuthTypes.APIKey,
                    UseHybridSearch = false
                });

            }
            else if (settings.UsesPostgreSQL)
            {
                //Use PostgreSQL DB memory store
                //NOTE: You must have enabled pgvector extension in your PostgreSQL database for this to work.
                kernelMemoryBuilder = kernelMemoryBuilder.WithPostgresMemoryDb(new PostgresConfig()
                {
                    ConnectionString = settings.ConnectionStrings.PostgreSQL
                });
            }
            else
            {
                //Use file memory store
                kernelMemoryBuilder = kernelMemoryBuilder.WithSimpleVectorDb(new Microsoft.KernelMemory.MemoryStorage.DevTools.SimpleVectorDbConfig { StorageType = Microsoft.KernelMemory.FileSystem.DevTools.FileSystemTypes.Disk, Directory = knnDirectory });
            }

            //Configure document intelligence if configured
            if (!settings.AzureOpenAIChatCompletion.SupportsImages && settings.UsesAzureDocIntelligence)
            {
                kernelMemoryBuilder = kernelMemoryBuilder.WithAzureAIDocIntel(new AzureAIDocIntelConfig()
                {
                    Endpoint = settings.DocumentIntelligence.Endpoint,
                    APIKey = settings.DocumentIntelligence.ApiKey,
                    Auth = AzureAIDocIntelConfig.AuthTypes.APIKey
                });
            }

            //Build the memory store
            kernelMemory = kernelMemoryBuilder.Build<MemoryServerless>();
        }

        public async void Start()
        {
            if (Status == WebEmbeddingServiceStatusEnum.Idle)
            {
                await ProcessEmbeddings();
            }
        }

        private async void EmbeddingTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            await ProcessEmbeddings();
        }

        private async Task ProcessEmbeddings()
        {
            if (Status == WebEmbeddingServiceStatusEnum.Idle)
            {
                //Do we have any URLs where the status is Completed and the last update is more than the crawl frequency?
                var completedURLs = dbContext.CrawlerStatuses.Where(x => x.IsActive && x.LastStatus == CrawlerStatusEnum.Completed && x.LastUpdate != null && x.LastUpdate.Value.AddHours(CrawlFrequencyHours) < DateTime.Now);
                //if so, reset the status to Queued
                foreach (var url in completedURLs)
                {
                    url.LastStatus = CrawlerStatusEnum.Queued;
                    url.LastUpdate = DateTime.Now;
                    dbContext.SaveChanges();
                    OnURLStateChanged?.Invoke(this, url);
                }

                //are there any URLs to embed?
                var queuedURLs = dbContext.CrawlerStatuses.Where(x => x.IsActive && x.LastStatus == CrawlerStatusEnum.Queued);

                if (queuedURLs.Count() > 0)
                {
                    Status = WebEmbeddingServiceStatusEnum.Embedding;
                    OnEmbedServiceStateChanged?.Invoke(this, Status);

                    foreach (var url in queuedURLs)
                    {
                        //Remove any prior embeddings for the URL
                        if (kernelMemory != null)
                            await kernelMemory.DeleteDocumentAsync(url.Id.ToString());

                        //remove any spaces from the url user id
                        string theUserId = url.UserId.Replace(" ", "");

                        //Add new embeddings for the URL
                        TagCollection tags = new TagCollection();
                        tags.Add("user", url.UserId);
                        if (kernelMemory != null)
                        {
                            OnStatusUpdate?.Invoke(this, $"Embedding in progress for: {url.URL}");
                            await kernelMemory.ImportWebPageAsync(url.URL, url.Id.ToString(), tags, theUserId);
                            //Update the status of the URL
                            url.LastStatus = CrawlerStatusEnum.Embedding;
                            url.LastUpdate = DateTime.Now;
                            dbContext.SaveChanges();
                            OnURLStateChanged?.Invoke(this, url);
                            while (!await kernelMemory.IsDocumentReadyAsync(url.Id.ToString(), theUserId))
                            {
                                Thread.Sleep(500);
                            }

                            //Update the status of the URL
                            url.LastStatus = CrawlerStatusEnum.Completed;
                            url.LastUpdate = DateTime.Now;
                            dbContext.SaveChanges();
                            OnURLStateChanged?.Invoke(this, url);
                        }
                    }

                    Status = WebEmbeddingServiceStatusEnum.Idle;
                    OnEmbedServiceStateChanged?.Invoke(this, Status);
                }
                OnStatusUpdate?.Invoke(this, $"");
            }
        }
    }
}
