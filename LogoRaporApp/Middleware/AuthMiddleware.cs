namespace LogoRaporApp.Middleware
{
    public class AuthMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // Static dosyalar ve account sayfaları kontrol dışı
            if (path.StartsWith("/account") ||
                path.StartsWith("/lib") ||
                path.StartsWith("/css") ||
                path.StartsWith("/js") ||
                path == "/")
            {
                await _next(context);
                return;
            }

            var user = context.Session.GetString("user");

            if (string.IsNullOrEmpty(user))
            {
                context.Response.Redirect("/Account/Login");
                return;
            }

            await _next(context);
        }
    }
}