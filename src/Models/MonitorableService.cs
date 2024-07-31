namespace Microsoft.Azure.SpaceFx {
    public partial class Core {
        /// <summary>
        /// Interface for services that can be monitored for health status.
        /// </summary>
        public interface IMonitorableService {
            /// <summary>
            /// Gets a value indicating whether the service is healthy.  If false, will trigger a critical log and stop the application.
            /// </summary>
            /// <returns>True if the service is healthy; otherwise, false.</returns>
            bool IsHealthy();
        }
    }
}