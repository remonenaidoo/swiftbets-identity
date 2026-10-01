using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SwiftBets.BuildingBlocks.Core;
using SwiftBets.BuildingBlocks.Outbox;
using SwiftBets.BuildingBlocks.Persistence;
using SwiftBets.Contracts.Messaging;
using SwiftBets.Contracts.Notifications;
using SwiftBets.Identity.Application.Ports;

namespace SwiftBets.Identity.Infrastructure.Messaging;

/// <summary>
/// Hands account email to the notifications service through the outbox, so a request survives a restart and a
/// broker outage. Identity builds the links because it owns the account pages' URLs.
/// </summary>
public sealed class NotificationRequestMessenger(ISqlConnectionFactory connections, IOutbox outbox, IOptions<AccountEmailOptions> options, TimeProvider time) : IAccountMessenger
{
    public Task SendEmailVerificationAsync(Guid userId, string email, string token, CancellationToken cancellationToken) =>
        RequestAsync(userId, email, "account.verify-email", new() { ["link"] = AccountLinks.Build(options.Value, "account/verify", token) }, cancellationToken);

    public Task SendPasswordResetAsync(Guid userId, string email, string token, CancellationToken cancellationToken) =>
        RequestAsync(userId, email, "account.reset-password", new() { ["link"] = AccountLinks.Build(options.Value, "account/reset-password", token) }, cancellationToken);

    public Task SendAlreadyRegisteredAsync(Guid userId, string email, CancellationToken cancellationToken) =>
        RequestAsync(userId, email, "account.already-registered", new()
        {
            ["signInLink"] = AccountLinks.Build(options.Value, "account/sign-in", null),
            ["resetLink"] = AccountLinks.Build(options.Value, "account/forgot-password", null),
        }, cancellationToken);

    private async Task RequestAsync(Guid userId, string email, string template, Dictionary<string, string> data, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var request = new NotificationRequestedV1(Guid.CreateVersion7(), userId, NotificationChannel.Email, template, email, data, false, now);
        var envelope = EventEnvelope<NotificationRequestedV1>.Create(request, now, CorrelationContext.CorrelationId ?? CorrelationContext.NewId());
        await using var connection = (SqlConnection)await connections.OpenAsync(cancellationToken);
        await using var transaction = connection.BeginTransaction();
        await outbox.EnqueueAsync(transaction, Topics.NotificationRequested, userId.ToString(), envelope, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
