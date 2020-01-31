using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace CodeHive.DfaLex.Tests
{
    public class NfaTests : TestBase
    {
        public NfaTests(ITestOutputHelper helper)
            : base(helper)
        { }

        public static IEnumerable<object[]> GetMatchables()
        {
            static object[] Make(string section, string regex, IMatchable matchable, bool reversed = false) =>
                new object[] { (section, regex, matchable, reversed).Labeled($"{section}: {regex}") };

            yield return Make("Catenate",      "ab",    Pattern.Match("a").Then("b"), true);
            yield return Make("Alternate",     "a|b|c", Pattern.AnyOf("a", "b", "c"));
            yield return Make("Question",      "a?",    Pattern.Maybe("a"));
            yield return Make("Question Lazy", null,    Pattern.MaybeLazy("a"));
            yield return Make("Star",          "a*",    Pattern.MaybeRepeat("a"));
            yield return Make("Star Lazy",     null,    Pattern.MaybeRepeatLazy("a"));
            yield return Make("Plus",          "a+",    Pattern.Repeat("a"));
            yield return Make("Plus Lazy",     null,    Pattern.RepeatLazy("a"));
        }

        [Theory]
        [MemberData(nameof(GetMatchables))]
        public void CheckMatchable(Labeled<(string section, string _, IMatchable matchable, bool reversed)> t)
        {
            CheckNfa(t.Data.matchable, t.Data.section);
        }

        [Theory]
        [MemberData(nameof(GetMatchables))]
        public void CheckRegex(Labeled<(string section, string regex, IMatchable _, bool reversed)> t)
        {
            if (t.Data.regex != null)
            {
                CheckNfa(Pattern.Regex(t.Data.regex), t.Data.section);
            }
        }

        [Theory]
        [MemberData(nameof(GetMatchables))]
        public void CheckReverse(Labeled<(string section, string _, IMatchable matchable, bool reversed)> t)
        {
            var section = t.Data.reversed ? $"{t.Data.section} Reversed" : t.Data.section;
            CheckNfa(t.Data.matchable.Reversed, section);
        }

        [Fact]
        public void TestGroupMatch()
        {
            var regex = Pattern.Regex("(((a+)b)+c)+");

            var nfa = Nfa<int>.GetBuilder();
            var start = nfa.AddState();
            var accept = nfa.AddState(1);

            var state = regex.AddToNfa(nfa, accept);
            nfa.AddEpsilon(start, state);

            PrintDot(nfa.Build(), start);
        }

        private void CheckNfa(IMatchable regex, string resourceSection)
        {
            var nfa = Nfa<int>.GetBuilder();
            var start = nfa.AddState();
            var accept = nfa.AddState(1);

            var state = regex.AddToNfa(nfa, accept);
            nfa.AddEpsilon(start, state);

            CheckNfa(nfa.Build(), state, $"NfaTests.out.txt#{resourceSection}");
        }
    }
}
