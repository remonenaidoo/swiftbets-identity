using Microsoft.Extensions.Logging.Abstractions;
using SwiftBets.BuildingBlocks.Messaging;
using SwiftBets.BuildingBlocks.Testing;
using SwiftBets.Contracts.Compliance;
using SwiftBets.Contracts.Messaging;
using SwiftBets.Identity.Application.Accounts;
using SwiftBets.Identity.Domain;
using SwiftBets.Identity.Infrastructure.Messaging;

namespace SwiftBets.Identity.Infrastructure.Tests;

public sealed class SelfExclusionConsumerTests(SqlServerFixture sql)
{
    [Fact]
    public async Task Exclusion_from_compliance_self_excludes_the_account_and_repeats_are_harmless()
    {
        var db = await IdentityDatabase.CreateAsync(sql);
        (await db.Register.HandleAsync(new RegisterCommand("ex@example.com", "a long passphrase", new DateOnly(1990, 1, 1), "ZA", "ZAR"), CancellationToken.None)).IsSuccess.ShouldBeTrue();
        var user = (await db.Users.FindByLoginAsync("ex@example.com", CancellationToken.None))!;
        var consumer = new SelfExclusionStartedConsumer(db.Status, NullLogger<SelfExclusionStartedConsumer>.Instance);
        var message = Message(user.UserId, RestrictionKind.SelfExclusion, db.Time.GetUtcNow());

        await consumer.HandleAsync(message, CancellationToken.None);
        await consumer.HandleAsync(message, CancellationToken.None);

        (await db.Users.FindByIdAsync(user.UserId, CancellationToken.None))!.Status.ShouldBe(AccountStatus.SelfExcluded);
        (await db.Sessions.PasswordAsync("ex@example.com", "a long passphrase", CancellationToken.None)).Error!.Code.ShouldBe("account_self_excluded");
    }

    [Fact]
    public async Task Account_reopens_when_compliance_reports_the_exclusion_over_but_not_one_an_operator_set()
    {
        var db = await IdentityDatabase.CreateAsync(sql);
        var byCompliance = await RegisterAsync(db, "c@example.com");
        var byOperator = await RegisterAsync(db, "o@example.com");
        var now = db.Time.GetUtcNow();
        await new SelfExclusionStartedConsumer(db.Status, NullLogger<SelfExclusionStartedConsumer>.Instance).HandleAsync(Message(byCompliance, RestrictionKind.CoolingOff, now), CancellationToken.None);
        await db.Status.HandleAsync(byOperator, AccountStatus.SelfExcluded, "phoned in", "ops-1", CancellationToken.None);
        var consumer = new RestrictionsChangedConsumer(db.Status, db.Time);

        await consumer.HandleAsync(Snapshot(byCompliance, [new Restriction(RestrictionKind.CoolingOff, now, now.AddDays(7), "r")], now), CancellationToken.None);
        (await db.Users.FindByIdAsync(byCompliance, CancellationToken.None))!.Status.ShouldBe(AccountStatus.SelfExcluded);

        await consumer.HandleAsync(Snapshot(byCompliance, [], now), CancellationToken.None);
        await consumer.HandleAsync(Snapshot(byOperator, [], now), CancellationToken.None);

        (await db.Users.FindByIdAsync(byCompliance, CancellationToken.None))!.Status.ShouldBe(AccountStatus.Active);
        (await db.Users.FindByIdAsync(byOperator, CancellationToken.None))!.Status.ShouldBe(AccountStatus.SelfExcluded);
        (await db.Sessions.PasswordAsync("c@example.com", "a long passphrase", CancellationToken.None)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Exclusion_for_an_unknown_account_is_skipped()
    {
        var db = await IdentityDatabase.CreateAsync(sql);
        var consumer = new SelfExclusionStartedConsumer(db.Status, NullLogger<SelfExclusionStartedConsumer>.Instance);

        await Should.NotThrowAsync(() => consumer.HandleAsync(Message(Guid.NewGuid(), RestrictionKind.CoolingOff, db.Time.GetUtcNow()), CancellationToken.None));
    }

    private static async Task<Guid> RegisterAsync(IdentityDatabase db, string email)
    {
        (await db.Register.HandleAsync(new RegisterCommand(email, "a long passphrase", new DateOnly(1990, 1, 1), "ZA", "ZAR"), CancellationToken.None)).IsSuccess.ShouldBeTrue();
        return (await db.Users.FindByLoginAsync(email, CancellationToken.None))!.UserId;
    }

    private static ConsumedEvent<RestrictionsChangedV1> Snapshot(Guid userId, IReadOnlyList<Restriction> restrictions, DateTimeOffset at) =>
        new(EventEnvelope<RestrictionsChangedV1>.Create(new RestrictionsChangedV1(userId, 2, [], restrictions, null, null, KycStatus.NotStarted, at), at, "corr-2"),
            Topics.RestrictionsChanged, 0, 0, new Dictionary<string, string>());

    private static ConsumedEvent<SelfExclusionStartedV1> Message(Guid userId, RestrictionKind kind, DateTimeOffset at) =>
        new(EventEnvelope<SelfExclusionStartedV1>.Create(new SelfExclusionStartedV1(userId, kind, at, at.AddMonths(6), "customer request", "self"), at, "corr-1"),
            Topics.SelfExclusionStarted, 0, 0, new Dictionary<string, string>());
}
