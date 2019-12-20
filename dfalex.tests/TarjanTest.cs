using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace CodeHive.DfaLex.Tests
{
    /// <summary>
    /// Test DFA cycles and destiny finding
    /// </summary>
    public class TarjanTest
    {
        private readonly Random      r      = new Random(0x4D45524D);
        private readonly List<State> states = new List<State>();
        private          int[]       cycleNumbers;

        [Fact]
        public void Test()
        {
            //tested up to 10000 once, but that takes a very long time (O(n^3))
            for (var n = 0; n < 500; n++)
            {
                var pcycle = r.NextDouble();
                pcycle *= pcycle * pcycle;
                var plink = r.NextDouble();
                var paccept = r.NextDouble();
                RandomDfa(n, pcycle, plink, paccept);
                Check();
            }
        }

        private void RandomDfa(int nstates, double Pcycle, double Plink, double Paccept)
        {
            cycleNumbers = new int[nstates];
            states.Clear();
            var cycleCounter = 0;
            while (states.Count < nstates)
            {
                var pos = states.Count;
                var cycSz = 1;
                if (r.NextDouble() < Pcycle)
                {
                    cycSz = Math.Min(r.Next(20) + 1, nstates - pos);
                }

                for (var i = 0; i < cycSz; ++i)
                {
                    int? accept = null;
                    if (r.NextDouble() < Paccept)
                    {
                        accept = r.Next(8);
                    }

                    states.Add(new State(pos + i, accept));
                }

                if (cycSz > 1)
                {
                    for (var i = 0; i < cycSz; ++i)
                    {
                        cycleNumbers[pos + i] = cycleCounter;
                        if (i != 0)
                        {
                            states[pos + i].Link(states[pos + i - 1]);
                        }
                    }

                    states[pos].Link(states[pos + cycSz - 1]);
                    ++cycleCounter;
                }
                else
                {
                    cycleNumbers[pos] = -1;
                }
            }

            //link
            for (var pos = 1; pos < nstates; ++pos)
            {
                var nLinks = (int) Math.Round(Plink * pos);
                for (var i = 0; i < nLinks; i++)
                {
                    var target = r.Next(pos);
                    states[pos].Link(states[target]);
                }
            }

            for (var pos = 0; pos < nstates; ++pos)
            {
                states[pos].MoveLink0(r);
            }
        }

        private void Check()
        {
            var nStates = states.Count;
            var starts = new List<DfaState<int?>>();
            //find roots that cover all the states
            {
                var reached = new bool[states.Count];
                for (var i = 0; i < nStates; i++)
                {
                    var src = states[i];
                    foreach (var dest in src.GetSuccessorStates())
                    {
                        if (cycleNumbers[src.Number] != cycleNumbers[dest.GetStateNumber()])
                        {
                            Debug.Assert(dest.GetStateNumber() < src.Number);
                            reached[dest.GetStateNumber()] = true;
                        }
                    }
                }

                for (var i = nStates - 2; i >= 0; --i)
                {
                    if (cycleNumbers[i] >= 0 && cycleNumbers[i] == cycleNumbers[i + 1] && reached[i + 1])
                    {
                        reached[i] = true;
                    }
                }

                for (var i = 0; i < nStates; i++)
                {
                    if (i == 0 || cycleNumbers[i] < 0 || cycleNumbers[i] != cycleNumbers[i - 1])
                    {
                        if (!reached[i])
                        {
                            starts.Add(states[i]);
                        }
                    }
                }
            }

            var auzInfo = new DfaAuxiliaryInformation<int?>(starts);
            var gotCycles = auzInfo.GetCycleNumbers();
            Assert.Equal(nStates, gotCycles.Length);
            for (var i = 0; i < nStates; i++)
            {
                if (cycleNumbers[i] < 0)
                {
                    Assert.True(gotCycles[i] < 0);
                }
                else
                {
                    Assert.True(gotCycles[i] >= 0);
                    if (i > 0)
                    {
                        Assert.Equal(cycleNumbers[i] == cycleNumbers[i - 1], gotCycles[i] == gotCycles[i - 1]);
                    }
                }
            }
        }

        private class State : DfaState<int?>
        {
            private readonly  List<DfaState<int?>> transitions = new List<DfaState<int?>>();
            private readonly  int?                 accept;
            internal readonly int                  Number;

            public State(int number, int? accept)
            {
                this.accept = accept;
                Number = number;
            }

            public void Link(DfaState<int?> target)
            {
                transitions.Add(target);
            }

            public void MoveLink0(Random r)
            {
                if (transitions.Count > 1)
                {
                    var d = r.Next(transitions.Count);
                    if (d != 0)
                    {
                        var t = transitions[d];
                        transitions[d] = transitions[0];
                        transitions[0] = t;
                    }
                }
            }

            public override DfaState<int?> GetNextState(char ch)
            {
                if (ch <= transitions.Count)
                {
                    return transitions[ch];
                }

                return null;
            }

            public override int? GetMatch()
            {
                return accept;
            }

            public override int GetStateNumber()
            {
                return Number;
            }

            public override void EnumerateTransitions(DfaTransitionConsumer<int?> consumer)
            {
                for (var i = 0; i < transitions.Count; ++i)
                {
                    consumer((char) i, (char) i, transitions[i]);
                }
            }

            public override IEnumerable<DfaState<int?>> GetSuccessorStates()
            {
                return transitions;
            }

            public override bool HasSuccessorStates()
            {
                return transitions.Any();
            }
        }
    }
}
