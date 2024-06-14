namespace Microsoft.Azure.SpaceFx.IntegrationTests;

[Collection(nameof(TestSharedContext))]
public class DirectToAppTests : IClassFixture<TestSharedContext> {
    readonly TestSharedContext _context;

    public DirectToAppTests(TestSharedContext context) {
        _context = context;
    }

    [Fact]
    public void TestSendingAMessage() {
        DateTime maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);
        MessageFormats.Testing.SimpleMessage? response = null;

        // Register a callback event to catch the response
        void SimpleMessageReceivedEventHandler(object? _, MessageFormats.Testing.SimpleMessage _response) {
            response = _response;
            MessageHandler<MessageFormats.Testing.SimpleMessage>.MessageReceivedEvent -= SimpleMessageReceivedEventHandler;
        }

        MessageHandler<MessageFormats.Testing.SimpleMessage>.MessageReceivedEvent += SimpleMessageReceivedEventHandler;


        MessageFormats.Testing.SimpleMessage testMessage = new() {
            RequestHeader = new MessageFormats.Common.RequestHeader() {
                TrackingId = Guid.NewGuid().ToString(),
                CorrelationId = Guid.NewGuid().ToString()
            },
            Message = "Testing"
        };

        Task.Run(async () => {
            await TestSharedContext.SPACEFX_CLIENT.DirectToApp(TestSharedContext.TARGET_SVC_APP_ID, testMessage);
        });


        while (response == null && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (response == null) throw new TimeoutException($"Failed to hear {nameof(response)} after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TestSharedContext.TARGET_SVC_APP_ID} is deployed");

        Assert.NotNull(response);
    }
}