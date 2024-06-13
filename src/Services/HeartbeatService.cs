
namespace Microsoft.Azure.SpaceFx;

public partial class Core {
    public partial class Services {
        /// <summary>
        /// Trigger a pulse to let other apps know that this app is only and ready to work
        /// </summary>
        public class HeartbeatService : IHostedService {
            private readonly ILogger<HeartbeatService> _logger;
            private readonly TimeSpan HeartBeatPulseTiming;
            private readonly TimeSpan HeartBeatHeardTolerance;
            private readonly Core.Client _client;
            private readonly IServiceProvider _serviceProvider;
            private readonly IHostApplicationLifetime _appLifetime;
            private readonly string _appId;
            private readonly string _appVersion;
            private readonly ConcurrentDictionary<string, MessageFormats.Common.HeartBeatPulse> _heartbeatsHeard;
            private readonly Core.APP_CONFIG _appConfig;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public HeartbeatService(ILogger<HeartbeatService> logger, IServiceProvider serviceProvider, Core.Client client, IHostApplicationLifetime appLifetime) {
                try {
                    _logger = logger;
                    _client = client;
                    _serviceProvider = serviceProvider;
                    _heartbeatsHeard = new ConcurrentDictionary<string, MessageFormats.Common.HeartBeatPulse>();
                    _appId = _client.GetAppID().Result;
                    _appConfig = _serviceProvider.GetService<Core.APP_CONFIG>() ?? new APP_CONFIG();
                    _appLifetime = appLifetime;
                    _appVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown";

                    HeartBeatPulseTiming = TimeSpan.FromMilliseconds(_appConfig.HEARTBEAT_PULSE_TIMING_MS);
                    HeartBeatHeardTolerance = TimeSpan.FromMilliseconds(_appConfig.HEARTBEAT_RECEIVED_TOLERANCE_MS);

                    _logger.LogInformation("Services.{serviceName} Initialized.  HeartBeatPulseTiming: {pulseTiming}   HeartBeatHeardTolerance: {pulseHeardTolerance} ", nameof(HeartbeatService), HeartBeatPulseTiming, HeartBeatHeardTolerance);
                } catch (Exception ex) {
                    logger.LogCritical("Failed to initialize Services.{serviceName}.  Error: {ex}.  Stack Trace: {stack}", nameof(MessageReceiver), ex.Message, ex.StackTrace);
                    appLifetime.StopApplication();
                }
            }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

            /// <summary>
            /// Store a heard heartbeat to the dictionary
            /// </summary>
            /// <returns>void</returns>
            internal void StoreServiceHeartbeat(MessageFormats.Common.HeartBeatPulse pulse) {
                using (var scope = _serviceProvider.CreateScope()) {
                    try {
                        _logger.LogTrace("Received heartbeat for {appId}", pulse.AppId);
                        _heartbeatsHeard.AddOrUpdate(pulse.AppId, pulse, (__, _) => pulse);

                        _logger.LogTrace("Heartbeat successfully stored for {appId}", pulse.AppId);
                    } catch (Exception ex) {
                        _logger.LogError("Failure storing heartbeat for '{appId}'.  Error: {errorMessage}", pulse.AppId, ex.Message);
                        _appLifetime.StopApplication();
                    }
                }
            }


            /// <summary>
            /// Get the heard heartbeats
            /// </summary>
            /// <returns>void</returns>
            internal List<MessageFormats.Common.HeartBeatPulse> RetrieveServiceHeartbeats() {
                List<MessageFormats.Common.HeartBeatPulse> returnList = new();
                DateTime heartbeatStaleTime = DateTime.UtcNow.Subtract(HeartBeatHeardTolerance);

                try {
                    _logger.LogTrace("Retrieving heartbeats from cache");
                    foreach (var _hearbeat in _heartbeatsHeard) {
                        if (_hearbeat.Value.CurrentSystemTime.ToDateTime().ToUniversalTime() >= heartbeatStaleTime) {
                            returnList.Add(_hearbeat.Value);
                        }
                    }
                    _logger.LogTrace("Successfully retrieved {count} heartbeat(s)", returnList.Count);
                } catch (Exception ex) {
                    _logger.LogError("Failure querying for service heartbeats.  Error: {errorMessage}", ex.Message);
                    returnList = new();
                }

                return returnList;
            }

            /// <summary>
            /// Cleanup any stale heartbeats heard
            /// </summary>
            /// <returns>void</returns>
            internal void RemoveStaleHeartbeatsFromCache() {
                DateTime heartbeatStaleTime = DateTime.UtcNow.Subtract(HeartBeatHeardTolerance);
                try {
                    var staleHeartbeats = _heartbeatsHeard.Where(kvp => kvp.Value.CurrentSystemTime.ToDateTime().ToUniversalTime() < heartbeatStaleTime).ToList();

                    foreach (var _staleHeartbeat in staleHeartbeats) {
                        _logger.LogTrace("Stale heartbeat detected for {appId}.  Removing from cache", _staleHeartbeat.Key);
                        _heartbeatsHeard.Remove(_staleHeartbeat.Key, out _);
                        _logger.LogTrace("Stale heartbeat successfully removed for {appId}.", _staleHeartbeat.Key);
                    }
                } catch (Exception ex) {
                    _logger.LogError("Failed to clean up stale heartbeats.  Error: {errorMessage}", ex.Message);
                }
            }

            public Task StartAsync(CancellationToken cancellationToken) {
                return Task.Run(async () => {
                    DateTime heartbeat_startTime = DateTime.UtcNow;

                    while (!cancellationToken.IsCancellationRequested) {
                        using (var scope = _serviceProvider.CreateScope()) {
                            try {
                                string id = Guid.NewGuid().ToString();

                                MessageFormats.Common.HeartBeatPulse pulse = new() {
                                    ResponseHeader = new() {
                                        TrackingId = id,
                                        CorrelationId = id,
                                        Status = MessageFormats.Common.StatusCodes.Healthy
                                    },
                                    AppId = _appId,
                                    CurrentSystemTime = Timestamp.FromDateTime(DateTime.UtcNow),
                                    AppStartTime = Timestamp.FromDateTime(heartbeat_startTime),
                                    PulseFrequencyMS = (int) HeartBeatPulseTiming.TotalMilliseconds,
                                    AppVersion = _appVersion
                                };

                                _logger.LogDebug("Heartbeat Pulse. Status: {status}.  Frequency {freq}", pulse.ResponseHeader.Status.ToString(), HeartBeatPulseTiming.TotalMilliseconds);
                                await _client.PublishMsg(nameof(MessageFormats.Common.Topics.HeartbeatPulse), pulse);
                                await _client.SendTelemetryMetric(metricName: "Heartbeat", metricValue: 1);
                            } catch (Exception e) {
                                //UnWrap aggregate exceptions
                                if (e is AggregateException aggregateException) {
                                    foreach (var innerException in aggregateException.Flatten().InnerExceptions) {
                                        _logger.LogError(innerException, "Exception in Heartbeat Service background tasks.");
                                    }
                                } else {
                                    _logger.LogError(e, "Exception in Heartbeat Service");
                                }
                                _appLifetime.StopApplication();
                            }
                        }
                        await Task.Delay(HeartBeatPulseTiming);
                    }
                });

            }

            public Task StopAsync(CancellationToken cancellationToken) {
                throw new NotImplementedException();
            }
        }
    }
}
