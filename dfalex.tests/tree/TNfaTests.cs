using CodeHive.DfaLex.tree;
using Xunit;
using Xunit.Abstractions;

namespace CodeHive.DfaLex.Tests.tree
{
    public class TNfaTests : TestBase
    {
        public TNfaTests(ITestOutputHelper helper) : base(helper)
        { }

        [Fact]
        public void TestGroupMatch()
        {
            var regex = Pattern.Regex("(((a+)b)+c)+");

            var tnfa = RegexToNfa.Convert(regex);

            PrintDot(tnfa);
        }
    }
}
