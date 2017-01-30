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
            //lrs.FreeImageResizeBenchmark();
            //lrs.MagicScalerResizeBenchmark();
            new BenchmarkSwitcher(typeof(Program).GetTypeInfo().Assembly).Run(args);
        }
    }
}
