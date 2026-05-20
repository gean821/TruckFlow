namespace TruckFlow.Extensions.Auth
{
    public static class RefreshCookieHelper
    {
        public const string CookieName = "tf_refresh";

        public static CookieOptions BuildOptions(
            IWebHostEnvironment env,
            DateTime? expiresAt = null) =>
            new()
            {
                HttpOnly = true,
                Secure = !env.IsDevelopment(),
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = expiresAt
            };

        public static void SetRefresh(
            HttpResponse response,
            IWebHostEnvironment env,
            string rawToken,
            DateTime expiresAt) =>
            response.Cookies.Append(CookieName, rawToken, BuildOptions(env, expiresAt));

        public static void ClearRefresh
        (HttpResponse response,
        IWebHostEnvironment env) =>
            response.Cookies.Delete(CookieName, BuildOptions(env));
    }
}