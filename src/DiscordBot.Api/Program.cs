using System.Text.Json.Serialization;
using DiscordBot.Api.Extensions;
using DiscordBot.Api.Middleware;
using DiscordBot.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Local overrides (gitignored). Re-apply env vars afterward so CLI overrides (e.g. production migrations) win.
builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.local.json",
    optional: true,
    reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// Railway (and similar PaaS) inject PORT; bind HTTP on all interfaces.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "DiscordBot API", Version = "v1" });
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddDashboardCors(builder.Configuration);

builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

app.ValidateRequiredConfiguration();

app.UseCors("Dashboard");

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "DiscordBot API v1");
        options.RoutePrefix = "swagger";
    });
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Logger.LogInformation("DiscordBot API starting. Environment: {Environment}", app.Environment.EnvironmentName);

app.Run();
