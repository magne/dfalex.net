using System.Collections.Generic;
using CodeHive.DfaLex.tree;
using FluentAssertions;
using Xunit;

namespace CodeHive.DfaLex.Tests.tree
{
    public class DFATTableBuilderTests
    {
        private readonly TDFATransitionTable.Builder builder;

        public DFATTableBuilderTests()
        {
            builder = new TDFATransitionTable.Builder();
        }

        [Fact]
        public void TestBuilder()
        {
            var q0 = new DFAState(null, new byte[] {1}, null);
            var q1 = new DFAState(null, new byte[] {2}, null);
            var empty = new List<Instruction>();
            builder.AddTransition(q0, InputRange.Make('a', 'c'), q1, empty);

            var dfa = builder.build();

            dfa.ToString().Should().Be("q0-a-c -> q1 []\n");

            var n = new TDFATransitionTable.NextState();
            dfa.NewStateAndInstructions(0, 'b', n);

            n.nextState.Should().Be(1);
            n.instructions.Length.Should().Be(0);
        }
    }
}
