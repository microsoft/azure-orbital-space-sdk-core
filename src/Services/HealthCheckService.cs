
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Azure.SpaceFx;

public partial class Core {
    public partial class Services {
        public class HealthCheckService : AppCallbackHealthCheck.AppCallbackHealthCheckBase {
            private readonly ILogger<HealthCheckService> _logger;
            private readonly IServiceProvider _serviceProvider;
            private readonly IHostApplicationLifetime _appLifetime;
            private readonly IEnumerable<IHostedService> _hostedServices;
            private readonly Services.ResourceUtilizationMonitor _resourceUtilizationMonitor;
            private readonly Services.MessageReceiver _messageReceiver;
            private readonly Services.HeartbeatService _heartbeatService;
            private readonly Services.PluginLoader _pluginLoader;

            public HealthCheckService(ILogger<HealthCheckService> logger, IServiceProvider serviceProvider, IHostApplicationLifetime appLifetime, IEnumerable<IHostedService> hostedServices, Services.MessageReceiver messageReceiver, Services.HeartbeatService heartbeatService, Services.ResourceUtilizationMonitor resourceUtilizationMonitor, Services.PluginLoader pluginLoader) {
                _logger = logger;
                _hostedServices = hostedServices;
                _serviceProvider = serviceProvider;
                _appLifetime = appLifetime;
                _messageReceiver = messageReceiver;

                _resourceUtilizationMonitor = resourceUtilizationMonitor;
                _heartbeatService = heartbeatService;
                _pluginLoader = pluginLoader;

                _logger.LogInformation("Services.{serviceName} Initialized.", nameof(HealthCheckService));

            }

            /// <summary>
            /// Performs a health check on the services in the application.
            /// </summary>
            /// <param name="request">The request object.</param>
            /// <param name="context">The server call context.</param>
            /// <returns>A task representing the asynchronous operation. The task result contains the health check response.</returns>
            public override Task<HealthCheckResponse> HealthCheck(Empty request, ServerCallContext context) {

                using (var scope = _serviceProvider.CreateScope()) {
                    var monitorableServices = _serviceProvider.GetServices<IMonitorableService>().ToList();
                    List<string> unhealthyServices = new List<string>();
                    unhealthyServices.AddRange(monitorableServices.Where(service => !service.IsHealthy()).Select(service => service.GetType().Name));

                    if (unhealthyServices.Any()) {
                        _logger.LogCritical("Health check failed. Unhealthy services: {services}", string.Join(", ", unhealthyServices.Select(service => service.GetType().Name)));
                    }

                    if (!_heartbeatService.IsHealthy())
                        unhealthyServices.Add(nameof(Services.HeartbeatService));

                    if (!_messageReceiver.IsHealthy())
                        unhealthyServices.Add(nameof(Services.MessageReceiver));

                    if (!_resourceUtilizationMonitor.IsHealthy())
                        unhealthyServices.Add(nameof(Services.ResourceUtilizationMonitor));

                    if (unhealthyServices.Any()) {
                        _logger.LogCritical($"Health check failed. Unhealthy services detected.  Services failing health check: {string.Join(", ", unhealthyServices.Select(service => service.GetType().Name))}");
                        _appLifetime.StopApplication();
                        throw new RpcException(new Status(StatusCode.Unknown, $"Health check failed. Unhealthy services detected.  Services failing health check: {string.Join(", ", unhealthyServices.Select(service => service.GetType().Name))}"));
                    }

                    _logger.LogDebug("Health check passed.  All services report healthy.");
                }

                return Task.FromResult(new HealthCheckResponse());
            }


        }
    }
}
