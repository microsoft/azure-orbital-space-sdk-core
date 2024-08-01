
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Azure.SpaceFx;

public partial class Core {
    public partial class Services {
        public class LivenessCheck : IHealthCheck {
            private readonly ILogger<LivenessCheck> _logger;
            private readonly IServiceProvider _serviceProvider;
            private readonly IHostApplicationLifetime _appLifetime;
            private readonly Services.ResourceUtilizationMonitor _resourceUtilizationMonitor;
            private readonly Services.MessageReceiver _messageReceiver;
            private readonly Services.HeartbeatService _heartbeatService;
            private readonly Services.PluginLoader _pluginLoader;

            public LivenessCheck(ILogger<LivenessCheck> logger, IServiceProvider serviceProvider, IHostApplicationLifetime appLifetime, Services.MessageReceiver messageReceiver, Services.PluginLoader pluginLoader, Services.HeartbeatService heartbeatService, Services.ResourceUtilizationMonitor resourceUtilizationMonitor) {
                _logger = logger;
                _serviceProvider = serviceProvider;
                _appLifetime = appLifetime;
                _messageReceiver = messageReceiver;
                _pluginLoader = pluginLoader;
                _resourceUtilizationMonitor = resourceUtilizationMonitor;
                _heartbeatService = heartbeatService;

                _logger.LogInformation("Services.{serviceName} Initialized.", nameof(LivenessCheck));

            }

            public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
                using (var scope = _serviceProvider.CreateScope()) {
                    bool coreServiceHealthy = true;
                    List<string> unhealthyServices = new List<string>();

                    // Get all hosted services that implement IMonitorableService
                    List<IMonitorableService> monitorableServices = _serviceProvider.GetServices<IHostedService>().Where(service => service is IMonitorableService).Cast<IMonitorableService>().ToList();


                    // Check the core services seperately
                    coreServiceHealthy = _messageReceiver.IsHealthy();
                    if (!coreServiceHealthy) {
                        unhealthyServices.Add(_messageReceiver.GetType().Name);
                    }

                    _logger.LogDebug($"Health check service: '{_messageReceiver.GetType().Name}'.  IsHealthy: {coreServiceHealthy}");


                    coreServiceHealthy = _resourceUtilizationMonitor.IsHealthy();
                    if (!coreServiceHealthy) {
                        unhealthyServices.Add(_resourceUtilizationMonitor.GetType().Name);
                    }

                    _logger.LogDebug($"Health check service: '{_resourceUtilizationMonitor.GetType().Name}'.  IsHealthy: {coreServiceHealthy}");

                    coreServiceHealthy = _heartbeatService.IsHealthy();
                    if (!coreServiceHealthy) {
                        unhealthyServices.Add(_heartbeatService.GetType().Name);
                    }

                    _logger.LogDebug($"Health check service: '{_heartbeatService.GetType().Name}'.  IsHealthy: {coreServiceHealthy}");

                    coreServiceHealthy = _pluginLoader.IsHealthy();
                    if (!coreServiceHealthy) {
                        unhealthyServices.Add(_pluginLoader.GetType().Name);
                    }

                    _logger.LogDebug($"Health check service: '{_pluginLoader.GetType().Name}'.  IsHealthy: {coreServiceHealthy}");



                    foreach (IMonitorableService service in monitorableServices) {
                        bool isHealthy = service.IsHealthy();
                        _logger.LogDebug($"Health check service: '{service.GetType().Name}'.  IsHealthy: {isHealthy}");
                        if (!isHealthy) {
                            unhealthyServices.Add(service.GetType().Name);
                        }
                    }

                    if (unhealthyServices.Any()) {
                        string unhealthServicesOutput = string.Join(",", unhealthyServices);
                        _logger.LogCritical($"Unhealthy services detected.  Services reporting unhealthy: {unhealthServicesOutput}");
                        _logger.LogCritical("Triggering application stop.");
                        _appLifetime.StopApplication();
                        // throw new RpcException(new Status(StatusCode.Unknown, $"Unhealthy services detected.  Services reporting unhealthy: {unhealthServicesOutput}"));
                        return Task.FromResult(HealthCheckResult.Unhealthy($"Unhealthy services detected.  Services reporting unhealthy: {unhealthServicesOutput}"));
                    }

                    _logger.LogDebug("All services report healthy.");
                }

                return Task.FromResult(HealthCheckResult.Healthy("Health check passed.  All services report healthy."));
            }
        }
    }
}
