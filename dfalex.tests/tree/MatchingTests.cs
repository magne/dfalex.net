using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeHive.DfaLex.tree;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace CodeHive.DfaLex.Tests.tree
{
    [Collection("History")]
    public class MatchingTests
    {
        private readonly ITestOutputHelper helper;

        public MatchingTests(ITestOutputHelper helper)
        {
            this.helper = helper;
            State.ResetCount();
            History.ResetCount();
        }

        [Fact]
        public void TestSimplest()
        {
            var result = MakeInterpreter("a").interpret("a");
            result.ToString().Should().Be("0-0");
        }

        [Fact]
        public void testTwoRangesAndOnePlus()
        {
            MakeInterpreter("a+b").interpret("ab").ToString().Should().Be("0-1");
        }

        [Fact]
        public void testTwoRangesAndOnePlusNoMatch()
        {
            MakeInterpreter("a+b").interpret("aba").ToString().Should().Be("NO_MATCH");
        }

        [Fact]
        public void testTwoRangesAndTwoPlusNoMatch()
        {
            MakeInterpreter("a+b+").interpret("aba").ToString().Should().Be("NO_MATCH");
        }

        [Fact]
        public void testTwoRangesMatch()
        {
            MakeInterpreter("ab").interpret("ab").ToString().Should().Be("0-1");
        }

        [Fact]
        public void testTwoRangesNoMatch()
        {
            var interpreter = MakeInterpreter("ab");
            var result = interpreter.interpret("aba");
            result.ToString().Should().Be("NO_MATCH");
        }

        private TDFAInterpreter MakeInterpreter(string regex)
        {
            var parsed = new ParserProvider().Parse(regex);
            var tnfa = new RegexToNFA().convert(parsed);

            helper.WriteLine(Print(tnfa));

            return new TDFAInterpreter(TNFAToTDFA.Make(tnfa));
        }

        private string Print(TNFA tnfa)
        {
            var buf = new StringBuilder();

            buf.AppendLine($"digraph TNFA {{");
            buf.AppendLine("rankdir=LR;");
            buf.AppendLine("n999999 [style=invis];"); // Invisible start node
            buf.AppendLine($"n999999 -> {StateName(tnfa.initialState)}"); // Edge into start state

            string stateName;
            while (closureQ.Any())
            {
                var state = closureQ.Dequeue();
                stateName = StateName(state);

                foreach (var t in tnfa.availableEpsilonTransitionsFor(state))
                {
                    var label = $"{(t.priority == Transition.Priority.LOW ? "-" : string.Empty)} ε {(t.tag.Equals(Tag.None) ? string.Empty : t.tag.ToString() )}";
                    buf.AppendLine($"{stateName} -> {StateName(t.state)} [label=\"{label}\"]");
                }

                var irs = tnfa.transitions.Keys.Where(k => k.Key.Equals(state)).Select(k => k.Value);
                foreach (var ir in irs)
                {
                    foreach (var t in tnfa.availableTransitionsFor(state, ir))
                    {
                        var label = $"{(t.priority == Transition.Priority.LOW ? "-" : string.Empty)} {ir} {(t.tag.Equals(Tag.None) ? string.Empty : t.tag.ToString() )}";
                        buf.AppendLine($"{stateName} -> {StateName(t.state)} [label=\"{label}\"]");
                    }
                }
            }

            stateName = StateName(tnfa.finalState);
            buf.AppendLine($"{stateName}[label=\"{stateName}\",peripheries=2]");
            buf.AppendLine("}");

            return buf.ToString();
        }

        private          int                        nextStateNum;
        private          bool                       useStateNumbers = false;
        private readonly IDictionary<State, string> names           = new Dictionary<State, string>();
        private readonly Queue<State>               closureQ        = new Queue<State>();

        private string StateName(State state)
        {
            if (!names.TryGetValue(state, out var ret))
            {
                var nameNum = useStateNumbers ? state.Id : nextStateNum++;
                ret = $"S{nameNum}";
                names.Add(state, ret);
                closureQ.Enqueue(state);
            }

            return ret;
        }
    }
}
