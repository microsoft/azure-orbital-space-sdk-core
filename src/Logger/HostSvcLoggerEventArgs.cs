namespace Microsoft.Extensions.Logging.SpaceFX.Logger {
    public class HostSvcLoggerProviderEventArgs {
        public HostSvcLogger HostSvcLogger { get; }
        public HostSvcLoggerProviderEventArgs(HostSvcLogger logger) {
            HostSvcLogger = logger;
        }
    }
}