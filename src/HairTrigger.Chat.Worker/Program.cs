using HairTrigger.Chat.Infrastructure;
using HairTrigger.Chat.Worker;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);

// Add Infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Add SignalR services so worker can publish to groups via IHubContext and Redis backplane
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
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("HairTrigger.Chat.Worker"))
            .AddConsoleExporter();
    });

// Add hosted service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
