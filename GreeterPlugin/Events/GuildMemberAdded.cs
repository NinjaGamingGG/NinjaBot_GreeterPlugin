using Dapper;
using Dapper.Contrib.Extensions;
using DSharpPlus;
using DSharpPlus.EventArgs;
using GreeterPlugin.DatabaseRecords;
using GreeterPlugin.PluginHelpers;
using MySqlConnector;
using Serilog;

namespace GreeterPlugin.Events;

public static class GuildMemberAdded
{
    
    public static async Task GuildMemberAddedEvent(DiscordClient client, GuildMemberAddEventArgs args)
    {
        GuildSettingsRecord? guildSettingsRecord;
        UserJoinedDataRecord? userJoinedDataRecord;
        var connectionString = GreeterPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        try
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            guildSettingsRecord = await connection.QueryFirstOrDefaultAsync<GuildSettingsRecord>(
                "SELECT * FROM GuildSettingsIndex WHERE GuildId = @GuildId", new { GuildId = args.Guild.Id });

            if (guildSettingsRecord == null)
                return;

            userJoinedDataRecord = await connection.QueryFirstOrDefaultAsync<UserJoinedDataRecord>(
                "SELECT * FROM UserJoinedDataIndex WHERE GuildId = @GuildId AND UserId = @UserId",
                new { GuildId = args.Guild.Id, UserId = args.Member.Id });
            await connection.CloseAsync();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex,"Error while Reading Database Records");
            return;
        }


        if (userJoinedDataRecord == null)
        {
            try
            {
                var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                
                var highest = await connection.QueryAsync<int>(
                    "SELECT MAX(UserIndex) FROM UserJoinedDataIndex WHERE GuildId = @GuildId",
                    new { GuildId = args.Guild.Id });

                var highestIndex = highest.FirstOrDefault();

                userJoinedDataRecord = new UserJoinedDataRecord()
                {
                    GuildId = args.Guild.Id,
                    UserId = args.Member.Id,
                    UserIndex = highestIndex + 1,
                    WasGreeted = false
                };

                await connection.InsertAsync(userJoinedDataRecord);
                await connection.CloseAsync();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex,"Error while Reading or Updating Database Records");
                return;
            }
            
        }
            
        var welcomeChannel = args.Guild.GetChannel(guildSettingsRecord.WelcomeChannelId);

        await GenerateWelcomeMessageWithImage.Generate(client, args.Member, guildSettingsRecord, userJoinedDataRecord, welcomeChannel, args.Guild);
    }
}