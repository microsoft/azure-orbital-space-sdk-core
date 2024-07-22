namespace Microsoft.Azure.SpaceFx.IntegrationTestHost;

public class Program {
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddJsonFile("/workspaces/spacesdk-core/test/debugHost/appsettings.json", optional: true, reloadOnChange: true);

        builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(50051, o => o.Protocols = HttpProtocols.Http2))
        .ConfigureServices((services) => {
            services.AddAzureOrbitalFramework();
            services.AddHostedService<WorkerBravo>();
            services.AddSingleton<Core.IMessageHandler<MessageFormats.Testing.SimpleMessage>, MessageHandler<MessageFormats.Testing.SimpleMessage>>();
            services.AddSingleton<Utils.PluginDelegates>();
        }).ConfigureLogging((logging) => {
            logging.AddProvider(new Microsoft.Extensions.Logging.SpaceFX.Logger.HostSvcLoggerProvider());
        });

        var app = builder.Build();

        app.UseRouting();
        app.UseEndpoints(endpoints => {
            endpoints.MapGrpcService<Core.Services.MessageReceiver>();
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