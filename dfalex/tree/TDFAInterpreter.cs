using System;
using System.Collections.Generic;

namespace CodeHive.DfaLex.tree
{
    /// <summary>
    /// Interprets the known TDFA states. Compiles missing states on the fly.
    /// </summary>
    // TODO: Rename to Pattern. Make public.
    // ReSharper disable once InconsistentNaming
    internal class TDFAInterpreter
    {
        private const int COMPILE_THRESHOLD = 2;

        private readonly SortedSet<DFAState> states = new SortedSet<DFAState>();

        internal readonly TDFATransitionTable.Builder     tdfaBuilder = new TDFATransitionTable.Builder();
        private readonly  TNFAToTDFA                      tnfa2tdfa;
        private           TNFAToTDFA.StateAndInstructions startState;

        internal TDFAInterpreter(TNFAToTDFA tnfa2tdfa)
        {
            this.tnfa2tdfa = tnfa2tdfa;
        }

        public static TDFAInterpreter compile(String regex)
        {
            var parsed = Pattern.Regex(regex);
            var tnfa = RegexToNfa.Convert(parsed);
            return new TDFAInterpreter(TNFAToTDFA.Make(tnfa));
        }

        /** @return the range containing input. Null if there isn't one. */
        InputRange findInputRange(IList<InputRange> ranges, char input)
        {
            var l = 0;
            var r = ranges.Count - 1;
            while (l <= r)
            {
                var m = (l + r) / 2;
                var splitPoint = ranges[m];
                if (splitPoint.Contains(input))
                {
                    return splitPoint;
                }

                if (input < splitPoint.From)
                {
                    r = m - 1;
                }
                else
                {
                    l = m + 1;
                }
            }

            return null; // Found nothing
        }

        public MatchResultTree interpret(string input)
        {
            var inputRanges = InputRangeCleanup.CleanUp(tnfa2tdfa.tnfa.AllInputRanges);
            if (startState == null)
            {
                var startUnexpanded = tnfa2tdfa.ConvertToDfaState(tnfa2tdfa.tnfa.initialState);
                startState = tnfa2tdfa.oneStep(startUnexpanded, null);

                states.Add(startState.dfaState);
            }

            var dfaState = startState.dfaState;

            var cacheHits = 0;
            TDFATransitionTable tdfa = null;
            var tdfaState = -1;

            foreach (var instruction in startState.instructions)
            {
                instruction.Execute(-1);
            }

            var newState = new TDFATransitionTable.NextState(); // Output parameter to save allocations
            var inputLen = input.Length; // Prevent re-executing on every loop step.
            for (var pos = 0; pos < inputLen; pos++)
            {
                var a = input[pos];

                // If there is a TDFA, see if it has a transition. Execute if there and continue.
                if (tdfa != null)
                {
                    tdfa.NewStateAndInstructions(tdfaState, a, newState);
                    if (newState.found)
                    {
                        foreach (var i in newState.instructions)
                        {
                            i.Execute(pos);
                        }

                        tdfaState = newState.nextState;
                        continue;
                    }

                    tdfa = null;
                    dfaState = tdfaBuilder.mapping.deoptimized[tdfaState];
                    tdfaState = -1;
                    cacheHits = 0;
                }
                else
                {
                    // Find the transition in the builder. Execute if there and continue.
                    var nextState = tdfaBuilder.AvailableTransition(dfaState, a);
                    if (nextState != null)
                    {
                        foreach (var i in nextState.instructions)
                        {
                            i.Execute(pos);
                        }

                        dfaState = nextState.nextState;

                        cacheHits++;
                        if (cacheHits > COMPILE_THRESHOLD)
                        {
                            tdfa = tdfaBuilder.build();
                            tdfaState = tdfaBuilder.mapping.mapping[dfaState];
                        }

                        continue;
                    }
                }

                cacheHits = 0; // We got here because the cache hasn't seen this transition before.

                var inputRange = findInputRange(inputRanges, a);
                if (inputRange == null)
                {
                    return RealMatchResult.NoMatchResult.SINGLETON;
                }

                // TODO this is ugly. Clearly, e should return StateAndPositions.
                var uu = tnfa2tdfa.oneStep(dfaState.threads, inputRange);
                if (uu == null)
                {
                    // There is no matching NFA state.
                    return RealMatchResult.NoMatchResult.SINGLETON;
                }

                var u = uu.dfaState;

                IDictionary<History, History> mapping = new Dictionary<History, History>();

                // If there is a valid mapping, findMappableStates will modify mapping into it.
                var mappedState = tnfa2tdfa.findMappableState(states, u, mapping);

                var nextState2 = mappedState;
                var c = new List<Instruction>(uu.instructions);
                if (mappedState == null)
                {
                    mapping = null; // Won't be needed then.
                    nextState2 = u;
                    states.Add(nextState2);
                }
                else
                {
                    var mappingInstructions = tnfa2tdfa.MappingInstructions(mapping);
                    c.AddRange(mappingInstructions);
                }

                foreach (Instruction instruction in c)
                {
                    instruction.Execute(pos);
                }

                System.Diagnostics.Debug.Assert(historiesOk(nextState2.threads));

                tdfaBuilder.AddTransition(dfaState, inputRange, nextState2, c);

                dfaState = nextState2;
            }

            // Restore full state before extracing information.
            if (tdfa != null)
            {
                dfaState = tdfaBuilder.mapping.deoptimized[tdfaState];
            }

            var fin = dfaState.finalHistories;
            if (fin == null)
            {
                return RealMatchResult.NoMatchResult.SINGLETON;
            }

            var parentOf = tnfa2tdfa.makeParentOf();
            return new RealMatchResult(fin, input, parentOf);
        }

        /** Invariant: opening and closing tags must have same length histories. */
        private bool historiesOk(IList<RThread> threads)
        {
            foreach (var thread in threads)
            {
                var histEnum = thread.Histories.GetEnumerator();
                while (histEnum.MoveNext())
                {
                    var h1 = histEnum.Current;
                    histEnum.MoveNext();
                    var h2 = histEnum.Current;
                    if (h1 != null)
                    {
                        System.Diagnostics.Debug.Assert(h2 != null);
                        if (h1.Size != h2.Size)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}
