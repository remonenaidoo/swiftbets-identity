namespace SwiftBets.Identity.Application.Ports;

/// <summary>Account emails. The token is the raw single-use value; only its hash is stored.</summary>
public interface IAccountMessenger
{
    Task SendEmailVerificationAsync(Guid userId, string email, string token, CancellationToken cancellationToken);

    Task SendPasswordResetAsync(Guid userId, string email, string token, CancellationToken cancellationToken);

    /// <summary>Sent instead of a second account, so registering cannot be used to learn which emails have accounts.</summary>
    Task SendAlreadyRegisteredAsync(Guid userId, string email, CancellationToken cancellationToken);
}
