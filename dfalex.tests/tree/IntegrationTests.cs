using System.Collections.Generic;
using System.Linq;
using CodeHive.DfaLex.tree;
using FluentAssertions;
using Xunit;

namespace CodeHive.DfaLex.Tests.tree
{
    [Collection("History")]
    public class IntegrationTests
    {
        public IntegrationTests()
        {
            History.ResetCount();
        }

        [Fact]
        public void shouldNotMatch()
        {
            var tdfaInterpreter = TDFAInterpreter.compile("(((a+)b)+c)+");
            var res = tdfaInterpreter.interpret("aabbccaaaa");
            res.ToString().Should().Be("NO_MATCH");
        }

        [Fact]
        public void shouldNotMatchTwoBs()
        {
            var tdfaInterpreter = TDFAInterpreter.compile("(((a+)b)+c)+");
            var res = tdfaInterpreter.interpret("aabbc");
            res.ToString().Should().Be("NO_MATCH");
        }

        [Fact]
        public void testMemoryAfterExecution()
        {
            var interpreter = TDFAInterpreter.compile("(((a+)b)+c)+");
            var res = (RealMatchResult) interpreter.interpret("abcaabaaabc");

            res.matchPositionsDebugString().Should().Be("(0, ) (10, ) (3, 0, ) (10, 2, ) (6, 3, 0, ) (9, 5, 1, ) (6, 3, 0, ) (8, 4, 0, ) ");

            res.getRoot().getChildren().AsString().Should().Be("[abc, aabaaabc]");
            var iter = res.getRoot().getChildren();
            var children = (List<TreeNode>) iter.First().getChildren();
            children.AsString().Should().Be("[ab]");
            children[0].getChildren().AsString().Should().Be("[a]");

            children = (List<TreeNode>) iter.Skip(1).First().getChildren();
            children.AsString().Should().Be("[aab, aaab]");
            children[0].getChildren().AsString().Should().Be("[aa]");
            children[1].getChildren().AsString().Should().Be("[aaa]");
        }

        [Fact]
        public void testMatchExampleFromPaperTomLehrer()
        {
            var parsed = new ParserProvider().Parse("(([a-zA-Z ]*),([0-9]+);)+");
            var tnfa = new RegexToNFA().convert(parsed);

            var interpreter = new TDFAInterpreter(TNFAToTDFA.Make(tnfa));
            var res = (RealMatchResult) interpreter.interpret("Tom Lehrer,01;Alan Turing,23;");
            res.matchPositionsDebugString().Should().Be("(0, ) (28, ) (14, 0, ) (28, 13, ) (14, 0, ) (24, 9, ) (26, 11, ) (27, 12, ) ");
        }

        [Fact]
        public void testMatchRanges()
        {
            var parsed = new ParserProvider().Parse("[a-b][b-c]");
            var tnfa = new RegexToNFA().convert(parsed);

            var interpreter = new TDFAInterpreter(TNFAToTDFA.Make(tnfa));
            var interpreted = interpreter.interpret("ab");
            interpreted.Should().BeOfType<RealMatchResult>();
        }

        [Fact]
        public void testMemoryAfterExecutionSimple()
        {
            var parsed = new ParserProvider().Parse("((a+)b)+");
            var tnfa = new RegexToNFA().convert(parsed);
            var interpreter = new TDFAInterpreter(TNFAToTDFA.Make(tnfa));
            var res = (RealMatchResult) interpreter.interpret("abab");
            res.matchPositionsDebugString().Should().Be("(0, ) (3, ) (2, 0, ) (3, 1, ) (2, 0, ) (2, 0, ) ");
        }

        [Fact]
        public void testNoInstructions()
        {
            var interpreter = TDFAInterpreter.compile("a+b+");
            interpreter.interpret("aab");
            interpreter.tdfaBuilder.build().ToString().Should().Be("q0-a-a -> q1 []\nq1-a-a -> q1 []\nq1-b-b -> q2 [2->3, 1->4, 4<- pos, c↑(3), c↓(4)]\n");
        }

        [Fact]
        public void testOtherLehrer()
        {
            var parsed = new ParserProvider().Parse("(.*?(.*?),([0-9]+);)+");
            var tnfa = new RegexToNFA().convert(parsed);
            var interpreter = new TDFAInterpreter(TNFAToTDFA.Make(tnfa));
            var res = (RealMatchResult) interpreter.interpret("Tom Lehrer,01;Alan Turing,23;");

            res.matchPositionsDebugString().Should().Be("(0, ) (28, ) (14, 0, ) (28, 13, ) (14, 0, ) (24, 9, ) (26, 11, ) (27, 12, ) ");
        }

        [Fact]
        public void testTwoGreedy()
        {
            var parsed = new ParserProvider().Parse(".*(.*)");
            var tnfa = new RegexToNFA().convert(parsed);

            var interpreter = new TDFAInterpreter(TNFAToTDFA.Make(tnfa));
            var res = (RealMatchResult) interpreter.interpret("aaaa");

            res.matchPositionsDebugString().Should().Be("(0, ) (3, ) (4, ) (3, ) ");
        }

        [Fact]
        public void testTwoNonGreedy()
        {
            var parsed = new ParserProvider().Parse("(.*?(.*?))+");
            var tnfa = new RegexToNFA().convert(parsed);

            var interpreter = new TDFAInterpreter(TNFAToTDFA.Make(tnfa));
            var res = (RealMatchResult) interpreter.interpret("aaaa");

            res.matchPositionsDebugString().Should().Be("(0, ) (3, ) (3, 1, 0, ) (3, 2, 0, ) (4, 2, 0, ) (3, 2, 0, ) ");
        }

        [Fact]
        public void integrationTestWithUnion()
        {
            var interpreter = TDFAInterpreter.compile("((a+)(b|c|d))+");
            var res = (RealMatchResult) interpreter.interpret("abac");
            res.matchPositionsDebugString().Should().Be("(0, ) (3, ) (2, 0, ) (3, 1, ) (2, 0, ) (2, 0, ) (3, 1, ) (3, 1, ) ");
        }

        [Fact]
        public void testComplexRegex()
        {
            var interpreter = TDFAInterpreter.compile("(.*?([a-z]+\\.)*([A-Z][a-zA-Z]*))*.*?");
            var res = (RealMatchResult) interpreter.interpret("aa java.io.File aa java.io.File;");
            res.matchPositionsDebugString().Should().Be("(0, ) (31, ) (15, 0, ) (30, 14, ) (24, 19, 8, 3, ) (26, 23, 10, 7, ) (27, 11, ) (30, 14, ) ");
        }

        [Fact]
        public void testGroupMatch()
        {
            var tdfaInterpreter = TDFAInterpreter.compile("(((a+)b)+c)+");
            var result = tdfaInterpreter.interpret("aaabcaaabcaabc");
            result.start().Should().Be(0);
            result.end().Should().Be(13);
            result.start(1).Should().Be(10);
            result.end(1).Should().Be(13);
            result.start(2).Should().Be(10);
            result.end(2).Should().Be(12);
            result.start(3).Should().Be(10);
            result.end(3).Should().Be(11);
        }
    }
}
