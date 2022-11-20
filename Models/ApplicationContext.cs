using Microsoft.EntityFrameworkCore;
 
namespace HabraBot
{
    public class ApplicationContext : DbContext
    {
        public DbSet<TimeOffItem> TimeOffItems { get; set; }
        public DbSet<UserInfo> UserInfos { get; set; }
        // public ApplicationContext()
        // {
        //     Database.EnsureCreated();
        // }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=tlgbot;Username=postgres;Password=99Troubles!");
        }
    }
}