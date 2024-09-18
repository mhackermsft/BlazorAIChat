namespace BlazorAIChat.Models
{
    public class CrawlerStatus
    {
        public Guid Id { get; set; }
        public string URL { get; set; } = string.Empty;
        public string LastStatus { get; set; } = string.Empty;
        public DateTime? LastUpdate { get; set; }
        public bool IsActive { get; set; }
        public string UserId { get; set; } = string.Empty;

    }
}
