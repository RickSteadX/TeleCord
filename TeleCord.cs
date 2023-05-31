using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace App
{
    public class DiscordBot
    {
        public DiscordSocketClient _client;
        public TelegramBot telegramBotInstance;
        public readonly string token;

        public DiscordBot(string token)
        {
            this.token = token;
        }

        public async Task RunBotAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            });
            _client.Log += Log;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _client.MessageReceived += HandleCommandAsync;

            await Task.Delay(-1);
        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            var context = new SocketCommandContext(_client, message);

            if (message.Author.IsBot) return;
            if (message.Content == string.Empty) return;

            Message sentMessage = await telegramBotInstance.botClient.SendTextMessageAsync(
                chatId: 368224807,
                text: $"{message.Author.Username}: {message.Content}"
            );
        }
    }

    public class TelegramBot
    {
        public readonly TelegramBotClient botClient;
        public readonly CancellationTokenSource cts;
        public DiscordBot discordBotInstance;

        public TelegramBot(string accessToken)
        {
            botClient = new TelegramBotClient(accessToken);
            cts = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            ReceiverOptions receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await botClient.GetMeAsync();
            Console.WriteLine($"Start listening for @{me.Username}");
        }

        public void Stop()
        {
            cts.Cancel();
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message)
                return;

            if (message.Text is not { } messageText)
                return;

            var chatId = message.Chat.Id;
            Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

            var guild = discordBotInstance._client.GetGuild(868119144288096277);
            var channel = guild?.GetChannel(868119144288096280) as SocketTextChannel;

            await channel.SendMessageAsync($"{message.From.Username}: {messageText}");
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }
    }

    public static class Program
    {
        public static async Task Main(string[] args)
        {
            string discordToken = "";
            string telegramToken = "";
            DiscordBot discordBot = new DiscordBot(discordToken);
            TelegramBot telegramBot = new(telegramToken);
            discordBot.telegramBotInstance = telegramBot;
            telegramBot.discordBotInstance = discordBot;

            await telegramBot.StartAsync();
            Task.Run(discordBot.RunBotAsync).GetAwaiter().GetResult();
        }
    }
}