using Microsoft.Azure.SpaceFx;

namespace Microsoft.Extensions.Logging.SpaceFX.Logger {
    public class HostSvcLogger : ILogger {
        private readonly string LOGGING_SVC_APP_ID = "hostsvc-logging";
        private readonly string _categoryName;

        public HostSvcLogger(string categoryName) {
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull {

            // Create a new logging scope with the given state
            var scope = this;

            // Set the state of the logging scope
            scope.Log(LogLevel.Information, new EventId(), state, null, formatter: (s, e) => s.ToString() ?? string.Empty);

            // Return the logging scope as an IDisposable object
            return new DisposableScope(scope);
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
            if (!IsEnabled(logLevel)) {
                return;
            }

            if (formatter == null) return;

            if (!Core.IsOnline()) return; // Client is not online - return cleanly

            Task.Run(async () => {
                // Prevent hostsvc-logging from retransmitting it's log messages.  Otherwise it'll cause a race condition
                if (string.Equals(LOGGING_SVC_APP_ID, await Core.GetAppID(), StringComparison.CurrentCultureIgnoreCase)) return;

                string message = formatter(state, exception);
                string trackingId = Guid.NewGuid().ToString();

                Microsoft.Azure.SpaceFx.MessageFormats.Common.LogMessage logMessage = new() {
                    RequestHeader = new() {
                        TrackingId = trackingId,
                        CorrelationId = trackingId
                    },
                    Message = message,
                    Priority = Microsoft.Azure.SpaceFx.MessageFormats.Common.Priority.Low,
                    SubCategory = _categoryName,
                    Category = "MSFTSpaceFxLoggingModule",
                    LogLevel = logLevel switch {
                        LogLevel.Trace => Microsoft.Azure.SpaceFx.MessageFormats.Common.LogMessage.Types.LOG_LEVEL.Trace,
                        LogLevel.Critical => Microsoft.Azure.SpaceFx.MessageFormats.Common.LogMessage.Types.LOG_LEVEL.Critical,
                        LogLevel.Debug => Microsoft.Azure.SpaceFx.MessageFormats.Common.LogMessage.Types.LOG_LEVEL.Debug,
                        LogLevel.Error => Microsoft.Azure.SpaceFx.MessageFormats.Common.LogMessage.Types.LOG_LEVEL.Error,
                        LogLevel.Warning => Microsoft.Azure.SpaceFx.MessageFormats.Common.LogMessage.Types.LOG_LEVEL.Warning,
                        _ => Microsoft.Azure.SpaceFx.MessageFormats.Common.LogMessage.Types.LOG_LEVEL.Info,
                    },
                    LogTime = Timestamp.FromDateTime(DateTime.UtcNow),
                };

                await Core.DirectToApp(appId: LOGGING_SVC_APP_ID, message: logMessage);

            });

        }

        private class DisposableScope : IDisposable {
            private readonly ILogger _logger;

            public DisposableScope(ILogger logger) {
                _logger = logger;
            }

            public void Dispose() {
                // End the logging scope
            }
        }
    }


}
