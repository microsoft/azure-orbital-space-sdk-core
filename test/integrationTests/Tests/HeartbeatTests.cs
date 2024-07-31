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
        TimeSpan heartbeatReceivedTolerance = TimeSpan.FromMilliseconds(TestSharedContext.HEARTBEAT_RECEIVED_TOLERANCE_MS);

        Console.WriteLine($"Waiting for {heartbeatReceivedTolerance} seconds, then checking for services heard...");
        Thread.Sleep(heartbeatReceivedTolerance);

        List<MessageFormats.Common.HeartBeatPulse> heartBeats = TestSharedContext.SPACEFX_CLIENT.ServicesOnline();

        heartBeats.ForEach((_heartBeat) => {
            Console.WriteLine($"Service Online: {_heartBeat.AppId}");
        });

        // We'll get at least 2 heartbeats - one for
        // this service, and one for the host service

        Assert.True(heartBeats.Count() > 1);
    }


    [Fact]
    public void CheckLivenessProbeWasReceived() {
        DateTime maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);

        while (TestSharedContext.HEALTH_CHECK_RECEIVED == false && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (TestSharedContext.HEALTH_CHECK_RECEIVED == false) {
            throw new TimeoutException($"Failed to hear Liveness Check service after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that Liveness probe is configured correctly");
        }

        Assert.True(TestSharedContext.HEALTH_CHECK_RECEIVED);
    }
}