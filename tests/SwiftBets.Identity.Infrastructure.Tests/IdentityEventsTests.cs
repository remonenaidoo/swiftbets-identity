using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SwiftBets.BuildingBlocks.Messaging;
using SwiftBets.BuildingBlocks.Outbox;
using SwiftBets.BuildingBlocks.Persistence;
using SwiftBets.BuildingBlocks.Testing;
using SwiftBets.Identity.Application.Accounts;
using SwiftBets.Identity.Domain;
using SwiftBets.Identity.Infrastructure.Messaging;

namespace SwiftBets.Identity.Infrastructure.Tests;

public sealed class IdentityEventsTests(SqlServerFixture sql)
{
    [Fact]
    public async Task Each_account_change_commits_with_its_event()
    {
        var db = await IdentityDatabase.CreateAsync(sql);
        await db.Register.HandleAsync(new RegisterCommand("events@example.com", "a long passphrase", new DateOnly(1990, 1, 1), "ZA", "ZAR"), CancellationToken.None);
        var user = (await db.Users.FindByLoginAsync("events@example.com", CancellationToken.None))!;
        await db.Users.MarkEmailVerifiedAsync(user.UserId, db.Time.GetUtcNow());
        await db.Users.MarkEmailVerifiedAsync(user.UserId, db.Time.GetUtcNow());

        (await db.Status.HandleAsync(user.UserId, AccountStatus.Suspended, "chargeback review", "operator-1", CancellationToken.None)).IsSuccess.ShouldBeTrue();

        var rows = await OutboxAsync(db.ConnectionString);
        rows.Select(r => r.EventType).ShouldBe(
            ["identity.user-registered", "identity.email-verified", "identity.account-status-changed", "identity.session-revoked"]);
        rows.ShouldAllBe(r => r.MessageKey == user.UserId.ToString());
        rows.ShouldAllBe(r => r.Topic.StartsWith("swiftbets.identity.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_refused_status_change_publishes_nothing()
    {
        var db = await IdentityDatabase.CreateAsync(sql);
        await db.Register.HandleAsync(new RegisterCommand("noop@example.com", "a long passphrase", new DateOnly(1990, 1, 1), "ZA", "ZAR"), CancellationToken.None);
        var user = (await db.Users.FindByLoginAsync("noop@example.com", CancellationToken.None))!;

        (await db.Users.ChangeStatusAsync(user.UserId, AccountStatus.Suspended, AccountStatus.Closed, "stale form", "operator-1", db.Time.GetUtcNow())).ShouldBeFalse();

        (await OutboxAsync(db.ConnectionString)).Select(r => r.EventType).ShouldBe(["identity.user-registered"]);
    }

    [Fact]
    public async Task Account_email_can_be_handed_to_notifications_through_the_outbox()
    {
        var db = await IdentityDatabase.CreateAsync(sql);
        var options = Options.Create(new AccountEmailOptions { PublicBaseUrl = "https://swiftbets.test/", Delivery = AccountEmailDelivery.Notifications });
        var outbox = new SqlServerOutbox(Options.Create(new KafkaOptions { BootstrapServers = "unused:9092", Environment = "test", ClientId = "identity-tests" }), db.Time);
        var messenger = new NotificationRequestMessenger(new SqlServerConnectionFactory(db.ConnectionString), outbox, options, db.Time);
        var userId = Guid.NewGuid();

        await messenger.SendPasswordResetAsync(userId, "reset@example.com", "raw/token", CancellationToken.None);

        var row = (await OutboxAsync(db.ConnectionString)).Single();
        row.EventType.ShouldBe("notifications.notification-requested");
        row.Topic.ShouldBe("swiftbets.notifications.notification-requested.v1.test");
        using var payload = JsonDocument.Parse(row.Payload);
        var data = payload.RootElement.GetProperty("payload");
        data.GetProperty("template").GetString().ShouldBe("account.reset-password");
        data.GetProperty("recipient").GetString().ShouldBe("reset@example.com");
        data.GetProperty("data").GetProperty("link").GetString().ShouldBe("https://swiftbets.test/account/reset-password?token=raw%2Ftoken");
    }

    private sealed record OutboxRow(string Topic, string MessageKey, string EventType, byte[] Payload);

    private static async Task<List<OutboxRow>> OutboxAsync(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        return [.. await connection.QueryAsync<OutboxRow>("SELECT Topic, MessageKey, EventType, Payload FROM outbox.Messages ORDER BY Sequence")];
    }
}
