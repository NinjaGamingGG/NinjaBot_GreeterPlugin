using Dapper;
using DSharpPlus;
using DSharpPlus.Entities;
using GreeterPlugin.DatabaseRecords;
using MySqlConnector;
using Serilog;

namespace GreeterPlugin.PluginHelpers;

public static class StartupIndex 
{
    public static async Task StartupTask(DiscordClient client)
    {
        var connectionString = GreeterPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        List<GuildSettingsRecord> guildSettingsRecords;
        try
        {
            var mysqlConnection = new MySqlConnection(connectionString);
            var guildConfigRecords = await mysqlConnection.QueryAsync<GuildSettingsRecord>("SELECT * FROM GuildSettingsIndex");
            guildSettingsRecords = guildConfigRecords.ToList();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex,"Error wile reading Greeter Plugin Guild Settings from Database");
            return;
        }


        foreach (var guildConfig in guildSettingsRecords)
        {
            var guild = await client.GetGuildAsync(guildConfig.GuildId);
            await IndexAllGuildMembers(guild);
        }
    }

    private static async Task IndexAllGuildMembers(DiscordGuild guild)
    {
        var guildMembers = guild.GetAllMembersAsync();

        var guildMembersAsList = new List<DiscordMember>();

        await foreach (var member in guildMembers)
        {
            guildMembersAsList.Add(member);
        }
        
        var guildMembersSorted = guildMembersAsList.OrderBy(x => x.JoinedAt);
        
        var currentGuildMemberIndex = 1;
        

        var connectionString = GreeterPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        
        foreach (var guildMember in guildMembersSorted)
        {
            int recordExists;
            try
            {
                await using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                recordExists = await connection.ExecuteScalarAsync<int>("SELECT * FROM UserJoinedDataIndex WHERE GuildId = @GuildId AND UserId = @UserId", new {GuildId = guild.Id, UserId = guildMember.Id});
                await connection.CloseAsync();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex,"Unable to read UserJoinedData Record on Startup Index of GreeterPlugin");
                continue;
            }
           

            if (recordExists != 0)
            {
                currentGuildMemberIndex++;
                continue;
            }
            
            var userJoinedDataRecord = new UserJoinedDataRecord()
            {
                UserId = guildMember.Id,
                GuildId = guild.Id,
                UserIndex = currentGuildMemberIndex,
                WasGreeted = false
            };
            

            try
            {
                await using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                await connection.ExecuteAsync("INSERT INTO UserJoinedDataIndex (UserId, GuildId, UserIndex, WasGreeted) VALUES (@UserId, @GuildId, @UserIndex, @WasGreeted)", userJoinedDataRecord);
                await connection.CloseAsync();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex,"Unable to insert new UserJoinedData Record on Startup Index of GreeterPlugin");
                continue;
            }
            
            
            currentGuildMemberIndex++;
                
        }
    }
}