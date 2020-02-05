using CodeHive.DfaLex.tree;
using FluentAssertions;
using Xunit;

namespace CodeHive.DfaLex.Tests.tree
{
    [Collection("History")]
    public class HistoryTests
    {
        public HistoryTests()
        {
            History.ResetCount();
        }

        [Fact]
        public void CtorTest()
        {
            var h = new History();

            h.ToString().Should().Be("0(0)");
        }

        [Fact]
        public void CtorTest2()
        {
            var h = new History();
            h.cur = 1;
            var h2 = new History(h.id, 2, h);

            h2.ToString().Should().Be("0(2 1)");
        }

        [Fact]
        public void EnumeratorTest()
        {
            var h = new History();

            h.Should().Equal(0);
        }

        [Fact]
        public void EnumeratorTest2()
        {
            var h = new History();
            h.cur = 1;
            var h2 = new History(h.id, 2, h);

            h2.Should().Equal(2, 1);
        }
    }
}
