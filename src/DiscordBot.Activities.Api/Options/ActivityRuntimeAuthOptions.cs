namespace DiscordBot.Activities.Api.Options;

public sealed class ActivityRuntimeAuthOptions
{
    public const string SectionName = "ActivitiesAuth";
    public bool AllowMissingActivityInstanceInDevelopment { get; set; }
}
