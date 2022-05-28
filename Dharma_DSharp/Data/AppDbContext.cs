using Dharma_DSharp.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Dharma_DSharp.Data
{
    public class AppDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = "Data Source=appdb.db";
            optionsBuilder.UseSqlite(connectionString)
                .LogTo(Log.Logger.Information, Microsoft.Extensions.Logging.LogLevel.Information);

            base.OnConfiguring(optionsBuilder);
        }

        public DbSet<AllianceMember> Member { get; set; }
    }
}
