using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace HabraBot
{
class Program
    {
        static ITelegramBotClient bot = new TelegramBotClient("5526032863:AAE3KmnBevOx5BwPlPNiDebIryUUVMCMrfM");
        static ApplicationContext dc= new ApplicationContext();
        static CommandExecuter ce = new CommandExecuter(bot,dc);
        //Функция обработки сообщения отправленного боту
        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {            
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            if(update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                var message = update.Message;
                if(message.Text!=null){
                    var com=message.Text.Split(" ")[0].ToLower();
                    if (ce.MethodDictionary.ContainsKey(com)){
                        typeof(CommandExecuter).GetMethod(ce.MethodDictionary[com]).Invoke(ce,new Message[] { message });
                    }
                    else{
                        if (ce.MethodWordDictionary.ContainsKey(message.Text)){
                        typeof(CommandExecuter).GetMethod(ce.MethodWordDictionary[message.Text]).Invoke(ce,new Message[] { message });
                    }
                        else
                        ce.EmptyAnswerProcAsync(message);
                    } 
                }
                              
            }          

            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
            {                
                await HandleCallbackQuery(botClient, update.CallbackQuery);
                return;
            }
        }
        public static async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {       
            ce.AddDayComProcAsync(callbackQuery); 
            return;
        }
        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Некоторые действия
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }        
        static void Main(string[] args)
        {
            Console.WriteLine("Запущен бот " + bot.GetMeAsync().Result.FirstName); 
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // receive all update types
            };
            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
            Console.ReadLine();
        }
    }
}


