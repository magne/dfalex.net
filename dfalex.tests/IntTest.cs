using System.Collections.Generic;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace CodeHive.DfaLex.Tests
{
    public class IntTest : TestBase
    {
        private readonly ITestOutputHelper helper;

        public IntTest(ITestOutputHelper helper)
            : base(helper)
        {
            this.helper = helper;
        }

        [Fact]
        public void TestTo100K()
        {
            var builder = new DfaBuilder<int?>();
            for (var i = 0; i < 100000; ++i)
            {
                builder.AddPattern(Pattern.Match(i.ToString()), i % 7);
            }

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var start = builder.Build(null);
            var numstates = CountStates(start);
            stopWatch.Stop();
            var telapsed = stopWatch.ElapsedMilliseconds;
            helper.WriteLine($"Mininmized 100000 numbers -> value mod 7 (down to {numstates} states) in {telapsed * .001} seconds");
            Assert.Equal(null, StringMatcher<int?>.MatchWholeString(start, ""));
            Assert.Equal(null, StringMatcher<int?>.MatchWholeString(start, "100001"));
            for (var i = 0; i < 100000; ++i)
            {
                Assert.Equal(i % 7, StringMatcher<int?>.MatchWholeString(start, i.ToString()));
            }

            Assert.Equal(36, numstates);
        }

        [Fact]
        public void TestSimultaneousLanguages()
        {
            var builder = new DfaBuilder<int?>();
            for (var i = 0; i < 100000; ++i)
            {
                if ((i % 21) == 0)
                {
                    builder.AddPattern(Pattern.Match(i.ToString()), 3);
                }
                else if ((i % 3) == 0)
                {
                    builder.AddPattern(Pattern.Match(i.ToString()), 1);
                }
                else if ((i % 7) == 0)
                {
                    builder.AddPattern(Pattern.Match(i.ToString()), 2);
                }
            }

            var langs = new List<ISet<int?>>();
            {
                var s1 = new HashSet<int?>();
                s1.Add(1);
                s1.Add(3);
                var s2 = new HashSet<int?>();
                s2.Add(2);
                s2.Add(3);
                langs.Add(s1);
                langs.Add(s2);
            }
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var starts = builder.Build(langs, null);
            var start3 = starts[0];
            var start7 = starts[1];
            var numstates = CountStates(start3, start7);
            stopWatch.Stop();
            var telapsed = stopWatch.ElapsedMilliseconds;
            helper.WriteLine($"Mininmized 1000000 numbers -> divisible by 7 and 3 (down to {numstates} states) in {telapsed * .001} seconds");
            for (var i = 0; i < 100000; ++i)
            {
                if ((i % 21) == 0)
                {
                    Assert.Equal((int) 3, StringMatcher<int?>.MatchWholeString(start3, i.ToString()));
                    Assert.Equal((int) 3, StringMatcher<int?>.MatchWholeString(start7, i.ToString()));
                }
                else if ((i % 3) == 0)
                {
                    Assert.Equal((int) 1, StringMatcher<int?>.MatchWholeString(start3, i.ToString()));
                    Assert.Equal(null,    StringMatcher<int?>.MatchWholeString(start7, i.ToString()));
                }
                else if ((i % 7) == 0)
                {
                    Assert.Equal(null,    StringMatcher<int?>.MatchWholeString(start3, i.ToString()));
                    Assert.Equal((int) 2, StringMatcher<int?>.MatchWholeString(start7, i.ToString()));
                }
            }

            Assert.Equal(137, numstates);
        }
    }
}
