using System.Security.Claims;

namespace BlazorAIChat
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
            if (context.Request.Headers.TryGetValue("X-MS-CLIENT-PRINCIPAL-ID", out var userId) &&
                !string.IsNullOrEmpty(userId))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId!)
                };

                if (context.Request.Headers.TryGetValue("X-MS-CLIENT-PRINCIPAL-NAME", out var userName))
                {
                    claims.Add(new Claim(ClaimTypes.Name, userName!));
                }

                if (context.Request.Headers.TryGetValue("X-MS-CLIENT-PRINCIPAL-IDP", out var idp))
                {
                    claims.Add(new Claim("idp", idp!));
                }

                var identity = new ClaimsIdentity(claims, "EasyAuth");
                context.User = new ClaimsPrincipal(identity);
            }

            await _next(context);
        }
    }
}
