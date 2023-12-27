using System;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
#if Windows_NT
using System.Security.Principal;
using BenchmarkDotNet.Diagnostics.Windows;
#endif
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using System.Threading.Tasks;

namespace ImageProcessing
{
    public class ShortRunWithMemoryDiagnoserConfig : ManualConfig
    {
        public ShortRunWithMemoryDiagnoserConfig()
        {
            this.AddJob(Job.ShortRun
                .WithWarmupCount(5)
                .WithIterationCount(5));

            this.AddColumnProvider(DefaultColumnProviders.Instance);
            this.AddLogger(ConsoleLogger.Default);
            this.AddExporter(MarkdownExporter.GitHub);
            this.AddDiagnoser(MemoryDiagnoser.Default);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // MagicScaler requires Windows Imaging Component (WIC) which is only available on Windows
                this.AddFilter(new NameFilter(name => !name.StartsWith("MagicScalerBenchmark")));
            }

            if (RuntimeInformation.OSArchitecture is not (Architecture.X86 or Architecture.X64))
            {
                // ImageFlow native binaries are currently only available for X86 and X64
                this.AddFilter(new NameFilter(name => !name.StartsWith("ImageFlow")));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                RuntimeInformation.OSArchitecture == Architecture.Arm64)
            {
                // FreeImage native binaries are not available for Windows ARM64
                this.AddFilter(new NameFilter(name => !name.StartsWith("FreeImage")));
            }

#if Windows_NT
            if (this.IsElevated)
            {
                this.AddDiagnoser(new NativeMemoryProfiler());
            }
#endif
        }

#if Windows_NT
        private bool IsElevated
        {
            get
            {
                return OperatingSystem.IsWindows() && new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
#endif
    }

    public static class Program
    {
        public async static Task Main(string[] args)
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
                case ConsoleKey.NumPad0:
                    try
                    {
                        var lrs = new LoadResizeSave();
                        lrs.SystemDrawingBenchmark();
                        lrs.ImageSharpBenchmark();
                        lrs.ImageSharpTargetedDecodeBenchmark();
                        lrs.MagickBenchmark();
                        if (RuntimeInformation.OSArchitecture is Architecture.X86 or Architecture.X64)
                        {
                            await lrs.ImageFlowBenchmark();
                        }
                        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                            RuntimeInformation.OSArchitecture != Architecture.Arm64)
                        {
                            lrs.FreeImageBenchmark();
                        }
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            lrs.MagicScalerBenchmark();
                        }
                        lrs.SkiaBitmapBenchmark();
                        lrs.NetVipsBenchmark();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                    break;
                case ConsoleKey.D1:
                case ConsoleKey.NumPad1:
                    BenchmarkRunner.Run<Resize>(config);
                    break;
                case ConsoleKey.D2:
                case ConsoleKey.NumPad2:
                    BenchmarkRunner.Run<LoadResizeSave>(config);
                    break;
                case ConsoleKey.D3:
                case ConsoleKey.NumPad3:
                    // Only run the *Parallel benchmarks
                    BenchmarkRunner.Run<LoadResizeSaveParallel>(config
                        .AddFilter(new NameFilter(name => name.EndsWith("Parallel"))));
                    break;
                default:
                    Console.WriteLine("Unrecognized command.");
                    break;
            }
        }
    }
}