

namespace Microsoft.Azure.SpaceFx.IntegrationTests;

[Collection(nameof(TestSharedContext))]
public class ProtoTests : IClassFixture<TestSharedContext> {
    readonly TestSharedContext _context;
    readonly string GenericGuid = Guid.NewGuid().ToString();
    readonly int GenericInt = 12345;
    readonly string GenericString = "Where's the kaboom?";
    readonly MapField<string, string> GenericMetaData = new() { { "Marvin", "Martian" } };
    readonly RepeatedField<string> GenericRepeatedString = new() { "Marvin", "Martian" };
    readonly MessageFormats.Common.StatusCodes GenericStatus = MessageFormats.Common.StatusCodes.Ready;
    readonly Timestamp GenericTimeStamp = Timestamp.FromDateTime(DateTime.MaxValue.ToUniversalTime());
    readonly StringValue GenericStringProto = new() { Value = "Looney Tunes" };
    readonly Any GenericAny = Any.Pack(new StringValue() { Value = "Looney Tunes" });

    public ProtoTests(TestSharedContext context) {
        _context = context;
    }

    [Fact]
    public void Topics_EnumTest() {
        List<string> possibleEnumValues = new List<string>() { "HeartbeatPulse" };
        CheckEnumerator<MessageFormats.Common.Topics>(possibleEnumValues);
    }

    [Fact]
    public void StatusCodes_EnumTest() {
        List<string> possibleEnumValues = new List<string>() { "Successful", "Unauthorized", "Forbidden", "NotFound", "GeneralFailure", "Healthy", "Ready", "Pending", "Transmitting", "NotApplicable", "Rejected", "Request", "ServiceUnavailable", "Timeout", "InternalServiceError", "InvalidArgument", "Unknown" };
        CheckEnumerator<MessageFormats.Common.StatusCodes>(possibleEnumValues);
    }

    [Fact]
    public void HostServices_EnumTest() {
        List<string> possibleEnumValues = new List<string>() { "Logging", "Position", "Sensor", "Link" };
        CheckEnumerator<MessageFormats.Common.HostServices>(possibleEnumValues);
    }

    [Fact]
    public void Priority_EnumTest() {
        List<string> possibleEnumValues = new List<string>() { "Low", "Medium", "High", "Critical" };
        CheckEnumerator<MessageFormats.Common.Priority>(possibleEnumValues);
    }

    [Fact]
    public void PlatformServices_EnumTest() {
        List<string> possibleEnumValues = new List<string>() { "Deployment", "Mts" };
        CheckEnumerator<MessageFormats.Common.PlatformServices>(possibleEnumValues);
    }

    [Fact]
    public void RequestHeader() {
        // Arrange
        List<string> expectedProperties = new() { "TrackingId", "CorrelationId", "Metadata", "AppId", "OriginAppId" };

        var request = new MessageFormats.Common.RequestHeader() {
            TrackingId = GenericGuid,
            CorrelationId = GenericGuid,
        };

        request.Metadata.Add(GenericMetaData);

        Assert.Equal(GenericGuid, request.TrackingId);
        Assert.Equal(GenericGuid, request.CorrelationId);
        Assert.True(request.Metadata.ContainsKey("Marvin"));
        CheckProperties<MessageFormats.Common.RequestHeader>(expectedProperties);
    }

    [Fact]
    public void ResponseHeader() {
        // Arrange
        List<string> expectedProperties = new() { "TrackingId", "CorrelationId", "Status", "Message", "AppId", "Metadata", "OriginAppId" };

        var request = new MessageFormats.Common.ResponseHeader() {
            TrackingId = GenericGuid,
            CorrelationId = GenericGuid,
            Status = GenericStatus,
            Message = GenericString,
            AppId = GenericString
        };

        request.Metadata.Add(GenericMetaData);

        Assert.Equal(GenericGuid, request.TrackingId);
        Assert.Equal(GenericGuid, request.CorrelationId);
        Assert.Equal(GenericStatus, request.Status);
        Assert.Equal(GenericString, request.Message);
        Assert.Equal(GenericString, request.AppId);
        Assert.True(request.Metadata.ContainsKey("Marvin"));
        CheckProperties<MessageFormats.Common.ResponseHeader>(expectedProperties);
    }

    [Fact]
    public void HeartBeatPulse() {
        // Arrange
        List<string> expectedProperties = new() { "ResponseHeader", "AppId", "CurrentSystemTime", "AppStartTime", "PulseFrequencyMS", "AppVersion" };

        var request = new MessageFormats.Common.HeartBeatPulse() {
            AppId = GenericString,
            CurrentSystemTime = GenericTimeStamp,
            AppStartTime = GenericTimeStamp,
            PulseFrequencyMS = GenericInt,
            AppVersion = GenericString
        };

        Assert.Equal(GenericString, request.AppVersion);
        Assert.Equal(GenericString, request.AppId);
        Assert.Equal(GenericTimeStamp, request.CurrentSystemTime);
        Assert.Equal(GenericTimeStamp, request.AppStartTime);
        Assert.Equal(GenericInt, request.PulseFrequencyMS);

        CheckProperties<MessageFormats.Common.HeartBeatPulse>(expectedProperties);
    }

    [Fact]
    public void DirectToApp() {
        // Arrange
        List<string> expectedProperties = new() { "ResponseHeader", "SourceAppId", "MessageType", "Message" };

        var request = new MessageFormats.Common.DirectToApp() {
            SourceAppId = GenericString,
            MessageType = GenericString,
            Message = GenericAny,
        };

        Assert.Equal(GenericString, request.SourceAppId);
        Assert.Equal(GenericString, request.MessageType);
        Assert.Equal(GenericAny, request.Message);

        CheckProperties<MessageFormats.Common.DirectToApp>(expectedProperties);
    }

    [Fact]
    public void CacheItem() {
        // Arrange
        List<string> expectedProperties = new() { "RequestHeader", "Name", "CreationTime", "ExpirationTime", "Item" };

        var request = new MessageFormats.Common.CacheItem() {
            Name = GenericString,
            CreationTime = GenericTimeStamp,
            ExpirationTime = GenericTimeStamp,
            Item = GenericAny,
        };

        Assert.Equal(GenericString, request.Name);
        Assert.Equal(GenericTimeStamp, request.CreationTime);
        Assert.Equal(GenericTimeStamp, request.ExpirationTime);
        Assert.Equal(GenericAny, request.Item);

        CheckProperties<MessageFormats.Common.CacheItem>(expectedProperties);
    }

    [Fact]
    public void LogMessage() {
        // Arrange
        List<string> expectedProperties = new() { "RequestHeader", "LogLevel", "Message", "Priority", "Category", "SubCategory", "IntValues", "FloatValues", "DateTimeValues", "StringValues", "LogTime", "LogReceivedTime", "LogTimeUserReadable" };

        var request = new MessageFormats.Common.LogMessage() {
            LogLevel = MessageFormats.Common.LogMessage.Types.LOG_LEVEL.Info,
            Priority = MessageFormats.Common.Priority.High,
            Message = GenericString,
            Category = GenericString,
            SubCategory = GenericString
        };

        Assert.Equal(MessageFormats.Common.LogMessage.Types.LOG_LEVEL.Info, request.LogLevel);
        Assert.Equal(MessageFormats.Common.Priority.High, request.Priority);
        Assert.Equal(GenericString, request.Message);
        Assert.Equal(GenericString, request.Category);
        Assert.Equal(GenericString, request.SubCategory);

        CheckProperties<MessageFormats.Common.LogMessage>(expectedProperties);
    }

    [Fact]
    public void LogMessageResponse() {
        // Arrange
        List<string> expectedProperties = new() { "ResponseHeader" };
        CheckProperties<MessageFormats.Common.LogMessageResponse>(expectedProperties);
    }

    [Fact]
    public void TelemetryMetricResponse() {
        // Arrange
        List<string> expectedProperties = new() { "ResponseHeader" };
        CheckProperties<MessageFormats.Common.TelemetryMetricResponse>(expectedProperties);
    }

    [Fact]
    public void HealthCheckRequest() {
        // Arrange
        List<string> expectedProperties = new() { "RequestHeader" };
        CheckProperties<MessageFormats.Common.HealthCheckRequest>(expectedProperties);
    }

    [Fact]
    public void HealthCheckResponse() {
        // Arrange
        List<string> expectedProperties = new() { "ResponseHeader" };
        CheckProperties<MessageFormats.Common.HealthCheckResponse>(expectedProperties);
    }

    [Fact]
    public void PluginHealthCheckRequest() {
        // Arrange
        List<string> expectedProperties = new() { "RequestHeader" };
        CheckProperties<MessageFormats.Common.PluginHealthCheckRequest>(expectedProperties);
    }

    [Fact]
    public void PluginHealthCheckResponse() {
        // Arrange
        List<string> expectedProperties = new() { "ResponseHeader" };
        CheckProperties<MessageFormats.Common.PluginHealthCheckResponse>(expectedProperties);
    }


    [Fact]
    public void PluginConfigurationRequest() {
        // Arrange
        List<string> expectedProperties = new() { "RequestHeader" };
        CheckProperties<MessageFormats.Common.PluginConfigurationRequest>(expectedProperties);
    }

    [Fact]
    public void PluginConfigurationResponse() {
        // Arrange
        List<string> expectedProperties = new() { "ResponseHeader" };
        CheckProperties<MessageFormats.Common.PluginHealthCheckResponse>(expectedProperties);

        expectedProperties = new() { "Status", "PluginName", "PluginPath", "ProcessingOrder", "Enabled", "CorePermissions", "Permissions" };
        CheckProperties<MessageFormats.Common.PluginConfigurationResponse.Types.PluginConfig>(expectedProperties);

        var plugin_request = new MessageFormats.Common.PluginConfigurationResponse.Types.PluginConfig() {
            Status = GenericStatus,
            PluginName = GenericString,
            PluginPath = GenericString,
            ProcessingOrder = GenericInt,
            Enabled = true,
            CorePermissions = GenericString,
            Permissions = GenericString
        };

        Assert.Equal(GenericStatus, plugin_request.Status);
        Assert.Equal(GenericString, plugin_request.PluginName);
        Assert.Equal(GenericString, plugin_request.PluginPath);
        Assert.Equal(GenericInt, plugin_request.ProcessingOrder);
        Assert.Equal(GenericString, plugin_request.CorePermissions);
        Assert.Equal(GenericString, plugin_request.Permissions);

        var request = new MessageFormats.Common.PluginConfigurationResponse();
        request.Plugins.Add(plugin_request);

        Assert.Contains(plugin_request, request.Plugins);
    }


    [Fact]
    public void VerifyResponseFromRequestUtil() {
        MessageFormats.Common.LogMessage logMessageRequest = new() {
            RequestHeader = new MessageFormats.Common.RequestHeader() {
                TrackingId = GenericGuid,
                CorrelationId = GenericGuid,
            },
            LogLevel = MessageFormats.Common.LogMessage.Types.LOG_LEVEL.Info,
            Priority = MessageFormats.Common.Priority.High,
            Message = GenericString,
            Category = GenericString,
            SubCategory = GenericString
        };

        MessageFormats.Common.LogMessageResponse logMessageResponse = Microsoft.Azure.SpaceFx.Core.Utils.ResponseFromRequest<MessageFormats.Common.LogMessage, MessageFormats.Common.LogMessageResponse>(logMessageRequest, new MessageFormats.Common.LogMessageResponse());

        Assert.Equal(GenericGuid, logMessageResponse.ResponseHeader.TrackingId);
        Assert.Equal(GenericGuid, logMessageResponse.ResponseHeader.CorrelationId);
        Assert.Equal(Microsoft.Azure.SpaceFx.MessageFormats.Common.StatusCodes.Unknown.ToString(), logMessageResponse.ResponseHeader.Status.ToString());
    }

    private static void CheckProperties<T>(List<string> expectedProperties) where T : IMessage, new() {
        T testMessage = new T();
        List<string> actualProperties = testMessage.Descriptor.Fields.InFieldNumberOrder().Select(field => field.PropertyName).ToList();

        Console.WriteLine($"...checking properties for {typeof(T)}");

        Assert.Equal(0, expectedProperties.Count(_prop => !actualProperties.Contains(_prop)));  // Check if there's any properties missing in the message
        Assert.Equal(0, actualProperties.Count(_prop => !expectedProperties.Contains(_prop)));  // Check if there's any properties we aren't expecting
    }

    private static void CheckEnumerator<T>(List<string> expectedEnumValues) where T : System.Enum {
        // Loop through and try to set all the enum values
        foreach (string enumValue in expectedEnumValues) {
            // This will throw a hard exception if we pass an item that doesn't work
            object? parsedEnum = System.Enum.Parse(typeof(T), enumValue);
            Assert.NotNull(parsedEnum);
        }

        // Make sure we don't have any extra values we didn't test
        int currentEnumCount = System.Enum.GetNames(typeof(T)).Length;

        Assert.Equal(expectedEnumValues.Count, currentEnumCount);
    }
}
