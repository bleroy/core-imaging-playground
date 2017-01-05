using BenchmarkDotNet.Running;

namespace ImageProcessing
{
    using System.Reflection;

    public class Program
    {
        public static void Main(string[] args)
        {
            new BenchmarkSwitcher(typeof(Program).GetTypeInfo().Assembly).Run(args);
        }
    }
}
