using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Newtonsoft.Json;
using Weather_Discord_Bot.Bot.Weather_Data;

namespace Weather_Discord_Bot.Bot
{
    class Weather_Discord_Bot
    {
        private DiscordSocketClient _client;
        private readonly HttpClient _httpClient = new HttpClient();

        private Tokens _tokens;

        public static Task Main(string[] args) => new Weather_Discord_Bot().MainAsync();

        public async Task MainAsync()
        {
            _tokens = JsonConvert.DeserializeObject<Tokens>(File.ReadAllText("config.json"));

            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds |
                                 GatewayIntents.GuildMessages |
                                 GatewayIntents.MessageContent |
                                 GatewayIntents.GuildMessageReactions
            };

            _client = new DiscordSocketClient(config);

            _client.Log += Log;
            _client.Ready += ReadyAsync;
            _client.SlashCommandExecuted += SlashCommandHandler;

            var token = _tokens.DiscordBotToken;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task ReadyAsync()
        {
            foreach (var guild in _client.Guilds)
            {
                Console.WriteLine($"Bot is in Guild -> [{guild.Name}] with ID [{guild.Id}]");

                var weatherCommand = new SlashCommandBuilder()
                    .WithName("weather")
                    .WithDescription("Gets the weather information for a specified city")
                    .AddOption("city", ApplicationCommandOptionType.String, "The name of the city", isRequired: true);

                try
                {
                    await _client.Rest.CreateGuildCommand(weatherCommand.Build(), guild.Id);
                    Console.WriteLine($"Created /weather command in guild: {guild.Name}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to create /weather command in guild: {guild.Name}, Error: {ex}");
                }
            }
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            switch (command.Data.Name)
            {
                case "weather":
                    var city = command.Data.Options.First().Value.ToString();
                    var (weatherInfo, iconUrl) = await GetWeatherAsync(city);

                    if (weatherInfo != null)
                    {
                        var embed = GenerateWeatherEmbed(weatherInfo, iconUrl);

                        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} Printed weather for {command.User.Username}");
                        await command.RespondAsync(embed: embed);
                    }
                    else
                    {
                        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} Could not get weather for {city}.");
                        await command.RespondAsync($"Could not get weather for {city}. Please try again.");
                    }
                    break;
            }
        }

        private Embed GenerateWeatherEmbed(string weatherInfo, string iconUrl)
        {
            return new EmbedBuilder()
                            .WithTitle("Weather Information")
                            .WithDescription(weatherInfo)
                            .WithThumbnailUrl(iconUrl)
                            .WithColor(Color.Blue)
                            .Build();
        }

        private async Task<(string, string)> GetWeatherAsync(string location)
        {
            var response = await _httpClient.GetAsync($"http://api.weatherapi.com/v1/current.json?key={_tokens.WeatherApiKey}&q={location}&aqi=no");

            if (!response.IsSuccessStatusCode)
            {
                return (null, null);
            }

            var json = await response.Content.ReadAsStringAsync();

            var data = JsonConvert.DeserializeObject<WeatherData>(json);

            if (data.Location.Name != null && data.Current.Temp_C != null && data.Current.Condition != null)
            {
                var weatherInfo = $"**Weather for {data.Location.Name}, {data.Location.Region}, {data.Location.Country}:**\n" +
                                  $"Temperature: {data.Current.Temp_C}°C\n" +
                                  $"Condition: {data.Current.Condition.Text}\n" +
                                  $"Wind: {data.Current.Wind_Kph} km/h\n" +
                                  $"Humidity: {data.Current.Humidity}%\n" +
                                  $"Cloud: {data.Current.Cloud}%";

                var fullIconUrl = $"http:{data.Current.Condition.Icon}";

                return (weatherInfo, fullIconUrl);
            }

            return (null, null);
        }
    }
}
