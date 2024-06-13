namespace Microsoft.Azure.SpaceFx.IntegrationTestHost;

public class MessageHandler<T> : Core.IMessageHandler<T> where T : notnull {
    private readonly ILogger<MessageHandler<T>> _logger;
    private readonly Utils.PluginDelegates _pluginDelegates;
    private readonly Core.Services.PluginLoader _pluginLoader;
    private readonly IServiceProvider _serviceProvider;
    public MessageHandler(ILogger<MessageHandler<T>> logger, Utils.PluginDelegates pluginDelegates, Core.Services.PluginLoader pluginLoader, IServiceProvider serviceProvider) {
        _logger = logger;
        _pluginDelegates = pluginDelegates;
        _pluginLoader = pluginLoader;
        _serviceProvider = serviceProvider;
    }

    public void MessageReceived(T message, MessageFormats.Common.DirectToApp fullMessage) {
        using (var scope = _serviceProvider.CreateScope()) {
            _logger.LogInformation($"Found {typeof(T).Name}");

            switch (typeof(T).Name) {
                case string messageType when messageType.Equals(typeof(MessageFormats.Testing.SimpleMessage).Name, StringComparison.CurrentCultureIgnoreCase):
                    SimpleMessageHandler(input_request: message as MessageFormats.Testing.SimpleMessage);
                    break;
            }
        }
    }

    private void SimpleMessageHandler(MessageFormats.Testing.SimpleMessage? input_request) {
        if (input_request == null) return;
        _logger.LogInformation($"Doing something with value: {input_request.Message}");

        MessageFormats.Testing.SimpleMessage? pluginResult =
                        _pluginLoader.CallPlugins<MessageFormats.Testing.SimpleMessage?, PluginBase>(
                            orig_request: input_request,
                            pluginDelegate: _pluginDelegates.SimpleMessage);


        (MessageFormats.Testing.SimpleMessage? output_request, MessageFormats.Testing.ComplexMessage? output_response) =
                                _pluginLoader.CallPlugins<MessageFormats.Testing.SimpleMessage?, PluginBase, MessageFormats.Testing.ComplexMessage>(
                                    orig_request: input_request, orig_response: new MessageFormats.Testing.ComplexMessage(),
                                    pluginDelegate: _pluginDelegates.SimpleComplexMessage);

    }
}