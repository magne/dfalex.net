using Xunit;

namespace CodeHive.DfaLex.Tests
{
    public class StringMatcherTest
    {
        [Fact]
        public void TestStringMatcher()
        {
            DfaState<int> dfa;
            {
                var builder = new DfaBuilder<int>();
                builder.AddPattern(Pattern.Regex("a[ab]*b"), 1);
                builder.AddPattern(Pattern.Regex("a[ab]*c"), 2);
                dfa = builder.Build(null);
            }
            var matcher = new StringMatcher<int>("bbbbbaaaaaaaaaaaaaaaaaaaaaaaabbbbcaaaaaaabbbaaaaaaa");
            var found = matcher.FindNext(dfa, out var result);
            Assert.True(found);
            Assert.Equal(2, result);
            Assert.Equal("aaaaaaaaaaaaaaaaaaaaaaaabbbbc", matcher.LastMatch);
            Assert.Equal(5, matcher.LastMatchStart);
            Assert.Equal(34, matcher.LastMatchEnd);
            found = matcher.FindNext(dfa, out result);
            Assert.True(found);
            Assert.Equal(1, result);
            Assert.Equal("aaaaaaabbb", matcher.LastMatch);
            found = matcher.FindNext(dfa, out result);
            Assert.False(found);
            Assert.Equal(0, result);

            matcher.SetPositions(15, 20, 33);
            Assert.Equal("aaaaa", matcher.LastMatch);
            matcher.FindNext(dfa, out result);
            Assert.Equal("aaaaaaaaabbbb", matcher.LastMatch);
            found = matcher.FindNext(dfa, out result);
            Assert.False(found);
        }
    }
}
