using System.Collections.Generic;
using CodeHive.DfaLex.tree;
using FluentAssertions;
using Xunit;

namespace CodeHive.DfaLex.Tests.tree
{
    [Collection("History")]
    public class MappingInstructionTests
    {
        [Fact]
        public void SimpleTest()
        {
            History.ResetCount();
            IDictionary<History, History> m = new Dictionary<History, History>();
            var h = new[]
            {
                new History(), new History(), new History(),
                new History(), new History(), new History(),
            };
            m.Add(h[3], h[0]);
            m.Add(h[5], h[3]);
            m.Add(h[0], h[1]);
            m.Add(h[1], h[4]);

            var converter = new TNFAToTDFA(null);
            var instructions = converter.MappingInstructions(m);

            instructions.AsString().Should().Be("[1->4, 0->1, 3->0, 5->3]");
        }
    }
}
