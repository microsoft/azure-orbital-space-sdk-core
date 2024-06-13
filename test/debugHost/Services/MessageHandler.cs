
namespace PayloadApp.DebugHost;

public class MessageHandler<T> : Microsoft.Azure.SpaceFx.Core.IMessageHandler<T> where T : notnull {
    private readonly ILogger<MessageHandler<T>> _logger;
    private readonly Utils.PluginDelegates _pluginDelegates;
    private readonly Microsoft.Azure.SpaceFx.Core.Services.PluginLoader _pluginLoader;
    private readonly IServiceProvider _serviceProvider;
    public MessageHandler(ILogger<MessageHandler<T>> logger, Utils.PluginDelegates pluginDelegates, Microsoft.Azure.SpaceFx.Core.Services.PluginLoader pluginLoader, IServiceProvider serviceProvider) {
        _logger = logger;
        _pluginDelegates = pluginDelegates;
        _pluginLoader = pluginLoader;
        _serviceProvider = serviceProvider;
    }

    public void MessageReceived(T message, DirectToApp fullMessage) {
        using (var scope = _serviceProvider.CreateScope()) {
            _logger.LogInformation($"Found {typeof(T).Name}");

            switch (typeof(T).Name) {
                case string messageType when messageType.Equals(typeof(SimpleMessage).Name, StringComparison.CurrentCultureIgnoreCase):
                    SimpleMessageHandler(input_request: message as SimpleMessage);
                    break;
            }
        }
    }

    private void SimpleMessageHandler(SimpleMessage? input_request) {
        if (input_request == null) return;
        _logger.LogInformation($"Doing something with value: {input_request.Message}");

        SimpleMessage? pluginResult =
                        _pluginLoader.CallPlugins<SimpleMessage?, PluginBase>(
                            orig_request: input_request,
                            pluginDelegate: _pluginDelegates.SimpleMessage);


        (SimpleMessage? output_request, ComplexMessage? output_response) =
                                _pluginLoader.CallPlugins<SimpleMessage?, PluginBase, ComplexMessage>(
                                    orig_request: input_request, orig_response: new ComplexMessage(),
                                    pluginDelegate: _pluginDelegates.SimpleComplexMessage);

    }
}