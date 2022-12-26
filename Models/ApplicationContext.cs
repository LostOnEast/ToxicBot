using Microsoft.EntityFrameworkCore; 
namespace HabraBot
{
    public class ApplicationContext : DbContext
    {
        public DbSet<TimeOffItem> TimeOffItems { get; set; }
        public DbSet<UserInfo> UserInfos { get; set; }
        public ApplicationContext(DbContextOptions<ApplicationContext> options) 
            :base(options)
        {
            Database.EnsureCreated();
        }
    }
}