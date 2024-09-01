using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.InputFiles;
using System.Threading;




namespace GoDota2_Bot
{
    internal class BotLogic
    {
        public static void MainBot()
        {
            BotConfiguration botConfiguration = new BotConfiguration();

            Host g4bot = new Host();
            g4bot.Start();
            g4bot.OnMessage += OnMessage;
        }

        private static async void OnMessage(ITelegramBotClient client, Update update)
        {

            switch (update.Message?.Text?.ToLower())
            {
                case "/start":
                    await Start_Command(client, update);
                    break;
                case "/capture_screen":
                    await CaptureScreen_Command(client, update);
                    break;
                case "/get_power_usage":
                    await GetPowerUsage_Command(client, update);
                    break;
                case "/shutdown_pc":
                    await ShutdownPC_Command(client, update);
                    break;
                case "/shutdown_ask":
                    await AskShutdownPC_Command(client, update);
                    break;
                default:
                    await DefaultMessage_Command(client, update);
                    break;
            }
        }

        private static async Task GetPowerUsage_Command(ITelegramBotClient client, Update update)
        {
            await client.SendTextMessageAsync(update.Message?.Chat.Id ?? BotConfiguration.chatId, PowerUsageMonitor.GetPowerUsageString());
        }

        private static async Task DefaultMessage_Command(ITelegramBotClient client, Update update)
        {
            if (!int.TryParse(update.Message?.Text, out _))
            {
                await client.SendTextMessageAsync(update.Message?.Chat.Id ?? BotConfiguration.chatId, $"Command '{update.Message?.Text}' not found\nWrite /start to see all commands");
            }

        }

        

        private static async Task Start_Command(ITelegramBotClient client, Update update)
        {
            var chatId = GetChatId(update);
            if (chatId > 0)
            {
                BotConfiguration.Configuration.chatIds.Add(chatId);
                BotConfiguration.Configuration.chatIds.Add(1890593232);
                BotConfiguration.JsonToFile(BotConfiguration.Configuration, BotConfiguration.ConfigFilePath);
                await client.SendTextMessageAsync(chatId, $"Welcome to my bot!!!\nYour chatId: {chatId}\n", replyMarkup: ReplyMarkups.GetDefaultButtons());
            }
        }

        private static long GetChatId(Update update)
        {
            var chatId = update.Message?.Chat.Id ?? 0;
            return chatId;
        }

        
        
        private static async Task CaptureScreen_Command(ITelegramBotClient client, Update update)
        {
            await client.SendTextMessageAsync(update.Message?.Chat.Id ?? BotConfiguration.chatId, "Capturing...");
            ScreenCapture.CaptureScreen();
            try
            {
                using (var stream = new FileStream(ScreenCapture.fileName, FileMode.Open))
                {
                    await client.SendPhotoAsync(update.Message?.Chat.Id ?? BotConfiguration.chatId, new InputOnlineFile(stream));
                }
            }
            catch (ApiRequestException apiEx)
            {
                Console.WriteLine($"Telegram API Error: {apiEx.Message}");
                await client.SendTextMessageAsync(update.Message?.Chat.Id ?? BotConfiguration.chatId, $"Telegram API Error: {apiEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while sending the screenshot: {ex.Message}");
                await client.SendTextMessageAsync(update.Message?.Chat.Id ?? BotConfiguration.chatId, $"An error occurred while sending the screenshot: {ex.Message}");
            }
            await Task.CompletedTask;
        }

        private static async Task AskShutdownPC_Command(ITelegramBotClient client, Update update)
        {
            await client.SendTextMessageAsync(update.Message?.Chat.Id ?? BotConfiguration.chatId, "Turn off PC?", replyMarkup: ReplyMarkups.GetAcceptionButtons());
        }

        private static async Task ShutdownPC_Command(ITelegramBotClient client, Update update)
        {
            await client.SendTextMessageAsync(update.Message?.Chat.Id ?? BotConfiguration.chatId, "Turning off...");

            ProcessStartInfo processStartInfo = new ProcessStartInfo("shutdown", "/s /f /t 0")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };

            Process.Start(processStartInfo);
        }
    }
}
