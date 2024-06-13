using Microsoft.Azure.SpaceFx.MessageFormats.Common;
using Microsoft.Azure.SpaceFx.MessageFormats.Testing;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.SpaceFx.IntegrationTestHostPlugin;
public class Class1 : IntegrationTestHost.PluginBase {
    public override ILogger Logger { get; set; }

    public Class1() {
        LoggerFactory loggerFactory = new();
        Logger = loggerFactory.CreateLogger<Class1>();
    }

    public override Task BackgroundTask() => Task.Run(() => {
        string testConfig = Core.GetConfigSetting("testConfig").Result;
        if (testConfig != "Hello Space World") {
            throw new ApplicationException("testConfig is not set to 'Hello Space World'");
        }
        while (true) {
            Logger.LogInformation("Heard the background task!");
            Thread.Sleep(1000);
        }
    });

    public override void ConfigureLogging(ILoggerFactory loggerFactory) => Logger = loggerFactory.CreateLogger<Class1>();

    public override Task<PluginHealthCheckResponse> PluginHealthCheckResponse() {
        throw new NotImplementedException();
    }

    public override Task<SimpleMessage?> SimpleMessage(SimpleMessage? input_request) => Task.Run(() => {
        if (input_request == null) return input_request;
        Logger.LogInformation("Received SimpleMessage from {appId}.  Sending message back", input_request.RequestHeader.AppId);

        Core.DirectToApp(appId: input_request.RequestHeader.AppId, message: input_request).Wait();

        return input_request;
    });


    public override Task<(SimpleMessage?, ComplexMessage?)> SimpleComplexMessage(SimpleMessage? input_request, ComplexMessage? input_response) => Task.Run(() => {
        Logger.LogInformation("I heard a SimpleComplex Message!");
        return (input_request, input_response);
    });
}
