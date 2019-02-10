using BenchmarkDotNet.Running;
using System;

namespace ImageProcessing
{
    public class Program
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
                        lrs.ImageSharpBenchmark();
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
                    BenchmarkRunner.Run<Resize>();
                    break;
                case ConsoleKey.D2:
                    BenchmarkRunner.Run<LoadResizeSave>();
                    break;
                case ConsoleKey.D3:
                    BenchmarkRunner.Run<LoadResizeSaveParallel>();
                    break;
                default:
                    Console.WriteLine("Unrecognized command.");
                    break;
            }
        }
    }
}
