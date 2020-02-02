using Xunit;
using Xunit.Abstractions;

namespace CodeHive.DfaLex.Tests
{
    public class DfaTests : TestBase
    {
        public DfaTests(ITestOutputHelper helper)
            : base(helper)
        { }

        [Fact]
        public void TestIt()
        {
            CheckDfa(Pattern.Regex("(a*b|d)+c?c?c?"), "It");
        }

        private void CheckDfa(IMatchable regex, string resourceSection)
        {
            var nfa = Nfa<int>.GetBuilder();
            var startStates = new[] {AddToNfa(nfa, regex, 1), AddToNfa(nfa, Pattern.Regex("a*(b|c)d+"), 2)};

            var rawDfa = new DfaFromNfa<int>(nfa.Build(), startStates, null).GetDfa();
            var minimalDfa = new DfaMinimizer<int>(rawDfa).GetMinimizedDfa();
            // PrintDot(minimalDfa);
            var dfa = new SerializableDfa<int>(minimalDfa);

            CheckDfa(dfa.GetStartStates()[0], $"DfaTests.out.txt#{resourceSection}");
        }

        private static int AddToNfa<T>(Nfa<T>.Builder nfa, IMatchable pattern, T match)
        {
            var start = nfa.AddState();
            var accept = nfa.AddState(match);

            var state = pattern.AddToNfa(nfa, accept, CaptureGroup.NoGroup);
            nfa.AddEpsilon(start, state, NfaTransitionPriority.Normal, Tag.None);

            return start;
        }
    }
}
