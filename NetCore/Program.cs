using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;

namespace ImageProcessing
{
    public class ShortRunWithMemoryDiagnoserConfig : ManualConfig
    {
        public ShortRunWithMemoryDiagnoserConfig()
        {
            this.Add(Job.ShortRun
                .With(CsProjCoreToolchain.NetCoreApp22)
                .WithId(".Net Core 2.2 CLI")
                .WithWarmupCount(5)
                .WithIterationCount(5));

            this.Add(DefaultColumnProviders.Instance);
            this.Add(ConsoleLogger.Default);
            this.Add(MarkdownExporter.GitHub);
            this.Add(MemoryDiagnoser.Default);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // MagicScaler requires Windows Imaging Component (WIC) which is only available on Windows
                this.Add(new NameFilter(name => !name.StartsWith("MagicScaler")));
            }

            // Disable this policy because the benchmarks refer
            // to a non-optimized SkiaSharp that we do not own.
            this.Options |= ConfigOptions.DisableOptimizationsValidator;
        }
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
            var config = new ShortRunWithMemoryDiagnoserConfig();

            Console.WriteLine(@"Choose an image resizing benchmarks:

0. Just run ""Load, Resize, Save"" once, don't benchmark
1. Resize
2. Load, resize, save
3. Load, resize, save in parallel

");
            switch (Console.ReadKey().Key)
            {
                case ConsoleKey.D0:
                    try
                    {
                        var lrs = new LoadResizeSave();
                        lrs.SystemDrawingBenchmark();
                        lrs.ImageSharpBenchmark();
                        lrs.MagickBenchmark();
                        lrs.FreeImageBenchmark();
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            lrs.MagicScalerBenchmark();
                        }
                        lrs.SkiaBitmapBenchmark();
                        lrs.SkiaCanvasBenchmark();
                        lrs.NetVipsBenchmark();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                    break;
                case ConsoleKey.D1:
                    BenchmarkRunner.Run<Resize>(config);
                    break;
                case ConsoleKey.D2:
                    BenchmarkRunner.Run<LoadResizeSave>(config);
                    break;
                case ConsoleKey.D3:
                    // Only run the *Parallel benchmarks
                    BenchmarkRunner.Run<LoadResizeSaveParallel>(config
                        .With(new NameFilter(name => name.EndsWith("Parallel"))));
                    break;
                default:
                    Console.WriteLine("Unrecognized command.");
                    break;
            }
        }
    }
}