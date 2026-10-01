using SwiftBets.BuildingBlocks.Core;
using SwiftBets.Contracts.Identity;
using SwiftBets.Contracts.Messaging;
using SwiftBets.Identity.Domain;
using ContractStatus = SwiftBets.Contracts.Identity.AccountStatus;
using DomainStatus = SwiftBets.Identity.Domain.AccountStatus;

namespace SwiftBets.Identity.Infrastructure.Messaging;

/// <summary>Builds identity's events; they are keyed by user id so each customer's events stay in order.</summary>
internal static class IdentityEvents
{
    public static EventEnvelope<UserRegisteredV1> Registered(User user, DateTimeOffset now) =>
        Envelope(new UserRegisteredV1(user.UserId, user.Brand, user.Country, user.Currency, now), now);

    public static EventEnvelope<EmailVerifiedV1> EmailVerified(Guid userId, DateTimeOffset now) =>
        Envelope(new EmailVerifiedV1(userId, now), now);

    public static EventEnvelope<AccountStatusChangedV1> StatusChanged(Guid userId, DomainStatus from, DomainStatus to, string reason, string changedBy, DateTimeOffset now) =>
        Envelope(new AccountStatusChangedV1(userId, Map(from), Map(to), reason, changedBy, now), now);

    public static EventEnvelope<SessionRevokedV1> AllSessionsRevoked(Guid userId, string reason, DateTimeOffset now) =>
        Envelope(new SessionRevokedV1(userId, null, reason, now), now);

    private static ContractStatus Map(DomainStatus status) => Enum.Parse<ContractStatus>(status.ToString());

    private static EventEnvelope<T> Envelope<T>(T payload, DateTimeOffset now)
        where T : IEventContract =>
        EventEnvelope<T>.Create(payload, now, CorrelationContext.CorrelationId ?? CorrelationContext.NewId());
}
