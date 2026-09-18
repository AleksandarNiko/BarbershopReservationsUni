using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BarbershopReservationsUni.Data
{
    // Provides a design-time factory for EF Core tools (migrations, scaffolding)
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BarbershopReservationsUniDbContext>
    {
        public BarbershopReservationsUniDbContext CreateDbContext(string[] args)
        {
            // Locate the Web project's appsettings.json for the connection string
            var basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "BarbershopReservationsUni.Web"));

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                // Fallback to LocalDB if not found
                connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=BarbershopReservationsDb;Trusted_Connection=True;MultipleActiveResultSets=true";
            }

            var builder = new DbContextOptionsBuilder<BarbershopReservationsUniDbContext>();
            builder.UseSqlServer(connectionString);

            return new BarbershopReservationsUniDbContext(builder.Options);
        }
    }
}
