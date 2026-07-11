using System.Diagnostics;
using Xunit;

namespace DiscordBot.Activities.IntegrationTests.PostgreSql;

public sealed class DockerFactAttribute : FactAttribute
{
    public DockerFactAttribute()
    {
        if (!DockerAvailable())
            Skip = "Docker daemon unavailable; PostgreSQL Testcontainers tests require Docker.";
    }

    private static bool DockerAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("docker", "info")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            if (process is null) return false;
            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
