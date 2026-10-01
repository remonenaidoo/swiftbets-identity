using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SwiftBets.BuildingBlocks.Core;
using SwiftBets.BuildingBlocks.Messaging;
using SwiftBets.BuildingBlocks.Outbox;
using SwiftBets.BuildingBlocks.Persistence;
using SwiftBets.Identity.Application;
using SwiftBets.Identity.Application.Ports;
using SwiftBets.Identity.Infrastructure.Messaging;
using SwiftBets.Identity.Infrastructure.Persistence;
using SwiftBets.Identity.Infrastructure.Security;

namespace SwiftBets.Identity.Infrastructure;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSqlServerPersistence(Required(configuration, "ConnectionStrings:SbIdentity"));
        services.AddValidatedOptions<IdentityOptions>(configuration, IdentityOptions.SectionName);
        services.AddValidatedOptions<AccountEmailOptions>(configuration, AccountEmailOptions.SectionName);
        services.AddSingleton<IUserStore, SqlUserStore>();
        services.AddSingleton<ITokenStore, SqlTokenStore>();
        services.AddSingleton<RsaTokenIssuer>();
        services.AddSingleton<ITokenIssuer>(sp => sp.GetRequiredService<RsaTokenIssuer>());
        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
        if (configuration.GetValue<AccountEmailDelivery>($"{AccountEmailOptions.SectionName}:Delivery") == AccountEmailDelivery.Notifications)
        {
            services.AddSingleton<IAccountMessenger, NotificationRequestMessenger>();
        }
        else
        {
            services.AddSingleton<IAccountMessenger, SmtpAccountMessenger>();
        }

        // Identity's events and notification requests leave through the outbox; the relay runs in every replica.
        services.AddKafkaMessaging(configuration);
        services.AddSqlServerOutbox(configuration, runRelay: configuration.GetValue("Outbox:RunRelay", true));
        services.AddSingleton(TimeProvider.System);
        services.AddHostedService<DemoUserSeeder>();
        return services;
    }

    private static string Required(IConfiguration configuration, string key) =>
        configuration[key] is { Length: > 0 } value ? value : throw new InvalidOperationException($"Configuration '{key}' is required.");
}
