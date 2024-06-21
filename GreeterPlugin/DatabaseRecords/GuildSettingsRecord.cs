using Dapper.Contrib.Extensions;

namespace GreeterPlugin.DatabaseRecords;

[Table("GuildSettingsIndex")]
public record GuildSettingsRecord
{
    [ExplicitKey]
    public ulong GuildId { get; set; }
    
    public ulong WelcomeChannelId { get; set; }
    
    public string WelcomeMessage { get; set; } = string.Empty;
    
    public string WelcomeImageUrl { get; set; } = string.Empty;
    
    public string WelcomeImageText { get; set; } = string.Empty;
    
    public double ProfilePictureOffsetX { get; set; }
    
    public double ProfilePictureOffsetY { get; set; }
}