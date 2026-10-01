using System.Collections.Concurrent;
using SwiftBets.Identity.Application.Ports;

namespace SwiftBets.Identity.Infrastructure.Tests;

public sealed class RecordingMessenger : IAccountMessenger
{
    public ConcurrentQueue<(string Kind, string Email, string? Token)> Sent { get; } = new();

    public Task SendEmailVerificationAsync(Guid userId, string email, string token, CancellationToken cancellationToken) => Record("verify", email, token);

    public Task SendPasswordResetAsync(Guid userId, string email, string token, CancellationToken cancellationToken) => Record("reset", email, token);

    public Task SendAlreadyRegisteredAsync(Guid userId, string email, CancellationToken cancellationToken) => Record("already-registered", email, null);

    public string LastToken(string kind) => Sent.Last(s => s.Kind == kind).Token!;

    private Task Record(string kind, string email, string? token)
    {
        Sent.Enqueue((kind, email, token));
        return Task.CompletedTask;
    }
}
