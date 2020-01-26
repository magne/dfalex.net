using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace CodeHive.DfaLex.Tests
{
    public class RegexSpeedTest
    {
        private const int SpinUp = 1000;

        private readonly ITestOutputHelper helper;

        public RegexSpeedTest(ITestOutputHelper helper)
        {
            this.helper = helper;
        }

        [Fact]
        public void NotFoundReplaceTest()
        {
            var patString = ("01235|/|456*1|abc|_|\\..*|013|0?1?2?3?4?57");
            string src;
            {
                var sb = new StringBuilder();
                for (var i = 0; i < 10000; i++)
                {
                    sb.Append("0123456789");
                }

                src = sb.ToString();
            }

            var dotNetCount = TimeDotNet(src, patString);
            var srCount = TimeSearchAndReplaceBuilder(src, patString);
            var matcherCount = TimeMatcher(src, patString);
            helper.WriteLine("Search+Replace per second in 100K string, patterns not found:");
            helper.WriteLine($"DotNet Regex: {dotNetCount}    SearchAndReplaceBuilder: {srCount}    StringMatcher: {matcherCount}\n");
        }

        private int TimeDotNet(string src, string patString)
        {
            var count = 0;
            var options = System.Text.RegularExpressions.RegexOptions.Compiled;
            var dotnetPat = new Regex(patString, options);
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var str = src;
            for (var t = stopWatch.ElapsedMilliseconds; t < SpinUp + 1000; t = stopWatch.ElapsedMilliseconds)
            {
                str = dotnetPat.Replace(str, string.Empty);
                if (t >= SpinUp)
                {
                    ++count;
                }
            }

            Assert.Equal(src, str);
            return count;
        }

        private int TimeSearchAndReplaceBuilder(string src, string patString)
        {
            Func<string, string> replacer;
            {
                var builder = new SearchAndReplaceBuilder();
                builder.AddReplacement(Pattern.Regex(patString), (dest, srcStr, s, e) => 0);
                replacer = builder.BuildStringReplacer();
            }

            var count = 0;
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var str = src;
            for (var t = stopWatch.ElapsedMilliseconds; t < SpinUp + 1000; t = stopWatch.ElapsedMilliseconds)
            {
                str = replacer(str);
                if (t >= SpinUp)
                {
                    ++count;
                }
            }

            Assert.Equal(src, str);
            return count;
        }

        private int TimeMatcher(string src, string patString)
        {
            DfaState<bool> startState;
            {
                var builder = new DfaBuilder<bool>();
                builder.AddPattern(Pattern.Regex(patString), true);
                startState = builder.Build(null);
            }

            var count = 0;
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            for (var t = stopWatch.ElapsedMilliseconds; t < SpinUp + 1000; t = stopWatch.ElapsedMilliseconds)
            {
                var m = new StringMatcher<bool>(src);
                if (m.FindNext(startState, out _))
                {
                    throw new Exception("not supposed to find a match");
                }

                if (t >= SpinUp)
                {
                    ++count;
                }
            }

            return count;
        }
    }
}
