using System.Runtime.InteropServices;
using Dapper;
using DSharpPlus;
using DSharpPlus.Entities;
using GreeterPlugin.DatabaseRecords;
using MySqlConnector;
using Serilog;

namespace GreeterPlugin.PluginHelpers;

public static class GenerateWelcomeMessageWithImage
{
    public static async Task Generate(DiscordClient client, DiscordMember member,
        GuildSettingsRecord guildSettingsRecord, UserJoinedDataRecord userJoinedDataRecord, DiscordChannel welcomeChannel, DiscordGuild guild)
    {
        var welcomeCard = Path.Combine(GreeterPlugin.StaticPluginDirectory, "temp", $"welcomeCard{member.Id}.png");

        await GenerateWelcomeImage.Generator(member.Username,
            member.AvatarUrl,
            guildSettingsRecord.WelcomeImageText,
            userJoinedDataRecord.UserIndex,
            guildSettingsRecord.WelcomeImageUrl,
            true,
            guildSettingsRecord.ProfilePictureOffsetX,
            guildSettingsRecord.ProfilePictureOffsetY,
            welcomeCard, 300);

        var messageBuilder = new DiscordMessageBuilder();
        
        if (guildSettingsRecord.WelcomeMessage.Contains("{usermention}"))
        {
            guildSettingsRecord.WelcomeMessage =
                guildSettingsRecord.WelcomeMessage.Replace("{usermention}", member.Mention);
        }
        
        messageBuilder.WithContent(guildSettingsRecord.WelcomeMessage);

        var filestream = File.Open(welcomeCard, FileMode.Open);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            await client.SendMessageAsync(welcomeChannel, messageBuilder);

            var connectionString = GreeterPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
            try
            {
                var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                await connection.ExecuteAsync(
                    "UPDATE UserJoinedDataIndex SET WasGreeted = @WasGreeted WHERE GuildId = @GuildId AND UserId = @UserId",
                    new { WasGreeted = true, GuildId = guild.Id, UserId = member.Id });
                await connection.CloseAsync();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex, "Database Error while Updating User Joined Data Index for {UserId} on {GuildId}", member.Id, guild.Id);
            }
            return;
        }
        
        messageBuilder.AddFile(filestream);

        await client.SendMessageAsync(welcomeChannel, messageBuilder);

        filestream.Close();
        await filestream.DisposeAsync();

        if (!IsFileLocked.Check(welcomeCard, 10))
        {
            File.Delete(welcomeCard);
        }
        else
        {
            Log.Error("[Greeter Plugin] Failed to delete welcome card, file appears to be locked! Filepath: {FilePath}",
                welcomeCard);
        }


        
        try
        {
            var connectionString = GreeterPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
            var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            await connection.ExecuteAsync(
                "UPDATE UserJoinedDataIndex SET WasGreeted = @WasGreeted WHERE GuildId = @GuildId AND UserId = @UserId",
                new { WasGreeted = true, GuildId = guild.Id, UserId = member.Id });
            await connection.CloseAsync();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex, "Database Error while Updating User Joined Data Index for {UserId} on {GuildId}", member.Id, guild.Id);
        }
    }
}