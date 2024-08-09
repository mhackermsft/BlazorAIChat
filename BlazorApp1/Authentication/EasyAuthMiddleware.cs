using BlazorAIChat.Models;
using System.Security.Claims;

namespace BlazorAIChat.Authentication
{
    public class EasyAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public EasyAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            //Only process if the user is authenticated with Easy Auth via App Service.
            if (context.Request.Headers.TryGetValue("X-MS-CLIENT-PRINCIPAL-ID", out var userId) &&
                !string.IsNullOrEmpty(userId))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId!)
                };

                //We set the name claim to the Principal Name if it exists, otherwise we use the userId.
                if (context.Request.Headers.TryGetValue("X-MS-CLIENT-PRINCIPAL-NAME", out var userName))
                {
                    claims.Add(new Claim(ClaimTypes.Name, userName!));
                }
                else
                {
                    claims.Add(new Claim(ClaimTypes.Name, userId!));
                }

                if (context.Request.Headers.TryGetValue("X-MS-CLIENT-PRINCIPAL-IDP", out var idp))
                {
                    claims.Add(new Claim("idp", idp!));
                }

                //Get the database context from the DI container
                using (var scope = context.RequestServices.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AIChatDBContext>();

                    //get the user details from the database
                    var dbUser = await dbContext.Users.FindAsync(userId);
                    if (dbUser != null)
                    {
                        //if the dbUser.Name has a value, replace the name claim with the value from the database
                        if (!string.IsNullOrEmpty(dbUser.Name))
                        {
                            claims.Remove(claims.First(c => c.Type == ClaimTypes.Name));
                            claims.Add(new Claim(ClaimTypes.Name, dbUser.Name));
                        }
                        else
                        {
                            //if the dbUser.Name is empty, set the dbUser.Name to the value from the claim
                            dbUser.Name = claims.First(c => c.Type == ClaimTypes.Name).Value;
                            await dbContext.SaveChangesAsync();
                        }

                        claims.Add(new Claim(ClaimTypes.Role, Enum.GetName(dbUser.Role)!));
                        claims.Add(new Claim(ClaimTypes.Email, dbUser.Email));
                        claims.Add(new Claim("dateRequested", dbUser.DateRequested.ToString()));
                        if (dbUser.DateApproved != null)
                        {
                            claims.Add(new Claim("dateApproved", dbUser.DateApproved.Value.ToString()));
                        }
                        if (dbUser.ApprovedBy != null)
                        {
                            claims.Add(new Claim("approvedBy", dbUser.ApprovedBy));
                        }
                    }
                    else
                    {
                        claims.Add(new Claim(ClaimTypes.Role, Enum.GetName(UserRoles.Guest)!));
                    }

                    var identity = new ClaimsIdentity(claims, "EasyAuth");
                    context.User = new ClaimsPrincipal(identity);

                }
            }

            await _next(context);
        }
    }
}
