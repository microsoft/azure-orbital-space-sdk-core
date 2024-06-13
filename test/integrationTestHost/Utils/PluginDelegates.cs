namespace Microsoft.Azure.SpaceFx.IntegrationTestHost;

public class Utils {
    public class PluginDelegates {
        private readonly ILogger<PluginDelegates> _logger;
        private readonly List<Microsoft.Azure.SpaceFx.Core.Models.PLUG_IN> _plugins;
        private readonly IServiceProvider _serviceProvider;
        public PluginDelegates(ILogger<PluginDelegates> logger, IServiceProvider serviceProvider) {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _plugins = _serviceProvider.GetService<List<Core.Models.PLUG_IN>>() ?? new List<Core.Models.PLUG_IN>(); ;
        }

        internal Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage? SimpleMessage((Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage? input_request, PluginBase plugin) input) {
            const string methodName = nameof(input.plugin.SimpleMessage);

            if (input.input_request is null || input.input_request is default(Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage)) {
                _logger.LogDebug("Plugin {pluginName} / {pluginMethod}: Received empty input.  Returning empty results", input.plugin.ToString(), methodName);
                return input.input_request;
            }
            _logger.LogDebug("Plugin {pluginMethod}: START", methodName);

            Task<Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage?> pluginTask;

            try {
                pluginTask = input.plugin.SimpleMessage(input_request: input.input_request);
                pluginTask.Wait();
                input.input_request = pluginTask.Result;
            } catch (Exception ex) {
                _logger.LogError("Plugin {pluginName} / {pluginMethod}: Error: {errorMessage}", input.plugin.ToString(), methodName, ex.Message);
            }

            _logger.LogDebug("Plugin {pluginName} / {pluginMethod}: END", input.plugin.ToString(), methodName);
            return input.input_request;
        }

        internal (Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage? output_request, Microsoft.Azure.SpaceFx.MessageFormats.Testing.ComplexMessage? output_response) SimpleComplexMessage((Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage? input_request, Microsoft.Azure.SpaceFx.MessageFormats.Testing.ComplexMessage? input_response, PluginBase plugin) input) {
            const string methodName = nameof(input.plugin.SimpleComplexMessage);
            if (input.input_request is null || input.input_request is default(Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage)) {
                _logger.LogDebug("Plugin {Plugin_Name} / {methodName}: Received empty input.  Returning empty results", input.plugin.ToString(), methodName);
                return (input.input_request, input.input_response);
            }
            this._logger.LogDebug("Plugin {Plugin_Name} / {methodName}: START", input.plugin.ToString(), methodName);

            Task<(Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage? output_request, Microsoft.Azure.SpaceFx.MessageFormats.Testing.ComplexMessage? output_response)> pluginTask;



            try {
                pluginTask = input.plugin.SimpleComplexMessage(input_request: input.input_request, input_response: input.input_response);
                pluginTask.Wait();

                input.input_request = pluginTask.Result.output_request;
                input.input_response = pluginTask.Result.output_response;
            } catch (Exception ex) {
                _logger.LogError("Error in plugin '{Plugin_Name}:{methodName}'.  Error: {errMsg}", input.plugin.ToString(), methodName, ex.Message);
            }


            _logger.LogDebug("Plugin {Plugin_Name} / {methodName}: END", input.plugin.ToString(), methodName);
            return (input.input_request, input.input_response);
        }
    }
}