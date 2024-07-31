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
            if (context.Request.Headers.ContainsKey("X-MS-CLIENT-PRINCIPAL-NAME"))
            {
                var userName = context.Request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"].ToString();
                var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userName)
            };
                var identity = new ClaimsIdentity(claims, "EasyAuth");
                context.User = new ClaimsPrincipal(identity);
            }

            await _next(context);
        }
    }

}
