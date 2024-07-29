namespace Microsoft.Azure.SpaceFx;

public partial class Core {
    public partial class Services {
        /// <summary>
        /// Trigger a pulse to let other apps know that this app is only and ready to work
        /// </summary>
        public class PluginLoader : IHostedService {
            private readonly ILogger<PluginLoader> _logger;
            private readonly IServiceProvider _serviceProvider;
            private readonly List<Models.PLUG_IN> _plugins;
            private readonly List<Core.IPluginBase> _loadedPlugins;
            private readonly IHostApplicationLifetime _appLifetime;
            private readonly TimeSpan HeartBeatPulseTiming;
            private readonly Core.APP_CONFIG _appConfig;
            private List<System.Type> _pluginTypes;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public PluginLoader(ILogger<PluginLoader> logger, IServiceProvider serviceProvider, IHostApplicationLifetime appLifetime) {
                try {
                    _logger = logger;
                    _serviceProvider = serviceProvider;

                    _plugins = new List<Models.PLUG_IN>();
                    _loadedPlugins = new List<IPluginBase>();
                    _pluginTypes = new List<System.Type>();
                    _appLifetime = appLifetime;
                    _appConfig = _serviceProvider.GetService<Core.APP_CONFIG>() ?? new APP_CONFIG();
                    HeartBeatPulseTiming = TimeSpan.FromMilliseconds(_appConfig.HEARTBEAT_PULSE_TIMING_MS);
                    _logger.LogInformation("Services.{serviceName} Initialized", nameof(PluginLoader));
                } catch (Exception ex) {
                    logger.LogCritical("Failed to initialize Services.{serviceName}.  Error: {ex}.  Stack Trace: {stack}", nameof(PluginLoader), ex.Message, ex.StackTrace);
                    appLifetime.StopApplication();
                }

            }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

            /// <summary>
            /// Returns all the currently loaded plugins
            /// </summary>
            /// <returns></returns>
            public List<Models.PLUG_IN> RetrievePlugins() {
                return _plugins;
            }

            public Task StartAsync(CancellationToken cancellationToken) {
                return Task.Run(async () => {
                    while (!cancellationToken.IsCancellationRequested) {
                        using (var scope = _serviceProvider.CreateScope()) {
                            try {
                                ScanAndLoadPlugins();
                            } catch (Exception e) {
                                _logger.LogError(e, "Exception while scanning and loading plugins");
                            }

                            _logger.LogDebug("Plugins loaded: '{pluginCount}'", _loadedPlugins.Count);
                        }
                        await Task.Delay(HeartBeatPulseTiming);
                    }
                });
            }

            public Task StopAsync(CancellationToken cancellationToken) {
                throw new NotImplementedException();
            }

            private void ScanAndLoadPlugins() {
                ILoggerFactory loggerFactory;

                if (!Directory.Exists(_appConfig.PLUGIN_DIRECTORY)) {
                    // If the plugin directory doesn't exist, we can't load plugins.  Quietly return and try again later
                    return;
                }

                foreach (string file in System.IO.Directory.GetFiles(_appConfig.PLUGIN_DIRECTORY, "*.json.spacefx_plugin")) {
                    string plugin_json = System.IO.File.ReadAllText(file);
                    Models.PLUG_IN? config_plugin;

                    try {
                        config_plugin = JsonSerializer.Deserialize<Models.PLUG_IN>(plugin_json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    } catch (Exception ex) {
                        _logger.LogError("Failed to load plugin from '{file}'.  Error: {error}", file, ex);
                        System.IO.File.Move(file, file + ".error");
                        _appLifetime.StopApplication();
                        continue;
                    }

                    if (config_plugin == null) {
                        _logger.LogError("Failed to load plugin from '{file}'.  Invalid schema", file);
                        System.IO.File.Move(file, file + ".error");
                        continue;
                    }

                    // This updates the plugin file to just the filename incase someone put a full path in the file
                    config_plugin.PLUGINFILE = System.IO.Path.GetFileName(config_plugin.PLUGINFILE);

                    // Reset the plugin file to use the supplied plugin directory
                    config_plugin.PLUGINFILE = Path.Combine(_appConfig.PLUGIN_DIRECTORY, config_plugin.PLUGINFILE);

                    if (!File.Exists(config_plugin.PLUGINFILE)) {
                        _logger.LogError(
                            "Plugin '{name}' not found at '{path}'. Plugin will not be loaded",
                            Path.GetFileName(config_plugin.PLUGINFILE),
                            Path.GetDirectoryName(config_plugin.PLUGINFILE));
                        continue;
                    }

                    if (_plugins.Any(_loadedPlugins => _loadedPlugins.PLUGINFILE == config_plugin.PLUGINFILE)) {
                        // This plugin is already loaded.  Nothing to do
                        continue;
                    }

                    _logger.LogInformation("New plugin found at '{plugin}'.  Initializing...", config_plugin.PLUGINFILE);

                    foreach (KeyValuePair<string, string> kvp in config_plugin.CONFIGURATION) {
                        _logger.LogDebug("Plugin '{configFileName}' Configuration: '{key}' = '{value}'", config_plugin.PLUGINFILE, kvp.Key, kvp.Value);
                    }

                    System.Type plugin_type;
                    try {
                        // Dynamically load the plugin
                        Assembly plugin_assembly = Assembly.LoadFrom(Path.GetFullPath(config_plugin.PLUGINFILE));

                        // Trigger a new instance
                        plugin_type = plugin_assembly.GetTypes().Where(p => typeof(IPluginBase).IsAssignableFrom(p) && p.IsClass
                                            && p.FullName != "Microsoft.CodeAnalysis.EmbeddedAttribute"
                                            && p.FullName != "System.Runtime.CompilerServices.NullableAttribute"
                                            && p.FullName != "System.Runtime.CompilerServices.NullableContextAttribute")
                                    .ToList().First();

                        _pluginTypes.Add(plugin_type);
                    } catch (Exception ex) {
                        _logger.LogError(
                            "Plugin '{name}' failed to laod.  Error: '{err}'",
                            Path.GetFileName(config_plugin.PLUGINFILE), ex);
                        continue;
                    }

                    IPluginBase? plugin = (IPluginBase?) Activator.CreateInstance(plugin_type);

                    if (plugin == null) {
                        _logger.LogError("Plugin '{name}' did not load successfully.  Probably an invalid .dll.  Removing config to prevent reloads", config_plugin.PLUGINFILE);
                        System.IO.File.Move(file, file + ".error");
                        continue;
                    }

                    _loadedPlugins.Add(plugin);

                    _logger.LogDebug("Configuring logging for plugin {plugin}", config_plugin.PLUGINFILE);

                    // Inject the logger factory for the plugin
                    loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
                    try {
                        plugin.ConfigureLogging(loggerFactory);
                    } catch (NotImplementedException) {
                        _logger.LogWarning("Plugin '{pluginName}' has not implemented '{pluginBackgroundTask}'.", config_plugin.PLUGINFILE, nameof(plugin.ConfigureLogging));
                    } catch (Exception ex) {
                        _logger.LogError("Failure in Plugin '{filename}', Method '{methodName}': {errorMessage}", config_plugin.PLUGINFILE, nameof(plugin.ConfigureLogging), ex.Message);
                    }

                    _logger.LogDebug("Logging successfully configured for plugin {plugin}", config_plugin.PLUGINFILE);

                    if (config_plugin.CALCULATED_CORE_PERMISSIONS.HasFlag(Models.PLUG_IN.CorePermissions.ALLOW_BACKGROUND_TASK)) {
                        _logger.LogInformation("Starting BackgroundTask for plugin '{plugin_name}'", config_plugin.PLUGINFILE);

                        try {
                            plugin.BackgroundTask();
                        } catch (NotImplementedException) {
                            _logger.LogWarning("Plugin '{pluginName}' has not implemented '{pluginBackgroundTask)}'.", config_plugin.PLUGINFILE, nameof(plugin.BackgroundTask));
                        } catch (Exception ex) {
                            _logger.LogError("Failure in Plugin '{filename}', Method '{methodName}': {errorMessage}", config_plugin.PLUGINFILE, nameof(plugin.BackgroundTask), ex.Message);
                            _appLifetime.StopApplication();
                        }

                        _logger.LogDebug("BackgroundTask successfully started for plugin {plugin}", config_plugin.PLUGINFILE);
                    }

                    _logger.LogInformation("Plugin initialized '{plugin}'", config_plugin.PLUGINFILE);

                    _plugins.Add(config_plugin);

                    _logger.LogInformation("Plugins loaded: '{pluginCount}'", _loadedPlugins.Count);
                }
            }

            private List<System.Type> GetTypes(System.Type interfaceType) {
                List<System.Type> returnTypes;

                try {
                    returnTypes = AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(a => a.GetTypes())
                                .Where(p => interfaceType.IsAssignableFrom(p) && p.IsClass
                                        && p.FullName != "Microsoft.CodeAnalysis.EmbeddedAttribute"
                                        && p.FullName != "System.Runtime.CompilerServices.NullableAttribute"
                                        && p.FullName != "System.Runtime.CompilerServices.NullableContextAttribute")
                                .ToList();
                } catch (ReflectionTypeLoadException e) {
                    returnTypes = new List<System.Type>();

                    if (e.Types != null) {
                        e.Types.ToList().ForEach(t => {
                            if (t != null) returnTypes.Add(t);
                        });
                    }
                }

                return returnTypes;
            }

            public MessageFormats.Common.PluginHealthCheckMultiResponse CallPluginHealthCheck(MessageFormats.Common.PluginHealthCheckRequest request) {
                MessageFormats.Common.PluginHealthCheckMultiResponse response = Utils.ResponseFromRequest(request, new MessageFormats.Common.PluginHealthCheckMultiResponse());

                response.ResponseHeader.Status = MessageFormats.Common.StatusCodes.Successful;

                using (var scope = _serviceProvider.CreateScope()) {

                    foreach (var _loadedPlugin in _loadedPlugins) {
                        System.Type? typedPlugin;
                        Models.PLUG_IN? config_plugin;

                        typedPlugin = _pluginTypes.FirstOrDefault(_type => _type.FullName == _loadedPlugin.GetType().FullName);

                        if (typedPlugin == null) continue;

                        config_plugin = _plugins.FirstOrDefault(_plugin => _plugin.PLUGINFILE == typedPlugin.Module.FullyQualifiedName);

                        if (config_plugin == null) continue;

                        if (!config_plugin.ENABLED) {
                            _logger.LogWarning("Plugin '{pluginName}' is disabled.  Skipping plugin.", config_plugin.PLUGINFILE);
                            continue;
                        }

                        try {
                            _logger.LogTrace("Querying '{plugin_name}' Health Check", config_plugin.PLUGINFILE);

                            MessageFormats.Common.PluginHealthCheckResponse _plugin_response = _loadedPlugin.PluginHealthCheckResponse().Result;
                            response.PluginHealthCheckResponses.Add(_plugin_response);
                        } catch (UnauthorizedAccessException) {
                            // The plugin doesn't have permissions to make the call
                            _logger.LogWarning("Plugin '{pluginName}':  Does not have required permissions for call", config_plugin.PLUGINFILE);
                        } catch (NotImplementedException) {
                            // The plugin doesn't implement the method we called.  Log it here and loop to the next one
                            _logger.LogWarning("Plugin '{pluginName}':  Method not implemented.", config_plugin.PLUGINFILE);
                        } catch (Exception ex) {
                            // The plugin has a hard failure.  Log it here and loop to the next one.
                            _logger.LogError("Plugin '{pluginName}':  Plugin Error.  Exception: {errorMsg}", config_plugin.PLUGINFILE, ex.Message);
                        }
                    };

                    if (response.PluginHealthCheckResponses.Any(_response => _response.ResponseHeader.Status != MessageFormats.Common.StatusCodes.Healthy && _response.ResponseHeader.Status != MessageFormats.Common.StatusCodes.Successful)) {
                        response.ResponseHeader.Status = MessageFormats.Common.StatusCodes.GeneralFailure;
                        response.ResponseHeader.Message = "One or more plugins did not have a successful health check";
                    }
                }

                return response;
            }

            public T? CallPlugins<T, V>(T orig_request, Func<(T? input_request, V plugin), T?> pluginDelegate) {
                if (orig_request == null) throw new ArgumentNullException("pluginInput");

                T? plugin_request = orig_request;
                using (var scope = _serviceProvider.CreateScope()) {

                    foreach (V _loadedPlugin in _loadedPlugins) {
                        System.Type? typedPlugin;
                        Models.PLUG_IN? config_plugin;
                        if (plugin_request is null) {
                            _logger.LogError("Input Request is null; skipping Plugin.");
                            continue;
                        }

                        typedPlugin = _pluginTypes.FirstOrDefault(_type => _type.FullName == _loadedPlugin.GetType().FullName);

                        if (typedPlugin == null) continue;

                        config_plugin = _plugins.FirstOrDefault(_plugin => _plugin.PLUGINFILE == typedPlugin.Module.FullyQualifiedName);

                        if (config_plugin == null) continue;

                        if (!config_plugin.ENABLED) {
                            _logger.LogWarning("Plugin '{pluginName}' is disabled.  Skipping plugin.", config_plugin.PLUGINFILE);
                            continue;
                        }

                        try {
                            _logger.LogTrace("Passing request to plugin '{plugin_name}'", config_plugin.PLUGINFILE);

                            // Call the plugin delegate with the trailing inputs / outputs
                            T? tmp_plugin_request = pluginDelegate((input_request: plugin_request, plugin: _loadedPlugin));

                            if (config_plugin.CALCULATED_CORE_PERMISSIONS.HasFlag(Models.PLUG_IN.CorePermissions.ALLOW_WRITEBACK)) {
                                _logger.LogTrace("'{plugin_name}' has writeback.  Updating request", config_plugin.PLUGINFILE);
                                // The plugin has permissions to update the request/response passed to other plugins.  Save it here
                                plugin_request = tmp_plugin_request;
                            }
                        } catch (UnauthorizedAccessException) {
                            // The plugin doesn't have permissions to make the call
                            _logger.LogWarning("Plugin '{pluginName}':  Does not have required permissions for call", config_plugin.PLUGINFILE);
                        } catch (NotImplementedException) {
                            // The plugin doesn't implement the method we called.  Log it here and loop to the next one
                            _logger.LogWarning("Plugin '{pluginName}':  Method not implemented.", config_plugin.PLUGINFILE);
                        } catch (Exception ex) {
                            // The plugin has a hard failure.  Log it here and loop to the next one.
                            _logger.LogError("Plugin '{pluginName}':  Plugin Error.  Exception: {errorMsg}", config_plugin.PLUGINFILE, ex.Message);
                        }
                    };

                }

                return plugin_request;
            }

            public (T?, U?) CallPlugins<T, V, U>(T? orig_request, U orig_response, Func<(T? input_request, U? input_response, V plugin), (T? output_request, U? output_response)> pluginDelegate) {
                if (orig_request == null) throw new ArgumentNullException("pluginInput");

                // Seed the initial set of inputs and outputs received from the plugins
                T? plugin_request = orig_request;
                U? plugin_response = orig_response;
                using (var scope = _serviceProvider.CreateScope()) {

                    foreach (V _loadedPlugin in _loadedPlugins) {
                        System.Type? typedPlugin;
                        Models.PLUG_IN? config_plugin;

                        if (plugin_request is null) {
                            _logger.LogError("Input Request is null; skipping Plugin.");
                            continue;
                        }

                        typedPlugin = _pluginTypes.FirstOrDefault(_type => _type.FullName == _loadedPlugin.GetType().FullName);

                        if (typedPlugin == null) continue;

                        config_plugin = _plugins.FirstOrDefault(_plugin => _plugin.PLUGINFILE == typedPlugin.Module.FullyQualifiedName);

                        if (config_plugin == null) continue;


                        if (!config_plugin.ENABLED) {
                            _logger.LogWarning("Plugin '{pluginName}' is disabled.  Skipping plugin.", config_plugin.PLUGINFILE);
                            continue;
                        }

                        try {
                            _logger.LogTrace("Passing request to plugin '{plugin_name}'", config_plugin.PLUGINFILE);

                            // Call the plugin delegate with the trailing inputs / outputs
                            (T? tmp_plugin_request, U? tmp_plugin_response) = pluginDelegate((input_request: plugin_request, input_response: plugin_response, plugin: _loadedPlugin));

                            if (config_plugin.CALCULATED_CORE_PERMISSIONS.HasFlag(Models.PLUG_IN.CorePermissions.ALLOW_WRITEBACK)) {
                                _logger.LogTrace("'{plugin_name}' has writeback.  Updating request", config_plugin.PLUGINFILE);
                                // The plugin has permissions to update the request/response passed to other plugins.  Save it here
                                plugin_request = tmp_plugin_request;
                                plugin_response = tmp_plugin_response;
                            }
                        } catch (UnauthorizedAccessException) {
                            // The plugin doesn't have permissions to make the call
                            _logger.LogWarning("Plugin '{pluginName}':  Does not have required permissions for call", config_plugin.PLUGINFILE);
                        } catch (NotImplementedException) {
                            // The plugin doesn't implement the method we called.  Log it here and loop to the next one
                            _logger.LogWarning("Plugin '{pluginName}':  Method not implemented.", config_plugin.PLUGINFILE);
                        } catch (Exception ex) {
                            // The plugin has a hard failure.  Log it here and loop to the next one.
                            _logger.LogError("Plugin '{pluginName}':  Plugin Error.  Exception: {errorMsg}", config_plugin.PLUGINFILE, ex.Message);
                        }
                    };

                }
                return (plugin_request, plugin_response);
            }
        }
    }
}
