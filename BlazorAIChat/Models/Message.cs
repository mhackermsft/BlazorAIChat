using Microsoft.KernelMemory;

namespace BlazorAIChat.Models
{
    public record Message
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

        public DateTime TimeStamp { get; set; }

        public string Prompt { get; set; }

        public string Completion { get; set; }

        public List<string> Citations { get; set; }

        public Message(string sessionId, string prompt, string completion = "")
        {
            Id = Guid.NewGuid().ToString();
            Type = nameof(Message);
            SessionId = sessionId;
            TimeStamp = DateTime.UtcNow;
            Prompt = prompt;
            Completion = completion;
            Citations = new List<string>();
        }
    }
}
