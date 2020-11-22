using System;
using FluentAssertions;
using Xunit;

namespace dfalex.util.tests
{
    public class LexContextTests
    {
        [Fact]
        public void WhenMoveNextNotCalled_ShouldBeBeforeInput()
        {
            var sut = LexContext.Create("");

            sut.IsBeforeInput.Should().BeTrue();
            sut.IsEndOfInput.Should().BeFalse();
        }

        [Fact]
        public void WhenEmptyAndMoveNextCalled_ShouldBeEndOfInput()
        {
            var sut = LexContext.Create("");

            sut.MoveNext();

            sut.IsEndOfInput.Should().BeTrue();
            sut.IsBeforeInput.Should().BeFalse();
        }

        [Theory]
        [InlineData("",                 1, 0, 0)]
        [InlineData("\n",               2, 0, 1)]
        [InlineData("\r",               1, 0, 1)]
        [InlineData("\t",               1, 4, 1)]
        [InlineData("a",                1, 1, 1)]
        [InlineData("a\n",              2, 0, 2)]
        [InlineData("a\r",              1, 0, 2)]
        [InlineData("a\t",              1, 4, 2)]
        [InlineData("a\t\t",            1, 8, 3)]
        [InlineData("abc\t",            1, 4, 4)]
        [InlineData("abc \t",           1, 8, 5)]
        [InlineData("line1\n\nline3\n", 4, 0, 13)]
        public void LocationShouldReflectInput(string input, int line, int column, long position)
        {
            var expected = new Location(line, column, position);
            var sut = LexContext.Create(input);

            while (sut.MoveNext())
            { }

            sut.Location.Should().Be(expected);
        }

        [Theory]
        [InlineData("\t",     8)]
        [InlineData("    \t", 8)]
        [InlineData("\t\t ",   17)]
        public void LocationShouldRespectTabWitdh(string input, int column)
        {
            var expected = new Location(1, column, input.Length);
            var sut = LexContext.Create(input);
            sut.TabWidth = 8;

            while (sut.MoveNext())
            { }

            sut.Location.Should().Be(expected);
        }

        public class WhenDisposingLexContext
        {
            [Fact]
            public void Dispose_ShouldCallChildDispose()
            {
                TestLexContext sut;
                using (sut = new TestLexContext())
                {
                    sut.DisposeCalled.Should().Be(0);
                }

                sut.DisposeCalled.Should().Be(1);
            }

            [Fact]
            public void Dispose_ShouldOnlyBeCalledOnce()
            {
                TestLexContext sut;
                using (sut = new TestLexContext())
                {
                    sut.DisposeCalled.Should().Be(0);
                }

                sut.Dispose();

                sut.DisposeCalled.Should().Be(1);
            }

            [Fact]
            public void Close_ShouldCallChildDispose()
            {
                var sut = new TestLexContext();
                sut.DisposeCalled.Should().Be(0);

                sut.Close();

                sut.DisposeCalled.Should().Be(1);
            }

            [Fact]
            public void Finalizer_ShouldCallChildDispose()
            {
                var disposeCalled = new TestLexContext.Counter();
                CreateTemporaryLexContext(disposeCalled);

                GC.Collect();
                GC.WaitForPendingFinalizers();

                disposeCalled.Value.Should().Be(1);
            }

            private static void CreateTemporaryLexContext(TestLexContext.Counter counter)
            {
                var sut = new TestLexContext(counter);
                sut.DisposeCalled.Should().Be(0);
            }
        }

        private class TestLexContext : LexContext
        {
            internal class Counter
            {
                public int Value { get; private set; }

                public void Increment() => Value++;
            }

            private readonly Counter disposeCalled;

            public int DisposeCalled => disposeCalled.Value;

            public TestLexContext() : this(new Counter())
            { }

            public TestLexContext(Counter counter)
            {
                disposeCalled = counter;
            }

            protected override void Dispose(bool disposing)
            {
                if (!IsDisposed)
                {
                    disposeCalled.Increment();
                }

                base.Dispose(disposing);
            }

            protected override int AdvanceInner()
            {
                throw new NotImplementedException();
            }
        }
    }
}
