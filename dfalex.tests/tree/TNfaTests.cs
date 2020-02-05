using System.Collections.Generic;
using CodeHive.DfaLex.tree;
using Xunit;
using Xunit.Abstractions;

namespace CodeHive.DfaLex.Tests.tree
{
    public class TNfaTests : TestBase
    {
        public TNfaTests(ITestOutputHelper helper)
            : base(helper)
        { }

        public static IEnumerable<object[]> GetMatchables()
        {
            static object[] Make(string section, string regex, IMatchable matchable, bool reversed = false) =>
                new object[] { (section, regex, matchable, reversed).Labeled($"{section}: {regex}") };

            yield return Make("Catenate",      "ab",         Pattern.Match("a").Then("b"), true);
            yield return Make("Alternate",     "a|b",        Pattern.AnyOf("a", "b"));
            yield return Make("Alternate3",    "a|b|c",      Pattern.AnyOf("a", "b", "c"));
            yield return Make("Question",      "a?",         Pattern.Maybe("a"));
            yield return Make("Question Lazy", "a??",        Pattern.MaybeLazy("a"));
            yield return Make("Star",          "a*",         Pattern.MaybeRepeat("a"));
            yield return Make("Star Lazy",     "a*?",        Pattern.MaybeRepeatLazy("a"));
            yield return Make("Plus",          "a+",         Pattern.Repeat("a"));
            yield return Make("Plus Lazy",     "a+?",        Pattern.RepeatLazy("a"));
            yield return Make("Range",         "[a-b][b-c]", Pattern.Match(CharRange.Range('a', 'b')).Then(CharRange.Range('b', 'c')), true);
        }

        [Theory]
        [MemberData(nameof(GetMatchables))]
        public void CheckMatchable(Labeled<(string section, string _, IMatchable matchable, bool reversed)> t)
        {
            CheckTNfa(t.Data.matchable, t.Data.section);
        }

        [Theory]
        [MemberData(nameof(GetMatchables))]
        public void CheckRegex(Labeled<(string section, string regex, IMatchable _, bool reversed)> t)
        {
            if (t.Data.regex != null)
            {
                CheckTNfa(Pattern.Regex(t.Data.regex), t.Data.section);
            }
        }

        [Theory]
        [MemberData(nameof(GetMatchables))]
        public void CheckReverse(Labeled<(string section, string _, IMatchable matchable, bool reversed)> t)
        {
            var section = t.Data.reversed ? $"{t.Data.section} Reversed" : t.Data.section;
            CheckTNfa(t.Data.matchable.Reversed, section);
        }

        [Fact]
        public void TestGroupMatch()
        {
            var regex = Pattern.Regex("(((a+)b)+c)+");
            var tnfa = RegexToNfa.Convert(regex);

            PrintDot(tnfa);
        }

        private void CheckTNfa(IMatchable regex, string resourceSection)
        {
            var tnfa = RegexToNfa.Convert(regex);

            PrintDot(tnfa);
            CheckTNfa(tnfa, $"tree.TNfaTests.out.txt#{resourceSection}", true);
        }
    }
}
