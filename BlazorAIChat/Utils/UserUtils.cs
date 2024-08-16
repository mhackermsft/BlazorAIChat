using BlazorAIChat.Models;
using System.Security.Claims;

namespace BlazorAIChat.Utils
{
    public static class UserUtils
    {
        public static User ConvertPrincipalToUser(ClaimsPrincipal principal)
        {
            var user = new User() { Id=string.Empty};
            user.Id = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value??string.Empty;
            user.Name = principal.FindFirst(ClaimTypes.Name)?.Value??string.Empty;
            user.Email = principal.FindFirst(ClaimTypes.Email)?.Value??string.Empty;
            user.Role = Enum.Parse<UserRoles>(principal.FindFirst(ClaimTypes.Role)?.Value ?? UserRoles.Guest.ToString());
            user.DateRequested = DateTime.Parse(principal.FindFirst("dateRequested")?.Value ?? DateTime.Now.ToString());
            user.DateApproved = DateTime.Parse(principal.FindFirst("dateApproved")?.Value ?? DateTime.Now.ToString());
            user.ApprovedBy = principal.FindFirst("approvedBy")?.Value ?? string.Empty;
            return user;

        }
    }
}
