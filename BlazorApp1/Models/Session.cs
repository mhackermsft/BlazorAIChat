using System.Text.Json.Serialization;

namespace BlazorAIChat.Models
{
    public record Session
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; }

        public string Type { get; set; }

        /// <summary>
        /// Partition key
        /// </summary>
        public string SessionId { get; set; }

        public string UserId { get; set; }

        public string Name { get; set; }

        public DateTime SessionCreatedAt { get; set; }

        public Session()
        {
            Id = Guid.NewGuid().ToString();
            Type = nameof(Session);
            SessionId = this.Id;
            Name = Constants.NEW_CHAT;
            UserId = string.Empty;
        }
    }
}
