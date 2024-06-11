namespace Microsoft.Azure.SpaceFx.IntegrationTests;

[Collection(nameof(TestSharedContext))]
public class CoreTests : IClassFixture<TestSharedContext> {
    readonly TestSharedContext _context;

    public CoreTests(TestSharedContext context) {
        _context = context;
    }

    [Fact]
    public void CheckHeartBeatsAreHeard() {
        List<MessageFormats.Common.HeartBeatPulse> heartbeatPulses = new List<MessageFormats.Common.HeartBeatPulse>();
        heartbeatPulses = Core.ServicesOnline();

        // We ran too fast - wait a little bit and try again
        if (heartbeatPulses.Count < 2) {
            Thread.Sleep(6000);
            heartbeatPulses = Core.ServicesOnline();
        }

        Assert.True(heartbeatPulses.Count >= 2);
    }
}