using BenchmarkDotNet.Running;
using System.Reflection;

namespace ImageProcessing
{

    public class Program
    {
        public static void Main(string[] args)
        {
            //var lrs = new LoadResizeSave();
            //lrs.SystemDrawingResizeBenchmark();
            //lrs.ImageSharpBenchmark();
            //lrs.MagickResizeBenchmark();
            new BenchmarkSwitcher(typeof(Program).GetTypeInfo().Assembly).Run(args);
        }
    }
}
