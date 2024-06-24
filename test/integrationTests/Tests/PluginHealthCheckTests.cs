namespace Microsoft.Azure.SpaceFx.IntegrationTests;

[Collection(nameof(TestSharedContext))]
public class PluginHealthCheckTests : IClassFixture<TestSharedContext> {
    readonly TestSharedContext _context;

    public PluginHealthCheckTests(TestSharedContext context) {
        _context = context;
    }

    [Fact]
    public void QueryPluginHealthCheck() {
        DateTime maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);
        MessageFormats.Common.PluginHealthCheckMultiResponse? response = null;

        // Register a callback event to catch the response
        void PluginHealthCheckMultiResponseReceivedEventHandler(object? _, MessageFormats.Common.PluginHealthCheckMultiResponse _response) {
            response = _response;
            MessageHandler<MessageFormats.Common.PluginHealthCheckMultiResponse>.MessageReceivedEvent -= PluginHealthCheckMultiResponseReceivedEventHandler;
        }

        MessageHandler<MessageFormats.Common.PluginHealthCheckMultiResponse>.MessageReceivedEvent += PluginHealthCheckMultiResponseReceivedEventHandler;

        MessageFormats.Common.PluginHealthCheckRequest requestMessage = new() {
            RequestHeader = new MessageFormats.Common.RequestHeader() {
                TrackingId = Guid.NewGuid().ToString(),
                CorrelationId = Guid.NewGuid().ToString()
            }
        };

        Task.Run(async () => {
            await TestSharedContext.SPACEFX_CLIENT.DirectToApp(TestSharedContext.TARGET_SVC_APP_ID, requestMessage);
        });


        while (response == null && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (response == null) throw new TimeoutException($"Failed to hear {nameof(response)} after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TestSharedContext.TARGET_SVC_APP_ID} is deployed");

        Assert.NotNull(response);
        Assert.Equal(MessageFormats.Common.StatusCodes.Successful, response.ResponseHeader.Status);
        Assert.NotNull(response.PluginHealthCheckResponses);
        Assert.True(response.PluginHealthCheckResponses.Count > 0);
    }

}