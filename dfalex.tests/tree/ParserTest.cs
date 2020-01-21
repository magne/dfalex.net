using System;
using CodeHive.DfaLex.tree;
using FluentAssertions;
using Xunit;

namespace CodeHive.DfaLex.Tests.tree
{
    public class ParserTest
    {
        private readonly ParserProvider pp;

        public ParserTest()
        {
            pp = new ParserProvider();
        }

        [Fact]
        public void Regexp4()
        {
            var r = pp.Parse("aaa");

            r.ToString().Should().Be("aaa");
        }

        [Fact]
        public void testBasic()
        {
            var s = pp.Parse(".*");
            s.Should().BeOfType<ParserProvider.Star>();

            var ss = (ParserProvider.Star) s;
            ss.elementary.Should().BeOfType<ParserProvider.Any>();

            s.ToString().Should().Be(".*");
        }

        [Fact]
        public void testDontParseRange()
        {
            Action a = () => pp.Parse("[a--f]");
            a.Should().Throw<ArgumentException>().Which.Message.Should().StartWith("Invalid regular expression: \"[a--f]\"");
        }

        [Fact]
        public void testEscaped()
        {
            var s = pp.Parse("\\(");
            s.Should().BeOfType<ParserProvider.EscapedChar>();

            ((ParserProvider.EscapedChar) s).inputRange.From.Should().Be('(');
            s.ToString().Should().Be("\\(");
        }

        [Fact]
        public void testGroup2()
        {
            var s = pp.Parse("(aaa|bbb)");
            s.Should().BeOfType<ParserProvider.Group>();

            s.ToString().Should().Be("(aaa|bbb)");
        }

        [Fact]
        public void testNoSet()
        {
            Action a = () => pp.Parse("[a-fgk-zA-B][");
            a.Should().Throw<ArgumentException>().Which.Message.Should().StartWith("Invalid regular expression: \"[a-fgk-zA-B][\"");
        }

        [Fact]
        public void testOptional1()
        {
            var s = pp.Parse("[a-fgk-zA-B]?");
            s.Should().BeOfType<ParserProvider.Optional>();
            ((ParserProvider.Optional) s).elementary.Should().BeAssignableTo<ParserProvider.Set>();

            s.ToString().Should().Be("[A-Ba-gk-z]?");
        }

        [Fact]
        public void testOptional2()
        {
            var s = pp.Parse(".?");
            s.Should().BeOfType<ParserProvider.Optional>();
            ((ParserProvider.Optional) s).elementary.Should().BeOfType<ParserProvider.Any>();

            s.ToString().Should().Be(".?");
        }

        [Fact]
        public void testPlus1()
        {
            var s = pp.Parse("[a-fgk-zA-B]+");
            s.Should().BeOfType<ParserProvider.Plus>();
            ((ParserProvider.Plus) s).elementary.Should().BeAssignableTo<ParserProvider.Set>();

            s.ToString().Should().Be("[A-Ba-gk-z]+");
        }

        [Fact]
        public void testPlus2()
        {
            var s = pp.Parse(".+");
            s.Should().BeOfType<ParserProvider.Plus>();
            ((ParserProvider.Plus) s).elementary.Should().BeOfType<ParserProvider.Any>();

            s.ToString().Should().Be(".+");
        }

        [Fact]
        public void testRegexp()
        {
            var rr = pp.Parse("aaa|bbb");

            rr.ToString().Should().Be("aaa|bbb");
        }

        [Fact]
        public void testSet()
        {
            var s = pp.Parse("[^a-fgk-zA-B]");

            s.ToString().Should().Be("[^A-Ba-gk-z]");
        }

        [Fact]
        public void testSimpleNotEmpty()
        {
            // TODO Should fail?
            Action a = () => pp.Parse("");
            a();
        }

        [Fact]
        public void testStar1()
        {
            var s = pp.Parse("[a-fgk-zA-B]*");
            s.Should().BeOfType<ParserProvider.Star>();
            ((ParserProvider.Star) s).elementary.Should().BeAssignableTo<ParserProvider.Set>();

            s.ToString().Should().Be("[A-Ba-gk-z]*");
        }

        [Fact]
        public void testStar2()
        {
            var s = pp.Parse(".*");
            s.Should().BeOfType<ParserProvider.Star>();
            ((ParserProvider.Star) s).elementary.Should().BeOfType<ParserProvider.Any>();

            s.ToString().Should().Be(".*");
        }

        [Fact]
        public void testUnion()
        {
            var u = pp.Parse("[a-fgk-zA-B]*|aaa") as ParserProvider.Union;

            u.ToString().Should().Be("[A-Ba-gk-z]*|aaa");
            u.left.Should().BeOfType<ParserProvider.Star>();
            u.right.Should().BeOfType<ParserProvider.Simple>();
        }

        [Fact]
        public void testUnion2()
        {
            var u = pp.Parse("[a-fgk-zA-B]*|(aaa|bbb)") as ParserProvider.Union;

            u.ToString().Should().Be("[A-Ba-gk-z]*|(aaa|bbb)");
            u.left.Should().BeOfType<ParserProvider.Star>();
            u.right.Should().BeOfType<ParserProvider.Group>();
        }

        [Fact]
        public void testUnion3()
        {
            var u = pp.Parse("bbb|aaa");

            u.ToString().Should().Be("bbb|aaa");
        }

        [Fact]
        public void unionBig()
        {
            var rr = pp.Parse("aaa|bbb");

            rr.ToString().Should().Be("aaa|bbb");
        }

        [Fact]
        public void unionBig2()
        {
            var rr = pp.Parse("aaa|(bbb)?");

            rr.Should().BeOfType<ParserProvider.Union>();
            var u = (ParserProvider.Union) rr;
            var right = u.right;
            right.Should().BeOfType<ParserProvider.Optional>();
            var elementary = ((ParserProvider.Optional) right).elementary;
            elementary.Should().BeOfType<ParserProvider.Group>();
            var body = ((ParserProvider.Group) elementary).body;
            body.Should().BeOfType<ParserProvider.Simple>();

            rr.ToString().Should().Be("aaa|(bbb)?");
        }
    }
}
