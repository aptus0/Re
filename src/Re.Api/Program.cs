using Re.Application;
using Re.Domain.Exceptions;
using Re.Infrastructure;
using Re.Persistence;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// Portable installations get a stable, per-user signing key automatically.
// Production environments can still override it with JwtSettings__SecretKey.
if (string.IsNullOrWhiteSpace(builder.Configuration["JwtSettings:SecretKey"]))
{
    var securityDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ReSoft", "Re", "Security");
    Directory.CreateDirectory(securityDirectory);
    var keyPath = Path.Combine(securityDirectory, "jwt-signing.key");
    var signingKey = File.Exists(keyPath)
        ? File.ReadAllText(keyPath)
        : Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));

    if (!File.Exists(keyPath))
    {
        File.WriteAllText(keyPath, signingKey);
        File.SetAttributes(keyPath, File.GetAttributes(keyPath) | FileAttributes.Hidden);
    }

    builder.Configuration["JwtSettings:SecretKey"] = signingKey;
}
builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "Re.Api";
});

var parentPid = builder.Configuration.GetValue<int?>("parent-pid");
if (parentPid.HasValue)
{
    Task.Run(async () =>
    {
        try
        {
            var parent = System.Diagnostics.Process.GetProcessById(parentPid.Value);
            await parent.WaitForExitAsync();
        }
        catch { }
        finally
        {
            Environment.Exit(0);
        }
    });
}

// ── Servisler ──────────────────────────────────────────────────────────────
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPersistence(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// .NET 10 built-in OpenAPI
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((doc, ctx, ct) =>
    {
        doc.Info = new()
        {
            Title   = "Re ERP API",
            Version = "v1",
            Description = "Re ERP – Stok, Satış, Cari ve Finans API"
        };
        return Task.CompletedTask;
    });
});

// CORS
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddAuthorization();

// ── Pipeline ───────────────────────────────────────────────────────────────
var app = builder.Build();

// Migration & seed
await Re.Persistence.DependencyInjection.MigrateAndSeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Scalar UI (modern Swagger alternatiifi) – isteğe bağlı
    // app.MapScalarApiReference();
}

app.UseExceptionHandler(errApp =>
{
    errApp.Run(async ctx =>
    {
        ctx.Response.ContentType = "application/json";
        var ex = ctx.Features
            .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;

        int statusCode;
        string message;

        if (ex is EntityNotFoundException)
        {
            statusCode = 404; message = ex.Message;
        }
        else if (ex is UnauthorizedAccessException)
        {
            statusCode = 401; message = "Yetkisiz erişim.";
        }
        else if (ex is DomainException)
        {
            statusCode = 400; message = ex.Message;
        }
        else
        {
            statusCode = 500; message = "Beklenmeyen bir hata oluştu.";
        }

        ctx.Response.StatusCode = statusCode;
        await ctx.Response.WriteAsync(
            JsonSerializer.Serialize(new
            {
                success = false,
                message,
                errors  = new[] { message }
            }));
    });
});

// app.UseHttpsRedirection();
app.UseCors("ReCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    Status    = "Healthy",
    Service   = "Re ERP API",
    Version   = "1.0.0",
    Timestamp = DateTime.UtcNow
})).WithTags("Health");

try
{
    app.Run();
}
catch (Exception ex) when (ex is System.IO.IOException || ex is System.Net.Sockets.SocketException)
{
    // API is already running or port is in use. Exiting gracefully.
    Console.WriteLine("API port is already in use. Exiting gracefully.");
}

