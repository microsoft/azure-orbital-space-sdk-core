namespace PayloadApp.DebugHost;

public class Worker : BackgroundService, Microsoft.Azure.SpaceFx.Core.IMonitorableService {
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Microsoft.Azure.SpaceFx.Core.Client _client;
    private readonly string _appId;
    private readonly string _all_xfer_dir;

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider) {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _client = _serviceProvider.GetService<Microsoft.Azure.SpaceFx.Core.Client>() ?? throw new NullReferenceException($"{nameof(Microsoft.Azure.SpaceFx.Core.Client)} is null");
        _appId = _client.GetAppID().Result;
        _all_xfer_dir = _client.GetXFerDirectories().Result.root_directory.Replace("xfer", "allxfer");
    }

    public bool IsHealthy() {
        return true;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(async () => {
        while (!stoppingToken.IsCancellationRequested) {
            using (var scope = _serviceProvider.CreateScope()) {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage testMessage = new() {

                    Message = "Testing"
                };

                _client.DirectToApp(_appId, testMessage).Wait();

                _logger.LogInformation("Services online: {count}", _client.ServicesOnline().Count);

                await Task.Delay(1000, stoppingToken);
            }
        }
    });
}
