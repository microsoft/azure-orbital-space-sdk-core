namespace Microsoft.Azure.SpaceFx;

/// <summary>
/// Base class for plugins to initiate from
/// </summary>
public partial class Core {
    public interface IPluginBase {
        void ConfigureLogging(ILoggerFactory loggerFactory);
        Task<MessageFormats.Common.PluginHealthCheckResponse> PluginHealthCheckResponse();
        Task BackgroundTask();
    }
}