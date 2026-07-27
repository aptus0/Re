using Re.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Re.Persistence;

/// <summary>
/// EF Core design-time factory – 'dotnet ef migrations add' komutları için gerekli.
/// </summary>
public class ReDbContextFactory : IDesignTimeDbContextFactory<ReDbContext>
{
    public ReDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..\\Re.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=ReDev;Trusted_Connection=True;";

        var optionsBuilder = new DbContextOptionsBuilder<ReDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sql =>
        {
            sql.MigrationsAssembly(typeof(ReDbContextFactory).Assembly.FullName);
            sql.CommandTimeout(120);
        });

        return new ReDbContext(optionsBuilder.Options);
    }
}

