using Microsoft.EntityFrameworkCore;
using System.Configuration;

namespace BlazorAIChat
{
    public class AIChatDBContext : DbContext
    {

        protected readonly IConfiguration _configuration;

        public AIChatDBContext(IConfiguration configuration)
        {
           _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // connect to SQLite database
            options.UseSqlite(_configuration.GetConnectionString("ConfigDatabase"));
        }

        public DbSet<Models.User> Users { get; set; }
        public DbSet<Models.Config> Config { get; set; }
    }
}
