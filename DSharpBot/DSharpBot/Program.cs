using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Reflection.Metadata.Ecma335;

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
            case "stock":
                string symbol = command.Data.Options.FirstOrDefault(o => o.Name == "symbol").Value.ToString();
                if(string.IsNullOrEmpty(symbol))
                {
                    await command.RespondAsync(null, null, false, false, null, null, CreateEmbed("Invalid Symbol!", "Please provide me a valid stock symbol.", Color.Red));
                    return;
                }
                Embed stockEmbed = await GetStockDataAsync(symbol);
                if (stockEmbed != null)
                {
                    await command.RespondAsync(embeds: new[] { stockEmbed });
                }
                else
                {
                    await command.RespondAsync("Error: Unable to retrieve stock data.");
                }
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

        guild_cmd.WithName("ping").WithDescription("Answers with pong");

        guild_cmd.WithName("hey").WithDescription("Answers with hey :)");

        guild_cmd.WithName("stock").WithDescription("Returns a stock price").AddOption("symbol", ApplicationCommandOptionType.String, "e.g. TSLA or AMZN", true);

        try
        {
            await guild.CreateApplicationCommandAsync(guild_cmd.Build());
        }catch(ApplicationCommandException ex)
        {
            Console.WriteLine(JsonConvert.SerializeObject(ex.Errors, Formatting.Indented));
        }
    }

    public async Task<Embed> GetStockDataAsync(string symbol)
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
                //JProperty latestPriceProperty = timeSeriesDay.FirstOrDefault().ToObject<JObject>().Properties().FirstOrDefault(property => property.Name.EndsWith("close"));
                if (timeSeriesDay is JObject timeSeriesObj)
                {
                    // Handle JObject case
                    JProperty latestPriceProperty = timeSeriesObj.Properties().FirstOrDefault(property => property.Name.EndsWith("close"));
                    if (latestPriceProperty != null)
                    {
                        decimal latestPrice = (decimal)latestPriceProperty.Value;
                        string title = $"Stock data for {symbol}";
                        string description = $"Latest closing price: {latestPrice:C}";
                        return CreateEmbed(title, description, Color.Green);
                    }
                }
                else if (timeSeriesDay is JProperty timeSeriesProp && timeSeriesProp.Value is JObject timeSeriesPropObj)
                {
                    // Handle JProperty case
                    JProperty latestPriceProperty = timeSeriesPropObj.Properties().FirstOrDefault(property => property.Name.EndsWith("close"));
                    if (latestPriceProperty != null)
                    {
                        decimal latestPrice = (decimal)latestPriceProperty.Value;
                        string title = $"Stock data for {symbol}";
                        string description = $"Latest closing price: {latestPrice:C}";
                        return CreateEmbed(title, description, Color.Green);
                    }
                }
            }
        }
        return null;
    }
    private Embed CreateEmbed(string title, string description, Color color)
    {
        var builder = new EmbedBuilder();
        builder
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(color);
        return builder.Build();
    }
}