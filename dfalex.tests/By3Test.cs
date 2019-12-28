using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace CodeHive.DfaLex.Tests
{
    public class By3Test : TestBase
    {
        public By3Test(ITestOutputHelper helper)
            : base(helper)
        { }

        [Fact]
        public void Test()
        {
            //make pattern for whole numbers divisible by 3

            //digits mod 3
            var d0 = CharRange.AnyOf("0369");
            var d1 = Pattern.Match(CharRange.AnyOf("147")).ThenMaybeRepeat(d0);
            var d2 = Pattern.Match(CharRange.AnyOf("258")).ThenMaybeRepeat(d0);

            var plus2 = Pattern.MaybeRepeat(d1.Then(d2)).Then(Pattern.AnyOf(d1.Then(d1), d2));

            var minus2 = Pattern.MaybeRepeat(d2.Then(d1)).Then(Pattern.AnyOf(d2.Then(d2), d1));

            var by3 = Pattern.MaybeRepeat(Pattern.AnyOf(d0, d1.Then(d2), plus2.Then(minus2)));

            var builder = new DfaBuilder<bool>();
            builder.AddPattern(by3, true);
            var start = builder.Build(new HashSet<bool> { true }, null);
            Assert.Equal(3, CountStates(start));
            CheckDfa(start, "By3Test.out.txt", false);
        }
    }
}
