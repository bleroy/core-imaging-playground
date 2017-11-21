using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using System;
using System.Linq;

namespace ImageProcessing
{
    public class MultipleCliConfig : ManualConfig
    {
        public MultipleCliConfig()
        {
            this.Add(Job.RyuJitX64.With(CsProjCoreToolchain.NetCoreApp11).WithId(".Net Core 1.1 CLI"));
            this.Add(Job.RyuJitX64.With(CsProjCoreToolchain.NetCoreApp20).WithId(".Net Core 2.0 CLI"));
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var config = new MultipleCliConfig()
                   .With(DefaultConfig.Instance.GetLoggers().ToArray())
                   .With(DefaultConfig.Instance.GetColumnProviders().ToArray())
                   .With(MemoryDiagnoser.Default);

            Console.WriteLine(@"Choose an image resizing benchmarks:

0. Just run ""Load, Resize, Save"" once, don't benchmark
1. Resize
2. Load, resize, save
3. Load, resize, save in parallel

");
            switch (Console.ReadKey().Key)
            {
                case ConsoleKey.D0:
                    var lrs = new LoadResizeSave();
                    lrs.ImageSharpBenchmark();
                    lrs.MagickResizeBenchmark();
                    lrs.FreeImageResizeBenchmark();
                    lrs.MagicScalerResizeBenchmark();
                    lrs.SkiaBitmapLoadResizeSaveBenchmark();
                    lrs.SkiaCanvasLoadResizeSaveBenchmark();
                    break;
                case ConsoleKey.D1:
                    BenchmarkRunner.Run<Resize>(config);
                    break;
                case ConsoleKey.D2:
                    BenchmarkRunner.Run<LoadResizeSave>(config);
                    break;
                case ConsoleKey.D3:
                    BenchmarkRunner.Run<LoadResizeSaveParallel>(config);
                    break;
                default:
                    Console.WriteLine("Unrecognized command.");
                    break;
            }
        }
    }
}
