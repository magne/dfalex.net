using CodeHive.DfaLex.tree;
using FluentAssertions;
using Xunit;

namespace CodeHive.DfaLex.Tests.tree
{
    public class DFATableTest
    {
        private const int s1 = 0;
        private const int s2 = 1;
        private const int s3 = 2;
        private const int s4 = 3;

        private readonly TDFATransitionTable           table;
        private readonly TDFATransitionTable.NextState n;

        public DFATableTest()
        {
            table = new TDFATransitionTable(new char[] {'c', '1'}, new[] {'k', 'm'}, new[] {s1, s2}, new[] {s3, s4}, new[] {new Instruction[0], new Instruction[0]});
            n = new TDFATransitionTable.NextState();
        }

        [Fact]
        public void TestTable1()
        {
            table.NewStateAndInstructions(200, 'd', n);
            n.found.Should().BeFalse();
        }

        [Fact]
        public void TestTable2()
        {
            table.NewStateAndInstructions(s1, 'c', n);
            n.found.Should().BeTrue();
            n.nextState.Should().Be(s3);
        }

        [Fact]
        public void TestTable3()
        {
            table.NewStateAndInstructions(s1, 'k', n);
            n.found.Should().BeTrue();
            n.nextState.Should().Be(s3);
        }

        [Fact]
        public void TestTable4()
        {
            table.NewStateAndInstructions(s1, 'l', n);
            n.found.Should().BeFalse();
        }

        [Fact]
        public void TestTable5()
        {
            table.NewStateAndInstructions(s2, 'l', n);
            n.found.Should().BeTrue();
            n.nextState.Should().Be(s4);
        }
    }
}
