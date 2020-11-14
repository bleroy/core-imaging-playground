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
using BenchmarkDotNet.Toolchains.CsProj;

namespace ImageProcessing
{
    public class ShortRunWithMemoryDiagnoserConfig : ManualConfig
    {
        public ShortRunWithMemoryDiagnoserConfig()
        {
            this.AddJob(Job.ShortRun
#if NETCOREAPP2_1
                .WithToolchain(CsProjCoreToolchain.NetCoreApp21)
                .WithId(".Net Core 2.1 CLI")
#elif NETCOREAPP3_1
                .WithToolchain(CsProjCoreToolchain.NetCoreApp31)
                .WithId(".Net Core 3.1 CLI")
#elif NET5_0
                .WithToolchain(CsProjCoreToolchain.NetCoreApp50)
                .WithId(".Net 5.0 CLI")
#endif

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
                // ImageMagick native binaries are currently only available for X86 and X64
                this.AddFilter(new NameFilter(name => !name.StartsWith("Magick")));
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
                return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
#endif
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
                        if (RuntimeInformation.OSArchitecture is Architecture.X86 or Architecture.X64)
                        {
                            lrs.MagickBenchmark();
                        }
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
                        .AddFilter(new NameFilter(name => name.EndsWith("Parallel"))));
                    break;
                default:
                    Console.WriteLine("Unrecognized command.");
                    break;
            }
        }
    }
}
