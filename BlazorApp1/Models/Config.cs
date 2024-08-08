namespace BlazorAIChat.Models
{
    public class Config
    {
        public required Guid Id { get; set; }
        public bool RequireAccountApprovals { get; set; } = true;
        public int ExpirationDays { get; set; } = 60;
    }
}
