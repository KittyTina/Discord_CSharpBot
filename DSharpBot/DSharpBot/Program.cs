using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

public class Program
{
    public static Task Main(string[] args) => new Program().MainAsync();
    
    private DiscordSocketClient _client;
    private IConfigurationRoot _config;
    public async Task MainAsync()
    {

        _config = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("config.json")
        .Build();

        _client = new DiscordSocketClient();
        _client.Log += Log;
        _client.Ready += Client_Ready;
        await _client.LoginAsync(TokenType.Bot, _config["BOT_TOKEN"]);
        await _client.StartAsync();

        await Task.Delay(-1);
    }
    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
    public async Task Client_Ready()
    {

        var guild = _client.GetGuild(ulong.Parse(_config["GUILD_ID"]));
        var guild_cmd = new SlashCommandBuilder();

        guild_cmd.WithName("ping");
        guild_cmd.WithDescription("Answers with pong");

        var global_cmd = new SlashCommandBuilder();
        global_cmd.WithName("global-ping");
        global_cmd.WithDescription("Answers with global-pong");

        try
        {
            await guild.CreateApplicationCommandAsync(guild_cmd.Build());
            await guild.CreateApplicationCommandAsync(global_cmd.Build());
        }catch(ApplicationCommandException ex)
        {
            Console.WriteLine(JsonConvert.SerializeObject(ex.Errors, Formatting.Indented));
        }

    }
}