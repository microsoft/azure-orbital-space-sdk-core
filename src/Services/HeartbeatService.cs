
namespace Microsoft.Azure.SpaceFx;

public partial class Core {
    public partial class Services {
        /// <summary>
        /// Trigger a pulse to let other apps know that this app is only and ready to work
        /// </summary>
        public class HeartbeatService : IHostedService, Core.IMonitorableService {
            private readonly ILogger<HeartbeatService> _logger;
            private readonly TimeSpan HeartBeatPulseTiming;
            private readonly TimeSpan _heartBeatHeardTolerance;
            private readonly Core.Client _client;
            private readonly IServiceProvider _serviceProvider;
            private readonly IHostApplicationLifetime _appLifetime;
            private readonly string _appId;
            private readonly string _appVersion;
            private readonly ConcurrentDictionary<string, MessageFormats.Common.HeartBeatPulse> _heartbeatsHeard;
            private readonly Core.APP_CONFIG _appConfig;
            private readonly DateTime _appStartTime;
            public bool IsHealthy() {
                if (_heartbeatsHeard.IsEmpty && DateTime.UtcNow > _appStartTime.Add(_heartBeatHeardTolerance * 2)) {
                    // Log a critical error and return a false value to indicate an unhealthy state.
                    _logger.LogCritical("No heartbeats have been heard in the last {tolerance}. Returning unhealthy", _heartBeatHeardTolerance);
                    return false;
                }

                return true;
            }

            public HeartbeatService(ILogger<HeartbeatService> logger, IServiceProvider serviceProvider, Core.Client client, IHostApplicationLifetime appLifetime) {
                _appStartTime = DateTime.UtcNow;
                _logger = logger;
                _client = client;
                _serviceProvider = serviceProvider;
                _heartbeatsHeard = new ConcurrentDictionary<string, MessageFormats.Common.HeartBeatPulse>();
                _appId = _client.GetAppID().Result;
                _appConfig = _serviceProvider.GetService<Core.APP_CONFIG>() ?? new APP_CONFIG();
                _appLifetime = appLifetime;
                _appVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown";

                HeartBeatPulseTiming = TimeSpan.FromMilliseconds(_appConfig.HEARTBEAT_PULSE_TIMING_MS);
                _heartBeatHeardTolerance = TimeSpan.FromMilliseconds(_appConfig.HEARTBEAT_RECEIVED_TOLERANCE_MS);

                _logger.LogInformation("Services.{serviceName} Initialized.  HeartBeatPulseTiming: {pulseTiming}   HeartBeatHeardTolerance: {pulseHeardTolerance} ", nameof(HeartbeatService), HeartBeatPulseTiming, _heartBeatHeardTolerance);

            }


            /// <summary>
            /// Stores a received heartbeat signal into a concurrent dictionary. If the heartbeat for the given appId already exists,
            /// it updates the existing entry with the new heartbeat signal. This method is critical for monitoring the health and
            /// availability of services by tracking their heartbeat signals.
            /// </summary>
            /// <param name="pulse">The heartbeat signal to be stored, encapsulated in a HeartBeatPulse object. This object contains
            /// the appId of the service sending the heartbeat and the timestamp of the heartbeat, among other potential metadata.</param>
            /// <remarks>
            /// This method uses a scoped service provider to create a new scope for dependency injection, ensuring that any
            /// scoped services used within the method are properly disposed of once the operation completes.
            ///
            /// In case of an exception during the storage operation, the method logs the error and stops the application by
            /// calling StopApplication on the application lifetime service. This behavior indicates that failing to store a
            /// heartbeat is considered a critical error that requires immediate attention.
            /// </remarks>
            /// <exception cref="Exception">Throws an exception if there is a failure in storing the heartbeat signal. The application
            /// is stopped in response to such exceptions.</exception>
            internal void StoreServiceHeartbeat(MessageFormats.Common.HeartBeatPulse pulse) {
                // Create a new scope for dependency injection to ensure scoped services are properly disposed of after use.
                using (var scope = _serviceProvider.CreateScope()) {
                    try {
                        // Log the receipt of a heartbeat for a specific appId.
                        _logger.LogTrace("Received heartbeat for {appId}", pulse.AppId);
                        // Add or update the heartbeat in the concurrent dictionary. If the appId already exists, update its value with the new pulse.
                        _heartbeatsHeard.AddOrUpdate(pulse.AppId, pulse, (__, _) => pulse);
                        // Log the successful storage of the heartbeat for the appId.
                        _logger.LogTrace("Heartbeat successfully stored for {appId}", pulse.AppId);
                    } catch (Exception ex) {
                        // Log an error if there's an exception during the storage process and stop the application.
                        _logger.LogError(ex, "Failure storing heartbeat for '{appId}'.", pulse.AppId);
                        _appLifetime.StopApplication();
                    }
                }
            }

            /// <summary>
            /// Retrieves a list of service heartbeats that are considered active based on a predefined tolerance period.
            /// This method filters out heartbeats that are older than the tolerance period, returning only the recent ones.
            /// </summary>
            /// <returns>A list of <see cref="MessageFormats.Common.HeartBeatPulse"/> objects representing the active heartbeats.</returns>
            /// <remarks>
            /// The method calculates the stale time for heartbeats by subtracting the tolerance period from the current UTC time.
            /// Only heartbeats with a timestamp greater than or equal to this calculated stale time are considered active and returned.
            ///
            /// In case of an exception, such as issues accessing the heartbeat storage, the method logs the error and returns an empty list,
            /// indicating no active heartbeats could be retrieved.
            /// </remarks>
            /// <exception cref="Exception">Catches and logs exceptions related to querying heartbeats but does not rethrow them, ensuring
            /// the method always returns a list (which may be empty in case of errors).</exception>
            internal List<MessageFormats.Common.HeartBeatPulse> RetrieveServiceHeartbeats() {
                try {
                    // Calculate the stale time for heartbeats based on the tolerance period.
                    DateTime heartbeatStaleTime = DateTime.UtcNow.Subtract(_heartBeatHeardTolerance);
                    // Return a list of heartbeats that are newer than the calculated stale time.
                    return _heartbeatsHeard.Values.Where(p => p.CurrentSystemTime.ToDateTime().ToUniversalTime() >= heartbeatStaleTime).ToList();
                } catch (Exception ex) {
                    // Log an error if there's a failure querying for service heartbeats, then return an empty list.
                    _logger.LogError("Failure querying for service heartbeats. Error: {errorMessage}", ex.Message);
                    return new List<MessageFormats.Common.HeartBeatPulse>();
                }
            }

            /// <summary>
            /// Removes stale heartbeats from the cache based on a predefined tolerance period. If no heartbeats are detected within
            /// this period, a critical log is generated, and an exception is thrown to potentially trigger a restart of the service.
            /// </summary>
            /// <remarks>
            /// This method calculates the stale time for heartbeats by subtracting the heartbeat tolerance period from the current UTC time.
            /// It then filters out and removes any heartbeats that are older than this calculated stale time from the cache.
            ///
            /// If, after the cleanup, the cache is empty and the current time exceeds the application start time by at least the tolerance period,
            /// it logs a critical error and throws an ApplicationException. This behavior is intended to trigger a restart mechanism in environments
            /// like Kubernetes, where a pod can be restarted upon application failure.
            ///
            /// Exceptions thrown within this method are logged and rethrown, indicating a severe issue that requires immediate attention.
            /// </remarks>
            /// <exception cref="ApplicationException">Thrown when no heartbeats have been heard within the tolerance period, indicating
            /// a potential failure in the heartbeat mechanism.</exception>
            /// <exception cref="Exception">General exceptions are caught, logged, and rethrown, indicating issues with the cleanup process.</exception>
            internal void RemoveStaleHeartbeatsFromCache() {
                try {
                    // Calculate the stale time for heartbeats based on the tolerance period.
                    DateTime heartbeatStaleTime = DateTime.UtcNow.Subtract(_heartBeatHeardTolerance);
                    // Filter out heartbeats that are older than the calculated stale time.
                    var staleHeartbeats = _heartbeatsHeard.Where(kvp => kvp.Value.CurrentSystemTime.ToDateTime().ToUniversalTime() < heartbeatStaleTime).ToList();

                    // Log the number of stale heartbeats identified for removal.
                    _logger.LogTrace("Removing {Count} stale heartbeats from cache.", staleHeartbeats.Count);
                    // Remove each stale heartbeat from the cache.
                    foreach (var _staleHeartbeat in staleHeartbeats) {
                        _heartbeatsHeard.TryRemove(_staleHeartbeat.Key, out _);
                    }
                    // Log successful removal of stale heartbeats.
                    _logger.LogTrace("All stale heartbeats successfully removed.");

                    // Check if the cache is empty and the current time exceeds the app start time by the tolerance period.
                    if (_heartbeatsHeard.IsEmpty && DateTime.UtcNow > _appStartTime.Add(_heartBeatHeardTolerance * 2)) {
                        // Log a critical error and throw an exception to potentially trigger a service restart.
                        _logger.LogCritical("No heartbeats have been heard in the last {tolerance}. Triggering an exception to restart the pod.", _heartBeatHeardTolerance);
                        throw new ApplicationException($"No heartbeats have been heard in the last {_heartBeatHeardTolerance}. Triggering an exception to restart the pod.");
                    }
                } catch (Exception ex) {
                    // Log any exceptions that occur during the process and rethrow to handle them accordingly.
                    _logger.LogError(ex, "Exception while removing stale heartbeats from cache.");
                    throw;
                }
            }

            /// <summary>
            /// Starts the asynchronous heartbeat service that periodically sends heartbeat messages until a cancellation is requested.
            /// This service creates a new heartbeat message with a unique ID, application information, and sends it to a specified topic.
            /// Additionally, it sends a telemetry metric indicating a heartbeat event.
            /// </summary>
            /// <param name="cancellationToken">A token to observe while waiting for the task to complete. It triggers cancellation of the heartbeat loop.</param>
            /// <returns>A task representing the asynchronous operation of the heartbeat service.</returns>
            /// <remarks>
            /// The method runs in a loop until the cancellation token is triggered. Each iteration creates a new scope for dependency injection,
            /// generates a new heartbeat message with a unique ID, and sends this message to a messaging client. If an exception occurs during
            /// the message sending process, the exception is logged, and the application is stopped.
            ///
            /// This method uses Task.Run to ensure the loop runs on a background thread, allowing the service to send heartbeat messages
            /// asynchronously without blocking the main thread.
            ///
            /// In case of an AggregateException, it unwraps and logs each inner exception separately. For other exceptions, it logs the error
            /// and stops the application to handle the failure.
            /// </remarks>
            /// <exception cref="Exception">Logs and handles exceptions that occur during the heartbeat message creation or sending process.</exception>
            public Task StartAsync(CancellationToken cancellationToken) {
                // Run the heartbeat service in a separate background task to avoid blocking the main thread.
                return Task.Run(async () => {
                    // Continue sending heartbeats until a cancellation request is received.
                    while (!cancellationToken.IsCancellationRequested) {
                        // Create a new scope for dependency injection for each heartbeat cycle.
                        using (var scope = _serviceProvider.CreateScope()) {
                            try {
                                // Generate a unique identifier for this heartbeat instance.
                                string id = Guid.NewGuid().ToString();

                                // Create a new heartbeat pulse message with relevant information.
                                MessageFormats.Common.HeartBeatPulse pulse = new() {
                                    ResponseHeader = new() {
                                        TrackingId = id,
                                        CorrelationId = id,
                                        Status = MessageFormats.Common.StatusCodes.Healthy
                                    },
                                    AppId = _appId,
                                    CurrentSystemTime = Timestamp.FromDateTime(DateTime.UtcNow),
                                    AppStartTime = Timestamp.FromDateTime(_appStartTime),
                                    PulseFrequencyMS = (int) HeartBeatPulseTiming.TotalMilliseconds,
                                    AppVersion = _appVersion
                                };

                                // Log the transmission of the heartbeat pulse.
                                _logger.LogDebug("Transmitting heartbeat Pulse. Status: {status}.  Frequency {freq}", pulse.ResponseHeader.Status.ToString(), HeartBeatPulseTiming.TotalMilliseconds);
                                // Publish the heartbeat message to the specified topic.
                                await _client.PublishMsg(nameof(MessageFormats.Common.Topics.HeartbeatPulse), pulse);
                                // Send a telemetry metric indicating a heartbeat event.
                                await _client.SendTelemetryMetric(metricName: "Heartbeat", metricValue: 1);
                            } catch (Exception e) {
                                // Handle exceptions, specifically unwrapping and logging AggregateExceptions.
                                if (e is AggregateException aggregateException) {
                                    foreach (var innerException in aggregateException.Flatten().InnerExceptions) {
                                        _logger.LogError(innerException, "Exception in Heartbeat Service background tasks.");
                                    }
                                } else {
                                    // Log non-AggregateExceptions.
                                    _logger.LogError(e, "Exception in Heartbeat Service");
                                }
                                // Stop the application in case of an exception, indicating a critical failure.
                                _appLifetime.StopApplication();
                            }
                        }
                        // Wait for the specified pulse frequency before sending the next heartbeat.
                        await Task.Delay(HeartBeatPulseTiming);
                    }
                });
            }

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }
}
