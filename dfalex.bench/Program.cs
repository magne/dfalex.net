using BenchmarkDotNet.Running;

namespace CodeHive.DfaLex.Bench
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkRunner.Run(typeof(Program).Assembly);
        }
    }
}
