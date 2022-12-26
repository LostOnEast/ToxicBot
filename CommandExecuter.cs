using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HabraBot // Note: actual namespace depends on the project name.
{
public class CommandExecuter
{
    private ITelegramBotClient _bot {get;set;}
    private ApplicationContext _dc {get;set;}
    private BotSettings _configuration;
    public Dictionary<string,string> MethodDictionary= new Dictionary<string, string>(); 
    public Dictionary<string,string> MethodWordDictionary= new Dictionary<string, string>(); 
    public CommandExecuter(ITelegramBotClient bot,ApplicationContext dc,BotSettings configuration){
        
        this._configuration = configuration;
        _bot=bot;
        _dc=dc;
        //add tlg commands
        var mis = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);        
        foreach (var item in mis.Where(t=>t.Name.Contains("TlgCom")))
        {
            var s="/"+item.Name.Split("TlgCom")[0].ToLower();            
            var w=item.CustomAttributes.Last().ConstructorArguments[0].Value.ToString();
            MethodDictionary.Add(s,item.Name);
            MethodWordDictionary.Add(w,item.Name);            
        }       
    }
    private async void PutInfoAsync(DateTime startDate, DateTime finishDate, long id){
                    var usr=_dc.UserInfos.Find(id);
                    if(usr!=null){
                        var t =new TimeOffItem{StartDate=startDate.ToUniversalTime(),FinishDate=finishDate.ToUniversalTime()};
                        t.RequestDate=DateTime.UtcNow;
                        t.UserId=id;
                        _dc.TimeOffItems.Add(t);
                        await _dc.SaveChangesAsync();                                 
                    }                   
    }
    private async Task<UserStat> GetUserStatAsync(long id){
                    
                    var stat=await _dc.TimeOffItems.Where(t=>t.UserId==id && t.FinishDate>DateTime.UtcNow.AddMonths(-1)).ToListAsync();                    
                    Task.WaitAll();
                    var cnt=stat.Count();
                    TimeSpan cnt_time=new TimeSpan(0);                     
                     foreach (var item in stat)
                     {
                         cnt_time+=item.FinishDate-item.StartDate;
                     }
                     var r = new UserStat{TimeOffsCount=cnt,TimeOffsSpan=cnt_time};
                    return r;                    
    }
    
    public async void  AddDayComProcAsync(CallbackQuery callbackQuery){
                    var cmd=callbackQuery.Data.ToString().Split(" ");
                    if(cmd.Length>1 & callbackQuery.Data.ToString()[0]=='/'){
                           try
                           {
                                var cdt=DateTime.UtcNow.AddDays(Convert.ToInt32(cmd[1]));
                                var sd= DateTime.Parse(cdt.ToString("dd.MM.yy")+" "+cmd[2].Split("-")[0]);
                                var fd= DateTime.Parse(cdt.ToString("dd.MM.yy")+" "+cmd[2].Split("-")[1]);
                                var statString="Нет данных о статистике";  
                                var res=_dc.UserInfos.Find(callbackQuery.From.Id);                              
                                var stat=await GetUserStatAsync(res.Id); 
                                statString=$"За прошедший месяц {stat.TimeOffsCount} отгулов, общей длительностью {stat.TimeOffsSpan.TotalHours}.";
                                await _bot.SendTextMessageAsync(callbackQuery.Message.Chat, "Ваш запрос принят. Проверяем статистику ваших отгулов.");
                                
                                //отправка сообщения боссу      
                                var chat_with_boss_id= res.ChatIdWithChief;
                                                                                            
                                if(chat_with_boss_id!=null){
                                    _bot.SendTextMessageAsync(chat_with_boss_id,$"Запрос выходного от @{callbackQuery.From.Username} с {sd} по {fd}. {statString}");
                                }
                                PutInfoAsync(sd,fd,callbackQuery.From.Id);                             
                           }
                           catch (System.Exception ex)
                           {                            
                                Console.WriteLine(ex.Message);
                           }                          
                           
                    }
    }
    [TelegramCommandDescription("Приветствие","Активации бота и сохранения данных пользователя.")]
    public async void  StartTlgComProcAsync(Message message){
                    var uinf=_dc.UserInfos.SingleOrDefault(t=>t.Id==message.From.Id);
                    if(uinf!=null){
                        _bot.SendTextMessageAsync(message.Chat, "Вы уже зарегестрированы.");
                    }
                    else{
                        _dc.UserInfos.Add(new UserInfo{Id=message.From.Id,TelegrammId=message.From.Username});
                        await _dc.SaveChangesAsync();
                        _bot.SendTextMessageAsync(message.Chat, "Теперь вы зарегестрированы в системе. Не забудьте добавить прикрепить своего руководителя с помощью команды /addboss");
                        
                    }
                    
    }
    [TelegramCommandDescription("Руководитель","Закрепление руководителя для отслеживания.")]
    public async void  AddBossTlgComProcAsync(Message message){
                    var cmd=message.Text.Split(" ");
                    if(cmd.Length<2)
                        _bot.SendTextMessageAsync(message.Chat, "Для данной команды необходимо передать Telegram Id вашего руководителя. Например \"/addboss @myboss\". удостоверьтесь что ваш руководитель зарегестрирован в системе и у него есть Telegram Id.");
                    else{
                        var clrname=cmd[1].Replace("@","");
                        var bss=_dc.UserInfos.SingleOrDefault(t=>t.TelegrammId==clrname);
                        if(bss!=null){
                            var usr=_dc.UserInfos.Find(message.From.Id);
                            usr.ChiefId=bss.Id;
                            usr.ChatIdWithChief=bss.Id;
                            _dc.SaveChanges();
                            _bot.SendTextMessageAsync(message.Chat, $"За вами закреплен руководитель {cmd[1]}");
                        }
                        else
                            _bot.SendTextMessageAsync(message.Chat, $"В системе нет зарегестрированного пользователя с Id {cmd[1]}");
                        
                    }
                    
                    
                    
    }
    [TelegramCommandDescription("Статистика","Просмотр статистики пользователя за месяц.")]
    public async void  StatsTlgComProcAsync(Message message){              
                  
                    var stat=await GetUserStatAsync(message.From.Id);
                    var statString=$"За прошедший месяц {stat.TimeOffsCount} отгулов, общей длительностью {stat.TimeOffsSpan.TotalHours}.";                    
                    _bot.SendTextMessageAsync(message.Chat,statString); 
    }
    [TelegramCommandDescription("Взять отгул","Запроса выходного на завтра.")]
    public async void  AddDayTlgComProcAsync(Message message){
                    var cmd=message.Text.Split(" ");
                    if(cmd.Length>1 & message.Text[0]=='/'){
                           try
                           {
                                var sd= DateTime.Parse(cmd[1]+" "+cmd[2].Split("-")[0]);
                                var fd= DateTime.Parse(cmd[1]+" "+cmd[2].Split("-")[1]);
                                PutInfoAsync(sd,fd,message.From.Id); 
                                _bot.SendTextMessageAsync(message.Chat, "Ваш запрос принят.");
                           }
                           catch (System.Exception)
                           {                            
                                throw;
                           }                           
                           
                    }
                    else {
                        InlineKeyboardMarkup keyboard = new(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("На завтра","/add_day 1 8:30-17:45"),
                            InlineKeyboardButton.WithCallbackData("На послезавтра","/add_day 2 8:30-17:45"),
                            
                        },
                        new[]   
                        {
                            
                            InlineKeyboardButton.WithCallbackData("Завтра до обеда","/add_day 1 8:30-14:00"),
                            InlineKeyboardButton.WithCallbackData("Завтра после обеда","/add_day 1 14:30-17:45"),
                            
                        },
                        new[]
                        {                            
                            InlineKeyboardButton.WithCallbackData("Послезавтра до обеда","/add_day 2 14:30-17:45"),
                            
                        },
                        new[]
                        {                            
                            
                            InlineKeyboardButton.WithCallbackData("Послезавтра после обеда","/add_day 2 14:30-17:45"),
                        }
                    });                    
                    await _bot.SendTextMessageAsync(message.Chat.Id, $"Выберите один из варинтов, или отправьте команду в формате /add_day {DateTime.Now.AddDays(1).ToString("dd.MM.yy")} 8:30-14:00:", replyMarkup: keyboard);
                        
                    }            
                    
    }
    
    [TelegramCommandDescription("Пустой ответ","Ответ на случай если команда не распознана.")]
    public async void  EmptyAnswerProcAsync(Message message){
                    
                    await _bot.SendTextMessageAsync(message.Chat, "Команда не распознана, попробуйте воспользоваться командай /help или встроенной клавиатурой.");
                    
    }
    [TelegramCommandDescription("Инструкция","Вывод основных команд бота.")]
    public async void  HelpTlgComProcAsync(Message message){

                    List<KeyboardButton[]> kbl= new List<KeyboardButton[]>();
                    
                    var sb = new StringBuilder("Список команд \r\n");
                    var mis = this.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public).OrderBy(t=>t.Name);                    
                    foreach (var item in mis.Where(t=>t.Name.Contains("TlgCom")))
                    {
                        var s="/"+item.Name.Split("TlgCom")[0].ToLower();
                        sb.Append($"{s} {item.CustomAttributes.Last().ConstructorArguments[1].Value}\r\n");  
                        var kbs=item.CustomAttributes.Last().ConstructorArguments[0].Value.ToString();
                        kbl.Add(new KeyboardButton[] {kbs});                                  
                    } 
                    ReplyKeyboardMarkup keyboard = new(kbl.ToArray())
                    {
                        ResizeKeyboard = true
                    };                    
                    sb.Append($"Такжу вы можете просто прислать необходимую дату отгула в формате {DateTime.Now.AddDays(1).ToString("dd.MM.yy")} 8:30-14:00");
                    await _bot.SendTextMessageAsync(message.Chat.Id, sb.ToString(), replyMarkup: keyboard);
    }
        
}
class TelegramCommandDescription : Attribute
{
    public string Name { get;}
    public string Description { get;}
    //public AgeValidationAttribute() { }
    public TelegramCommandDescription(string name, string description)
    {
        Name=name;
        Description = description;

    }
    
}
}


