using DiscordBot.Domain.Enums;

namespace DiscordBot.Domain.Entities;

public class GuildPanelButton : BaseEntity
{
    public Guid PanelId { get; set; }
    public GuildPanel Panel { get; set; } = null!;
    public string Label { get; set; } = string.Empty;
    public string? Emoji { get; set; }
    public PanelButtonStyle Style { get; set; } = PanelButtonStyle.Secondary;
    public PanelButtonActionType ActionType { get; set; } = PanelButtonActionType.CreateTicket;
    public Guid? TicketTypeId { get; set; }
    public string? Url { get; set; }
    public string? ResponseMessage { get; set; }
    public string? RoleDiscordId { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;
}
