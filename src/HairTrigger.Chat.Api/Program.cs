using HairTrigger.Chat.Api.Hubs;
using HairTrigger.Chat.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add Infrastructure services (DbContext, Redis, Repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var envOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS");
        string[] origins;

        if (!string.IsNullOrWhiteSpace(envOrigins))
        {
            origins = envOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        else
        {
            origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
                ?? new[] { 
                    "https://isj.vyg.re", 
                    "https://isj-dev.vyg.re", 
                    "https://api-chat-isj.vyg.re", 
                    "https://api-isj-dev.vyg.re", 
                    "http://localhost:3000", 
                    "http://localhost:5173" 
                };
        }

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Add JWT authentication (validates tokens issued by backend-isj)
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtIssuer = jwtSection["Issuer"] ?? "backend-isj";
var jwtAudience = jwtSection["Audience"] ?? "isj-client";
var jwtSigningKey = jwtSection["Key"] 
    ?? builder.Configuration["JWT_SECRET"] 
    ?? Environment.GetEnvironmentVariable("JWT_SECRET");

if (string.IsNullOrWhiteSpace(jwtSigningKey))
{
    jwtSigningKey = "fdad8fe0e3dca3c49ef56fd0776809c144ba0254f856804d90e9a4df139fe5b9";
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = "roles"
        };

        // Allow SignalR hub to receive token from query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Add SignalR with Redis backplane
var redisConnection = builder.Configuration.GetConnectionString("Redis");
var signalRBuilder = builder.Services.AddSignalR();

if (!string.IsNullOrEmpty(redisConnection))
{
    signalRBuilder.AddStackExchangeRedis(redisConnection, options =>
    {
        options.Configuration.ChannelPrefix = "ISJChat";
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
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "ISJ Chat API v1",
        Description = "Real-time chat API with SignalR hub for Indonesia Sehat Jiwa platform."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Masukkan token JWT yang diterbitkan oleh backend-isj (format: Bearer <token>)"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Seed database with test data (only in development)
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
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ISJ Chat API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

// Map SignalR hub
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
