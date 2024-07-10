using Microsoft.Azure.SpaceFx.MessageFormats.Common;

namespace Microsoft.Azure.SpaceFx;

public partial class Core {
    public partial class Services {
        public class MessageReceiver : AppCallback.AppCallbackBase {
            private readonly ILogger<MessageReceiver> _logger;
            private readonly IServiceProvider _serviceProvider;
            private readonly IHostApplicationLifetime _appLifetime;
            private readonly Core.Client _client;
            private readonly Services.ResourceUtilizationMonitor _resourceUtilizationMonitor;
            private readonly Services.HeartbeatService _heartbeatService;
            private readonly Services.PluginLoader _pluginLoader;
            private string _appId = string.Empty;
            private string _directToAppTopic = "DirectToApp-";
            private Assembly[] _assemblies;
            private readonly Core.APP_CONFIG _appConfig;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public MessageReceiver(ILogger<MessageReceiver> logger, Services.HeartbeatService heartbeatService, Services.ResourceUtilizationMonitor resourceUtilizationMonitor, Services.PluginLoader pluginLoader, Core.Client client, IServiceProvider serviceProvider, IHostApplicationLifetime appLifetime) {
                _logger = logger;
                _client = client;
                _serviceProvider = serviceProvider;
                _resourceUtilizationMonitor = resourceUtilizationMonitor;
                _heartbeatService = heartbeatService;
                _pluginLoader = pluginLoader;
                _appLifetime = appLifetime;

                try {
                    using (IServiceScope scope = _serviceProvider.CreateScope()) {
                        _resourceUtilizationMonitor.StartAsync(new CancellationToken());
                    }

                    using (IServiceScope scope = _serviceProvider.CreateScope()) {
                        _heartbeatService.StartAsync(new CancellationToken());
                    }

                    using (IServiceScope scope = _serviceProvider.CreateScope()) {
                        _pluginLoader.StartAsync(new CancellationToken());
                    }

                    _appConfig = _serviceProvider.GetService<Core.APP_CONFIG>() ?? new APP_CONFIG();
                    _assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    _logger.LogInformation("Services.{serviceName} Initialized", nameof(MessageReceiver));
                } catch (Exception ex) {
                    logger.LogCritical("Failed to initialize Services.{serviceName}.  Error: {ex}.  Stack Trace: {stack}", nameof(MessageReceiver), ex.Message, ex.StackTrace);
                    appLifetime.StopApplication();
                }
            }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

            /// <summary>
            /// Implement ListTopicSubscriptions to programmatically register for the topics as requested by the parent service
            /// </summary>
            /// <param name="request"></param>
            /// <param name="context"></param>
            /// <returns></returns>
            public override async Task<ListTopicSubscriptionsResponse> ListTopicSubscriptions(Empty request, ServerCallContext context) {
                ListTopicSubscriptionsResponse resultListTopics = new ListTopicSubscriptionsResponse();
                try {

                    _logger.LogTrace("Services.MessageReceiver.ListTopicSubscriptions entry");


                    // Call the TopicSubscription function from the caller and add the results to the result list topics
                    // if (_appConfig.TopicSubscriptions != null) {
                    //     _appConfig.TopicSubscriptions(_logger).ForEach((TopicSubscription _topicSubscription) => resultListTopics.Subscriptions.Add(_topicSubscription));
                    // }

                    _appId = await _client.GetAppID();
                    _directToAppTopic = $"{_directToAppTopic}{_appId}".ToUpper();

                    _logger.LogInformation("App {serviceName} v{version} is online.  Registered App ID: {_appId}", Assembly.GetEntryAssembly()?.GetName().Name, Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown", _appId);


                    // Add the DirectToApp topic
                    resultListTopics.Subscriptions.Add(
                        new TopicSubscription() { PubsubName = "pubsub", Topic = _directToAppTopic }
                    );

                    // Automatically subscribe to spacesdk-core items
                    foreach (var topic in System.Enum.GetValues<Microsoft.Azure.SpaceFx.MessageFormats.Common.Topics>()) {
                        resultListTopics.Subscriptions.Add(
                                            new TopicSubscription() { PubsubName = "pubsub", Topic = topic.ToString() }
                                        );
                    }

                    // Add a wildcard for the directToApp
                    // resultListTopics.Subscriptions.Add(
                    //                         new TopicSubscription() { PubsubName = "pubsub", Topic = $"{_directToAppTopic}/#" }
                    //                     );

                    foreach (var subscription in resultListTopics.Subscriptions) {
                        _logger.LogDebug("Subscribing to publisher/topic:    '{pubsub}'/'{directToApp}'", subscription.PubsubName, subscription.Topic);
                    }

                    // Log all the items we're subscribing to
                    _logger.LogInformation("Subscribed to {subCount} topics", resultListTopics.Subscriptions.Count);

                    _logger.LogTrace("Services.MessageReceiver.ListTopicSubscriptions exit");

                    _client.IS_ONLINE = true;

                } catch (Exception ex) {
                    _logger.LogCritical("Failed to initialize Services.{serviceName}.ListTopicSubscriptions.  Error: {ex}.  Stack Trace: {stack}", nameof(MessageReceiver), ex.Message, ex.StackTrace);
                    _appLifetime.StopApplication();
                }

                return resultListTopics;
            }

            /// <summary>
            /// Implement OnTopicEvent to handle incoming data via publisher/subscriber.  Messages received at sent to the MessageRouter for dissemination
            /// </summary>
            /// <param name="request"></param>
            /// <param name="context"></param>
            /// <returns></returns>
            public override Task<TopicEventResponse> OnTopicEvent(TopicEventRequest request, ServerCallContext context) {
                using (var scope = _serviceProvider.CreateScope()) {

                    _logger.LogTrace("Services.MessageReceiver.OnTopicEvent start");
                    _logger.LogDebug("Received Message Type '{topic}' from App '{source}'.  Message Length: {messageLength}", request.Topic, request.Source, request.Data.Length);

                    switch (request.Topic) {
                        case string topic when string.Equals(topic, nameof(MessageFormats.Common.Topics.HeartbeatPulse), StringComparison.CurrentCultureIgnoreCase):
                            MessageFormats.Common.HeartBeatPulse pulse = MessageFormats.Common.HeartBeatPulse.Parser.ParseFrom(request.Data);
                            _logger.LogTrace("Received Heartbeat.  AppId:'{appId}'  Status:'{status}'  AppStartTime:'{appStartTime}'  TrackingId:'{trackingId}'", pulse.AppId, pulse.ResponseHeader.Status, pulse.AppStartTime.ToDateTime().ToString("o"), pulse.ResponseHeader.TrackingId);
                            _heartbeatService.StoreServiceHeartbeat(pulse);
                            break;
                        case string topic when topic.Contains(_directToAppTopic, StringComparison.InvariantCultureIgnoreCase):
                            MessageFormats.Common.DirectToApp directToApp = MessageFormats.Common.DirectToApp.Parser.ParseFrom(request.Data.ToByteArray());
                            directToApp.SourceAppId = request.Source;
                            _logger.LogTrace("Received DirectToApp.  SourceAppId:'{appId}'  MessageType:'{messageType}'  (trackingId: '{trackingId}' / correlationId: '{correlationId}')", request.Source, directToApp.MessageType, directToApp.ResponseHeader.TrackingId, directToApp.ResponseHeader.CorrelationId);
                            try {
                                RouteMessageToMessageHanders(directToApp: directToApp);
                            } catch (Exception ex) {
                                _logger.LogError("Error occured in message handlers for message type '{messageType}'.  Error: {errorMsg}.  Stack Trace: {stackTrace}", directToApp.MessageType, ex.Message, ex.StackTrace);
                            }
                            break;
                    }

                    _logger.LogTrace("Services.MessageReceiver.OnTopicEvent exit");
                }
                TopicEventResponse returnResponse = new() {
                    Status = TopicEventResponse.Types.TopicEventResponseStatus.Success
                };
                return Task.FromResult(returnResponse);
            }

            private void RouteMessageToMessageHanders(MessageFormats.Common.DirectToApp directToApp) {
                IMessage? origMessageType;

                // Use reflection to find the assembly that represents this protobuf object
                System.Type protobufParentType = typeof(IMessage);
                System.Type? messageType = null;
                foreach (Assembly assembly in _assemblies) {
                    messageType = assembly.GetType(directToApp.MessageType);
                    if (messageType != null && messageType.GetInterfaces().Any(_interface => _interface.FullName == typeof(IMessage).FullName)) {
                        break;
                    }
                }

                // No protobuff objects exist for this proto message (we probably don't know what it is).  Dump it
                if (messageType == null) {
                    _logger.LogWarning("No assemblies are loaded for the object for message type '{messageType}'.  Disregarding message.", directToApp.MessageType);
                    return;
                }

                try {
                    // Create an instance of the message object we found
                    origMessageType = Activator.CreateInstance(messageType) as IMessage;

                    // Make sure there's no funny business when trying to create the object
                    if (origMessageType == null) {
                        _logger.LogTrace("Unable to create the object '{messageType}'.", directToApp.MessageType);
                        return;
                    }
                } catch (Exception ex) {
                    _logger.LogError("Unable to create the object '{messageType}'.  Error: '{exception}'", directToApp.MessageType, ex.Message);
                    throw;
                }

                // Automatically process the PluginHealthCheck message
                if (directToApp.MessageType == typeof(MessageFormats.Common.PluginHealthCheckRequest).FullName) {
                    _logger.LogInformation("Processing message type '{messageType}' from '{sourceApp}' (trackingId: '{trackingId}' / correlationId: '{correlationId}')", directToApp.MessageType, directToApp.SourceAppId, directToApp.ResponseHeader.TrackingId, directToApp.ResponseHeader.CorrelationId);
                    PluginHealthCheckRequest pluginHealthCheckRequest = directToApp.Message.Unpack<PluginHealthCheckRequest>();
                    PluginHealthCheckMultiResponse _pluginHealthCheckMultiResponse = _pluginLoader.CallPluginHealthCheck(pluginHealthCheckRequest);

                    _logger.LogInformation("Routing message type '{messageType}' to '{sourceApp}' (trackingId: '{trackingId}' / correlationId: '{correlationId}')", _pluginHealthCheckMultiResponse.GetType().Name, directToApp.SourceAppId, directToApp.ResponseHeader.TrackingId, directToApp.ResponseHeader.CorrelationId);
                    _client.DirectToApp(appId: directToApp.SourceAppId, message: _pluginHealthCheckMultiResponse).Wait();
                    return;
                };

                // Dynamically parse the message to it's underlying type
                Google.Protobuf.Reflection.TypeRegistry registry = Google.Protobuf.Reflection.TypeRegistry.FromMessages(origMessageType.Descriptor);

                // Check and see if there's an message handler that's registered to handle the message type
                System.Type genericType = origMessageType.GetType();
                System.Type serviceType = typeof(IMessageHandler<>).MakeGenericType(genericType);
                var messageHandlerService = _serviceProvider.GetService(serviceType);

                if (messageHandlerService == null) {
                    if (directToApp.MessageType != typeof(MessageFormats.Common.TelemetryMetricResponse).FullName &&
                        directToApp.MessageType != typeof(MessageFormats.Common.LogMessageResponse).FullName &&
                        directToApp.MessageType != typeof(MessageFormats.Common.PluginHealthCheckMultiResponse).FullName) {
                        _logger.LogWarning("No Message Handler registered for message type '{messageType}' ({messageName}).  Disregarding message.", directToApp.MessageType, nameof(MessageFormats.Common.LogMessageResponse));
                    };
                    return;
                };

                // Call the message handler and pass the deserialized message
                MethodInfo? targetMethod = serviceType.GetMethod("MessageReceived");
                if (targetMethod == null) {
                    _logger.LogError("Message Handler does not contain the 'MessageReceived' method for object type '{messageType}'.  Disregarding message.", directToApp.MessageType);
                    return;
                };

                try {
                    IMessage innerMessage = directToApp.Message.Unpack(registry);

                    // Auto populate the Request Header if it's missing anything
                    if (innerMessage.Descriptor.Fields.InFieldNumberOrder().Any(field => field.PropertyName.Equals("RequestHeader", StringComparison.InvariantCultureIgnoreCase))) {
                        MessageFormats.Common.RequestHeader requestHeaderBase = innerMessage.Descriptor.Fields.InFieldNumberOrder().First(field => field.PropertyName == "RequestHeader").Accessor.GetValue(innerMessage) as MessageFormats.Common.RequestHeader ?? new MessageFormats.Common.RequestHeader();

                        requestHeaderBase.AppId = directToApp.SourceAppId;
                        if (string.IsNullOrEmpty(requestHeaderBase.OriginAppId)) requestHeaderBase.OriginAppId = directToApp.SourceAppId;
                        if (string.IsNullOrEmpty(requestHeaderBase.TrackingId)) requestHeaderBase.TrackingId = Guid.NewGuid().ToString();
                        if (string.IsNullOrEmpty(requestHeaderBase.CorrelationId)) requestHeaderBase.CorrelationId = requestHeaderBase.TrackingId;

                        innerMessage.Descriptor.Fields.InFieldNumberOrder().First(field => field.PropertyName == "RequestHeader").Accessor.SetValue(innerMessage, requestHeaderBase);
                    }

                    // Auto populate the ResponseHeader if it's missing anything
                    if (innerMessage.Descriptor.Fields.InFieldNumberOrder().Any(field => field.PropertyName.Equals("ResponseHeader", StringComparison.InvariantCultureIgnoreCase))) {
                        MessageFormats.Common.ResponseHeader responseHeaderBase = innerMessage.Descriptor.Fields.InFieldNumberOrder().First(field => field.PropertyName == "ResponseHeader").Accessor.GetValue(innerMessage) as MessageFormats.Common.ResponseHeader ?? new MessageFormats.Common.ResponseHeader();

                        responseHeaderBase.AppId = directToApp.SourceAppId;
                        if (string.IsNullOrEmpty(responseHeaderBase.OriginAppId)) responseHeaderBase.OriginAppId = directToApp.SourceAppId;
                        if (string.IsNullOrEmpty(responseHeaderBase.TrackingId)) responseHeaderBase.TrackingId = Guid.NewGuid().ToString();
                        if (string.IsNullOrEmpty(responseHeaderBase.CorrelationId)) responseHeaderBase.CorrelationId = responseHeaderBase.TrackingId;
                        innerMessage.Descriptor.Fields.InFieldNumberOrder().First(field => field.PropertyName == "ResponseHeader").Accessor.SetValue(innerMessage, responseHeaderBase);
                    }

                    var targetParams = new object[] { innerMessage, directToApp }; // replace with actual parameter values

                    targetMethod.Invoke(messageHandlerService, targetParams);
                } catch (Exception ex) {
                    _logger.LogError("Error in message handler '{messageHandler}' for message type '{messageType}'.  Error: '{exception}'.  Stack Trace: '{stackTrace}'", serviceType.Assembly, directToApp.MessageType, ex.Message, ex.StackTrace);
                    throw;
                }

            }
        }
    }
}
