using CodeHive.DfaLex.rcx;
using FluentAssertions;
using Xunit;

namespace CodeHive.DfaLex.Tests.rcx
{
    public class DfaTests
    {
        [Theory]
        [InlineData("aa",      true)]
        [InlineData("aba",     false)]
        [InlineData("abba",    true)]
        [InlineData("abbba",   false)]
        [InlineData("abbbba",  true)]
        [InlineData("abbbbaa", false)]
        public void MatchTest(string s, bool expected)
        {
            var post = Nfa.Re2Post("a(bb)*a");
            var start = Nfa.Post2nfa(post);
            var dstate = Dfa.StartDState(start);

            var match = Dfa.Match(dstate, s);

            match.Should().Be(expected);
        }
    }
}
