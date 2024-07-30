using Microsoft.Extensions.DependencyInjection;
namespace Microsoft.Extensions.Logging {
    public static class ConsoleLoggerExtensions {
        public static IServiceCollection AddAzureOrbitalFramework(this IServiceCollection services) {
            services.AddGrpc();
            services.AddOptions();
            services.AddSingleton<Microsoft.Azure.SpaceFx.Core.Client>();
            services.AddSingleton<Microsoft.Azure.SpaceFx.Core.Services.MessageReceiver>();
            services.AddSingleton<Microsoft.Azure.SpaceFx.Core.Services.HealthCheckService>();
            services.AddSingleton<Microsoft.Azure.SpaceFx.Core.Services.HeartbeatService>();
            services.AddSingleton<Microsoft.Azure.SpaceFx.Core.Services.ResourceUtilizationMonitor>();
            services.AddSingleton<Microsoft.Azure.SpaceFx.Core.Services.PluginLoader>();

            services.AddHttpClient<HttpClient>().ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler {
                CheckCertificateRevocationList = true
            });
            services.AddHttpClient<HttpClient>("sidecar", client => {
                // Set the base address of the named client.
                client.BaseAddress = new Uri("http://localhost");
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler {
                CheckCertificateRevocationList = true
            });

            return services;
        }
    }
}

