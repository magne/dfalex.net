using System;
using Xunit;
using Xunit.Abstractions;

namespace CodeHive.DfaLex.Tests
{
    public class StringSearcherTest : TestBase
    {
        public StringSearcherTest(ITestOutputHelper helper) : base(helper)
        { }

        [Fact]
        public void Test()
        {
            var builder = new DfaBuilder<JavaToken?>();
            foreach (JavaToken tok in Enum.GetValues(typeof(JavaToken)))
            {
                builder.AddPattern(tok.Pattern(), tok);
            }

            var searcher = builder.BuildStringSearcher(null);
            var instr = ReadResource("SearcherTestInput.txt");
            var want = ReadResource("SearcherTestOutput.txt");
            var have = searcher.FindAndReplace(instr, TokenReplace);
            Assert.Equal(want, have);
        }

        [Fact]
        public void TestSingleToken()
        {
            var builder = new DfaBuilder<JavaToken?>();
            foreach (JavaToken tok in Enum.GetValues(typeof(JavaToken)))
            {
                builder.AddPattern(tok.Pattern(), tok);
            }

            var searcher = builder.BuildStringSearcher(null);
            var instr = "this";
            var want = "[THIS=this]";
            var have = searcher.FindAndReplace(instr, TokenReplace);
            Assert.Equal(want, have);
        }

        enum AccentedChar
        {
                Miserable
        }

        [Fact]
        public void TestSeparateAccentedCharacters()
        {
            var builder = new DfaBuilder<AccentedChar?>();
            builder.AddPattern(Pattern.Match("Les Mise\u0301rables"), AccentedChar.Miserable);

            var searcher = builder.BuildStringSearcher(null);
            var instr = "Les Mise\u0301rables";
            var want = "[Miserable=Les Mise\u0301rables]";
            var have = searcher.FindAndReplace(instr, TokenReplace);
            Assert.Equal(want, have);
        }

        [Fact]
        public void CrazyWontonTest()
        {
            var builder = new SearchAndReplaceBuilder();
            builder.AddReplacement(Pattern.RegexI("(<name>)"), StringReplacements.Delete);
            var replacer = builder.BuildStringReplacer();
            var instr = ReadResource("SearcherTestInput2.txt");
            var have = replacer(instr);
            var want = instr.Replace("<name>", string.Empty).Replace("<NAME>", string.Empty);
            Assert.Equal(want, have);
        }

        [Fact]
        public void TestReplaceFunc()
        {
            var builder = new SearchAndReplaceBuilder();

            foreach (JavaToken tok in Enum.GetValues(typeof(JavaToken)))
            {
                builder.AddReplacement(tok.Pattern(), (dest, src, s, e) => TokenReplace<JavaToken>(dest, tok, src, s, e));
            }

            var replacer = builder.BuildStringReplacer();
            var instr = ReadResource("SearcherTestInput.txt");
            var want = ReadResource("SearcherTestOutput.txt");
            var have = replacer(instr);
            Assert.Equal(want, have);
        }

        [Fact]
        public void RepositionTest()
        {
            var builder = new SearchAndReplaceBuilder();
            builder.AddReplacement(Pattern.RegexI("[a-z0-9]+ +[a-z0-9]+"), (dest, src, s, e) =>
            {
                for (e = s; src[e] != ' '; ++e)
                { }

                dest.Append(src, s, e).Append(", ");
                for (; src[e] == ' '; ++e)
                { }

                return e;
            });
            var replacer = builder.BuildStringReplacer();

            var instr = " one two  three   four five ";
            var want = " one, two, three, four, five ";
            var have = replacer(instr);
            Assert.Equal(want, have);
        }

        [Fact]
        public void ReplacementDeleteIgnoreTest()
        {
            var builder = new SearchAndReplaceBuilder();
            builder.AddReplacement(Pattern.RegexI("three"), StringReplacements.Ignore);
            builder.AddReplacement(Pattern.RegexI("[a-z0-9]+"), StringReplacements.Delete);
            var replacer = builder.BuildStringReplacer();

            var instr = " one two  three   four five ";
            var want = "    three     ";
            var have = replacer(instr);
            Assert.Equal(want, have);
        }

        [Fact]
        public void ReplacementSpaceOrNewlineTest()
        {
            var builder = new SearchAndReplaceBuilder();
            builder.AddReplacement(Pattern.RegexI("[\0- ]+"), StringReplacements.SpaceOrNewline);
            var replacer = builder.BuildStringReplacer();

            var instr = "    one \n two\r\n\r\nthree  \t four\n\n\nfive ";
            var want = " one\ntwo\nthree four\nfive ";
            var have = replacer(instr);
            Assert.Equal(want, have);
        }

        [Fact]
        public void ReplacementCaseTest()
        {
            var builder = new SearchAndReplaceBuilder();
            builder.AddReplacement(Pattern.RegexI("u[a-zA-z]*"), StringReplacements.ToUpper);
            builder.AddReplacement(Pattern.RegexI("l[a-zA-z]*"), StringReplacements.ToLower);
            var replacer = builder.BuildStringReplacer();

            var instr = "lAbCd uAbCd";
            var want = "labcd UABCD";
            var have = replacer(instr);
            Assert.Equal(want, have);
        }

        [Fact]
        public void ReplacementStringTest()
        {
            var builder = new SearchAndReplaceBuilder();
            builder.AddReplacement(Pattern.RegexI("[a-zA-z]*"), StringReplacements.String("x"));
            var replacer = builder.BuildStringReplacer();

            var instr = " one two  three   four five ";
            var want = " x x  x   x x ";
            var have = replacer(instr);
            Assert.Equal(want, have);
        }

        [Fact]
        public void ReplacementSurroundTest()
        {
            var builder = new SearchAndReplaceBuilder();
            builder.AddReplacement(Pattern.RegexI("[a-zA-z]*"), StringReplacements.Surround("(", StringReplacements.ToUpper, ")"));
            var replacer = builder.BuildStringReplacer();

            var instr = " one two  three   four five ";
            var want = " (ONE) (TWO)  (THREE)   (FOUR) (FIVE) ";
            var have = replacer(instr);
            Assert.Equal(want, have);
        }

        private static int TokenReplace<TToken>(IAppendable dest, TToken? mr, string src, int startPos, int endPos) where TToken : struct
        {
            dest.Append("[").Append(mr.ToString()).Append("=").Append(src, startPos, endPos).Append("]");
            return 0;
        }
    }
}
