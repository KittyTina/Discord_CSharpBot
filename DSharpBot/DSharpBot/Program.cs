using Discord;
using Discord.Net;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.VisualBasic;
using System.Runtime.InteropServices;
using System.Text;

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
            case "clearchat":
                if (!((SocketGuildUser)command.User).GuildPermissions.Administrator)
                {
                    await command.Channel.SendMessageAsync(null, false, CreateEmbed("Insufficient Permissions", "You don't have permissions to use this command!", Color.Red));
                    return;
                }
                double count = (double)command.Data.Options.First().Value;
                int counter = ((int)count);

                if (count <= 0)
                {
                    await command.Channel.SendMessageAsync(null, false, CreateEmbed("Logic Error", $"How am I supposed to remove {count} messages you donkey?????!!!", Color.Red));
                    return;
                }
                try 
                { 
                    await ClearChatAsync(command, counter);
                }catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                break;
            case "feedback":
                int rating = int.Parse(command.Data.Options.First(x => x.Name == "rating").Value.ToString());

                // Create a new paste with the rating data
                string pastebinApiKey = "YOUR_API_KEY_HERE";
                string pastebinApiUrl = "https://pastebin.com/api_post.php";
                string pasteData = $"Rating: {rating}";
                string postParams = $"api_option=paste&api_dev_key={pastebinApiKey}&api_paste_code={Uri.EscapeDataString(pasteData)}";
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.PostAsync(pastebinApiUrl, new StringContent(postParams, Encoding.UTF8, "application/x-www-form-urlencoded"));
                    if (response.IsSuccessStatusCode)
                    {
                        var responseText = await response.Content.ReadAsStringAsync();
                        var pasteUrl = responseText.Trim();
                        await command.RespondAsync($"Thank you for your feedback! Your rating of {rating} has been saved anonymously to {pasteUrl}");
                    }
                    else
                    {
                        await command.RespondAsync("Sorry, there was an error creating the paste. Please try again later.");
                    }
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
        var rmvcmd = new SlashCommandBuilder();

        guild_cmd.WithName("ping").WithDescription("Answers with pong");

        guild_cmd.WithName("hey").WithDescription("Answers with hey :)");

        guild_cmd.WithName("stock").WithDescription("Returns a stock price").AddOption("symbol", ApplicationCommandOptionType.String, "e.g. TSLA or AMZN", true);
        guild_cmd.WithName("feedback").WithDescription("Give the bot a feedback. This gets saved in a pastebin but anonymous!")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("rating")
                .WithDescription("1-5 that fits you how you would rate this bot.")
                .WithRequired(true)
                .AddChoice("Horrible", 1)
                .AddChoice("Yeah ... no, thank you", 2)
                .AddChoice("It's ok!", 3)
                .AddChoice("Pretty Good :)", 4)
                .AddChoice("Awesome! :D", 5)
                .WithType(ApplicationCommandOptionType.Integer)
                );

        rmvcmd.WithName("clearchat").WithDescription("Clears the chat with the according number").AddOption("count", ApplicationCommandOptionType.Number, "e.g 3", true)
            .WithDefaultPermission(false);

        try
        {
            await guild.CreateApplicationCommandAsync(guild_cmd.Build());
            await guild.CreateApplicationCommandAsync(rmvcmd.Build());
        }
        catch(HttpException ex)
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

    public async Task ClearChatAsync(SocketSlashCommand cmdctx, int count)
    {
        var channel = cmdctx.Channel as SocketTextChannel;
        var messages = await channel.GetMessagesAsync(count + 1).FlattenAsync(); // +1 for last message that the bot sends
        await channel.DeleteMessagesAsync(messages);
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