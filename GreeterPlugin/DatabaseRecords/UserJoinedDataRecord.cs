using Dapper.Contrib.Extensions;

namespace GreeterPlugin.DatabaseRecords;

[Table("UserJoinedDataIndex")]
public class UserJoinedDataRecord
{
    [ExplicitKey]
    public ulong EntryId { get; set; }
    
    public ulong UserId { get; set; }
    
    public ulong GuildId { get; set; }
    
    public int UserIndex { get; set; }
    
    public bool WasGreeted { get; set; }
    
}