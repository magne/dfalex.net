using System;
using System.Text;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;

namespace CodeHive.DfaLex.Bench
{
    [MemoryDiagnoser]
    public class NotFoundReplaceBenchmark
    {
        private const    string               Pattern = @"01235|/|456*1|abc|_|\..*|013|0?1?2?3?4?57";
        private readonly string               src;
        private readonly Regex                dotnetPat;
        private readonly Func<string, string> replacer;
        private readonly DfaState<bool>       startState;

        public NotFoundReplaceBenchmark()
        {
            var buf = new StringBuilder(100000);
            for (var i = 0; i < 10000; i++)
            {
                buf.Append("0123456789");
            }

            src = buf.ToString();

            dotnetPat = new Regex(Pattern, RegexOptions.Compiled);

            {
                var builder = new SearchAndReplaceBuilder();
                builder.AddReplacement(DfaLex.Pattern.Regex(Pattern), (dest, srcStr, s, e) => 0);
                replacer = builder.BuildStringReplacer();
            }

            {
                var builder = new DfaBuilder<bool>();
                builder.AddPattern(DfaLex.Pattern.Regex(Pattern), true);
                startState = builder.Build(null);
            }
        }

        [Benchmark]
        public void DotNetRegex()
        {
            dotnetPat.Replace(src, string.Empty);
        }

        [Benchmark]
        public void SearchAndReplaceBuilder()
        {
            replacer(src);
        }

        [Benchmark]
        public void Matcher()
        {
            var m = new StringMatcher<bool>(src);
            if (m.FindNext(startState, out _))
            {
                throw new DfaException("not supposed to find a match");
            }
        }
    }
}
