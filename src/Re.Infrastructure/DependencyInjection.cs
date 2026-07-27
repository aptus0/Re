using Re.Application.Common.Interfaces;
using Re.Application.Interfaces;
using Re.Infrastructure.Authentication;
using Re.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using Re.Infrastructure.Salesforce;

namespace Re.Infrastructure;

/// <summary>
/// Infrastructure katmanı DI kayıt extension metodu.
/// Program.cs içinden builder.Services.AddInfrastructure(configuration) ile çağrılır.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Services
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IDateTimeService, DateTimeService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<ISalesforceCliService, SalesforceCliService>();
        services.AddScoped<ISalesforceDataSyncService, SalesforceDataSyncService>();
        services.AddScoped<ISalesforceMetadataService, SalesforceMetadataService>();
        services.AddScoped<ISalesforceCompositeRestService, SalesforceCompositeRestService>();
        services.AddScoped<ISalesforceBulkService, SalesforceBulkService>();
        services.AddScoped<ISalesforceToolingService, SalesforceToolingService>();
        services.AddScoped<ISalesforceMcpServerService, SalesforceMcpServerService>();
        services.AddScoped<ISalesforceAgentforceService, SalesforceAgentforceService>();
        services.AddScoped<SalesforceSyncJobWorker>();

        // JWT Authentication
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JwtSettings:SecretKey eksik.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        // Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.Console()
            .WriteTo.File(
                path: "logs/Re-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30)
            .Enrich.FromLogContext()
            .CreateLogger();

        services.AddLogging(loggingBuilder =>
            loggingBuilder.AddSerilog(dispose: true));

        return services;
    }
}

