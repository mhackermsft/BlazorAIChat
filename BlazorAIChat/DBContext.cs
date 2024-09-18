using BlazorAIChat.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Configuration;

namespace BlazorAIChat
{
    public class AIChatDBContext : DbContext
    {

        protected readonly AppSettings _appSettings;

        public AIChatDBContext(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // connect to SQLite database
            options.UseSqlite(_appSettings.ConnectionStrings.ConfigDatabase);
        }

        public DbSet<Models.User> Users { get; set; }
        public DbSet<Models.Config> Config { get; set; }

        public DbSet<Models.Session> Sessions { get; set; }

        public DbSet<Models.SessionDocument> SessionDocuments { get; set; }
        public DbSet<Models.Message> Messages { get; set; }

        public DbSet<Models.CrawlerStatus> CrawlerStatuses { get; set; }
    }
}
