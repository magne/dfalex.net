using System;
using System.IO;
using Xunit;

namespace CodeHive.DfaLex.Tests
{
    public class ReverseFinderTest
    {
        [Fact]
        public void Test()
        {
            var revBuilder = new DfaBuilder<bool>();
            foreach (JavaToken tok in Enum.GetValues(typeof(JavaToken)))
            {
                revBuilder.AddPattern(Pattern.AllStrings.Then(tok.Pattern().Reversed), true);
            }

            var wantStart = revBuilder.Build(null);
            var want = _toString(wantStart);

            var builder = new DfaBuilder<JavaToken?>();
            foreach (JavaToken tok in Enum.GetValues(typeof(JavaToken)))
            {
                builder.AddPattern(tok.Pattern(), tok);
            }

            var haveStart = builder.BuildReverseFinder();
            var have = _toString(haveStart);
            Assert.Equal(want, have);

            //make sure we properly exclude the empty string from the reverse finder DFA
            builder.Clear();
            foreach (JavaToken tok in Enum.GetValues(typeof(JavaToken)))
            {
                if (((int) tok & 1) == 0)
                {
                    builder.AddPattern(tok.Pattern(), tok);
                }
                else
                {
                    builder.AddPattern(Pattern.Maybe(tok.Pattern()), tok);
                }
            }

            haveStart = builder.BuildReverseFinder();
            have = _toString(haveStart);
            Assert.Equal(want, have);
        }

        private string _toString(DfaState<bool> dfa)
        {
            var w = new StringWriter();
            var printer = new PrettyPrinter<bool>();
            printer.Print(w, dfa);
            return w.ToString();
        }
    }
}
