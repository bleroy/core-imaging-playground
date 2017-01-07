using BenchmarkDotNet.Running;
using System.Reflection;

namespace MonoImageProcessing
{
    class Program
    {
        public static void Main(string[] args)
        {
            //new LoadResizeSave().SkiaLoadResizeSaveBenchmark();
            new BenchmarkSwitcher(typeof(Program).GetTypeInfo().Assembly).Run(args);
        }
    }
}
