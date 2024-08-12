using System.ComponentModel.DataAnnotations;

namespace BlazorAIChat.Models
{
    public class User
    {

        [Key]
        public required string Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRoles Role { get; set; } = UserRoles.Guest;
        public DateTime DateRequested { get; set; } = DateTime.Now;
        public DateTime? DateApproved { get; set; }
        public string? ApprovedBy { get; set; }
    }
}
