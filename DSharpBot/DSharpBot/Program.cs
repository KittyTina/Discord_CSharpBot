using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json.Linq;

public class Program
{
    public static Task Main(string[] args) => new Program().MainAsync();
    
    private DiscordSocketClient _client;
    private IConfigurationRoot _config;

    public async Task MainAsync()
    {
        _config = new ConfigurationBuilder()
        .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "json/config.json"))
        .Build();

        _client = new DiscordSocketClient();
        
        _client.Log += Log;
        _client.Ready += Client_Ready;
        _client.SlashCommandExecuted += SlashCommandHandler;

        await _client.LoginAsync(TokenType.Bot, _config["BOT_TOKEN"]);
        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        switch (command.Data.Name)
        {
            case "ping":
                await command.RespondAsync("Pong!");
                break;
            case "hey":
                await command.RespondAsync("hey! :)");
                break;
        }
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

        guild_cmd.WithName("hey");
        guild_cmd.WithDescription("Answers with hey :)");
        

        try
        {
            await guild.CreateApplicationCommandAsync(guild_cmd.Build());
        }catch(ApplicationCommandException ex)
        {
            Console.WriteLine(JsonConvert.SerializeObject(ex.Errors, Formatting.Indented));
        }
    }

    public async Task GetStockDataAsync(string symbol)
    {
        string api_key = _config["API_KEY"];
        string url = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY_ADJUSTED&symbol={symbol}&apikey={api_key}";

        using (var client = new HttpClient())
        {
            HttpResponseMessage responseMessage = await client.GetAsync(url);
            if (responseMessage.IsSuccessStatusCode)
            {
                string responseContent = await responseMessage.Content.ReadAsStringAsync();
                JObject responseData = JObject.Parse(responseContent);
                JToken timeSeriesDay = responseData["Time Series (Daily)"];
                foreach (KeyValuePair<string, JToken> kvp in timeSeriesDay)
                {
                    string date = kvp.Key;
                    decimal closestPrice = (decimal)kvp.Value["4. close"];
                    Console.WriteLine($"{date} : {closestPrice}");
                }
            }
        }
    }
}