using System;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;

namespace ImageProcessing
{
    public class ShortRunWithMemoryDiagnoserConfig : ManualConfig
    {
        public ShortRunWithMemoryDiagnoserConfig()
        {
            this.Add(
                Job.RyuJitX64.With(CsProjCoreToolchain.NetCoreApp22).WithId(".Net Core 2.2 CLI")
                .WithWarmupCount(5)
                .WithIterationCount(5));

            this.Add(DefaultConfig.Instance.GetLoggers().ToArray());
            this.Add(DefaultConfig.Instance.GetColumnProviders().ToArray());
            this.Add(MemoryDiagnoser.Default);
        }
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
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
                        lrs.SystemDrawingResizeBenchmark();
                        lrs.ImageSharpResizeBenchmark();
                        lrs.MagickResizeBenchmark();
                        lrs.FreeImageResizeBenchmark();
                        lrs.MagicScalerResizeBenchmark();
                        lrs.SkiaBitmapLoadResizeSaveBenchmark();
                        lrs.SkiaCanvasLoadResizeSaveBenchmark();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                    break;
                case ConsoleKey.D1:
                    BenchmarkRunner.Run<Resize>(new ShortRunWithMemoryDiagnoserConfig());
                    break;
                case ConsoleKey.D2:
                    BenchmarkRunner.Run<LoadResizeSave>(new ShortRunWithMemoryDiagnoserConfig());
                    break;
                case ConsoleKey.D3:
                    BenchmarkRunner.Run<LoadResizeSaveParallel>(new ShortRunWithMemoryDiagnoserConfig());
                    break;
                default:
                    Console.WriteLine("Unrecognized command.");
                    break;
            }
        }
    }
}
