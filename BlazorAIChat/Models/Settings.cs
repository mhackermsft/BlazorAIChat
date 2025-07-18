namespace BlazorAIChat.Models
{

    public class AppSettings
    {
        public AzureOpenAIChatCompletionSettings AzureOpenAIChatCompletion { get; set; } = new AzureOpenAIChatCompletionSettings();
        public AzureOpenAIEmbeddingSettings AzureOpenAIEmbedding { get; set; } = new AzureOpenAIEmbeddingSettings();
        public bool RequireEasyAuth { get; set; } = true;
        public string SystemMessage { get; set; } = string.Empty;
        public ConnectionStringsSettings ConnectionStrings { get; set; } = new ConnectionStringsSettings();
        public CosmosDbSettings CosmosDb { get; set; } = new CosmosDbSettings();
        public DocumentIntelligenceSettings DocumentIntelligence { get; set; } = new DocumentIntelligenceSettings();
        public AzureAISearchSettings AzureAISearch { get; set; } = new AzureAISearchSettings();
        public McpConfig Mcp { get; set; } = new McpConfig();
        public bool AIDeterminesRagUsage { get; set; } = true;
        public bool UsesPostgreSQL => !string.IsNullOrEmpty(ConnectionStrings.PostgreSQL);
        public bool UsesCosmosDb => !string.IsNullOrEmpty(ConnectionStrings.CosmosDb);
        public bool UsesAzureAISearch => !string.IsNullOrEmpty(AzureAISearch.Endpoint) && !string.IsNullOrEmpty(AzureAISearch.ApiKey) && AzureAISearch.IndexPerChatSession;
        public bool UsesAzureDocIntelligence => !string.IsNullOrEmpty(DocumentIntelligence.Endpoint) && !string.IsNullOrEmpty(DocumentIntelligence.ApiKey);

        public bool UsesAzureAISearchSharedKnowledge => !string.IsNullOrEmpty(AzureAISearch.Endpoint) &&
            !string.IsNullOrEmpty(AzureAISearch.ApiKey) &&
            !string.IsNullOrEmpty(AzureAISearch.SharedIndex) &&
            !string.IsNullOrEmpty(AzureAISearch.SharedIndexAzureBlobStorageConnection) &&
            !string.IsNullOrEmpty(AzureAISearch.SharedIndexAzureBlobStorageContainer);
    }

    public class AzureOpenAIChatCompletionSettings
    {
        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string DeploymentName { get; set; } = string.Empty;
        public string Tokenizer { get; set; } = string.Empty;
        public int MaxInputTokens { get; set; } = 128000;
        public bool SupportsImages { get; set; } = false;
        public int ResponseChunkSize { get; set; } = 50;
    }

    public class AzureOpenAIEmbeddingSettings
    {
        public string DeploymentName { get; set; } = string.Empty;
        public string Tokenizer { get; set; } = string.Empty;
        public int MaxInputTokens { get; set; } = 8192;
    }

    public class ConnectionStringsSettings
    {
        public string PostgreSQL { get; set; } = string.Empty;
        public string CosmosDb { get; set; } = string.Empty;
        public string ConfigDatabase { get; set; } = string.Empty;
    }

    public class CosmosDbSettings
    {
        public string Database { get; set; } = string.Empty;
        public string Container { get; set; } = string.Empty;
    }

    public class DocumentIntelligenceSettings
    {
        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }

    public class AzureAISearchSettings
    {
        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public bool IndexPerChatSession { get; set; } = false;
        public string SharedIndex {  get; set; } = string.Empty;
        public string SharedIndexAzureBlobStorageConnection {  get; set; } = string.Empty;
        public string SharedIndexAzureBlobStorageContainer {  get; set; } = string.Empty;
    }

    public class McpConfig
    {
        public List<McpInput> Inputs { get; set; } = new();
        public Dictionary<string, McpServer> Servers { get; set; } = new();
    }

    public class McpInput
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Password { get; set; }
    }

    public class McpServer
    {
        public string Type { get; set; } = string.Empty;
        public string? Command { get; set; }
        public List<string>? Args { get; set; }
        public Dictionary<string, string>? Env { get; set; }
        public string? Url { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
    }

    public class UserMcpParameter
    {
        public int Id { get; set; }
        public required string UserId { get; set; }
        public required string InputId { get; set; }
        public required string EncryptedValue { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
