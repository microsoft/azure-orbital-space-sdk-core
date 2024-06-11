namespace PayloadApp.DebugHost;
public abstract class PluginBase : Microsoft.Azure.SpaceFx.Core.IPluginBase, IPluginBase {
    public abstract ILogger Logger { get; set; }
    public abstract Task BackgroundTask();
    public abstract void ConfigureLogging(ILoggerFactory loggerFactory);
    public abstract Task<PluginHealthCheckResponse> PluginHealthCheckResponse();
    public abstract Task<Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage?> SimpleMessage(Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage? input_request);
    public abstract Task<(Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage?, Microsoft.Azure.SpaceFx.MessageFormats.Testing.ComplexMessage?)> SimpleComplexMessage(Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage? input_request, Microsoft.Azure.SpaceFx.MessageFormats.Testing.ComplexMessage? input_response);
}

public interface IPluginBase {
    Task<Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage?> SimpleMessage(Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage? input_request);
    Task<(Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage?, Microsoft.Azure.SpaceFx.MessageFormats.Testing.ComplexMessage?)> SimpleComplexMessage(Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage? input_request, Microsoft.Azure.SpaceFx.MessageFormats.Testing.ComplexMessage? input_response);

}