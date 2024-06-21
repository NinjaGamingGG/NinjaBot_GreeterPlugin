using GreeterPlugin.CommandsModules;
using GreeterPlugin.Events;
using GreeterPlugin.PluginHelpers;
using NinjaBot_DC;
using CommonPluginHelpers;
using MySqlConnector;
using PluginBase;
using Serilog;

namespace GreeterPlugin;

public class GreeterPlugin : DefaultPlugin
{
    public static string StaticPluginDirectory = string.Empty;

    public static MySqlConnectionHelper MySqlConnectionHelper { get; private set; } = null!;

    
    public override void OnLoad()
    {
        var client = Worker.GetServiceDiscordClient();
        
        client.GuildMemberAdded += GuildMemberAdded.GuildMemberAddedEvent;

        if (ReferenceEquals(PluginDirectory, null))
        {
            OnUnload();
            return;
        }
        StaticPluginDirectory = PluginDirectory;
        Directory.CreateDirectory(Path.Combine(PluginDirectory, "temp"));

        var config = Worker.LoadAssemblyConfig(Path.Combine(PluginDirectory,"config.json"), GetType().Assembly, EnvironmentVariablePrefix);

        var tableStrings = new[]
        {
            "CREATE TABLE IF NOT EXISTS GuildSettingsIndex (GuildId BIGINT PRIMARY KEY, WelcomeChannelId BIGINT, WelcomeMessage TEXT, WelcomeImageUrl TEXT, WelcomeImageText TEXT, ProfilePictureOffsetX double, ProfilePictureOffsetY double)",
            "CREATE TABLE IF NOT EXISTS UserJoinedDataIndex (EntryId int NOT NULL AUTO_INCREMENT, GuildId BIGINT, UserId BIGINT, UserIndex INT, WasGreeted BOOL, PRIMARY KEY (EntryId))"
        };

        MySqlConnectionHelper = new MySqlConnectionHelper(EnvironmentVariablePrefix, config, Name);

        try
        {
            var connectionString = MySqlConnectionHelper.GetMySqlConnectionString();
            var connection = new MySqlConnection(connectionString);
            connection.Open();
            
            MySqlConnectionHelper.InitializeTables(tableStrings, connection);
            connection.Close();
        }
        catch (Exception)
        {
            Log.Fatal("Canceling the Startup of {PluginName} Plugin!", Name);
            return;
        }
        

        var slashCommands = Worker.GetServiceSlashCommandsExtension();
        slashCommands.RegisterCommands<SlashCommandModule>();

        Task.Run(async () => await StartupIndex.StartupTask(client));

        Log.Debug("[Greeter Plugin] Init Finished");
    }

    public override void OnUnload()
    {

    }
}