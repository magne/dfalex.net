using FluentAssertions;
using Xunit;

namespace dfalex.util.tests
{
    public class LexContextTests
    {
        [Fact]
        public void WhenAdvanceNotCalled_ShouldBeBeforeInput()
        {
            var sut = LexContext.Create("");

            sut.Current.Should().Be(LexContext.BeforeInput);
        }

        [Fact]
        public void WhenAdvanceNotCalled_ShouldBeInitialLocation()
        {
            var sut = LexContext.Create("");

            sut.Location.Should().Be(new Location(1, 0, 0));
        }

        [Fact]
        public void WhenEmptyAndAdvanceCalled_ShouldBeEndOfInput()
        {
            var sut = LexContext.Create("");

            sut.Advance();

            sut.Current.Should().Be(LexContext.EndOfInput);
        }

        [Fact]
        public void NewlineShouldIncrementLine()
        {
            var lc = LexContext.Create("line1\n\nline3\n");

            lc.TrySkipUntil(LexContext.EndOfInput).Should().BeTrue();

            lc.Location.Should().Be(new Location(4, 1, 14));
        }

        // TODO Test disposed
    }
}
