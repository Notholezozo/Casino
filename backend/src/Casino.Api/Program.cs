using System.Text.Json.Serialization;
using Casino.Api.Auth;
using Casino.Api.ErrorHandling;
using Casino.Api.Realtime;
using Casino.Application;
using Casino.Application.Abstractions;
using Casino.Application.Common;
using Casino.Infrastructure;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration métier
builder.Services.Configure<TablesOptions>(builder.Configuration.GetSection(TablesOptions.Section));
builder.Services.Configure<BlackjackOptions>(builder.Configuration.GetSection(BlackjackOptions.Section));

// ---- Couches
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);

// ---- Temps réel
builder.Services
    .AddSignalR()
    .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<IGameNotifier, SignalRGameNotifier>();
builder.Services.AddHostedService<GameClockService>();

// ---- API
builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();

builder.Services
    .AddAuthentication(SessionAuth.Scheme)
    .AddScheme<AuthenticationSchemeOptions, SessionTokenAuthenticationHandler>(SessionAuth.Scheme, null);
builder.Services.AddAuthorization();

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// En production l'API sert aussi le front compilé (wwwroot) : un seul processus à lancer.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<GameHub>("/hubs/game");
app.MapFallbackToFile("index.html");

app.Run();
