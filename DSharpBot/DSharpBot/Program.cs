using Discord;
using Discord.WebSocket;

public class Program
{
    public static Task Main(string[] args) => new Program().MainAsync();
    private DiscordSocketClient _client;
    public async Task MainAsync()
    {
        _client = new DiscordSocketClient();
        _client.Log += Log;
        await _client.LoginAsync(TokenType.Bot, "token");
        await _client.StartAsync();
        
    }
    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
}