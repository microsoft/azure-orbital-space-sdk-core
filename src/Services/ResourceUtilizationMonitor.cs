using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Azure.SpaceFx.MessageFormats.Common;
using OpenTelemetry.Metrics;

namespace Microsoft.Azure.SpaceFx;

public partial class Core {
    public partial class Services {
        /// <summary>
        /// Trigger a pulse to let other apps know that this app is only and ready to work
        /// </summary>
        public class ResourceUtilizationMonitor : IHostedService {
            private readonly ILogger<ResourceUtilizationMonitor> _logger;
            private IServiceProvider _serviceProvider;
            private readonly Services.HeartbeatService _heartbeatService;
            private readonly Core.Client _client;
            private readonly APP_CONFIG _appConfig;
            private DateTime _lastProcessorMonitorTime;
            private TimeSpan _lastProcessorTime;
            private readonly IHostApplicationLifetime _appLifetime;

            public ResourceUtilizationMonitor(ILogger<ResourceUtilizationMonitor> logger, IServiceProvider serviceProvider, Services.HeartbeatService heartbeatService, Core.Client client, IHostApplicationLifetime appLifetime) {
                _logger = logger;
                _logger.LogTrace("Services.{serviceName} Initialized", nameof(ResourceUtilizationMonitor));
                _serviceProvider = serviceProvider;
                _heartbeatService = heartbeatService;
                _client = client;
                _appLifetime = appLifetime;
                _appConfig = _serviceProvider.GetService<Core.APP_CONFIG>() ?? new APP_CONFIG();
                _lastProcessorMonitorTime = DateTime.MinValue;
                _lastProcessorTime = TimeSpan.MinValue;
            }

            public Task StartAsync(CancellationToken cancellationToken) {
                return Task.Run(async () => {
                    // Set the next garbage collection time
                    DateTime GarbageCollectionTime = DateTime.Now.Add(TimeSpan.FromMilliseconds(_appConfig.RESOURCE_SCAVENGER_TIMING_MS));

                    // Run until cancellation is requested
                    while (!cancellationToken.IsCancellationRequested) {
                        using (var scope = _serviceProvider.CreateScope()) {
                            try {
                                // If resource monitoring is enabled
                                if (_appConfig.RESOURCE_MONITOR_ENABLED) {
                                    using (Process currentDiagnostics = Process.GetCurrentProcess()) {
                                        // Generate a unique ID for this monitoring cycle
                                        string id = Guid.NewGuid().ToString();

                                        // Get the current time and processor time
                                        DateTime currentMonitorTime = DateTime.UtcNow;
                                        TimeSpan currentProcessorTime = currentDiagnostics.TotalProcessorTime;

                                        // Initialize a new telemetry multi message
                                        TelemetryMultiMetric telemetryMultiMsg = new() {
                                            RequestHeader = new() {
                                                TrackingId = id,
                                                CorrelationId = id,
                                            }
                                        };

                                        // If this is the first run, so we don't have any CPU stats yet.  Set the values so we can calculate it on the next run
                                        if (_lastProcessorTime == TimeSpan.MinValue || _lastProcessorMonitorTime == DateTime.MinValue) {
                                            _lastProcessorTime = currentDiagnostics.TotalProcessorTime;
                                            _lastProcessorMonitorTime = DateTime.UtcNow;
                                        } else {
                                            // Calculate CPU usage by comparing this pass to the previous pass.
                                            double cpuUsage = Math.Round(((currentProcessorTime - _lastProcessorTime).TotalMilliseconds / (currentMonitorTime - _lastProcessorMonitorTime).TotalMilliseconds / Environment.ProcessorCount * 100), 2);
                                            double cpuUsageMillicores = Math.Round(((currentProcessorTime - _lastProcessorTime).TotalMilliseconds / (currentMonitorTime - _lastProcessorMonitorTime).TotalMilliseconds / Environment.ProcessorCount * 1000), 2);

                                            // Add CPU stats to telemetry message
                                            telemetryMultiMsg.TelemetryMetrics.Add(CreateTelemetryMetric(trackingId: telemetryMultiMsg.RequestHeader.TrackingId, metricName: $"{nameof(ResourceUtilizationMonitor)}-Processor_TotalUsage_Percent", metricValue: (int) cpuUsage));
                                            telemetryMultiMsg.TelemetryMetrics.Add(CreateTelemetryMetric(trackingId: telemetryMultiMsg.RequestHeader.TrackingId, metricName: $"{nameof(ResourceUtilizationMonitor)}-Processor_TotalUsage_Millicores", metricValue: (int) cpuUsageMillicores));
                                        }

                                        // Add memory usage metrics to telemetry message
                                        telemetryMultiMsg.TelemetryMetrics.Add(CreateTelemetryMetric(trackingId: telemetryMultiMsg.RequestHeader.TrackingId, metricName: $"{nameof(ResourceUtilizationMonitor)}-PagedMemorySize64", metricValue: (int) currentDiagnostics.PagedMemorySize64));
                                        telemetryMultiMsg.TelemetryMetrics.Add(CreateTelemetryMetric(trackingId: telemetryMultiMsg.RequestHeader.TrackingId, metricName: $"{nameof(ResourceUtilizationMonitor)}-NonpagedSystemMemorySize64", metricValue: (int) currentDiagnostics.NonpagedSystemMemorySize64));
                                        telemetryMultiMsg.TelemetryMetrics.Add(CreateTelemetryMetric(trackingId: telemetryMultiMsg.RequestHeader.TrackingId, metricName: $"{nameof(ResourceUtilizationMonitor)}-PrivateMemorySize64", metricValue: (int) currentDiagnostics.PrivateMemorySize64));
                                        telemetryMultiMsg.TelemetryMetrics.Add(CreateTelemetryMetric(trackingId: telemetryMultiMsg.RequestHeader.TrackingId, metricName: $"{nameof(ResourceUtilizationMonitor)}-VirtualMemorySize64", metricValue: (int) currentDiagnostics.VirtualMemorySize64));

                                        // Update last processor time and monitor time
                                        _lastProcessorMonitorTime = currentMonitorTime;
                                        _lastProcessorTime = currentProcessorTime;

                                        // Send telemetry message
                                        await DirectToApp($"hostsvc-{nameof(MessageFormats.Common.HostServices.Logging)}".ToLower(), message: telemetryMultiMsg);
                                    }
                                }

                                // If resource scavenger is enabled and it's time for garbage collection
                                if (_appConfig.RESOURCE_SCAVENGER_ENABLED && DateTime.Now >= GarbageCollectionTime) {
                                    _logger.LogTrace("Triggering Garbage Collection");
                                    GC.Collect();
                                    GC.WaitForPendingFinalizers();
                                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                                    GarbageCollectionTime = DateTime.Now.Add(TimeSpan.FromMilliseconds(_appConfig.RESOURCE_SCAVENGER_TIMING_MS));
                                }

                                // Trigger cleanup of expired heartbeats and cache items
                                _logger.LogTrace("Trigger HeartBeat Expiration cleanup");
                                _heartbeatService.RemoveStaleHeartbeatsFromCache();

                                _logger.LogTrace("Trigger Cache Items Expiration cleanup");
                                await _client.ClearExpiredCacheItems();
                            } catch (Exception e) {
                                // Log exceptions
                                if (e is AggregateException aggregateException) {
                                    foreach (var innerException in aggregateException.Flatten().InnerExceptions) {
                                        _logger.LogError(innerException, "Exception in Resource Utilization Monitor background tasks.");
                                    }
                                } else {
                                    _logger.LogError(e, "Exception in Resource Utilization Monitor");
                                }
                            }
                        }
                        // Wait for the configured interval before the next iteration
                        await Task.Delay(_appConfig.RESOURCE_MONITOR_TIMING_MS);
                    }
                });
            }

            private MessageFormats.Common.TelemetryMetric CreateTelemetryMetric(string trackingId, string metricName, int metricValue) {
                return new MessageFormats.Common.TelemetryMetric() {
                    RequestHeader = new() {
                        TrackingId = trackingId,
                        CorrelationId = trackingId,
                    },
                    MetricName = metricName,
                    MetricValue = metricValue,
                    MetricTime = Timestamp.FromDateTime(DateTime.UtcNow)
                };

            }

            public Task StopAsync(CancellationToken cancellationToken) {
                return Task.CompletedTask;
            }
        }
    }
}
