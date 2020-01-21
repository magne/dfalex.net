using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using CodeHive.DfaLex.tree;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace CodeHive.DfaLex.Tests.tree
{
    public class Benchmarks
    {
        private const int InputSize = 10;
        private const int SpinUp    = 1000;

        private readonly ITestOutputHelper helper;

        public Benchmarks(ITestOutputHelper helper)
        {
            this.helper = helper;
        }

        [Fact]
        public void NonBackTracking()
        {
            const string regex = "((a+b)+c)+";

            var sb = new StringBuilder();
            for (var i = 0; i < InputSize; i++)
            {
                for (var j = 0; j < 200; j++)
                {
                    sb.Append('a');
                }

                sb.Append("bc");
            }

            var input = sb.ToString();

            var dotNetCount = TimeDotNet(input, regex);
            var treeCount = TimeTreeMatcher(input, regex);
            helper.WriteLine("Search per second in 2K string:");
            helper.WriteLine($"DotNet Regex: {dotNetCount}    Tree: {treeCount}\n");
        }

        [Theory]
        [InlineData(13)]
        [InlineData(14)]
        [InlineData(15)]
        [InlineData(16)]
        [InlineData(17)]
        [InlineData(18)]
        public void PathologicalBenchmark(int inputSize)
        {
            var b = new StringBuilder();
            for (var i = 0; i < inputSize; i++)
            {
                b.Append('a');
            }

            var input = b.ToString();

            // Build the regex
            b = new StringBuilder();
            for (var i = 0; i < inputSize; i++)
            {
                b.Append("a?");
            }

            for (var i = 0; i < inputSize; i++)
            {
                b.Append("a");
            }

            var regex = b.ToString();

            var dotNetCount = TimeDotNet(input, regex);
            var treeCount = TimeTreeMatcher(input, regex);
            helper.WriteLine("Pathological Search per second:");
            helper.WriteLine($"DotNet Regex: {dotNetCount}    Tree: {treeCount}\n");
        }

        [Fact]
        public void ClassNameBenchmark()
        {
            const string regex = "(.*?([a-z]+\\.)*([A-Z][a-zA-Z]*))*.*?";

            var b = new StringBuilder();
            helper.WriteLine(Directory.GetCurrentDirectory());
            var no = 5;
            foreach (var file in Directory.EnumerateFiles("../../../../dfalex", "*.cs", SearchOption.AllDirectories))
            {
                if (no-- == 0)
                {
                    break;
                }
                helper.WriteLine(file);
                using var sr = new StreamReader(file);
                b.Append(sr.ReadToEnd());
            }

            var input = b.ToString();
            input = input.Substring(input.Length * 3 / 4);

            var dotNetCount = TimeDotNet(input, regex);
            var treeCount = TimeTreeMatcher(input, regex);
            helper.WriteLine("Code Search per second:");
            helper.WriteLine($"DotNet Regex: {dotNetCount}    Tree: {treeCount}\n");
        }

        private int TimeDotNet(string input, string regex)
        {
            var count = 0;
            var options = System.Text.RegularExpressions.RegexOptions.Compiled;
            var dotnetPat = new Regex(regex, options);
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            for (var t = stopWatch.ElapsedMilliseconds; t < SpinUp + 1000; t = stopWatch.ElapsedMilliseconds)
            {
                var matches = dotnetPat.Matches(input);
                matches.Count.Should().NotBe(0);
                if (t >= SpinUp)
                {
                    ++count;
                }
            }

            return count;
        }

        private int TimeTreeMatcher(string input, string regex)
        {
            var count = 0;
            var interpreter = TDFAInterpreter.compile(regex);
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            for (var t = stopWatch.ElapsedMilliseconds; t < SpinUp + 1000; t = stopWatch.ElapsedMilliseconds)
            {
                var res = interpreter.interpret(input);
                res.group().Length.Should().NotBe(0);
                if (t >= SpinUp)
                {
                    ++count;
                }
            }

            return count;
        }
    }
}
