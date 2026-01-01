using HairTrigger.Chat.Domain.Interfaces;

namespace HairTrigger.Chat.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HairTrigger Chat Worker started at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                
                // TODO: Implement background processing tasks here
                // Examples:
                // - Process message queue
                // - Clean up old messages
                // - Send notifications
                // - Analytics processing
                
                var messageRepository = scope.ServiceProvider.GetRequiredService<IMessageRepository>();
                
                _logger.LogDebug("Worker cycle executed at: {time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Worker execution");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        _logger.LogInformation("HairTrigger Chat Worker stopped at: {time}", DateTimeOffset.Now);
    }
}
