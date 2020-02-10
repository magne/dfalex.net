using System.Collections.Generic;
using CodeHive.DfaLex.tree;
using Xunit;
using Xunit.Abstractions;

namespace CodeHive.DfaLex.Tests.tree
{
    public class TDFAInterpreterTests : TestBase
    {
        private readonly ITestOutputHelper helper;

        public TDFAInterpreterTests(ITestOutputHelper helper) : base(helper)
        {
            this.helper = helper;
        }

        public static IEnumerable<object[]> GetRegexes()
        {
            static object[] Make(string section, string regex, string[] input) =>
                new object[] {(regex, input).Labeled($"{section}: {regex}")};

            yield return Make("Catenate",      "ab",         new[] {"ab"});
            yield return Make("Alternate",     "a|b",        new[] {"a", "b"});
            yield return Make("Alternate3",    "a|b|c",      new[] {"a", "b", "c"});
            yield return Make("Question",      "a?a",        new[] {"a", "aa"});
            yield return Make("Question Lazy", "a??a",       new[] {"a", "aa"});
            yield return Make("Star",          "a*a",        new[] {"a", "aa", "aaa"});
            yield return Make("Star Lazy",     "a*?a",       new[] {"a", "aa", "aaa"});
            yield return Make("Plus",          "a+a",        new[] {"aa", "aaa"});
            yield return Make("Plus Lazy",     "a+?a",       new[] {"aa", "aaa"});
            yield return Make("Range",         "[a-b][b-c]", new[] {"ab", "ac", "bc"});
        }

        [Theory]
        [MemberData(nameof(GetRegexes))]
        public void TestInterpret(Labeled<(string regex, string[] input)> t)
        {
            var parsed = Pattern.Regex(t.Data.regex);
            var tnfa = RegexToNfa.Convert(parsed);
            var interpreter = new TDFAInterpreter(TNFAToTDFA.Make(tnfa));

            foreach (var i in t.Data.input)
            {
                interpreter.interpret(i);
            }

            helper.WriteLine(interpreter.tdfaBuilder.ToString());
            PrintDot(tnfa, interpreter.tdfaBuilder.build());
        }
    }
}
