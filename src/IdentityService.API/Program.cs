using IdentityService.API;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Async(c => c.File("Logs/logs.txt"))
    .WriteTo.Async(c => c.Console())
    .CreateLogger();

try
{
    Log.Information("Starting Dragon SSO - Identity Service");
    var builder = WebApplication.CreateBuilder(args);
    builder.Host
        .AddAppSettingsSecretsJson()
        .UseAutofac()
        .UseSerilog();

    await builder.AddApplicationAsync<IdentityServiceModule>();
    var app = builder.Build();

    // Fix lỗi Ocelot làm mất Host Header
    // Ép Identity Service luôn nghĩ mình đang chạy dưới dạng App__SelfUrl (sso.longdev.store)
    app.Use(async (context, next) =>
    {
        var selfUrl = builder.Configuration["App:SelfUrl"];
        if (!string.IsNullOrEmpty(selfUrl) && Uri.TryCreate(selfUrl, UriKind.Absolute, out var uri))
        {
            context.Request.Scheme = uri.Scheme;
            context.Request.Host = new HostString(uri.Authority);
        }
        await next();
    });

    await app.InitializeApplicationAsync();

    app.MapHealthChecks("/health/live");
    app.MapHealthChecks("/health/ready");

    // Seed data on first run
    Log.Information("Seeding database...");
    await app.Services.SeedDataAsync();
    Log.Information("Database seeded successfully");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Identity Service terminated unexpectedly!");
}
finally
{
    Log.CloseAndFlush();
}
