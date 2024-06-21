using Dapper;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using GreeterPlugin.DatabaseRecords;
using GreeterPlugin.PluginHelpers;
using MySqlConnector;
using Serilog;

namespace GreeterPlugin.CommandsModules;

[SlashCommandGroup("greeter", "Greeter Plugin Commands")]
// ReSharper disable once ClassNeverInstantiated.Global
public class SlashCommandModule : ApplicationCommandModule
{
    [SlashCommandGroup("config", "Greeter Plugin Config Commands")]
    public class ConfigSubGroup : ApplicationCommandModule
    {
        [SlashCommand("add", "Add a new config entry")]
        public async Task AddConfigCommand(InteractionContext context, [Option("WelcomeChannel", "Your Welcome Channel")] DiscordChannel channel, [Option("WelcomeMessage", "Your Welcome Message")] string message, [Option("WelcomeImageUrl", "Your Welcome Image Url")] string imageUrl, [Option("WelcomeImageText", "Your Welcome Image Text")] string imageText, [Option("ProfilePictureOffsetX", "Your Profile Picture Offset X")] double offsetX, [Option("ProfilePictureOffsetY", "Your Profile Picture Offset Y")] double offsetY)
        {
            var guildId = context.Guild.Id;
            var welcomeChannelId = channel.Id;

            var guildSettingsRecord = new GuildSettingsRecord()
            {
                GuildId = guildId,
                WelcomeChannelId = welcomeChannelId,
                WelcomeMessage = message,
                WelcomeImageUrl = imageUrl,
                WelcomeImageText = imageText,
                ProfilePictureOffsetX = offsetX,
                ProfilePictureOffsetY = offsetY
            };

            int inserted;
            var connectionString = GreeterPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
            
            try
            {
                await using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
            
                inserted = await connection.ExecuteAsync("INSERT INTO GuildSettingsIndex (GuildId, WelcomeChannelId, WelcomeMessage, WelcomeImageUrl, WelcomeImageText, ProfilePictureOffsetX, ProfilePictureOffsetY) VALUES (@GuildId, @WelcomeChannelId, @WelcomeMessage, @WelcomeImageUrl, @WelcomeImageText, @ProfilePictureOffsetX, @ProfilePictureOffsetY)", guildSettingsRecord);
                await connection.CloseAsync();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex,"Unable to Insert new Config into to Database on Guild {GuildId}", guildId);
                throw;
            }
            

            if (inserted == 0)
            {
                await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Failed to add config!"));
                return;
            }
            
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Config added!"));
        }
        
        
    }
    
    [SlashCommandGroup("debug", "asd")]
    public class DebugSubGroup : ApplicationCommandModule
    {
        [SlashCommand("generate", "asd")]
        public async Task GenerateCommand(InteractionContext context, [Option("User", "asd")] DiscordUser user)
        {
            await context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Generating Image..."));

            GuildSettingsRecord? guildSettingsRecord;
            UserJoinedDataRecord? userJoinedDataRecord;
            try
            {
                await using var connection = new MySqlConnection();
            
                guildSettingsRecord = await connection.QueryFirstOrDefaultAsync<GuildSettingsRecord>("SELECT * FROM GuildSettingsIndex WHERE GuildId = @GuildId", new {GuildId = context.Guild.Id});
            
                if (guildSettingsRecord == null)
                {
                    return;
                }
            
                userJoinedDataRecord = await connection.QueryFirstOrDefaultAsync<UserJoinedDataRecord>("SELECT * FROM UserJoinedDataIndex WHERE GuildId = @GuildId AND UserId = @UserId", new {GuildId = context.Guild.Id, UserId = context.Member.Id});
                await connection.CloseAsync();
            }
            catch (MySqlException ex)
            {
                Log.Error(ex, "Error while querying guild setting or user data record from database on Greeter Plugin Debug command");
                return;
            }


        
            if (userJoinedDataRecord == null)
            {
                return;
            }
            
            var welcomeChannel = context.Guild.GetChannel(guildSettingsRecord.WelcomeChannelId);

            await GenerateWelcomeMessageWithImage.Generate(context.Client, context.Member, guildSettingsRecord, userJoinedDataRecord, welcomeChannel, context.Guild);
            
            await context.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Operation Complete"));
        }
    }

    
}