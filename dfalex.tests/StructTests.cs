using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace CodeHive.DfaLex.Tests
{
    public class StructTests : TestBase
    {
        private enum EnumToken
        {
            If,
            Id
        }

        public StructTests(ITestOutputHelper helper) : base(helper)
        { }

        [Fact]
        public void DfaWithEnumResultsTest()
        {
            var builder = new DfaBuilder<EnumToken>();
            builder.AddPattern(Pattern.Match("if"), EnumToken.If);
            builder.AddPattern(Pattern.Regex("([A-Za-z])([A-Za-z0-9])*"), EnumToken.Id);
            var start = builder.Build(accepts => accepts.First());

            CheckDfa(start, "StructTests-1.txt");
        }

        [Fact]
        public void DfaWithIntResultsTest()
        {
            var builder = new DfaBuilder<int>();
            builder.AddPattern(Pattern.Regex("ab"), 0);
            builder.AddPattern(Pattern.Regex("bb"), 1);
            var start = builder.Build(null);

            CheckDfa(start, "StructTests-2.txt");
        }
    }
}
