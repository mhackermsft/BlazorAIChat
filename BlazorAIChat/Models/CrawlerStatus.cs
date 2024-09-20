using BlazorAIChat.Services;

namespace BlazorAIChat.Models
{
    public class CrawlerStatus
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public string URL { get; set; } = string.Empty;
        public CrawlerStatusEnum LastStatus { get; set; } = CrawlerStatusEnum.Queued;
        public DateTime? LastUpdate { get; set; }
        public bool IsActive { get; set; }
        public string UserId { get; set; } = string.Empty;

    }
}
