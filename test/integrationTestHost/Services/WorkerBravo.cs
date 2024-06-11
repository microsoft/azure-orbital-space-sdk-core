namespace Microsoft.Azure.SpaceFx.IntegrationTestHost;

public class WorkerBravo : BackgroundService {
    private readonly ILogger<WorkerBravo> _logger;
    private readonly IServiceProvider _serviceProvider;

    public WorkerBravo(ILogger<WorkerBravo> logger, IServiceProvider serviceProvider) {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            using (var scope = _serviceProvider.CreateScope()) {
                _logger.LogInformation("WorkerBravo running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }

    }
}
