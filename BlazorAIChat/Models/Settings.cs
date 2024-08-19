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

        public bool UsesPostgreSQL => !string.IsNullOrEmpty(ConnectionStrings.PostgreSQL);
        public bool UsesCosmosDb => !string.IsNullOrEmpty(ConnectionStrings.CosmosDb);
        public bool UsesAzureAISearch => !string.IsNullOrEmpty(AzureAISearch.Endpoint) && !string.IsNullOrEmpty(AzureAISearch.ApiKey);
        public bool UsesAzureDocIntelligence => !string.IsNullOrEmpty(DocumentIntelligence.Endpoint) && !string.IsNullOrEmpty(DocumentIntelligence.ApiKey);

    }

    public class AzureOpenAIChatCompletionSettings
    {
        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int MaxInputTokens { get; set; } = 128000;
        public bool SupportsImages { get; set; } = false;
    }

    public class AzureOpenAIEmbeddingSettings
    {
        public string Model { get; set; } = string.Empty;
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
    }
}
