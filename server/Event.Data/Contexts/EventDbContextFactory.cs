using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Event.Data.Contexts
{
    public class EventDbContextFactory : IDesignTimeDbContextFactory<EventDbContext>
    {
        public EventDbContext CreateDbContext(string[] args)
        {
            var baseDirectory = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var apiPath = baseDirectory;

            if (baseDirectory.EndsWith("Event.Data"))
            {
                apiPath = Path.Combine(baseDirectory, "../Event.API");
            }
            else if (!baseDirectory.EndsWith("Event.API") && Directory.Exists(Path.Combine(baseDirectory, "Event.API")))
            {
                apiPath = Path.Combine(baseDirectory, "Event.API");
            }

            var configuration = new ConfigurationBuilder()
                .SetBasePath(apiPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<EventDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            optionsBuilder.UseNpgsql(connectionString);

            return new EventDbContext(optionsBuilder.Options);
        }
    }
}
