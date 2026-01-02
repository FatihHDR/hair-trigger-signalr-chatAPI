using HairTrigger.Chat.Api.Hubs;
using HairTrigger.Chat.Infrastructure;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add Infrastructure services (DbContext, Redis, Repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
                ?? new[] { "http://localhost:3000", "http://localhost:5173" })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Add SignalR with Redis backplane
var redisConnection = builder.Configuration.GetConnectionString("Redis");
var signalRBuilder = builder.Services.AddSignalR();

if (!string.IsNullOrEmpty(redisConnection))
{
    signalRBuilder.AddStackExchangeRedis(redisConnection, options =>
    {
        options.Configuration.ChannelPrefix = "HairTriggerChat";
    });
}

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("HairTrigger.Chat.Api"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter();
    });

// Add controllers and OpenAPI/Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "HairTrigger Chat API",
        Description = "Real-time chat API with SignalR hub support"
    });
});

var app = builder.Build();

// Seed database with test data
if (app.Environment.IsDevelopment())
{
    await HairTrigger.Chat.Infrastructure.Data.SeedData.SeedAsync(app.Services);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "HairTrigger Chat API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

// Map SignalR hub
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
