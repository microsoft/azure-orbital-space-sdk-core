namespace Microsoft.Azure.SpaceFx;

/// <summary>
/// Base class for plugins to initiate from
/// </summary>
public partial class Core {
    public interface IServiceBase {
        void ConfigureLogging(ILoggerFactory loggerFactory);
        Task<MessageFormats.Common.PluginHealthCheckResponse> PluginHealthCheckResponse();
        Task BackgroundTask();
    }

    public interface IMessageHandler<T> where T : notnull {
        void MessageReceived(T message, MessageFormats.Common.DirectToApp fullMessage);
    }
}