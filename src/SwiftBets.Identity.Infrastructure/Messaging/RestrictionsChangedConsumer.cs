using SwiftBets.BuildingBlocks.Messaging;
using SwiftBets.Contracts.Compliance;
using SwiftBets.Identity.Application.Accounts;

namespace SwiftBets.Identity.Infrastructure.Messaging;

/// <summary>When compliance's snapshot shows no active cooling-off or self-exclusion, an account compliance closed reopens.</summary>
public sealed class RestrictionsChangedConsumer(ChangeStatusHandler handler, TimeProvider time) : IEventHandler<RestrictionsChangedV1>
{
    public async Task HandleAsync(ConsumedEvent<RestrictionsChangedV1> message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var snapshot = message.Envelope.Payload;
        var now = time.GetUtcNow();
        var excluded = snapshot.Restrictions.Any(r =>
            r.Kind is RestrictionKind.CoolingOff or RestrictionKind.SelfExclusion && r.StartsAt <= now && (r.EndsAt is null || r.EndsAt > now));
        if (!excluded)
        {
            await handler.EndComplianceExclusionAsync(snapshot.UserId, SelfExclusionStartedConsumer.ChangedBy, cancellationToken);
        }
    }
}
