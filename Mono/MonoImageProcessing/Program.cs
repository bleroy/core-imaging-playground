using System;
using BenchmarkDotNet.Running;

namespace MonoImageProcessing
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<ImageResizingBenchmarks>();
            Console.WriteLine(summary);
        }
    }
}
