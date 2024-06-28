namespace Microsoft.Azure.SpaceFx.IntegrationTests;

/// <summary>
/// We only get one opportunity to build our client per deployment
/// This class allows us to instantiate and share the build context across
/// multiple test runs
/// </summary>
public class TestSharedContext : IDisposable {
    internal static string TARGET_SVC_APP_ID = "spacesdk-core";
    private static TestSharedContext TextContext { get; set; } = null!;
    private static WebApplication _grpcHost { get; set; } = null!;
    internal static bool IS_ONLINE = false;
    internal static string APP_ID = "";
    internal static Core.Client SPACEFX_CLIENT = null!;
    internal static bool HOST_SVC_ONLINE = false;
    internal static TimeSpan MAX_TIMESPAN_TO_WAIT_FOR_MSG = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Setup the SpaceFx Core to be shared across tests
    /// </summary>
    public TestSharedContext() {
        if (_grpcHost != null) return;

        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddJsonFile("/workspaces/spacesdk-core/test/integrationTests/appsettings.json", optional: true, reloadOnChange: true);

        builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(50051, o => o.Protocols = HttpProtocols.Http2))
        .ConfigureServices((services) => {
            services.AddAzureOrbitalFramework();
            services.AddHostedService<ServiceCallback>();
            services.AddSingleton<Core.IMessageHandler<MessageFormats.Testing.SimpleMessage>, MessageHandler<MessageFormats.Testing.SimpleMessage>>();
            services.AddSingleton<Core.IMessageHandler<MessageFormats.Common.PluginHealthCheckMultiResponse>, MessageHandler<MessageFormats.Common.PluginHealthCheckMultiResponse>>();
        }).ConfigureLogging((logging) => {
            logging.AddProvider(new Microsoft.Extensions.Logging.SpaceFX.Logger.HostSvcLoggerProvider());
        });

        _grpcHost = builder.Build();

        _grpcHost.UseRouting();
        _grpcHost.UseEndpoints(endpoints => {
            endpoints.MapGrpcService<Core.Services.MessageReceiver>();
            endpoints.MapGet("/", async context => {
                await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
            });
        });

        _grpcHost.StartAsync();

        // Waiting for the _grpcHost to spin up
        while (TestSharedContext.IS_ONLINE == false) {
            Thread.Sleep(250);
        }

        Console.WriteLine($"Waiting for '{TARGET_SVC_APP_ID}' to come online...");
        List<MessageFormats.Common.HeartBeatPulse> heartBeats = TestSharedContext.SPACEFX_CLIENT.ServicesOnline();

        DateTime maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);

        while (heartBeats.Any(_heartbeat => _heartbeat.AppId.Equals(TARGET_SVC_APP_ID, StringComparison.InvariantCultureIgnoreCase) == false) && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(250);
            heartBeats = TestSharedContext.SPACEFX_CLIENT.ServicesOnline();
        }

        if (heartBeats.Any(_heartbeat => _heartbeat.AppId.Equals(TARGET_SVC_APP_ID, StringComparison.InvariantCultureIgnoreCase) == false)) {
            throw new TimeoutException($"Failed to get '{TARGET_SVC_APP_ID}' online after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}");
        }

        Console.WriteLine($"'{TARGET_SVC_APP_ID}' is online.");

    }

    public static void WritePropertyLineToScreen(string testName, string propertyName) {
        Console.WriteLine($"[{testName}] testing property '{propertyName}'");
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
    }
}

[CollectionDefinition(nameof(TestSharedContext))]
public class TestSharedContextCollection : ICollectionFixture<TestSharedContext> {
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}


public class MessageHandler<T> : Microsoft.Azure.SpaceFx.Core.IMessageHandler<T> where T : notnull {
    private readonly ILogger<MessageHandler<T>> _logger;
    private readonly Microsoft.Azure.SpaceFx.Core.Services.PluginLoader _pluginLoader;
    private readonly IServiceProvider _serviceProvider;
    public static event EventHandler<T>? MessageReceivedEvent;
    public MessageHandler(ILogger<MessageHandler<T>> logger, Microsoft.Azure.SpaceFx.Core.Services.PluginLoader pluginLoader, IServiceProvider serviceProvider) {
        _logger = logger;
        _pluginLoader = pluginLoader;
        _serviceProvider = serviceProvider;
    }

    public void MessageReceived(T message, MessageFormats.Common.DirectToApp fullMessage) {
        using (var scope = _serviceProvider.CreateScope()) {
            _logger.LogInformation($"Receieved message type '{typeof(T).Name}'");

            if (MessageReceivedEvent != null) {
                foreach (Delegate handler in MessageReceivedEvent.GetInvocationList()) {
                    Task.Factory.StartNew(
                        () => handler.DynamicInvoke(fullMessage.ResponseHeader.AppId, message));
                }
            }
        }
    }
}


public class ServiceCallback : BackgroundService {
    private readonly ILogger<ServiceCallback> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Microsoft.Azure.SpaceFx.Core.Client _client;
    private readonly string _appId;

    public ServiceCallback(ILogger<ServiceCallback> logger, IServiceProvider serviceProvider) {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _client = _serviceProvider.GetService<Microsoft.Azure.SpaceFx.Core.Client>() ?? throw new NullReferenceException($"{nameof(Microsoft.Azure.SpaceFx.Core.Client)} is null");
        _appId = _client.GetAppID().Result;


    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        // while (!stoppingToken.IsCancellationRequested) {
        using (var scope = _serviceProvider.CreateScope()) {
            TestSharedContext.IS_ONLINE = true;
            TestSharedContext.APP_ID = _appId;
            TestSharedContext.SPACEFX_CLIENT = _client;
        }
    }
}