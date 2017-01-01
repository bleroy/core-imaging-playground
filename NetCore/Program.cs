using BenchmarkDotNet.Running;
using System;

namespace ImageProcessing
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<ImageResizingBenchmarks>();
            Console.WriteLine(summary);
        }
    }
}
