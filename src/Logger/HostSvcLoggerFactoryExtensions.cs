namespace Microsoft.Extensions.Logging.SpaceFX.Logger {
    public static class HostSvcLoggerFactoryExtensions {
        public static ILoggerFactory AddCustomLogger(
            this ILoggerFactory factory, out HostSvcLoggerProvider logProvider) {
            logProvider = new HostSvcLoggerProvider();
            factory.AddProvider(logProvider);
            return factory;
        }
    }
}