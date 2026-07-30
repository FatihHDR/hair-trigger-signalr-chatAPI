using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace HairTrigger.Chat.Api;

/// <summary>
/// Global exception handler — converts all unhandled exceptions to structured
/// JSON ProblemDetails responses. Without this, ASP.NET returns empty 500 bodies
/// or Kestrel's default HTML error page.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception,
            "Unhandled exception on {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        var (statusCode, title) = exception switch
        {
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized,   "Unauthorized"),
            ArgumentNullException       => (HttpStatusCode.BadRequest,     "Bad Request"),
            ArgumentException           => (HttpStatusCode.BadRequest,     "Bad Request"),
            KeyNotFoundException        => (HttpStatusCode.NotFound,       "Not Found"),
            NotImplementedException     => (HttpStatusCode.NotImplemented, "Not Implemented"),
            _                          => (HttpStatusCode.InternalServerError, "Internal Server Error")
        };

        var problem = new ProblemDetails
        {
            Status   = (int)statusCode,
            Title    = title,
            Detail   = exception.Message,
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
        };
        problem.Extensions["requestId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode  = (int)statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
