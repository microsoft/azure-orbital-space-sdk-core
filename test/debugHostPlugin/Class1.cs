using Microsoft.Azure.SpaceFx;
using Microsoft.Azure.SpaceFx.MessageFormats.Common;
using Microsoft.Azure.SpaceFx.MessageFormats.Testing;
using Microsoft.Extensions.Logging;

namespace debugHostPlugin;
public class Class1 : PayloadApp.DebugHost.PluginBase {
    public override ILogger Logger { get; set; }

    public Class1() {
        LoggerFactory loggerFactory = new();
        this.Logger = loggerFactory.CreateLogger<Class1>();
    }

    public override Task BackgroundTask() => Task.Run(() => {

        while (true) {
            Logger.LogInformation("Heard the background task!");

            SimpleMessage simpleMessage = new SimpleMessage();
            simpleMessage.Message = "Simple Message";

            Core.DirectToApp(Core.GetAppID().Result, simpleMessage).Wait();

            Thread.Sleep(1000);
        }
    });

    public override void ConfigureLogging(ILoggerFactory loggerFactory) => Logger = loggerFactory.CreateLogger<Class1>();

    public override Task<PluginHealthCheckResponse> PluginHealthCheckResponse() {
        throw new NotImplementedException();
    }

    public override Task<SimpleMessage?> SimpleMessage(SimpleMessage? input_request) {
        Logger.LogInformation("I heard a Simple Message!");

        return Task.FromResult(input_request);
    }

    public override Task<(SimpleMessage?, ComplexMessage?)> SimpleComplexMessage(SimpleMessage? input_request, ComplexMessage? input_response) => Task.Run(() => {
        Logger.LogInformation("I heard a SimpleComplex Message!");
        return (input_request, input_response);
    });
}
