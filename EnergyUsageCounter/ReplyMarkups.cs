using Telegram.Bot.Types.ReplyMarkups;

namespace GoDota2_Bot
{
    internal class ReplyMarkups
    {
        public static IReplyMarkup GetDefaultButtons()
        {
            return new ReplyKeyboardMarkup(new[]
            {
        new[] { new KeyboardButton("/start") },
        new[] { new KeyboardButton("/capture_screen") },
        new[] { new KeyboardButton("/get_power_usage") },
        new[] { new KeyboardButton("/shutdown_ask") },
                    })
            {
                ResizeKeyboard = true
            };
        } 
        public static IReplyMarkup GetAcceptionButtons()
        {
            return new ReplyKeyboardMarkup(new[]
            {
        new[] { new KeyboardButton("/start") },
        new[] { new KeyboardButton("/shutdown_pc") },})
            {
                ResizeKeyboard = true
            };
        }

    }
}
