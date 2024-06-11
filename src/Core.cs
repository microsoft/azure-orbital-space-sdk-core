

using Microsoft.Azure.SpaceFx.MessageFormats.Common;

namespace Microsoft.Azure.SpaceFx;

public partial class Core {
    /// <summary>
    /// Enables apps to check if the client has been provisioned yet
    /// </summary>
    public static List<HeartBeatPulse> ServicesOnline(int timeoutMS = 10000) {
        if (Client._client == null) throw new Exception("Client is not provisioned.  Please deploy the client before trying to run this");
        return Client._client.ServicesOnline(timeoutMS);
    }

    /// <summary>
    /// Get the xfer directories
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static Task<(string inbox_directory, string outbox_directory, string root_directory)> GetXFerDirectories() {
        if (Client._client == null) throw new Exception("Client is not provisioned.  Please deploy the client before trying to run this");
        return Client._client.GetXFerDirectories();
    }


    /// <summary>
    /// Provide a method to wait for the sidecar to start reporting healthy with an optional timeout parameter
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static Task<Enums.SIDECAR_STATUS> WaitForOnline(TimeSpan? timeSpan = null) {
        if (Client._client == null) throw new Exception("Client is not provisioned.  Please deploy the client before trying to run this");
        return Client._client.WaitForOnline(timeSpan: timeSpan);
    }

    /// <summary>
    /// Enables apps to check if the client has been provisioned yet
    /// </summary>
    public static bool IsOnline() {
        return (Client._client != null && Client._client.IS_ONLINE);
    }

    /// <summary>
    /// Retrieve the directory where all configuration is stored for reading and parsing by apps
    /// </summary>
    /// <returns></returns>
    public static Task<string> GetConfigDirectory() {
        return Task.FromResult(APP_CONFIG._SPACEFX_CONFIG_DIR);
    }

    /// <summary>
    /// Retrieve the root directory for spacefx
    /// </summary>
    /// <returns></returns>
    public static Task<string> GetSpaceFxDirectory() {
        return Task.FromResult(APP_CONFIG._SPACEFX_DIR);
    }


    /// <summary>
    /// Retrieve the contents of the configuration from a the SpaceFX Config directory
    /// </summary>
    /// <returns></returns>
    public static Task<string> GetConfigSetting(string configFileName) {
        string? returnValue = "";
        if (!Directory.Exists(APP_CONFIG._SPACEFX_CONFIG_DIR) || !File.Exists(Path.Combine(APP_CONFIG._SPACEFX_CONFIG_DIR, configFileName))) {
            // We didn't find the configuration from the SpaceFX Config directory.  Let's check the plugins spacefx_plugins
            string pluginDir = File.ReadAllText(Path.Combine(APP_CONFIG._SPACEFX_CONFIG_DIR, "spacefx_dir_plugins"));

            if (string.IsNullOrWhiteSpace(pluginDir)) throw new Exception("No plugin directory found in spacefx_dir_plugins");

            List<Models.PLUG_IN> allPlugins = new List<Models.PLUG_IN>();

            // Loop through the spacefx_plugin json files we find in the plugin directory and load them
            foreach (string file in System.IO.Directory.GetFiles(pluginDir, "*.json.spacefx_plugin")) {
                try {
                    Models.PLUG_IN? plugin = JsonSerializer.Deserialize<Models.PLUG_IN>(System.IO.File.ReadAllText(file), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (plugin != null) {
                        allPlugins.Add(plugin);
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Error deserializing plugin file '{file}': {ex.Message}");
                }
            }

            returnValue = allPlugins
                .Where(plugin => plugin.CONFIGURATION != null && plugin.CONFIGURATION.Keys.Any(key => key.Equals(configFileName, StringComparison.InvariantCultureIgnoreCase)))
                .Select(plugin => plugin.CONFIGURATION.First(kvp => kvp.Key.Equals(configFileName, StringComparison.InvariantCultureIgnoreCase)).Value)
                .FirstOrDefault();
        } else {
            returnValue = File.ReadAllText(Path.Combine(APP_CONFIG._SPACEFX_CONFIG_DIR, configFileName));
        }

        if (string.IsNullOrWhiteSpace(returnValue))
            throw new FileNotFoundException($"Configuration item '{configFileName}' file not found in '{APP_CONFIG._SPACEFX_CONFIG_DIR}' nor in any plugin configurations (*.json.spacefx_plugin)");

        return Task.FromResult(returnValue);
    }


    /// <summary>
    /// Retrieve the plugins that are loaded by spacesdk-core
    /// </summary>
    /// <returns></returns>
    public static Task<List<Models.PLUG_IN>> GetPlugins(int timeoutMS = 10000) {
        if (Client._client == null) throw new Exception("Client is not provisioned.  Please deploy the client before trying to run this");
        return Client._client.GetPlugins(timeoutMS);
    }

    /// <summary>
    /// Send a telemetry metric
    /// </summary>
    /// <param name="appId">Name of the target app to receive the message</param>
    /// <param name="message">IMessage (protobuf object) to send</param>
    /// <returns></returns>
    public static Task SendTelemetryMetric(string metricName, int metricValue, Timestamp? metricTime = null) {
        if (Client._client == null) throw new Exception("Client is not provisioned.  Please deploy the client before trying to run this");
        return Client._client.SendTelemetryMetric(metricName, metricValue, metricTime);
    }

    /// <summary>
    /// Send a message directly to an App within the SDK
    /// </summary>
    /// <param name="appId">Name of the target app to receive the message</param>
    /// <param name="message">IMessage (protobuf object) to send</param>
    /// <returns></returns>
    public static Task DirectToApp(string appId, IMessage message) {
        if (Client._client == null) throw new Exception("Client is not provisioned.  Please deploy the client before trying to run this");
        return Client._client.DirectToApp(appId: appId, message: message);
    }

    /// <summary>
    /// Retrieves the DAPR_APP_ID supplied to the Client injector
    /// </summary>
    public static Task<string> GetAppID() {
        if (Client._client == null) throw new Exception("Client is not provisioned.  Please deploy the client before trying to run this");
        return Client._client.GetAppID();
    }

    /// <summary>
    /// Publish a topic to the Space Framework
    /// </summary>
    public static Task PublishMsg(string topic, IMessage message, string pubsubName = "pubsub") {
        if (Client._client == null) throw new Exception("Client is not provisioned.  Please deploy the client before trying to run this");
        return Client._client.PublishMsg(topic, message, pubsubName);
    }

    /// <summary>
    /// Save an item to the distrubuted cache
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="cacheItem"></param>
    /// <param name="cacheItemName"></param>
    /// <param name="expiration"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static Task<MessageFormats.Common.CacheItem> SaveCacheItem<T>(T cacheItem, string cacheItemName, DateTime? expiration = null) where T : IMessage, new() {
        if (Client._client == null) throw new Exception("Client is not provisioned.  Please deploy the client before trying to run this");
        return Client._client.SaveCacheItem<T>(cacheItem, cacheItemName, expiration);
    }

    public static Task<T?> GetCacheItem<T>(string cacheItemName) where T : IMessage, new() {
        if (Client._client == null) throw new Exception("Client is not provisioned.  Please deploy the client before trying to run this");
        return Client._client.GetCacheItem<T>(cacheItemName);
    }

    public static Task DeleteCacheItem(string cacheItemName) {
        if (Client._client == null) throw new Exception("Client is not provisioned.  Please deploy the client before trying to run this");
        return Client._client.DeleteCacheItem(cacheItemName);
    }

    /// <summary>
    /// The main Client Service for interacting with the SpaceSDK
    /// </summary>
    public class Client {
        protected internal static Client _client = null!;
        const int ClientDelayMS = 200;
        private const string DirectToAppPrefix = "DirectToApp-";
        private readonly ILogger<Client> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private Enums.SIDECAR_STATUS _sidecarStatus;
        internal readonly IServiceProvider _serviceProvider;
        private readonly Dapr.Client.Autogen.Grpc.v1.Dapr.DaprClient _daprClient;
        private readonly IHostApplicationLifetime _appLifetime;
        private string _appId;
        public bool IS_ONLINE = false;
        private readonly Core.APP_CONFIG _appConfig;
        private readonly string _cacheDir;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Client(ILogger<Client> logger, IHttpClientFactory httpClientFactory, IServiceProvider serviceProvider, IHostApplicationLifetime appLifetime) {
            try {
                _logger = logger;
                _sidecarStatus = Enums.SIDECAR_STATUS.PENDING;
                _appId = "";
                _httpClientFactory = httpClientFactory;
                _serviceProvider = serviceProvider;
                _appLifetime = appLifetime;
                Console.WriteLine($"Core.{nameof(Client)} Initialized");


                _daprClient = new Dapr.Client.Autogen.Grpc.v1.Dapr.DaprClient(Grpc.Net.Client.GrpcChannel.ForAddress($"http://127.0.0.1:{Environment.GetEnvironmentVariable("DAPR_GRPC_PORT")}", new Grpc.Net.Client.GrpcChannelOptions() { Credentials = Grpc.Core.ChannelCredentials.Insecure }));
                _appConfig = _serviceProvider.GetService<Core.APP_CONFIG>() ?? new APP_CONFIG();

                _cacheDir = Path.Combine(GetXFerDirectories().Result.root_directory, "tmp", "cache");
                System.IO.Directory.CreateDirectory(_cacheDir);

                _client = this;
            } catch (Exception ex) {
                Console.WriteLine($"Failed to initialize Core.{nameof(Client)}.  Error: {ex.Message}.  Stack Trace: {ex.StackTrace}");
                appLifetime.StopApplication();
            }
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        /// <summary>
        /// Retrieve the directory where all configuration is stored for reading and parsing by apps
        /// </summary>
        /// <returns></returns>
        public Task<string> GetConfigDirectory() {
            return Task.FromResult(_appConfig.SPACEFX_CONFIG_DIR);
        }

        /// <summary>
        /// Retrieve the contents of the configuration from a directory
        /// </summary>
        /// <returns></returns>
        public Task<string> GetConfigSetting(string configFileName) {
            string? returnValue = "";
            if (!Directory.Exists(APP_CONFIG._SPACEFX_CONFIG_DIR) || !File.Exists(Path.Combine(APP_CONFIG._SPACEFX_CONFIG_DIR, configFileName))) {
                // We didn't find the configuration from the SpaceFX Config directory.  Let's check the plugins spacefx_plugins
                string pluginDir = File.ReadAllText(Path.Combine(APP_CONFIG._SPACEFX_CONFIG_DIR, "spacefx_dir_plugins"));

                if (string.IsNullOrWhiteSpace(pluginDir)) throw new Exception("No plugin directory found in spacefx_dir_plugins");

                List<Models.PLUG_IN> allPlugins = new List<Models.PLUG_IN>();

                // Loop through the spacefx_plugin json files we find in the plugin directory and load them
                foreach (string file in System.IO.Directory.GetFiles(pluginDir, "*.json.spacefx_plugin")) {
                    try {
                        Models.PLUG_IN? plugin = JsonSerializer.Deserialize<Models.PLUG_IN>(System.IO.File.ReadAllText(file), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (plugin != null) {
                            allPlugins.Add(plugin);
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"Error deserializing plugin file '{file}': {ex.Message}");
                    }
                }

                returnValue = allPlugins
                    .Where(plugin => plugin.CONFIGURATION != null && plugin.CONFIGURATION.Keys.Any(key => key.Equals(configFileName, StringComparison.InvariantCultureIgnoreCase)))
                    .Select(plugin => plugin.CONFIGURATION.First(kvp => kvp.Key.Equals(configFileName, StringComparison.InvariantCultureIgnoreCase)).Value)
                    .FirstOrDefault();
            } else {
                returnValue = File.ReadAllText(Path.Combine(_appConfig.SPACEFX_CONFIG_DIR, configFileName));
            }

            if (string.IsNullOrWhiteSpace(returnValue))
                throw new FileNotFoundException($"Configuration item '{configFileName}' file not found in '{_appConfig.SPACEFX_CONFIG_DIR}' nor in any plugin configurations (*.json.spacefx_plugins)");

            return Task.FromResult(returnValue);
        }

        /// <summary>
        /// Retrieve the plugins that are loaded by spacesdk-core
        /// </summary>
        /// <returns></returns>
        public Task<List<Models.PLUG_IN>> GetPlugins(int timeoutMS = 10000) {
            Services.PluginLoader? _service = Client._client._serviceProvider.GetService<Services.PluginLoader>();
            DateTime maxWaitTime = DateTime.UtcNow.Add(TimeSpan.FromMilliseconds(timeoutMS));

            while (_service == null && DateTime.UtcNow <= maxWaitTime) {
                Task.Delay(ClientDelayMS).Wait();
                _service = Client._client._serviceProvider.GetService<Services.PluginLoader>();
            }

            if (_service == null) {
                throw new TimeoutException("Timed out waiting for Plugin Service to come online.");
            }

            return Task.FromResult(_service.RetrievePlugins());
        }


        /// <summary>
        /// Retrieve the services that are online
        /// </summary>
        /// <returns></returns>
        public List<MessageFormats.Common.HeartBeatPulse> ServicesOnline(int timeoutMS = 10000) {
            Services.HeartbeatService? _service = Client._client._serviceProvider.GetService<Services.HeartbeatService>();
            DateTime maxWaitTime = DateTime.UtcNow.Add(TimeSpan.FromMilliseconds(timeoutMS));

            while (_service == null && DateTime.UtcNow <= maxWaitTime) {
                Task.Delay(ClientDelayMS).Wait();
                _service = Client._client._serviceProvider.GetService<Services.HeartbeatService>();
            }

            if (_service == null) {
                throw new TimeoutException("Timed out waiting for Heartbeat Service to come online.");
            }

            return _service.RetrieveServiceHeartbeats();
        }

        /// <summary>
        /// Send a telemetry metric
        /// </summary>
        /// <param name="appId">Name of the target app to receive the message</param>
        /// <param name="message">IMessage (protobuf object) to send</param>
        /// <returns></returns>
        public async Task SendTelemetryMetric(string metricName, int metricValue, Timestamp? metricTime = null) {
            using (var scope = _serviceProvider.CreateScope()) {
                string id = Guid.NewGuid().ToString();

                MessageFormats.Common.TelemetryMetric telemetryMetric = new MessageFormats.Common.TelemetryMetric() {
                    RequestHeader = new() {
                        TrackingId = id,
                        CorrelationId = id,
                    },
                    MetricName = metricName,
                    MetricValue = metricValue,
                    MetricTime = metricTime ?? Timestamp.FromDateTime(DateTime.UtcNow)
                };

                await DirectToApp($"hostsvc-{nameof(MessageFormats.Common.HostServices.Logging)}".ToLower(), message: telemetryMetric);
            }
        }

        /// <summary>
        /// Send a message directly to an App within the SDK
        /// </summary>
        /// <param name="appId">Name of the target app to receive the message</param>
        /// <param name="message">IMessage (protobuf object) to send</param>
        /// <returns></returns>
        public async Task DirectToApp(string appId, IMessage message) {
            using (var scope = _serviceProvider.CreateScope()) {
                // Waiting for sidecar to come online
                while (IS_ONLINE == false) {
                    await Task.Delay(ClientDelayMS);
                }
                // Sidecar Online

                // Update the log message LogTime and Received time if this is a logging message
                if (message.GetType().FullName == typeof(MessageFormats.Common.LogMessage).FullName) {
                    message.Descriptor.Fields.InFieldNumberOrder().First(field => field.PropertyName == "LogTime").Accessor.SetValue(message, Timestamp.FromDateTime(DateTime.UtcNow));
                }

                MessageFormats.Common.DirectToApp directToAppMsg = new() {
                    ResponseHeader = new() {
                        TrackingId = Guid.NewGuid().ToString(),
                        CorrelationId = Guid.NewGuid().ToString()
                    },
                    MessageType = message.Descriptor.FullName,
                    Message = Any.Pack(message)
                };

                // Set the tracking ID and Correlation ID if we're using a request header
                if (message.Descriptor.Fields.InFieldNumberOrder().Any(field => field.PropertyName.Equals("RequestHeader", StringComparison.InvariantCultureIgnoreCase))) {
                    var requestHeaderBase = message.Descriptor.Fields.InFieldNumberOrder().First(field => field.PropertyName == "RequestHeader").Accessor.GetValue(message);
                    MessageFormats.Common.RequestHeader messageWithRequestHeader = requestHeaderBase as MessageFormats.Common.RequestHeader ?? new MessageFormats.Common.RequestHeader() { TrackingId = Guid.NewGuid().ToString(), CorrelationId = Guid.NewGuid().ToString() };

                    directToAppMsg.ResponseHeader.TrackingId = messageWithRequestHeader.TrackingId;
                    directToAppMsg.ResponseHeader.CorrelationId = messageWithRequestHeader.CorrelationId;
                }

                // Set the tracking ID and Correlation ID if we're using a response header
                if (message.Descriptor.Fields.InFieldNumberOrder().Any(field => field.PropertyName.Equals("ResponseHeader", StringComparison.InvariantCultureIgnoreCase))) {
                    var responseHeaderBase = message.Descriptor.Fields.InFieldNumberOrder().First(field => field.PropertyName == "ResponseHeader").Accessor.GetValue(message);
                    MessageFormats.Common.ResponseHeader messageWithResponseHeader = responseHeaderBase as MessageFormats.Common.ResponseHeader ?? new MessageFormats.Common.ResponseHeader() { TrackingId = Guid.NewGuid().ToString(), CorrelationId = Guid.NewGuid().ToString() };

                    directToAppMsg.ResponseHeader.TrackingId = messageWithResponseHeader.TrackingId;
                    directToAppMsg.ResponseHeader.CorrelationId = messageWithResponseHeader.CorrelationId;
                    directToAppMsg.ResponseHeader.Status = messageWithResponseHeader.Status;
                }

                string calculatedTopic = string.Format($"{DirectToAppPrefix}{appId}").ToUpper();
                // string calculatedTopic = string.Format($"{DirectToAppPrefix}{appId}/{message.Descriptor.Name}");

                // // Sensor Proto message - find and append the sensor ID
                // if (System.IO.Path.GetFileName(message.Descriptor.File.Name).Equals("Sensor.proto", StringComparison.InvariantCultureIgnoreCase)) {
                //     // Sensor ID is on main message under property "SensorId"
                //     switch (message.Descriptor.Name) {
                //         case string taskingPreCheckRequest when taskingPreCheckRequest.Equals("TaskingPreCheckRequest", StringComparison.InvariantCultureIgnoreCase):
                //         case string taskingPreCheckResponse when taskingPreCheckResponse.Equals("TaskingPreCheckResponse", StringComparison.InvariantCultureIgnoreCase):
                //         case string taskingRequest when taskingRequest.Equals("TaskingRequest", StringComparison.InvariantCultureIgnoreCase):
                //         case string taskingResponse when taskingResponse.Equals("TaskingResponse", StringComparison.InvariantCultureIgnoreCase):
                //         case string sensorData when sensorData.Equals("SensorData", StringComparison.InvariantCultureIgnoreCase):
                //             var messageField = message.Descriptor.Fields.InDeclarationOrder().First(field => field.Name.Equals("SensorId", StringComparison.InvariantCultureIgnoreCase));
                //             var messageFieldValue = messageField.Accessor.GetValue(message);
                //             if (messageFieldValue != null) calculatedTopic += string.Format($"/{messageFieldValue}");
                //             break;
                //     }
                // }

                // Sending message '{messageType}' to '{appId}' (on topic '{calculatedTopic}')  (trackingId: '{trackingId}' / correlationId: '{correlationId}')
                await PublishMsg(topic: calculatedTopic, message: directToAppMsg, pubsubName: "pubsub");

            }
        }

        /// <summary>
        /// Retrieve a list of online services this app can access
        /// </summary>
        /// <returns>Three strings - the inbox directory, the outbox directory, and the root xfer directory</returns>
        public Task<(string inbox_directory, string outbox_directory, string root_directory)> GetXFerDirectories() {
            // Create the directories if they don't already exist
            Directory.CreateDirectory(Path.Combine(_appConfig.XFER_DIRECTORY_ROOT, "inbox"));
            Directory.CreateDirectory(Path.Combine(_appConfig.XFER_DIRECTORY_ROOT, "outbox"));
            Directory.CreateDirectory(Path.Combine(_appConfig.XFER_DIRECTORY_ROOT, "tmp"));

            return Task.FromResult((inbox_directory: Path.Combine(_appConfig.XFER_DIRECTORY_ROOT, "inbox"), outbox_directory: Path.Combine(_appConfig.XFER_DIRECTORY_ROOT, "outbox"), root_directory: _appConfig.XFER_DIRECTORY_ROOT));
        }

        /// <summary>
        /// Waits for the dapr Client to come online
        /// </summary>
        public Task<Enums.SIDECAR_STATUS> WaitForOnline(TimeSpan? timeSpan = null) {
            TimeSpan maxWaitTimeSpan = timeSpan ?? TimeSpan.FromSeconds(60);
            DateTime maxWaitTime = DateTime.UtcNow.AddSeconds(maxWaitTimeSpan.TotalSeconds);
            string? daprPort = "";

            if (_sidecarStatus == Enums.SIDECAR_STATUS.ERROR) {
                string error_message = "Timed out waiting for healthy response from {healthEndPoint}";
                throw new TimeoutException(error_message);
            }

            if (_sidecarStatus != Enums.SIDECAR_STATUS.PENDING) {
                // Returning cached Client_STATUS {status}
                if (_sidecarStatus == Enums.SIDECAR_STATUS.ERROR) throw new Exception("Client status is ERROR.  Restart App");
                return Task.FromResult(_sidecarStatus);
            }

            //WaitForClient entry
            while (string.IsNullOrWhiteSpace(daprPort) && DateTime.UtcNow <= maxWaitTime) {
                try {
                    daprPort = System.Environment.GetEnvironmentVariable("DAPR_HTTP_PORT");
                } catch (Exception) {
                    System.Threading.Thread.Sleep(ClientDelayMS);
                    Console.WriteLine($"DAPR_HTTP_PORT environment variable not found.  Retrying in {ClientDelayMS}ms");
                }
            }

            if (string.IsNullOrWhiteSpace(daprPort)) {
                throw new TimeoutException("Timed out waiting for DAPR_HTTP_PORT environment variable");
            }

            string healthEndpoint = string.Format("http://localhost:{0}/v1.0/healthz", daprPort);

            using HttpClient httpClient = _httpClientFactory.CreateClient("sidecar");

            while (_sidecarStatus == Enums.SIDECAR_STATUS.PENDING && DateTime.UtcNow <= maxWaitTime) {
                try {
                    HttpResponseMessage httpResponse = httpClient.GetAsync(healthEndpoint).Result;
                    _sidecarStatus = httpResponse.IsSuccessStatusCode ? Enums.SIDECAR_STATUS.HEALTHY : Enums.SIDECAR_STATUS.PENDING;

                } catch (AggregateException ex) {
                    foreach (var innerException in ex.Flatten().InnerExceptions) {
                        Console.WriteLine(innerException.ToString(), ": One or many tasks failed.");
                    }
                    Console.WriteLine($"Client not found (failed to query healthz endpoint).  Likely not injected yet.  Retrying in {ClientDelayMS}ms");
                    Console.WriteLine($"Error querying Client health endpoint: {ex.Message}");
                    System.Threading.Thread.Sleep(ClientDelayMS);
                } catch (Exception ex) {
                    Console.WriteLine($"Client not found (failed to query healthz endpoint).  Likely not injected yet.  Retrying in {ClientDelayMS}ms");
                    Console.WriteLine($"Error querying Client health endpoint: {ex.Message}");
                    System.Threading.Thread.Sleep(ClientDelayMS);
                }
            }

            if (_sidecarStatus == Enums.SIDECAR_STATUS.PENDING) {
                string error_message = "Timed out waiting for healthy response from {healthEndPoint}";
                _sidecarStatus = Enums.SIDECAR_STATUS.ERROR;
                Console.WriteLine(error_message, healthEndpoint);
                throw new TimeoutException(error_message);
            }

            // WaitForClient exit
            return Task.FromResult(_sidecarStatus);
        }



        /// <summary>
        /// Retrieves the DAPR_APP_ID supplied to the Client injector
        /// </summary>
        public async Task<string> GetAppID() {
            // We already have out appId in cache.  Return it and close loop
            if (!string.IsNullOrWhiteSpace(_appId)) return _appId;

            using (var scope = _serviceProvider.CreateScope()) {
                string? httpResponse = null;
                string metadataEndpoint;

                // We need to query the endpoint to get our appId
                using (HttpClient httpClient = _httpClientFactory.CreateClient("sidecar")) {
                    // Query the Client's metadata endpoint to get the app id
                    string daprPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";
                    metadataEndpoint = string.Format("http://localhost:{0}/v1.0/metadata", daprPort);

                    while (httpResponse == null) {
                        try {
                            httpResponse = httpClient.GetStringAsync(metadataEndpoint).Result;
                        } catch (AggregateException ex) {
                            Console.WriteLine($"Client not found (failed to query metadata endpoint).  Likely not injected yet.  Retrying in {ClientDelayMS}ms");
                            foreach (var innerException in ex.Flatten().InnerExceptions) {
                                Console.WriteLine($"Error querying Client metadata endpoint: {innerException.Message}");
                            }
                            await Task.Delay(ClientDelayMS);
                            httpResponse = null;
                        } catch (Exception ex) {
                            Console.WriteLine($"Client not found (failed to query metadata endpoint).  Likely not injected yet.  Retrying in {ClientDelayMS}ms");
                            Console.WriteLine($"Error querying Client metadata endpoint: {ex.Message}");
                            await Task.Delay(ClientDelayMS);
                            httpResponse = null;
                        }
                    }
                }

                if (httpResponse == null || string.IsNullOrEmpty(httpResponse)) {
                    throw new NullReferenceException(string.Format("Response from '{0}'", metadataEndpoint));
                }

                // Parse the JSON string and extract the ID
                using (JsonDocument jsonDoc = JsonDocument.Parse(httpResponse)) {
                    JsonElement root = jsonDoc.RootElement;
                    _appId = root.GetProperty("id").GetString() ?? "";
                }

                if (string.IsNullOrWhiteSpace(_appId)) throw new NullReferenceException(string.Format("No 'ID' field property in response '{0}' from '{1}'", httpResponse, metadataEndpoint));

            }
            return _appId;
        }

        /// <summary>
        /// Publish a topic to the Space Framework
        /// </summary>
        public async Task PublishMsg(string topic, IMessage message, string pubsubName = "pubsub") {
            using (var scope = _serviceProvider.CreateScope()) {

                Dapr.Client.Autogen.Grpc.v1.PublishEventRequest request = new Dapr.Client.Autogen.Grpc.v1.PublishEventRequest {
                    PubsubName = pubsubName,
                    Topic = topic,
                    Data = message.ToByteString(),
                    DataContentType = "application/octet-stream"
                };

                await _daprClient.PublishEventAsync(request);
                //await channel.ShutdownAsync();
            }
        }
#pragma warning disable CS1998 // Disable async warning
        public async Task<MessageFormats.Common.CacheItem> SaveCacheItem<T>(T cacheItem, string cacheItemName, DateTime? expiration = null) where T : IMessage, new() {
            using (var scope = _serviceProvider.CreateScope()) {

                MessageFormats.Common.CacheItem cacheObject = new() {
                    RequestHeader = new() {
                        TrackingId = Guid.NewGuid().ToString()
                    },
                    Name = cacheItemName,
                    Item = Any.Pack(cacheItem),
                    CreationTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow),
                    ExpirationTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(expiration.GetValueOrDefault(DateTime.MaxValue).ToUniversalTime())
                };

                byte[] bytesToEncode = System.Text.Encoding.UTF8.GetBytes(cacheItemName);
                string cacheItemName_base64 = Convert.ToBase64String(bytesToEncode);

                using (var fileStream = new FileStream(Path.Combine(_cacheDir, cacheItemName_base64), FileMode.Create)) {
                    cacheObject.WriteTo(fileStream);
                }


                // Dapr.Client.Autogen.Grpc.v1.StateItem stateItem = new() {
                //     Key = cacheItemName,
                //     Value = cacheObject.ToByteString()
                // };


                // // Calculate the expiration by subtracting the current time from the expiration time and converting to seconds
                // double ttlInSeconds = (DateTime.UtcNow - (expiration ?? DateTime.MaxValue)).TotalSeconds;
                // stateItem.Metadata.Add("ttlInSEconds", ttlInSeconds.ToString());

                // Dapr.Client.Autogen.Grpc.v1.SaveStateRequest request = new() {
                //     StoreName = "statestore"
                // };

                // request.States.Add(stateItem);

                // var test = _daprClient.SaveState(request: request);

                return cacheObject;
            }
        }
#pragma warning restore CS1998

        public async Task<T?> GetCacheItem<T>(string cacheItemName) where T : IMessage, new() {
            using (var scope = _serviceProvider.CreateScope()) {
                byte[] bytesToEncode = System.Text.Encoding.UTF8.GetBytes(cacheItemName);
                string cacheItemName_base64 = Convert.ToBase64String(bytesToEncode);


                if (!File.Exists(Path.Combine(_cacheDir, cacheItemName_base64))) return default;

                ByteString raw_cache_item;

                using (var fileStream = new FileStream(Path.Combine(_cacheDir, cacheItemName_base64), FileMode.Open)) {
                    raw_cache_item = ByteString.FromStream(fileStream);
                }

                MessageFormats.Common.CacheItem cacheObject = MessageFormats.Common.CacheItem.Parser.ParseFrom(raw_cache_item);

                if (cacheObject.ExpirationTime.ToDateTime().ToUniversalTime() < DateTime.UtcNow) {
                    // Found an expired cache object - send the delete command
                    await DeleteCacheItem(cacheItemName: cacheItemName);
                    return default;
                }

                return cacheObject.Item.Unpack<T?>();
            }
        }

        /// <summary>
        /// Delete an item from the cache
        /// </summary>
        /// <param name="cacheItemName"></param>
        /// <returns></returns>
        public Task DeleteCacheItem(string cacheItemName) {

            byte[] bytesToEncode = System.Text.Encoding.UTF8.GetBytes(cacheItemName);
            string cacheItemName_base64 = Convert.ToBase64String(bytesToEncode);

            if (File.Exists(Path.Combine(_cacheDir, cacheItemName_base64))) {
                File.Delete(Path.Combine(_cacheDir, cacheItemName_base64));
            }

            return Task.CompletedTask;
        }

        /// <summary>
        ///  Remove all items in the cache
        /// </summary>
        /// <returns></returns>
        public Task ClearCache() {
            Directory.GetFiles(_cacheDir).ToList().ForEach(File.Delete);

            return Task.CompletedTask;
        }

        /// <summary>
        ///  Return all the items currently in the cache
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetAllCacheItems() {
            List<string> returnObject = new List<string>();

            await ClearExpiredCacheItems();

            string[] cacheFiles = Directory.GetFiles(_cacheDir);

            foreach (string file in cacheFiles) {
                byte[] bytes = Convert.FromBase64String(Path.GetFileName(file));
                string cacheItemName = System.Text.Encoding.UTF8.GetString(bytes);
                returnObject.Add(cacheItemName);
            }

            return returnObject;
        }

        internal Task ClearExpiredCacheItems() {
            using (var scope = _serviceProvider.CreateScope()) {

                Directory.GetFiles(_cacheDir).ToList().ForEach((file) => {
                    ByteString raw_cache_item;

                    using (var fileStream = new FileStream(Path.Combine(_cacheDir, file), FileMode.Open)) {
                        raw_cache_item = ByteString.FromStream(fileStream);
                    }

                    MessageFormats.Common.CacheItem cacheObject = MessageFormats.Common.CacheItem.Parser.ParseFrom(raw_cache_item);

                    if (cacheObject.ExpirationTime.ToDateTime().ToUniversalTime() < DateTime.UtcNow) {
                        File.Delete(file);
                    }
                });

            }
            return Task.CompletedTask;
        }
    }

}
