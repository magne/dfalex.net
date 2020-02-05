using System.Collections.Generic;
using CodeHive.DfaLex.tree;
using FluentAssertions;
using Xunit;
using static CodeHive.DfaLex.tree.Arraylike;

namespace CodeHive.DfaLex.Tests.tree
{
    [Collection("History")]
    public class TreeArrayTests
    {
        private const int N = 1000;

        public TreeArrayTests()
        {
            History.ResetCount();
        }

        [Fact]
        public void TestConstruction1()
        {
            var ta = new TreeArray(1);
            ta.payload.id.Should().Be(0);
        }

        [Fact]
        public void TestConstruction3()
        {
            var ta = new TreeArray(3);
            ta.payload.id.Should().Be(0);
            ta.left.Size.Should().Be(1);
            ta.left.payload.id.Should().Be(1);
            ta.right.Size.Should().Be(1);
            ta.right.payload.id.Should().Be(2);
        }

        [Fact]
        public void TestConstruction12()
        {
            var ta = new TreeArray(12);
            ta.payload.id.Should().Be(0);
            ta.left.Size.Should().Be(6);
            ta.right.Size.Should().Be(5);
            ta.left.left.Size.Should().Be(3);
            ta.left.right.Size.Should().Be(2);
            ta.right.left.Size.Should().Be(2);
            ta.right.right.Size.Should().Be(2);
        }

        [Fact]
        public void TestGet()
        {
            var ta = new TreeArray(N);
            for (var i = 0; i < N; i++)
            {
                ta.Get(i).id.Should().Be(i);
            }
        }

        [Fact]
        public void TestIterator()
        {
            var ta = new TreeArray(N);
            var i = 0;
            foreach (var h in ta)
            {
                h.id.Should().Be(i++);
            }
        }

        [Fact]
        public void TestUnique()
        {
            var ta = new TreeArray(N);
            for (var i = 0; i < N; i++)
            {
                for (var j = i + 1; j < N; j++)
                {
                    ta.Get(i).Equals(ta.Get(j)).Should().BeFalse();
                }
            }
        }

        [Fact]
        public void TestSet()
        {
            var ta = new TreeArray(12);
            var ta2 = (TreeArray) ta.Set(5, new History());
            ta.Get(5).id.Should().Be(5);
            ta2.Get(5).id.Should().NotBe(5);
            ta2.Get(4).id.Should().Be(4);
            ta2.Get(6).id.Should().Be(6);
        }
    }
}
