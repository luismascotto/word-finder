using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
#if DEBUG
        _ = BenchmarkRunner.Run<BenchmarksReadWords>(new DebugInProcessConfig());
#else
        _ = BenchmarkRunner.Run<BenchmarksReadWords>();
#endif
    }
}