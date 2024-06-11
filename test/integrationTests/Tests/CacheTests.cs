namespace Microsoft.Azure.SpaceFx.IntegrationTests;

[Collection(nameof(TestSharedContext))]
public class CacheTests : IClassFixture<TestSharedContext> {
    readonly TestSharedContext _context;

    public CacheTests(TestSharedContext context) {
        _context = context;
    }

    [Fact]
    public void CreateAndRetrieve_CacheItem() {
        DateTime maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);
        MessageFormats.Testing.SimpleMessage? retrievedMessage = null;
        Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage simpleMessage = new() {
            Message = "Testing"
        };

        bool isFinished = false;

        Task.Run(async () => {
            await TestSharedContext.SPACEFX_CLIENT.SaveCacheItem(cacheItem: simpleMessage, cacheItemName: "TestingMsg");
            isFinished = true;
        });


        while (isFinished == false && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (isFinished == false) throw new TimeoutException($"Failed to save cache after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.");


        isFinished = false;

        Task.Run(async () => {
            retrievedMessage = await TestSharedContext.SPACEFX_CLIENT.GetCacheItem<MessageFormats.Testing.SimpleMessage>(cacheItemName: "TestingMsg");
        });

        while (retrievedMessage == null && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        Assert.Equal(simpleMessage, retrievedMessage);
    }

    [Fact]
    public void Retrieve_NonExistantCache() {
        Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage? retrievedMessage = null;
        DateTime maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);
        bool isFinished = false;

        Task.Run(async () => {
            retrievedMessage = await TestSharedContext.SPACEFX_CLIENT.GetCacheItem<Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage>(cacheItemName: Guid.NewGuid().ToString());
            isFinished = true;
        });

        while (isFinished == false && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        Assert.Null(retrievedMessage);
    }

    [Fact]
    public void Test_ExpiredItem() {
        DateTime maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);

        Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage simpleMessage = new() {
            Message = "Expired Message"
        };

        bool isFinished = false;

        Task.Run(async () => {
            await TestSharedContext.SPACEFX_CLIENT.SaveCacheItem(cacheItem: simpleMessage, cacheItemName: "expiredTest", expiration: DateTime.UtcNow.AddMinutes(-5));
            isFinished = true;
        });


        while (isFinished == false && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (isFinished == false) throw new TimeoutException($"Failed to save cache after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.");


        MessageFormats.Testing.SimpleMessage? retrievedMessage = null;

        isFinished = false;

        Task.Run(async () => {
            retrievedMessage = await TestSharedContext.SPACEFX_CLIENT.GetCacheItem<MessageFormats.Testing.SimpleMessage>(cacheItemName: "expiredTest");
            isFinished = true;
        });

        while (isFinished == false && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        Assert.NotEqual(simpleMessage, retrievedMessage);
        Assert.Null(retrievedMessage);
    }

    [Fact]
    public void TestClearCache_Item() {
        DateTime maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);
        MessageFormats.Testing.SimpleMessage? retrievedMessage = null;
        List<string>? allCachedItems = null;
        Microsoft.Azure.SpaceFx.MessageFormats.Testing.SimpleMessage simpleMessage = new() {
            Message = "Testing"
        };

        bool isFinished = false;

        Task.Run(async () => {
            await TestSharedContext.SPACEFX_CLIENT.SaveCacheItem(cacheItem: simpleMessage, cacheItemName: "TestingMsg");
            isFinished = true;
        });


        while (isFinished == false && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (isFinished == false) throw new TimeoutException($"Failed to save cache after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.");


        isFinished = false;

        Task.Run(async () => {
            retrievedMessage = await TestSharedContext.SPACEFX_CLIENT.GetCacheItem<MessageFormats.Testing.SimpleMessage>(cacheItemName: "TestingMsg");
        });

        while (retrievedMessage == null && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        Assert.Equal(simpleMessage, retrievedMessage);

        maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);
        Task.Run(async () => {
            allCachedItems = await TestSharedContext.SPACEFX_CLIENT.GetAllCacheItems();
        });

        while (allCachedItems == null && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        Assert.NotNull(allCachedItems);

        maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);
        Task.Run(async () => {
            await TestSharedContext.SPACEFX_CLIENT.ClearCache();
            allCachedItems = await TestSharedContext.SPACEFX_CLIENT.GetAllCacheItems();
        });

        Assert.NotEmpty(allCachedItems);

    }
}