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
            //Account is never expired if EasyAuth is not required
            if (requireEasyAuth==false)
            {
                return false;
            }

            //Admin accounts never expire
            if (user.Role == UserRoles.Admin)
            {
                return false;
            }

            //If expiration days is set to 0, then accounts never expire
            if (config.ExpirationDays == 0)
            {
                return false;
            }

            //If account approvals are not required, then accounts never expire
            if (config.RequireAccountApprovals == false)
            {
                return false;
            }

            //If the account has been approved and the expiration date has passed, then the account is expired
            return user.DateApproved != null && user.DateApproved.Value.AddDays(config.ExpirationDays) <= DateTime.Now;
        }
    }

}
