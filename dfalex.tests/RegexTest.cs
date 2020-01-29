using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace CodeHive.DfaLex.Tests
{
    public class RegexTest : TestBase
    {
        public RegexTest(ITestOutputHelper helper)
            : base(helper)
        { }

        [Fact]
        public void Test()
        {
            var r1 = Pattern.Regex("if");
            var r2 = Pattern.Regex("[a-zA-Z][a-zA-Z0-9]*");
            var bld = new DfaBuilder<string>();
            bld.AddPattern(r1, "if");
            bld.AddPattern(r2, "id");
            var start = bld.Build(new HashSet<string>(new[] { "if", "id" }), accepts => accepts.First());
            PrintDot(start);
        }

        [Fact]
        public void TestRegexParser()
        {
            IMatchable p1, p2;
            p1 = Pattern.AnyOf("A", "B");
            p2 = Pattern.Regex("A|B");
            Check(p1, p2);

            p1 = Pattern.Match("A").Then(Pattern.AnyOf("C", "D"));
            p2 = Pattern.Regex("A(C|D)");
            Check(p1, p2);

            p1 = Pattern.Match("A").Then(Pattern.AnyOf("C", "D")).Then("B");
            p2 = Pattern.Regex("A(C|D)B");
            Check(p1, p2);

            p1 = Pattern.Match("A").ThenMaybe(Pattern.AnyOf("C", "D")).Then("B");
            p2 = Pattern.Regex("A(C|D)?B");
            Check(p1, p2);

            p1 = Pattern.Match("A").ThenRepeat(Pattern.AnyOf("C", "D")).Then("B");
            p2 = Pattern.Regex("A(C|D)+B");
            Check(p1, p2);

            p1 = Pattern.Match("A").ThenMaybeRepeat(Pattern.AnyOf("C", "D")).Then("B");
            p2 = Pattern.Regex("A(C|D)*B");
            Check(p1, p2);

#pragma warning disable 618
            p1 = Pattern.Match("A").ThenMaybe(Pattern.AnyOf("C", "D")).Then("B");
            p2 = Pattern.Regex("A(C|D)??B", RegexOptions.Legacy);
            Check(p1, p2);

            p1 = Pattern.Match("A").ThenMaybeRepeat(Pattern.AnyOf("C", "D")).Then("B");
            p2 = Pattern.Regex("A(C|D)+?B", RegexOptions.Legacy);
            Check(p1, p2);

            p1 = Pattern.Match("A").ThenMaybeRepeat(Pattern.AnyOf("C", "D")).Then("B");
            p2 = Pattern.Regex("A(C|D)*?B", RegexOptions.Legacy);
            Check(p1, p2);
#pragma warning restore 618

            p1 = Pattern.AnyOf(Pattern.Match("A").ThenMaybeRepeat("B"), Pattern.Match("C"));
            p2 = Pattern.Regex("AB*|C");
            Check(p1, p2);

            p1 = Pattern.Regex("\\s\\S\\d\\D\\w\\W");
            p2 = Pattern.Regex("[ \\t\\n\\x0B\\f\\r][^ \\t\\n\\x0B\\f\\r][0-9][^0-9][a-zA-Z_0-9][^a-zA-Z_0-9]");
            Check(p1, p2);

            p1 = Pattern.Regex("[^\\d][\\d]");
            p2 = Pattern.Regex("[\\D][^\\D]");
            Check(p1, p2);

            p1 = Pattern.Regex("[Cc][Aa][Tt][^0-9a-fA-F][^0-9a-f@-F]");
            p2 = Pattern.RegexI("cAt[^\\da-f][^\\d@-F]");
            Check(p1, p2);
        }

        private void Check(IMatchable pWant, IMatchable pHave)
        {
            var want = PToString(pWant);

            var have = PToString(pHave);
            if (!want.Equals(have))
            {
                Assert.Equal(want, have);
            }
        }

        private string PToString(IMatchable p)
        {
            var builder = new DfaBuilder<bool>();
            builder.AddPattern(p, true);
            var dfa = builder.Build(null);
            return PrettyPrinter.Print(dfa);
        }
    }
}
