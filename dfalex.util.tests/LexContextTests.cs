using System;
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

            sut.IsBeforeInput.Should().BeTrue();
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

            sut.IsEndOfInput.Should().BeTrue();
        }

        [Fact]
        public void NewlineShouldIncrementLine()
        {
            var lc = LexContext.Create("line1\n\nline3\n");

            lc.TrySkipUntilEndOfInput().Should().BeTrue();

            lc.Location.Should().Be(new Location(4, 1, 14));
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
