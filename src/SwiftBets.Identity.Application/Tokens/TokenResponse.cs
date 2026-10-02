namespace SwiftBets.Identity.Application.Tokens;

public sealed record TokenResponse(string AccessToken, string TokenType, int ExpiresIn, string? RefreshToken);

/// <summary>The app's rotated tokens, and a separate sign-in for the browser it opens.</summary>
public sealed record HandoffResponse(TokenResponse Device, TokenResponse Browser);
