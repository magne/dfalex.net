using CodeHive.DfaLex.tree;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace CodeHive.DfaLex.Tests.tree
{
    [Collection("History")]
    public class MatchingTests : TestBase
    {
        public MatchingTests(ITestOutputHelper helper)
            : base(helper)
        {
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
            var parsed = Pattern.Regex(regex);
            var tnfa = RegexToNfa.Convert(parsed);

            PrintDot(tnfa);

            return new TDFAInterpreter(TNFAToTDFA.Make(tnfa));
        }
    }
}
