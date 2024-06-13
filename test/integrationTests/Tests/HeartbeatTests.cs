namespace Microsoft.Azure.SpaceFx.IntegrationTests;

[Collection(nameof(TestSharedContext))]
public class HeartbeatTests : IClassFixture<TestSharedContext> {
    readonly TestSharedContext _context;

    public HeartbeatTests(TestSharedContext context) {
        _context = context;
    }

    [Fact]
    public void CheckHeartbeatsAreBeingReceived() {
        // Services send out HeartBeats to let other apps know they are online.
        // We have to give enough time for heartbeats to come in before we check
        Console.WriteLine($"Waiting for 3 seconds, then checking for services heard...");
        Thread.Sleep(3500);

        List<MessageFormats.Common.HeartBeatPulse> heartBeats = TestSharedContext.SPACEFX_CLIENT.ServicesOnline();

        heartBeats.ForEach((_heartBeat) => {
            Console.WriteLine($"Service Online: {_heartBeat.AppId}");
        });

        // We'll get at least 2 heartbeats - one for
        // this service, and one for the host service

        Assert.True(heartBeats.Count() > 1);
    }
}