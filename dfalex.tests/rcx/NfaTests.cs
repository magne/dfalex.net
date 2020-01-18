using System.Collections.Generic;
using System.Text;
using CodeHive.DfaLex.rcx;
using FluentAssertions;
using Xunit;

namespace CodeHive.DfaLex.Tests.rcx
{
    public class NfaTests
    {
        [Fact]
        public void Re2PostTest()
        {
            var post = Nfa.Re2Post("a(bb)+a");

            post.Should().Be("abb.+.a.");
        }

        [Fact]
        public void Post2NfaTest()
        {
            var post = Nfa.Re2Post("a(bb)+a");
            var start = Nfa.Post2nfa(post);

            // NfaToString(start).Should().Be("");
        }

        [Theory]
        [InlineData("aa", true)]
        [InlineData("aba", false)]
        [InlineData("abba", true)]
        [InlineData("abbba", false)]
        [InlineData("abbbba", true)]
        [InlineData("abbbbaa", false)]
        public void MatchTest(string s, bool expected)
        {
            var post = Nfa.Re2Post("a(bb)*a");
            var start = Nfa.Post2nfa(post);

            var match = Nfa.Match(start, s);

            match.Should().Be(expected);
        }

        private Queue<Nfa.State> queue = new Queue<Nfa.State>();

        private string NfaToString(Nfa.State s)
        {
            var buf = new StringBuilder();
            StateName(s);
            while (queue.Count > 0)
            {
                buf.AppendLine(PrintState(queue.Dequeue()));
            }

            return buf.ToString();
        }

        private string PrintState(Nfa.State s)
        {
            var ch = s.c == Nfa.State.Match ? "Match" : s.c == Nfa.State.Split ? "Split" : ((char) s.c).ToString();
            var sn2 = s.out2 != null ? $" | {StateName(s.out2)}" : string.Empty;
            return $"{StateName(s)}: {ch} -> {StateName(s.out1)}{sn2}";
        }

        private IDictionary<Nfa.State, string> names = new Dictionary<Nfa.State, string>();

        private string StateName(Nfa.State s)
        {
            if (s == null)
            {
                return null;
            }

            if (!names.TryGetValue(s, out var name))
            {
                name = "S" + names.Count;
                names.Add(s, name);
                queue.Enqueue(s);
            }

            return name;
        }
    }
}
