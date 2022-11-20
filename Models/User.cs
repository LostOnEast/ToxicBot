public class UserInfo{
    public long Id {get;set;}
    public long ChiefId {get;set;}
    public long ChatIdWithChief {get;set;}
}
public class TimeOffItem{
    public int Id {get;set;}
    //public User User {get;set;}
    public DateTime RequestDate {get;set;}
    public DateTime StartDate {get;set;}
    public DateTime FinishDate {get;set;}
    public long UserId {get;set;}

}
