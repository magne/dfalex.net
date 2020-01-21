using System.Collections.Generic;
using CodeHive.DfaLex.re1;
using FluentAssertions;
using Xunit;

namespace CodeHive.DfaLex.Tests.re1
{
    public class RegexTests
    {
        [Fact]
        public void ParseTest()
        {
            var regex = Regex.Parse("a(b*?)b(c*d+)");

            regex.ToString().Should().Be("Cat(Cat(Cat(Lit(a), Paren(1, NgStar(Lit(b)))), Lit(b)), Paren(2, Cat(Star(Lit(c)), Plus(Lit(d)))))");

            var prog = regex.Compile();
            // prog.ToString().Should().Be("");

            var sub = new List<int>();
            var match = prog.Recursive("abbcd", sub);
            match.Should().BeTrue();
            sub.Should().BeEquivalentTo(-1, -1, 1, 2, 3, 5);

            sub.Clear();
            match = prog.Recursive("abbcd", sub);
            match.Should().BeTrue();
            sub.Should().BeEquivalentTo(-1, -1, 1, 2, 3, 5);

            var subp = new int[6];
            match = Backtrack.Run(prog, "abbcd", subp);
            match.Should().BeTrue();
            sub.Should().BeEquivalentTo(-1, -1, 1, 2, 3, 5);

            match = ThompsonVm.Run(prog, "abbcd", subp);
            match.Should().BeTrue();
            sub.Should().BeEquivalentTo(-1, -1, 1, 2, 3, 5);

            prog.Reset();
            match = PikeVm.Run(prog, "abbcd", subp);
            match.Should().BeTrue();
            sub.Should().BeEquivalentTo(-1, -1, 1, 2, 3, 5);
        }
    }
}
