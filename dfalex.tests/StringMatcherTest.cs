using Xunit;

namespace CodeHive.DfaLex.Tests
{
    public class StringMatcherTest
    {
        [Fact]
        public void TestStringMatcher()
        {
            DfaState<int?> dfa;
            {
                var builder = new DfaBuilder<int?>();
                builder.AddPattern(Pattern.Regex("a[ab]*b"), 1);
                builder.AddPattern(Pattern.Regex("a[ab]*c"), 2);
                dfa = builder.Build(null);
            }
            var matcher = new StringMatcher<int?>("bbbbbaaaaaaaaaaaaaaaaaaaaaaaabbbbcaaaaaaabbbaaaaaaa");
            var result = matcher.FindNext(dfa);
            Assert.Equal(2, result);
            Assert.Equal("aaaaaaaaaaaaaaaaaaaaaaaabbbbc", matcher.LastMatch);
            Assert.Equal(5, matcher.LastMatchStart);
            Assert.Equal(34, matcher.LastMatchEnd);
            result = matcher.FindNext(dfa);
            Assert.Equal(1, result);
            Assert.Equal("aaaaaaabbb", matcher.LastMatch);
            result = matcher.FindNext(dfa);
            Assert.Null(result);

            matcher.SetPositions(15, 20, 33);
            Assert.Equal("aaaaa", matcher.LastMatch);
            matcher.FindNext(dfa);
            Assert.Equal("aaaaaaaaabbbb", matcher.LastMatch);
            result = matcher.FindNext(dfa);
            Assert.Null(result);
        }
    }
}
