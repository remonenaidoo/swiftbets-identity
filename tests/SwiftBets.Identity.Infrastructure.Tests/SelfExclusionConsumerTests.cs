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
    public async Task Exclusion_for_an_unknown_account_is_skipped()
    {
        var db = await IdentityDatabase.CreateAsync(sql);
        var consumer = new SelfExclusionStartedConsumer(db.Status, NullLogger<SelfExclusionStartedConsumer>.Instance);

        await Should.NotThrowAsync(() => consumer.HandleAsync(Message(Guid.NewGuid(), RestrictionKind.CoolingOff, db.Time.GetUtcNow()), CancellationToken.None));
    }

    private static ConsumedEvent<SelfExclusionStartedV1> Message(Guid userId, RestrictionKind kind, DateTimeOffset at) =>
        new(EventEnvelope<SelfExclusionStartedV1>.Create(new SelfExclusionStartedV1(userId, kind, at, at.AddMonths(6), "customer request", "self"), at, "corr-1"),
            Topics.SelfExclusionStarted, 0, 0, new Dictionary<string, string>());
}
