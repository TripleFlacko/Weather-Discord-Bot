﻿using Discord;
using Discord.WebSocket;
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
            _tokens = JsonConvert.DeserializeObject<Tokens>(File.ReadAllText("../../../config.json"));

            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds |
                                 GatewayIntents.GuildMessages |
                                 GatewayIntents.MessageContent |
                                 GatewayIntents.GuildMessageReactions
            };

            _client = new DiscordSocketClient(config);

            _client.Log += Log;
            _client.MessageReceived += MessageReceivedAsync;

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

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            IUserMessage castedMessage = (IUserMessage)message;

            if (message.Author.Id == _client.CurrentUser.Id)
                return;

            if (message.Content.StartsWith("!weather"))
            {
                if (message.Content.Length == 8)
                {
                    await castedMessage.ReplyAsync($"You forgot to type the city. Use this example: !weather london");
                    return;
                }

                var location = message.Content.Substring(9);

                var (weatherInfo, iconUrl) = await GetWeatherAsync(location);

                if (weatherInfo != null)
                {
                    var embed = new EmbedBuilder()
                                                .WithTitle("Weather Information")
                                                .WithDescription(weatherInfo)
                                                .WithThumbnailUrl(iconUrl)
                                                .WithColor(Color.Blue)
                                                .Build();

                    await castedMessage.ReplyAsync(embed: embed);
                }
                else
                {
                    await castedMessage.ReplyAsync($"Could not get weather for {location}. Please try again.");
                }
            }
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
