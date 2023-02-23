using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

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
        await _client.LoginAsync(TokenType.Bot, _config["BOT_TOKEN"]);
        await _client.StartAsync();

        await Task.Delay(-1);
    }
    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
}