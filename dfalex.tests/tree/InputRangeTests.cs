using System;
using System.Linq;
using CodeHive.DfaLex.tree;
using FluentAssertions;
using Xunit;

namespace CodeHive.DfaLex.Tests.tree
{
    public class InputRangeTests
    {
        [Fact]
        public void TestSingle()
        {
            var start = new[] { InputRange.Make('a', 'b') };
            InputRangeCleanup.CleanUp(start).Should().BeEquivalentTo(start);
        }

        [Fact]
        public void TestEmpty()
        {
            var start = Array.Empty<InputRange>();
            InputRangeCleanup.CleanUp(start).Should().BeEquivalentTo(start);
        }

        [Fact]
        public void TestNonIntersecting()
        {
            var start = new[]
            {
                InputRange.Make('a', 'b'),
                InputRange.Make('c', 'd')
            };
            InputRangeCleanup.CleanUp(start).Should().BeEquivalentTo(start);
        }

        [Fact]
        public void TestSimpleIntersecting()
        {
            var start = new[]
            {
                InputRange.Make('a', 'c'),
                InputRange.Make('c', 'd')
            };
            string.Join(", ", InputRangeCleanup.CleanUp(start)).Should().Be("a-b, c-c, d-d");
        }

        [Fact]
        public void TestEnclosing()
        {
            var start = new[]
            {
                InputRange.Make('a', 'g'),
                InputRange.Make('b', 'd')
            };
            string.Join(", ", InputRangeCleanup.CleanUp(start)).Should().Be("a-a, b-d, e-g");
        }

        [Fact]
        public void TestAny()
        {
            var start = new[]
            {
                InputRange.ANY,
                InputRange.Make('b', 'd')
            };
            string.Join(", ", InputRangeCleanup.CleanUp(start)).Should().Be("0x0-a, b-d, e-0xffff");
        }
    }
}
