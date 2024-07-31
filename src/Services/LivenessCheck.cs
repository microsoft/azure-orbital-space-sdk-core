
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

            public LivenessCheck(ILogger<LivenessCheck> logger, IServiceProvider serviceProvider, IHostApplicationLifetime appLifetime, Services.MessageReceiver messageReceiver, Services.HeartbeatService heartbeatService, Services.ResourceUtilizationMonitor resourceUtilizationMonitor) {
                _logger = logger;
                _serviceProvider = serviceProvider;
                _appLifetime = appLifetime;
                _messageReceiver = messageReceiver;

                _resourceUtilizationMonitor = resourceUtilizationMonitor;
                _heartbeatService = heartbeatService;

                _logger.LogInformation("Services.{serviceName} Initialized.", nameof(LivenessCheck));

            }

            public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
                // return Task.FromResult(HealthCheckResult.Unhealthy("The check indicates an unhealthy status."));

                using (var scope = _serviceProvider.CreateScope()) {
                    List<string> unhealthyServices = new List<string>();

                    var monitorableServices = _serviceProvider.GetServices<IMonitorableService>().ToList();

                    // Scan all the services utilizing the IMonitorableService interface and check if they are healthy
                    unhealthyServices.AddRange(monitorableServices.Where(service => !service.IsHealthy()).Select(service => service.GetType().Name));

                    if (!_heartbeatService.IsHealthy())
                        unhealthyServices.Add(_heartbeatService.GetType().Name);

                    if (!_messageReceiver.IsHealthy())
                        unhealthyServices.Add(_messageReceiver.GetType().Name);

                    if (!_resourceUtilizationMonitor.IsHealthy())
                        unhealthyServices.Add(_resourceUtilizationMonitor.GetType().Name);

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
