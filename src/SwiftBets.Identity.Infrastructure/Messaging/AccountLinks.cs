namespace SwiftBets.Identity.Infrastructure.Messaging;

internal static class AccountLinks
{
    public static string Build(AccountEmailOptions options, string path, string? token) =>
        $"{options.PublicBaseUrl.TrimEnd('/')}/{path}" + (token is null ? string.Empty : $"?token={Uri.EscapeDataString(token)}");
}
