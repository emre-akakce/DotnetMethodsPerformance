using BenchmarkDotNet.Running;
using PerformanceBenchmarks.Benchmarks;

namespace PerformanceBenchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--all")
        {
            // Run all benchmarks
            BenchmarkRunner.Run<LookupBenchmarks>();
            BenchmarkRunner.Run<ParallelBenchmarks>();
            BenchmarkRunner.Run<CollectionBuildingBenchmarks>();
            BenchmarkRunner.Run<JoinOperationBenchmarks>();
        }
        else if (args.Length > 0)
        {
            // Run specific benchmark based on argument
            switch (args[0].ToLower())
            {
                case "lookup":
                    BenchmarkRunner.Run<LookupBenchmarks>();
                    break;
                case "parallel":
                    BenchmarkRunner.Run<ParallelBenchmarks>();
                    break;
                case "collection":
                    BenchmarkRunner.Run<CollectionBuildingBenchmarks>();
                    break;
                case "join":
                    BenchmarkRunner.Run<JoinOperationBenchmarks>();
                    break;
                default:
                    ShowUsage();
                    break;
            }
        }
        else
        {
            ShowUsage();
            Console.WriteLine("\nRunning interactive menu...\n");
            RunInteractiveMenu();
        }
    }

    private static void ShowUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run                  - Interactive menu");
        Console.WriteLine("  dotnet run --all            - Run all benchmarks");
        Console.WriteLine("  dotnet run lookup           - Run lookup benchmarks");
        Console.WriteLine("  dotnet run parallel         - Run parallel operation benchmarks");
        Console.WriteLine("  dotnet run collection       - Run collection building benchmarks");
        Console.WriteLine("  dotnet run join             - Run join operation benchmarks");
    }

    private static void RunInteractiveMenu()
    {
        while (true)
        {
            Console.WriteLine("\n=== Benchmark Menu ===");
            Console.WriteLine("1. Lookup Benchmarks (List vs HashSet vs Dictionary)");
            Console.WriteLine("2. Parallel Benchmarks (foreach vs Parallel.ForEach vs PLINQ)");
            Console.WriteLine("3. Collection Building Benchmarks");
            Console.WriteLine("4. Join Operation Benchmarks (mapping campaigns to prices)");
            Console.WriteLine("5. Run All Benchmarks");
            Console.WriteLine("0. Exit");
            Console.Write("\nSelect option: ");

            var input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    BenchmarkRunner.Run<LookupBenchmarks>();
                    break;
                case "2":
                    BenchmarkRunner.Run<ParallelBenchmarks>();
                    break;
                case "3":
                    BenchmarkRunner.Run<CollectionBuildingBenchmarks>();
                    break;
                case "4":
                    BenchmarkRunner.Run<JoinOperationBenchmarks>();
                    break;
                case "5":
                    BenchmarkRunner.Run<LookupBenchmarks>();
                    BenchmarkRunner.Run<ParallelBenchmarks>();
                    BenchmarkRunner.Run<CollectionBuildingBenchmarks>();
                    BenchmarkRunner.Run<JoinOperationBenchmarks>();
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }
}
