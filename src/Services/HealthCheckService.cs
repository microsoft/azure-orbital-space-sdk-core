
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Azure.SpaceFx;

public partial class Core {
    public partial class Services {
        /// <summary>
        /// Trigger a pulse to let other apps know that this app is only and ready to work
        /// </summary>
        public class HealthCheckService : IHealthCheck {
            private readonly ILogger<HealthCheckService> _logger;
            private readonly IServiceProvider _serviceProvider;
            private readonly IHostApplicationLifetime _appLifetime;

            private readonly IEnumerable<IHostedService> _hostedServices;

            public HealthCheckService(ILogger<HealthCheckService> logger, IServiceProvider serviceProvider, IHostApplicationLifetime appLifetime, IEnumerable<IHostedService> hostedServices) {
                _logger = logger;
                _hostedServices = hostedServices;
                _serviceProvider = serviceProvider;
                _appLifetime = appLifetime;


                _logger.LogInformation("Services.{serviceName} Initialized.", nameof(HealthCheckService));

            }

            public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
                var unhealthyServices = _hostedServices
                    .OfType<IMonitorableService>()
                    .Where(service => !service.IsHealthy())
                    .Select(service => service.GetType().Name)
                    .ToList();

                if (unhealthyServices.Any()) {
                    _logger.LogError($"The following background services are unhealthy: {string.Join(", ", unhealthyServices)}.  Triggering hard failure.", unhealthyServices);
                    throw new ApplicationException($"The following background services are unhealthy: {string.Join(", ", unhealthyServices)}");
                }



                return Task.FromResult(HealthCheckResult.Healthy("All background services are healthy."));
            }
        }
    }
}
