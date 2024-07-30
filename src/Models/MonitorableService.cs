namespace Microsoft.Azure.SpaceFx;

public partial class Core {
    public interface IMonitorableService {
        bool IsHealthy();
    }
}