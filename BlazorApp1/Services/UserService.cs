namespace BlazorAIChat.Services
{
    using System.Security.Claims;
    using BlazorAIChat.Models;
    using BlazorAIChat.Utils;
    using Microsoft.AspNetCore.Components.Authorization;

    public class UserService
    {
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        ClaimsPrincipal? userPrincipal;

        public UserService(AuthenticationStateProvider authenticationStateProvider)
        {
            _authenticationStateProvider = authenticationStateProvider;
        }

        public async Task<User> GetCurrentUserAsync()
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            userPrincipal = authState.User;

            if (userPrincipal.Identity?.IsAuthenticated == true)
            {
                return UserUtils.ConvertPrincipalToUser(userPrincipal);
            }
            else
            {
                return new User
                {
                    Id = "Guest User",
                    Name = "Guest User",
                    Role = UserRoles.Guest
                };
            }
        }

        public bool DoesUserNeedToRequestAccess(User user, Config config, bool requireEasyAuth) =>
            user.Role == UserRoles.Guest && requireEasyAuth && userPrincipal?.Identity?.IsAuthenticated == true && config.RequireAccountApprovals;

        public bool IsUserAccountExpired(User user, Config config, bool requireEasyAuth)
        {
            if (user.Role == UserRoles.Admin)
            {
                return false;
            }

            if (config.ExpirationDays == 0)
            {
                return false;
            }

            return user.DateApproved != null && user.Role == UserRoles.User && requireEasyAuth && config.RequireAccountApprovals && user.DateApproved.Value.AddDays(config.ExpirationDays) <= DateTime.Now;
        }
    }

}
