using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace CodeHive.DfaLex.Tests
{
    public class DestinyTest : TestBase
    {
        public DestinyTest(ITestOutputHelper helper)
            : base(helper)
        { }

        [Fact]
        public void Test()
        {
            var builder = new DfaBuilder<JavaToken?>(null);
            foreach (JavaToken tok in Enum.GetValues(typeof(JavaToken)))
            {
                builder.AddPattern(tok.Pattern(), tok);
            }

            var start = builder.Build(new HashSet<JavaToken?>(Enum.GetValues(typeof(JavaToken)).Cast<JavaToken?>()), null);
            var auxInfo = new DfaAuxiliaryInformation<JavaToken?>(new[] { start });

            //calculate destinies the slow way
            var states = auxInfo.GetStatesByNumber();
            var slowDestinies = new List<ISet<JavaToken?>>(states.Count);
            var numStates = states.Count;
            for (var i = 0; i < numStates; i++)
            {
                slowDestinies.Add(new HashSet<JavaToken?>());
                var state = states[i];
                if (state.GetMatch() != null)
                {
                    slowDestinies[i].Add(state.GetMatch());
                }
            }

            //AtomicBoolean again = new AtomicBoolean(true);
            var again = true;
            while (again)
            {
                again = false;
                for (var i = 0; i < numStates; ++i)
                {
                    var set = slowDestinies[i];
                    var state = states[i];
                    state.EnumerateTransitions((f, l, target) =>
                    {
                        var targetSet = slowDestinies[target.GetStateNumber()];
                        var a = true;
                        foreach (var token in targetSet)
                        {
                            if (!set.Add(token))
                            {
                                a = false;
                            }
                        }

                        if (a)
                        {
                            again = true;
                        }
                    });
                }
            }

            /*
                PrettyPrinter p = new PrettyPrinter(true);
                PrintWriter pw = new PrintWriter(System.out);
                p.print(pw, start);
                pw.flush();
            */
            var destinies = auxInfo.GetDestinies();
            for (var i = 0; i < numStates; ++i)
            {
                var set = slowDestinies[i];
                JavaToken? wantDestiny = null;
                if (set.Count == 1)
                {
                    wantDestiny = set.FirstOrDefault();
                }

                Assert.Equal( /*"State " + i + " destiny",*/ wantDestiny, destinies[i]);
            }
        }
    }
}
