namespace lmsPortalBe.Services;

public static class JwtConstants
{
    public const string Secret = "JWT_SECRET";
    public const string Issuer = "JWT_ISSUER";
    public const string Audience = "JWT_AUDIENCE";
    public const string AccessTokenMinutes = "JWT_ACCESS_TOKEN_MINUTES";
    public const string RefreshTokenDays = "JWT_REFRESH_TOKEN_DAYS";

    public const int DefaultAccessTokenMinutes = 30;
    public const int DefaultRefreshTokenDays = 7;

    public const string RefreshTokenCookie = "refresh_token";
}
