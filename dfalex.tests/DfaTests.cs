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
            CheckDfa(Pattern.Regex("(a*b|d)+c?c?c?"), "");
        }

        private void CheckDfa(IMatchable regex, string resourceSection)
        {
            var nfa = new Nfa<int>();
            var startStates = new[] {AddToNfa(nfa, regex, 1), AddToNfa(nfa, Pattern.Regex("a*(b|c)d+"), 2)};

            var rawDfa = new DfaFromNfa<int>(nfa, startStates, null).GetDfa();
            var minimalDfa = new DfaMinimizer<int>(rawDfa).GetMinimizedDfa();
            PrintDot(minimalDfa);
            // serializableDfa = new SerializableDfa<TResult>(minimalDfa);

//            CheckNfa(nfa, state, $"NfaTests.out.txt#{resourceSection}", true);
        }

        private int AddToNfa<T>(Nfa<T> nfa, IMatchable pattern, T match)
        {
            var start = nfa.AddState();
            var accept = nfa.AddState(match);

            var state = pattern.AddToNfa(nfa, accept);
            nfa.AddEpsilon(start, state);

            return start;
        }
    }
}
