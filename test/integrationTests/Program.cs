

namespace Microsoft.Azure.SpaceFx.IntegrationTests;

public class Program {
    internal static TestSharedContext TEST_SHARED_CONTEXT = new();
    public static void Main(string[] args) {
        Console.WriteLine("--------- Starting Tests ---------");
        RunTests<HeartbeatTests>();
        RunTests<CacheTests>();
        RunTests<DirectToAppTests>();
        RunTests<ProtoTests>();
        RunTests<CoreTests>();
        Console.WriteLine("--------- All Tests successful ---------");
    }

    // Dynamically loop through tests and call them
    private static void RunTests<T>() {
        T? testWrapper = (T) Activator.CreateInstance(typeof(T), new object[] { TEST_SHARED_CONTEXT });

        Console.WriteLine($"...Test Class: {testWrapper.GetType().Name}: START");

        // Get all the methods from ProtoTests that are using the Fact attribute
        var methods = Assembly.GetExecutingAssembly().GetTypes()
                      .Where(t => t.FullName == typeof(T).FullName)
                      .SelectMany(t => t.GetMethods())
                      .Where(m => m.GetCustomAttributes(typeof(FactAttribute), false).Length > 0)
                      .ToArray();

        // Loop through what we found and run the test
        foreach (var testMethod in methods) {
            Console.WriteLine($"......Test '{testWrapper.GetType().Name} / {testMethod.Name}': START...");
            testMethod.DeclaringType?.GetMethod(testMethod.Name)?.Invoke(testWrapper, null);
            Console.WriteLine($"......Test '{testWrapper.GetType().Name} / {testMethod.Name}': SUCCESS");
        }

        Console.WriteLine($"...Test Class: {testWrapper.GetType().Name}: END");
    }
}