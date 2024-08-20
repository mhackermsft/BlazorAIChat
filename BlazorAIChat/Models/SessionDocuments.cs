namespace BlazorAIChat.Models
{
    public class SessionDocument
    {
        public Guid Id { get; set; }
        public string SessionId { get; set; }
        public string DocId { get; set; }
        public string FileNameOrUrl { get; set; }

        public SessionDocument()
        {
            Id = Guid.NewGuid();
            SessionId = string.Empty;
            DocId = string.Empty;
            FileNameOrUrl = string.Empty;
        }
    }
}
