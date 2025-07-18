using BlazorAIChat.Models;
using BlazorAIChat.Utils;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text;
using System.Text.RegularExpressions;

namespace BlazorAIChat.Services
{
    /// <summary>
    /// The AIService class provides various AI-related functionalities such as chat completion, document processing, and session summarization.
    /// </summary>
    public class AIService
    {
#pragma warning disable SKEXP0010, SKEXP0001, SKEXP0020, KMEXP00
        private readonly AppSettings settings;
        private readonly MemoryServerless? kernelMemory;
        private readonly HttpClient? httpClient;
        private readonly ITextTokenizer? chatCompletionTokenizer;
        private readonly ITextTokenizer? embeddingTokenizer;
        private readonly IChatCompletionService? chatCompletionService;
        private readonly IEmbeddingGenerator? textEmbeddingGenerationService;
        public ChatHistory history { get; private set; } = new ChatHistory();
        private readonly AIChatDBContext dbContext;
        private readonly ChatCompletionAgent sessionSummaryAgent;
        private readonly ChatCompletionAgent promptRewriteAgent;
        private readonly ChatCompletionAgent ragDecisionAgent;
        private readonly ChatCompletionAgent contextualQueryAgent;
        private readonly ChatHistoryService chatHistoryService;
        private readonly ILogger<AIService> logger;
        private readonly Kernel kernel;
        private readonly AISearchService? azureAISearchService;
        private readonly McpPluginProvider mcpPluginProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AIService"/> class.
        /// </summary>
        /// <param name="appSettings">The application settings.</param>
        /// <param name="chatHistoryService">The chat history service.</param>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <param name="dbContext">The database context.</param>
        /// <param name="kernel">The Semantic Kernel instance.</param>
        /// <param name="mcpPluginProvider">The MCP plugin provider for user-specific plugins.</param>
        public AIService(IOptions<AppSettings> appSettings, ChatHistoryService chatHistoryService, IHttpClientFactory httpClientFactory, AIChatDBContext dbContext, ILogger<AIService> logger, Kernel kernel, AISearchService aISearchService, McpPluginProvider mcpPluginProvider)
        {
            this.dbContext = dbContext;
            this.chatHistoryService = chatHistoryService;
            this.logger = logger;
            this.kernel = kernel;
            this.azureAISearchService = aISearchService;
            this.mcpPluginProvider = mcpPluginProvider;
            settings = appSettings.Value;
            httpClient = httpClientFactory.CreateClient("retryHttpClient");
            chatCompletionTokenizer = TokenizerFactory.GetTokenizerForModel(settings.AzureOpenAIChatCompletion.Tokenizer);
            embeddingTokenizer = TokenizerFactory.GetTokenizerForModel(settings.AzureOpenAIEmbedding.Tokenizer);

            logger.LogInformation("Initializing AIService with settings");

            chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            textEmbeddingGenerationService = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

            // Initialize agents
            (sessionSummaryAgent, promptRewriteAgent, ragDecisionAgent, contextualQueryAgent) = InitializeAgents();

            // Initialize Kernel Memory
            kernelMemory = InitializeKernelMemory();
        }

        /// <summary>
        /// Gets a formatted list of available functions in the kernel
        /// </summary>
        private string GetAvailableFunctionsList(Kernel kernel)
        {
            var sb = new StringBuilder();
            var plugins = kernel.Plugins;

            foreach (var plugin in plugins)
            {
                sb.AppendLine($"- Plugin: {plugin.Name}");
                foreach (var function in plugin.GetFunctionsMetadata())
                {
                    sb.AppendLine($"  • {function.Name}: {function.Description}");

                    // Include parameter info for better understanding
                    if (function.Parameters.Count > 0)
                    {
                        sb.AppendLine("    Parameters:");
                        foreach (var param in function.Parameters)
                        {
                            sb.AppendLine($"      - {param.Name}: {param.Description}");
                        }
                    }
                }
                sb.AppendLine();
            }

            return sb.ToString().Trim();
        }

        private (ChatCompletionAgent sessionSummaryAgent, ChatCompletionAgent promptRewriteAgent, ChatCompletionAgent ragDecisionAgent, ChatCompletionAgent contextualQueryAgent) InitializeAgents()
        {
            var sessionSummaryAgent = new ChatCompletionAgent
            {
                Name = "SessionSummaryAgent",
                Kernel = kernel,
                Instructions = """
                    Summarize this text. One to three words maximum length.
                    Plain text only. No punctuation, markup or tags.
                """,
                Arguments = new KernelArguments(
                    new OpenAIPromptExecutionSettings()
                    {
                        Temperature = 0
                    })
            };

            var promptRewriteAgent = new ChatCompletionAgent
            {
                Name = "PromptRewriteAgent",
                Kernel = kernel,
                Instructions = """
                    Rewrite the user prompt to remove all URLs but still make the question or request understandable.
                """,
                Arguments = new KernelArguments(
                    new OpenAIPromptExecutionSettings()
                    {
                        Temperature = 0.3f
                    })
            };

            var ragDecisionAgent = new ChatCompletionAgent
            {
                Name = "RAGDecisionAgent",
                Kernel = kernel,
                Instructions = $"""
                    Determine whether the question can be answered by the available tools or requires retrieval from stored knowledge.
                    
                    AVAILABLE KERNEL FUNCTIONS:
                    {GetAvailableFunctionsList(kernel)}
                    
                    RULES FOR DECISION:
                    1. If the question contains URLs, ALWAYS return 'true'
                    2. If the question refers to specific documents or uploaded content, return 'true'
                    3. If the question requires factual information, specific data, or domain knowledge like legal requirements, demographics, statistics, regulations, historical facts, or specific information about locations, return 'true'
                    4. If the question can be answered using only simple utility functions like getting the current time, date, or basic calculations, return 'false'
                    5. For ambiguous cases where both tools and retrieval might help, prefer 'true'
                    6. If you are unsure, return 'true'
                          
                    EXAMPLES:
                    - "What time is it?" → 'false' (can be answered by tools)
                    - "What is the weather in Detroit?" → 'false' (can be answered by tools)
                    - "Summarize the PDF I uploaded" → 'true' (needs retrieval)
                    - "What does this webpage say about climate change: https://example.com" → 'true' (contains URL)
                    - "How old do you have to be to get a driver's license in Ohio?" → 'true' (requires factual/legal information)
                    - "What are the requirements to vote in California?" → 'true' (requires factual/legal information)

                    
                    Analyze the question and respond with ONLY 'true' or 'false'.
                """,
                Arguments = new KernelArguments(
                    new OpenAIPromptExecutionSettings()
                    {
                        Temperature = 0,
                        MaxTokens = 10,
                    })
            };

            var contextualQueryAgent = new ChatCompletionAgent
            {
                Name = "ContextualQueryAgent",
                Kernel = kernel,
                Instructions = @"Condense the following chat history and user prompt into a concise, context-aware search query. Only output the search query, no explanations or extra text.",
                Arguments = new KernelArguments(
                    new OpenAIPromptExecutionSettings()
                    {
                        Temperature = 0.2f,
                        MaxTokens = 64
                    })
            };

            return (sessionSummaryAgent, promptRewriteAgent, ragDecisionAgent, contextualQueryAgent);
        }

        private MemoryServerless? InitializeKernelMemory()
        {
            ArgumentNullException.ThrowIfNull(textEmbeddingGenerationService);

            var kernelMemoryBuilder = new KernelMemoryBuilder()
                .WithAzureOpenAITextEmbeddingGeneration(new AzureOpenAIConfig() { Auth = AzureOpenAIConfig.AuthTypes.APIKey, APIKey = settings.AzureOpenAIChatCompletion.ApiKey, Deployment = settings.AzureOpenAIEmbedding.DeploymentName, Endpoint = settings.AzureOpenAIChatCompletion.Endpoint }, embeddingTokenizer, httpClient: httpClient)
                .WithAzureOpenAITextGeneration(new AzureOpenAIConfig() { Auth = AzureOpenAIConfig.AuthTypes.APIKey, APIKey = settings.AzureOpenAIChatCompletion.ApiKey, Deployment = settings.AzureOpenAIChatCompletion.DeploymentName, Endpoint = settings.AzureOpenAIChatCompletion.Endpoint }, chatCompletionTokenizer, httpClient);

            if (settings.UsesAzureAISearch && settings.AzureAISearch.IndexPerChatSession)
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
                kernelMemoryBuilder = kernelMemoryBuilder.WithPostgresMemoryDb(new PostgresConfig()
                {
                    ConnectionString = settings.ConnectionStrings.PostgreSQL
                });
            }
            else
            {
                kernelMemoryBuilder = kernelMemoryBuilder
                    .WithSimpleVectorDb(new Microsoft.KernelMemory.MemoryStorage.DevTools.SimpleVectorDbConfig { StorageType = Microsoft.KernelMemory.FileSystem.DevTools.FileSystemTypes.Disk, Directory = "KNN" })
                    .WithSimpleFileStorage(new Microsoft.KernelMemory.DocumentStorage.DevTools.SimpleFileStorageConfig() { StorageType = Microsoft.KernelMemory.FileSystem.DevTools.FileSystemTypes.Disk, Directory = "SFS" });
            }

            if (!settings.AzureOpenAIChatCompletion.SupportsImages && settings.UsesAzureDocIntelligence)
            {
                kernelMemoryBuilder = kernelMemoryBuilder.WithAzureAIDocIntel(new AzureAIDocIntelConfig()
                {
                    Endpoint = settings.DocumentIntelligence.Endpoint,
                    APIKey = settings.DocumentIntelligence.ApiKey,
                    Auth = AzureAIDocIntelConfig.AuthTypes.APIKey
                });
            }

            logger.LogInformation("Kernel Memory initialized successfully.");
            return kernelMemoryBuilder.Build<MemoryServerless>();
        }

        /// <summary>
        /// Processes a list of URLs with kernel memory.
        /// </summary>
        /// <param name="urls">The list of URLs to process.</param>
        /// <param name="currentSession">The current session.</param>
        /// <param name="currentUser">The current user.</param>
        private async Task ProcessURLsWithKernelMemory(List<string> urls, Session? currentSession, User currentUser)
        {
            if (kernelMemory != null && currentSession != null)
            {
                string index = currentSession.SessionId;
                TagCollection tags = new TagCollection { { "user", currentUser.Id } };
                foreach (var url in urls)
                {
                    // Check if the document already exists in the database
                    var doc = dbContext.SessionDocuments.FirstOrDefault(d => d.FileNameOrUrl == url && d.SessionId == index);
                    if (doc == null)
                    {
                        var docId = Guid.NewGuid().ToString();
                        // Import the web page into kernel memory
                        await kernelMemory.ImportWebPageAsync(url, docId, tags, index).ConfigureAwait(false);
                        // Wait until the document is ready
                        while (!await kernelMemory.IsDocumentReadyAsync(docId, index).ConfigureAwait(false))
                        {
                            await Task.Delay(500).ConfigureAwait(false);
                        }
                        // Add the document to the database
                        dbContext.SessionDocuments.Add(new SessionDocument() { DocId = docId, FileNameOrUrl = url, SessionId = index });
                        await dbContext.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the chat response asynchronously.
        /// </summary>
        /// <param name="prompt">The prompt for the chat.</param>
        /// <param name="message">The message object.</param>
        /// <param name="currentSession">The current session.</param>
        /// <param name="currentUser">The current user.</param>
        /// <returns>An asynchronous enumerable of streaming chat message content.</returns>
        public async Task<IAsyncEnumerable<List<StreamingChatMessageContent>>> GetChatResponseAsync(string prompt, Message message, Session currentSession, User currentUser)
        {
            logger.LogInformation("Getting chat response for session: {SessionId}, user: {UserId}", currentSession.SessionId, currentUser.Id);
            ArgumentNullException.ThrowIfNull(chatCompletionTokenizer);
            ArgumentNullException.ThrowIfNull(chatCompletionService);

            // Conditionally perform RAG only if needed
            bool ragNeeded = await ShouldUseRAGForPrompt(prompt, currentSession.SessionId);
            if (ragNeeded)
            {
                logger.LogInformation("RAG determined to be needed, performing document retrieval");
                await DoRAG(prompt, message, currentSession, currentUser).ConfigureAwait(false);
            }
            else
            {
                logger.LogInformation("RAG determined not needed, proceeding with direct completion");
                // Just add the prompt to history without RAG
                history.AddUserMessage(prompt);
            }

            // Clean up the chat history to fit within the token limit
            history = await AIUtils.CleanUpHistoryAsync(history, chatCompletionService, 10, 5);

            // Add user-specific MCP plugins to the kernel
            var userMcpPlugins = await mcpPluginProvider.GetUserPluginsAsync(currentUser.Id);
            foreach (var plugin in userMcpPlugins)
            {
                if (!kernel.Plugins.Contains(plugin))
                {
                    kernel.Plugins.Add(plugin);
                }
            }

            IAsyncEnumerable<StreamingChatMessageContent> streamingMessages;

            // Get the chat response as a stream of messages
            AzureOpenAIPromptExecutionSettings executionSettings = new()
            {
                Temperature = 0,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
            };

            
            streamingMessages = chatCompletionService.GetStreamingChatMessageContentsAsync(history, executionSettings, kernel);
            
            // Buffer and yield messages in chunks
            logger.LogInformation("Chat response generation completed for session: {SessionId}", currentSession.SessionId);
            return BufferMessagesInChunks(streamingMessages, settings.AzureOpenAIChatCompletion.ResponseChunkSize);
        }

        /// <summary>
        /// Determines whether RAG should be used for the given prompt
        /// </summary>
        /// <param name="prompt">The user's prompt</param>
        /// <param name="sessionId">The current session ID</param>
        /// <returns>True if RAG should be used, false otherwise</returns>
        private async Task<bool> ShouldUseRAGForPrompt(string prompt, string sessionId)
        {
            // If the configuration indicates that we should not use AI to determine RAG usage
            // we always return true to use RAG.
            if (settings.AIDeterminesRagUsage == false)
                return true;

            // Always use RAG if URLs are present in the prompt
            if (StringUtils.GetURLsFromString(prompt).Count > 0)
            {
                return true;
            }

            // Check if there are any documents in this session or if we are using shared knowledge store
            bool sessionHasDocuments = dbContext.SessionDocuments.Any(d => d.SessionId == sessionId);
            if (!sessionHasDocuments && !settings.UsesAzureAISearchSharedKnowledge)
            {
                // If no documents in session, don't bother with RAG unless there is a central knowledge store configured.
                return false;
            }

            // Use ChatCompletionAgent to analyze if retrieval is needed
            ChatHistory analysisHistory = new();
       
            analysisHistory.AddUserMessage(prompt);
            StringBuilder output = new();
            Microsoft.SemanticKernel.Agents.AgentThread? agentThread = null;
            await foreach (var response in ragDecisionAgent.InvokeAsync(analysisHistory, agentThread).ConfigureAwait(false))
            {
                output.Append(response.Message.ToString());
                agentThread = response.Thread;
            }
            var completionText = output.ToString().Trim().ToLower();
            if (agentThread is not null)
            {
                await agentThread.DeleteAsync();
            }
            return completionText.Contains("true");
        }

        private async IAsyncEnumerable<List<StreamingChatMessageContent>> BufferMessagesInChunks(IAsyncEnumerable<StreamingChatMessageContent> streamingMessages, int chunkSize)
        {
            List<StreamingChatMessageContent> buffer = new List<StreamingChatMessageContent>(chunkSize);

            await foreach (var message in streamingMessages)
            {
                buffer.Add(message);

                if (buffer.Count >= chunkSize)
                {
                    yield return new List<StreamingChatMessageContent>(buffer);
                    buffer.Clear();
                }
            }

            // Yield any remaining messages
            if (buffer.Count > 0)
            {
                yield return new List<StreamingChatMessageContent>(buffer);
            }
        }

        /// <summary>
        /// Generates a context-aware search query using recent chat history.
        /// </summary>
        private async Task<string> GenerateContextualQuery(string prompt, string sessionId, int historyTurns = 4)
        {
            var messages = await chatHistoryService.GetSessionMessagesAsync(sessionId).ConfigureAwait(false);
            var recentMessages = messages.OrderByDescending(m => m.TimeStamp).Take(historyTurns).OrderBy(m => m.TimeStamp).ToList();
            var sb = new StringBuilder();
            foreach (var msg in recentMessages)
            {
                sb.AppendLine($"User: {msg.Prompt}");
                if (!string.IsNullOrWhiteSpace(msg.Completion))
                    sb.AppendLine($"Assistant: {msg.Completion}");
            }
            sb.AppendLine($"User: {prompt}");
            // Use an agent to condense this into a search query
            ChatHistory agentHistory = new() { new ChatMessageContent(AuthorRole.User, sb.ToString()) };
            StringBuilder output = new();
            Microsoft.SemanticKernel.Agents.AgentThread? agentThread = null;
            await foreach (var response in contextualQueryAgent.InvokeAsync(agentHistory, agentThread).ConfigureAwait(false))
            {
                output.Append(response.Message.ToString());
                agentThread = response.Thread;
            }
            if (agentThread is not null)
            {
                await agentThread.DeleteAsync();
            }
            return output.ToString().Trim();
        }

        /// <summary>
        /// Filters and prioritizes search results from kernel memory and Azure AI Search.
        /// </summary>
        private List<string> FilterAndPrioritizeResults(SearchResult? searchData, List<(double? score, string? title, string? chunk)>? azureResults)
        {
            var combinedResults = new List<(double score, string content, string source)>();
            if (searchData != null && !searchData.NoResult)
            {
                foreach (var result in searchData.Results)
                {
                    // Use 1.0 as default relevance if not available
                    double relevance = 1.0;
                    if (result.GetType().GetProperty("Relevance") != null)
                    {
                        relevance = (double)(result.GetType().GetProperty("Relevance")?.GetValue(result) ?? 1.0);
                    }
                    foreach (var p in result.Partitions)
                    {
                        combinedResults.Add((relevance, p.Text, result.SourceName));
                    }
                }
            }
            if (azureResults != null)
            {
                foreach (var r in azureResults.Where(r => r.chunk != null))
                {
                    combinedResults.Add((r.score ?? 0.5, r.chunk!, r.title ?? "Unknown"));
                }
            }
            return combinedResults.OrderByDescending(r => r.score).Take(5).Select(r => $"SOURCE: {r.source}\n{r.content}").ToList();
        }

        private async Task DoRAG(string prompt, Message message, Session currentSession, User currentUser)
        {
            logger.LogInformation("Performing RAG for session: {SessionId}, user: {UserId}", currentSession.SessionId, currentUser.Id);
            var urls = StringUtils.GetURLsFromString(prompt);
            SearchResult? searchData = null;
            List<(double? score, string? title, string? chunk)>? sharedResults = null;
            string contextualQuery = await GenerateContextualQuery(prompt, currentSession.SessionId);
            if (urls.Count > 0)
            {
                await ProcessURLsWithKernelMemory(urls, currentSession, currentUser).ConfigureAwait(false);
                string messageToProcessNoURLS = await GenerateNewPromptForMessagesWithUrl(prompt).ConfigureAwait(false);
                searchData = await kernelMemory!.SearchAsync(contextualQuery, currentSession.Id, minRelevance: 0.5, limit: 5).ConfigureAwait(false);
            }
            else
            {
                searchData = await kernelMemory!.SearchAsync(contextualQuery, currentSession.Id, minRelevance: 0.5, limit: 5).ConfigureAwait(false);
            }
            if (azureAISearchService != null && azureAISearchService.IsReady)
            {
                sharedResults = await azureAISearchService.Search(contextualQuery, 5, exhaustive: false, semantic: true);
            }
            var relevantContent = FilterAndPrioritizeResults(searchData, sharedResults);
            if (relevantContent.Any())
            {
                history.AddUserMessage(
                    "I've found relevant information from your documents that may help answer your question:\n\n" +
                    string.Join("\n\n---\n\n", relevantContent)
                );
            }
            // Add the original prompt to the chat history
            history.AddUserMessage(prompt);
            logger.LogInformation("RAG completed for session: {SessionId}", currentSession.SessionId);
        }

        /// <summary>
        /// Generates a new prompt by removing URLs from the given message.
        /// </summary>
        /// <param name="theMessage">The message containing URLs.</param>
        /// <returns>The new prompt without URLs.</returns>
        private async Task<string> GenerateNewPromptForMessagesWithUrl(string theMessage)
        {

            // Create a chat history with the entire conversation
            ChatHistory sessionSummary = new() { new ChatMessageContent(AuthorRole.User, theMessage) };
            StringBuilder output = new();

            Microsoft.SemanticKernel.Agents.AgentThread? agentThread = null;
            // Get the updated prompt from the prompt rewrite agent
            await foreach (var response in promptRewriteAgent.InvokeAsync(sessionSummary, agentThread).ConfigureAwait(false))
            {
                output.Append(response.Message.ToString());
                agentThread = response.Thread;
            }
            var completionText = output.ToString();

            // Delete the thread if required.
            if (agentThread is not null)
            {
                await agentThread.DeleteAsync();
            }

            return completionText;
        }

        /// <summary>
        /// Summarizes the chat session name asynchronously.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns>The summarized session name.</returns>
        public async Task<string> SummarizeChatSessionNameAsync(string? sessionId)
        {
            logger.LogInformation("Summarizing chat session name for session: {SessionId}", sessionId);
            ArgumentNullException.ThrowIfNull(sessionId);
            ArgumentNullException.ThrowIfNull(chatCompletionService);

            // Get all messages from the session
            var messages = await chatHistoryService.GetSessionMessagesAsync(sessionId).ConfigureAwait(false);
            var conversationText = string.Join(" ", messages.Select(m => m.Prompt + " " + m.Completion));

            //Strip base64 encoded content from the conversation text. We don't need to send all of that to the agent.
            string pattern = @"data:image\/[a-zA-Z]+;base64,[^\s]+";
            conversationText = Regex.Replace(conversationText, pattern, string.Empty);

            // Create a chat history with the entire conversation
            ChatHistory sessionSummary = new() { new ChatMessageContent(AuthorRole.User, conversationText) };
            StringBuilder output = new();
            // Get the summary from the session summary agent
            Microsoft.SemanticKernel.Agents.AgentThread? agentThread = null;
            await foreach (var response in sessionSummaryAgent.InvokeAsync(sessionSummary, agentThread).ConfigureAwait(false))
            {
                output.Append(response.Message.ToString());
                agentThread = response.Thread;
            }
            var completionText = output.ToString();

            // Delete the thread if required.
            if (agentThread is not null)
            {
                await agentThread.DeleteAsync();
            }

            // Update the session name with the summary
            var session = await chatHistoryService.GetSessionAsync(sessionId).ConfigureAwait(false);
            session.Name = completionText;
            await chatHistoryService.UpdateSessionAsync(session).ConfigureAwait(false);
            logger.LogInformation("Chat session name summarized for session: {SessionId}", sessionId);
            return completionText;
        }

        /// <summary>
        /// Processes documents with kernel memory.
        /// </summary>
        /// <param name="memoryStream">The memory stream of the document.</param>
        /// <param name="filename">The filename of the document.</param>
        /// <param name="currentSession">The current session.</param>
        /// <param name="currentUser">The current user.</param>
        /// <returns>A boolean indicating whether the document was processed successfully.</returns>
        public async Task<bool> ProcessDocsWithKernelMemory(MemoryStream memoryStream, string filename, Session currentSession, User currentUser)
        {
            logger.LogInformation("Processing document: {Filename} for session: {SessionId}, user: {UserId}", filename, currentSession.SessionId, currentUser.Id);
            ArgumentNullException.ThrowIfNull(kernelMemory);

            // Check if the document already exists in the database
            var doc = dbContext.SessionDocuments.FirstOrDefault(d => d.FileNameOrUrl == filename && d.SessionId == currentSession.SessionId);
            if (doc != null)
            {
                return false;
            }

            var docId = Guid.NewGuid().ToString();
            string index = currentSession.SessionId;
            TagCollection tags = new TagCollection { { "user", currentUser.Id } };
            memoryStream.Position = 0;

            // Import the document into kernel memory
            await kernelMemory.ImportDocumentAsync(memoryStream, filename, docId, tags, index).ConfigureAwait(false);

            // Add the document to the database
            dbContext.SessionDocuments.Add(new SessionDocument() { DocId = docId, FileNameOrUrl = filename, SessionId = index });
            await dbContext.SaveChangesAsync().ConfigureAwait(false);

            // Wait until the document is ready
            while (!await kernelMemory.IsDocumentReadyAsync(docId, index).ConfigureAwait(false))
            {
                await Task.Delay(1000).ConfigureAwait(false);
            }

            logger.LogInformation("Document processed successfully: {Filename} for session: {SessionId}", filename, currentSession.SessionId);
            return true;
        }

        /// <summary>
        /// Deletes uploaded documents for a given session.
        /// </summary>
        /// <param name="sessionIdToDelete">The session identifier to delete documents for.</param>
        /// <returns>A boolean indicating whether the documents were deleted successfully.</returns>
        public async Task<bool> DeleteUploadedDocs(string? sessionIdToDelete)
        {
            logger.LogInformation("Deleting uploaded documents for session: {SessionId}", sessionIdToDelete);
            if (!string.IsNullOrEmpty(sessionIdToDelete))
            {
                if (kernelMemory != null)
                    await kernelMemory.DeleteIndexAsync(sessionIdToDelete).ConfigureAwait(false);

                // Remove documents from the database
                var docs = dbContext.SessionDocuments.Where(d => d.SessionId == sessionIdToDelete);
                dbContext.SessionDocuments.RemoveRange(docs);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);

                logger.LogInformation("Uploaded documents deleted for session: {SessionId}", sessionIdToDelete);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds an image to the chat.
        /// </summary>
        /// <param name="imageStream">The memory stream of the image.</param>
        /// <param name="uploadImageType">The type of the uploaded image.</param>
        /// <returns>A boolean indicating whether the image was added successfully.</returns>
        public bool AddImageToChat(MemoryStream imageStream, string uploadImageType)
        {
            logger.LogInformation("Adding image to chat with type: {ImageType}", uploadImageType);
            var sendMessage = new ChatMessageContentItemCollection
            {
                new ImageContent { Data = imageStream.ToArray(), MimeType = uploadImageType }
            };
            // Add the image to the chat history
            history.AddUserMessage(sendMessage);
            logger.LogInformation("Image added to chat successfully.");
            return true;
        }
    }
}
