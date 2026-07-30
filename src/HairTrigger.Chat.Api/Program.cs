using HairTrigger.Chat.Api;
using HairTrigger.Chat.Api.Hubs;
using HairTrigger.Chat.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Net;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add Infrastructure services (DbContext, Redis, Repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// Return structured JSON errors for all unhandled exceptions and HTTP error codes
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Instance = $"{ctx.HttpContext.Request.Method} {ctx.HttpContext.Request.Path}";
        ctx.ProblemDetails.Extensions["requestId"] = ctx.HttpContext.TraceIdentifier;
    };
});

// Global exception handler — catches unhandled exceptions and returns 500 JSON
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrWhiteSpace(origin)) return false;

            // Allow localhost on any port (3000, 5173, 4200, 3001, etc.)
            if (origin.StartsWith("http://localhost:") || origin.StartsWith("https://localhost:") || origin == "http://localhost" || origin == "https://localhost")
                return true;

            // Allow all vyg.re subdomains (isj.vyg.re, isj-dev.vyg.re, etc.)
            if (origin.EndsWith(".vyg.re") || origin == "https://vyg.re")
                return true;

            // Also check ALLOWED_ORIGINS env variable if configured
            var envOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS");
            if (!string.IsNullOrWhiteSpace(envOrigins))
            {
                var allowed = envOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (allowed.Contains(origin, StringComparer.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        })
        .SetIsOriginAllowedToAllowWildcardSubdomains()
        .AllowAnyMethod()
        .AllowAnyHeader()
        .WithExposedHeaders("x-signalr-user-agent")
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
            },

            // Called when a request has no token or an invalid token — return 401 JSON
            OnChallenge = async context =>
            {
                context.HandleResponse(); // suppress default empty-body 401

                var detail = context.AuthenticateFailure?.Message
                    ?? "Bearer token is missing or invalid.";

                // Distinguish between missing token and invalid/expired token
                if (context.AuthenticateFailure == null)
                    detail = "Authorization header with a valid Bearer token is required.";

                var problem = new ProblemDetails
                {
                    Status   = StatusCodes.Status401Unauthorized,
                    Title    = "Unauthorized",
                    Detail   = detail,
                    Instance = $"{context.Request.Method} {context.Request.Path}",
                };
                problem.Extensions["requestId"] = context.HttpContext.TraceIdentifier;

                context.Response.StatusCode  = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(problem);
            },

            // Called when a valid token doesn't have the required role/policy — return 403 JSON
            OnForbidden = async context =>
            {
                var problem = new ProblemDetails
                {
                    Status   = StatusCodes.Status403Forbidden,
                    Title    = "Forbidden",
                    Detail   = "You do not have permission to access this resource.",
                    Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}",
                };
                problem.Extensions["requestId"] = context.HttpContext.TraceIdentifier;

                context.Response.StatusCode  = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(problem);
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

// 1. Exception handler must be FIRST — catches anything below it
app.UseExceptionHandler();
app.UseStatusCodePages();

// 2. CORS
app.UseCors("AllowFrontend");

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

// 3. Auth — must come before MapControllers
app.UseAuthentication();
app.UseAuthorization();

// 4. Controllers & Hub
app.MapControllers();

// Map SignalR hub
app.MapHub<ChatHub>("/hubs/chat");

app.Run();