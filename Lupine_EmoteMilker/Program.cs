using Discord;
using Discord.Commands;
using Discord.WebSocket;
using LupineEmoteMilker.Handlers;
using LupineEmoteMilker.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace LupineEmoteMilker
{
    public class Program
    {
        private DiscordSocketClient _client;
        private ILoggingService _logger;
        private IServiceProvider _serviceProvider;
        private CommandHandler _commandHandler;
        private CommandService _commandService;

        public static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync()
        {
            var clientConfig = new DiscordSocketConfig()
            {
                //Need to enable all privileged intents in https://discord.com/developers/applications/ panel as well...
                GatewayIntents = GatewayIntents.MessageContent | GatewayIntents.AllUnprivileged,
                MessageCacheSize = 50,
                LogLevel = LogSeverity.Debug,
            };

            _client = new DiscordSocketClient(clientConfig);
            _commandService = new CommandService();
            _logger = new LoggingServiceBasic(_client, _commandService);
            _serviceProvider = ConfigureServices();
            _commandHandler = new CommandHandler(_client, _commandService, _serviceProvider);

            string token = Environment.GetEnvironmentVariable("EMOTEMILKER_TOKEN");

            if(string.IsNullOrEmpty(token))
                throw new TaskCanceledException("Token environmental variable doesn't exists or is empty");

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _client.Ready += OnClientReady;

            await Task.Delay(-1);
        }

        private async Task OnClientReady()
        {
            await _commandHandler.InstallCommandsAsync();
        }

        private IServiceProvider ConfigureServices()
        {
            var map = new ServiceCollection()
                .AddSingleton(_logger)
                .AddSingleton(_client)
                .AddSingleton(_commandService)
                .AddSingleton<WebEmotePullerService>();

            return map.BuildServiceProvider();
        }

    }
}