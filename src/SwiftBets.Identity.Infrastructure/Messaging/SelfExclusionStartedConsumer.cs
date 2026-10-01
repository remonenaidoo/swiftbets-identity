using Microsoft.Extensions.Logging;
using SwiftBets.BuildingBlocks.Messaging;
using SwiftBets.Contracts.Compliance;
using SwiftBets.Identity.Application.Accounts;
using SwiftBets.Identity.Domain;

namespace SwiftBets.Identity.Infrastructure.Messaging;

/// <summary>
/// A cooling-off or self-exclusion from compliance closes the account to sign-in: the status becomes self-excluded,
/// which revokes every refresh token and publishes SessionRevokedV1 so the gateway drops every device at once.
/// </summary>
public sealed partial class SelfExclusionStartedConsumer(ChangeStatusHandler handler, ILogger<SelfExclusionStartedConsumer> logger) : IEventHandler<SelfExclusionStartedV1>
{
    public const string ChangedBy = "compliance";

    public async Task HandleAsync(ConsumedEvent<SelfExclusionStartedV1> message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var started = message.Envelope.Payload;
        var reason = started.Kind == RestrictionKind.CoolingOff ? $"cooling-off until {started.EndsAt:yyyy-MM-dd}" : $"self-exclusion until {started.EndsAt:yyyy-MM-dd}";
        var result = await handler.HandleAsync(started.UserId, AccountStatus.SelfExcluded, reason, ChangedBy, cancellationToken);
        if (result.IsFailure && result.Error!.Code == "user_not_found")
        {
            LogUnknownAccount(started.UserId);
            return;
        }

        // A concurrent status change is retried by the consumer; anything else is a bug worth the dead-letter queue.
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Could not exclude {started.UserId}: {result.Error!.Code}.");
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Exclusion for unknown account {UserId} ignored")]
    private partial void LogUnknownAccount(Guid userId);
}
