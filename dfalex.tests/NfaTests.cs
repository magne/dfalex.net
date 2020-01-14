using Xunit;
using Xunit.Abstractions;

namespace CodeHive.DfaLex.Tests
{
    public class NfaTests : TestBase
    {
        public NfaTests(ITestOutputHelper helper) : base(helper)
        { }

        [Fact]
        public void TestNfa()
        {
            var nfa = new Nfa<int>();
            var start = nfa.AddState();
            var accept = nfa.AddState(1);

            var state1 = Pattern.Regex("abc+").AddToNfa(nfa, accept);
            nfa.AddEpsilon(start, state1);
            var state2 = Pattern.Regex("abc+").Reversed.AddToNfa(nfa, accept);
            nfa.AddEpsilon(start, state2);

            PrintDot(nfa, state1);
            CheckNfa(nfa, state1, "NfaTests.out.txt");
        }
    }
}
