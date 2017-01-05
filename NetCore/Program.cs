using BenchmarkDotNet.Running;
using System.Reflection;

namespace ImageProcessing
{

    public class Program
    {
        public static void Main(string[] args)
        {
            new BenchmarkSwitcher(typeof(Program).GetTypeInfo().Assembly).Run(args);
        }
    }
}
