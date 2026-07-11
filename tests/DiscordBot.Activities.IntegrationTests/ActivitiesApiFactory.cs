using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace DiscordBot.Activities.IntegrationTests;

public class ActivitiesApiFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "integration-test-signing-key-with-32-chars";

    public ActivitiesApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__ActivitiesDatabase", "Host=localhost;Port=5432;Database=discordbot_activities_tests;Username=postgres;Password=postgres");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "DiscordBot.Activities.Tests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "DiscordBot.Activity.Tests");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", SigningKey);
        Environment.SetEnvironmentVariable("Jwt__AccessTokenMinutes", "15");
        Environment.SetEnvironmentVariable("PlatformApi__BaseUrl", "http://localhost/");
        Environment.SetEnvironmentVariable("PlatformApi__ServiceToken", "test-service-token");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ActivitiesDatabase"] = "Host=localhost;Port=5432;Database=discordbot_activities_tests;Username=postgres;Password=postgres",
                ["Jwt:Issuer"] = "DiscordBot.Activities.Tests",
                ["Jwt:Audience"] = "DiscordBot.Activity.Tests",
                ["Jwt:SigningKey"] = SigningKey,
                ["Jwt:AccessTokenMinutes"] = "15",
                ["PlatformApi:BaseUrl"] = "http://localhost/",
                ["PlatformApi:ServiceToken"] = "test-service-token"
            });
        });

        return base.CreateHost(builder);
    }
}
