namespace PayloadApp.DebugHost;

public class Program {
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddJsonFile("/workspaces/spacesdk-core/test/debugHost/appsettings.json", optional: true, reloadOnChange: true);

        List<Microsoft.Azure.SpaceFx.Core.Models.PLUG_IN> plugins = new List<Microsoft.Azure.SpaceFx.Core.Models.PLUG_IN>();
        // plugins.Add(new Microsoft.Azure.SpaceFx.Core.Models.PLUG_IN() {
        //     PLUGIN_PATH = "/workspaces/spacesdk-core/test/debugHostPlugin/bin/Debug/net6.0/debugHostPlugin.dll",
        //     CORE_PERMISSIONS = Microsoft.Azure.SpaceFx.Core.Models.PLUG_IN.CorePermissions.ALL
        // });

        builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(50051, o => o.Protocols = HttpProtocols.Http2))
        .ConfigureServices((services) => {
            services.AddAzureOrbitalFramework();
            services.AddHostedService<Worker>();
            services.AddHostedService<WorkerBravo>();
            services.AddSingleton<Microsoft.Azure.SpaceFx.Core.IMessageHandler<Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage>, MessageHandler<Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage>>();
            services.AddSingleton(plugins);
            services.AddSingleton<Utils.PluginDelegates>();
        }).ConfigureLogging((logging) => {
            logging.AddProvider(new Microsoft.Extensions.Logging.SpaceFX.Logger.HostSvcLoggerProvider());
            // logging.ClearProviders();
            // logging.AddConsole();
        });

        var app = builder.Build();

        app.UseRouting();
        app.UseEndpoints(endpoints => {
            endpoints.MapGrpcService<Microsoft.Azure.SpaceFx.Core.Services.MessageReceiver>();
            endpoints.MapGrpcService<Microsoft.Azure.SpaceFx.Core.Services.HealthCheckService>();
            endpoints.MapGet("/", async context => {
                await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
            });
        });

        // Add a middleware to catch exceptions and stop the host gracefully
        app.Use(async (context, next) => {
            try {
                await next.Invoke();
            } catch (Exception ex) {
                Console.Error.WriteLine($"Exception caught in middleware: {ex.Message}");

                // Stop the host gracefully so it triggers the pod to error
                var lifetime = context.RequestServices.GetService<IHostApplicationLifetime>();
                lifetime?.StopApplication();
            }
        });

        app.Run();
    }
}