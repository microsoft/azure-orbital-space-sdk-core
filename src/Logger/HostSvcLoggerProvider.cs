namespace Microsoft.Extensions.Logging.SpaceFX.Logger {
    public class HostSvcLoggerProvider : ILoggerProvider {

        public HostSvcLoggerProvider() { }
        public HostSvcLoggerProvider(EventHandler<HostSvcLoggerProviderEventArgs> onCreateLogger) {
            OnCreateLogger = onCreateLogger;
        }
        public ConcurrentDictionary<string, HostSvcLogger> Loggers { get; set; } = new ConcurrentDictionary<string, HostSvcLogger>();

        public ILogger CreateLogger(string categoryName) {
            HostSvcLogger hostsvcLogger = Loggers.GetOrAdd(categoryName, new HostSvcLogger(categoryName));
            OnCreateLogger?.Invoke(this, new HostSvcLoggerProviderEventArgs(hostsvcLogger));
            return hostsvcLogger;
        }

        public void Dispose() {
            GC.SuppressFinalize(this);
        }

        public event EventHandler<HostSvcLoggerProviderEventArgs> OnCreateLogger = delegate { };
    }
}
