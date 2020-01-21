using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CodeHive.DfaLex.tree
{
    // ReSharper disable once InconsistentNaming
    internal class TNFAToTDFA
    {
        internal static TNFAToTDFA Make(TNFA tnfa)
        {
            return new TNFAToTDFA(tnfa);
        }

        private Instruction.InstructionMaker instructionMaker = Instruction.InstructionMaker.Get;

        private TDFATransitionTable.Builder tdfaBuilder = new TDFATransitionTable.Builder();

        internal readonly TNFA tnfa;

        internal TNFAToTDFA(TNFA tnfa)
        {
            this.tnfa = tnfa;
        }

        // Used to create the initial state of the DFA.
        internal List<RThread> ConvertToDfaState(State state)
        {
            var initialMemoryLocations = Arraylike.Make(tnfa.allTags().Count);
            return new List<RThread> {new RThread(state, initialMemoryLocations)};
        }

        internal class StateAndInstructions
        {
            internal DFAState          dfaState;
            internal List<Instruction> instructions;

            internal StateAndInstructions(DFAState dfaState, List<Instruction> instructions)
            {
                this.dfaState = dfaState;
                this.instructions = instructions;
            }
        }

        private class TransitioningThread
        {
            internal RThread           thread;
            internal Hunger            hunger;
            internal List<Instruction> instructions;

            internal TransitioningThread(RThread thread, Hunger hunger, List<Instruction> instructions)
            {
                this.thread = thread;
                this.hunger = hunger;
                this.instructions = instructions;
            }

            public override string ToString() => $"({thread}, {hunger}, {instructions.AsString()}";
        }

        internal enum Hunger
        {
            HUNGRY,
            FED
        }

        /// <summary>
        /// Niko and Aaron's closure.
        ///
        /// All states after following the epsilon edges of the NFA. Produces instructions
        /// when Tags are crossed. This is the transitive closure on the subgraph of epsilon
        /// edges.
        /// </summary>
        /// <param name="ir">the input range that was read. For start states, this is null.</param>
        /// <returns>The next state after state, for input a. Null if there isn't a follow-up state.</returns>
        internal StateAndInstructions oneStep(IList<RThread> threads, InputRange ir)
        {
            var newInner = new List<RThread>();
            ISet<State> seen = new HashSet<State>();
            var instructions = new List<Instruction>();

            var stack = new Stack<TransitioningThread>(); // normal priority
            var lowStack = new Stack<TransitioningThread>(); // low priority
            var workStack = new Stack<TransitioningThread>();

            Arraylike finalHistories = null;

            // Enqueue all states we're in as consuming thread to lowStack, or non-consuming if startState.
            foreach (var e in ((IEnumerable<RThread>) threads).Reverse())
            {
                var h = Hunger.HUNGRY;
                if (ir == null)
                {
                    h = Hunger.FED;
                }

                lowStack.Push(new TransitioningThread(e, h, new List<Instruction>()));
            }

            while (stack.Any() || lowStack.Any())
            {
                // take topmost as t from high if possible or else from low.
                TransitioningThread tt;
                if (stack.Any())
                {
                    tt = stack.Pop();
                }
                else
                {
                    tt = lowStack.Pop();
                    finalHistories = fillRet(newInner, instructions, workStack, finalHistories);
                }

                if (tt.hunger == Hunger.HUNGRY)
                {
                    var ts = tnfa.availableTransitionsFor(tt.thread.State, ir);
                    foreach (var transition in ts)
                    {
                        // push new thread with the new state that isn't consuming to high.
                        stack.Push(new TransitioningThread(new RThread(transition.state, tt.thread.Histories),
                            Hunger.FED, new List<Instruction>()));
                    }

                    continue;
                }

                if (seen.Contains(tt.thread.State))
                {
                    continue;
                }

                seen.Add(tt.thread.State);
                workStack.Push(tt);

                foreach (var trans in tnfa.availableEpsilonTransitionsFor(tt.thread.State))
                {
                    var tau = trans.tag;
                    var transInstr = new List<Instruction>();
                    var newHistories = tt.thread.Histories;

                    if (tau.IsStartTag || tau.IsEndTag)
                    {
                        var newHistoryOpening = new History();
                        var openingPos = positionFor(tau.Group.startTag);
                        transInstr.Add(instructionMaker.Reorder(newHistoryOpening, tt.thread.Histories.Get(openingPos)));
                        newHistories = newHistories.Set(openingPos, newHistoryOpening);

                        if (tau.IsStartTag)
                        {
                            transInstr.Add(instructionMaker.StorePosPlusOne(newHistoryOpening));
                        }
                        else
                        {
                            var newHistoryClosing = new History();
                            var closingPos = positionFor(tau.Group.endTag);
                            transInstr.Add(instructionMaker.Reorder(newHistoryClosing, tt.thread.Histories.Get(closingPos)));
                            newHistories = newHistories.Set(closingPos, newHistoryClosing);
                            transInstr.Add(instructionMaker.StorePos(newHistoryClosing));
                            transInstr.Add(instructionMaker.OpeningCommit(newHistoryOpening));
                            transInstr.Add(instructionMaker.ClosingCommit(newHistoryClosing));
                        }
                    }

                    // push new thread with the new state to the corresponding stack.
                    var newThread = new TransitioningThread(new RThread(trans.state, newHistories), Hunger.FED, transInstr);
                    switch (trans.priority)
                    {
                        case Transition.Priority.LOW:
                            lowStack.Push(newThread);
                            break;
                        case Transition.Priority.NORMAL:
                            stack.Push(newThread);
                            break;
                        default:
                            throw new DfaException();
                    }
                }
            }

            finalHistories = fillRet(newInner, instructions, workStack, finalHistories);

            if (!newInner.Any())
            {
                return null;
            }

            return new StateAndInstructions(new DFAState(newInner, DFAState.MakeComparisonKey(newInner), finalHistories), instructions);
        }

        /**
         * Empties the {@code workStack} and fills it into {@code newInner} and its instructions into {@code instructions}.
         *
         * @return The new final history, if we found one. Otherwise, param {@code finalHistories}.
         */
        private Arraylike fillRet(List<RThread> newInner, List<Instruction> instructions, Stack<TransitioningThread> workStack, Arraylike finalHistories)
        {
            // Add instructions in the order they were created.
            foreach (var thread in workStack.Reverse())
            {
                instructions.AddRange(thread.instructions);
            }

            while (workStack.Any())
            {
                var workTransition = workStack.Pop();
                newInner.Add(workTransition.thread);
                if (tnfa.finalState.Equals(workTransition.thread.State))
                {
                    Debug.Assert(finalHistories == null);
                    finalHistories = workTransition.thread.Histories;
                }
            }

            return finalHistories;
        }

        internal DFAState findMappableState(SortedSet<DFAState> states, DFAState u, IDictionary<History, History> mapping)
        {
            // `from` is a key that is smaller than all possible full keys. Likewise, `to` is bigger than all.
            var from = new DFAState(null, DFAState.MakeStateComparisonKey(u.threads), null);
            var toKey = DFAState.MakeStateComparisonKey(u.threads);
            // Assume that toKey is not full of Byte.MAX_VALUE. That would be really unlucky.
            // Also a bit unlikely, given that it'state an MD5 hash, and therefore pretty random.
            for (var i = toKey.Length - 1; true; i--)
            {
                if (toKey[i] != byte.MaxValue)
                {
                    toKey[i]++;
                    break;
                }
            }

            var to = new DFAState(null, toKey, null);

            // TODO: Wrong bounds: NavigableSet<DFAState> range = states.subSet(from, true, to, false);
            var range = states.GetViewBetween(from, to);
            foreach (var candidate in range)
            {
                if (isMappable(u, candidate, mapping))
                {
                    return candidate;
                }
            }

            return null;
        }

        /** @return a mapping into {@code mapping} if one exists and returns false otherwise. */
        private bool isMappable(DFAState first, DFAState second, IDictionary<History, History> mapping)
        {
            mapping.Clear();
            IDictionary<History, History> reverse = new Dictionary<History, History>();
            Debug.Assert(first.threads.Count == second.threads.Count);

            // A state is only mappable if its histories are mappable too.
            for (var i = 0; i < first.threads.Count; i++)
            {
                var mine = first.threads[i].Histories;
                var theirs = second.threads[i].Histories;
                var success = updateMap(mapping, reverse, mine, theirs);
                if (!success)
                {
                    return false;
                }
            }

            return true;
        }

        /**
         * Destructively update <code>map</code> until it maps from to to. A -1 entry in map means that
         * the value can still be changed. Other values are left untouched.
         *
         * @param map Must be at least as big as the biggest values in both from and to. Elements must
         *        be >= -1. -1 stands for unassigned.
         * @param from same length as to.
         * @param to same length as from.
         * @return True if the mapping was successful; false otherwise.
         */
        private bool updateMap(IDictionary<History, History> map, IDictionary<History, History> reverse, Arraylike from, Arraylike to)
        {
            Debug.Assert(from.Size == to.Size);

            // Go over the tag list and iteratively try to find counterexample.
            using var enumFrom = from.GetEnumerator();
            using var enumTo = to.GetEnumerator();
            while (enumFrom.MoveNext())
            {
                var res = enumTo.MoveNext();
                Debug.Assert(res);
                var historyFrom = enumFrom.Current;
                var historyTo = enumTo.Current;
                if (!map.ContainsKey(historyFrom))
                {
                    // If we don't know any mapping for h_from, we set it to the only mapping that can work.

                    if (reverse.ContainsKey(historyTo))
                    {
                        // But the target is taken already
                        return false;
                    }

                    map.Add(historyFrom, historyTo);
                    reverse.Add(historyTo, historyFrom);
                }
                else if (!map[historyFrom].Equals(historyTo) || !historyFrom.Equals(reverse[historyTo]))
                {
                    // Only mapping that could be chosen for h_from and h_to contradicts existing mapping.
                    return false;
                } // That means the existing mapping matches.
            }

            return true;
        }

        /** @return Ordered instructions for mapping. The ordering is such that they don't interfere with each other. */
        public IList<Instruction> MappingInstructions(IDictionary<History, History> map)
        {
            // Reverse topological sort of map.
            // For the purpose of this method, map is a restricted DAG.
            // Nodes have at most one incoming and outgoing edges.
            // The instructions that we return are the *edges* of the graph, histories are the nodes.
            // We identify every edge by its *source* node.
            var ret = new List<Instruction>(map.Count);
            var stack = new Stack<History>();
            var visitedSources = new HashSet<History>();

            // Go through the edges of the graph. Identify edge e by source node source:
            foreach (var source in map.Keys)
            {
                Debug.Assert(source != null);
                // Push e on stack, unless e deleted
                if (visitedSources.Contains(source))
                {
                    continue;
                }

                stack.Push(source);
                // while cur has undeleted following edges, mark cur as deleted, follow the edge, repeat.
                var src = source;
                while (src != null && !visitedSources.Contains(src))
                {
                    visitedSources.Add(src);
                    if (map.TryGetValue(src, out src))
                    {
                        stack.Push(src);
                    }
                }

                // walk stack backward, add to ret.
                stack.Pop(); // top element is no source node.
                while (stack.Count != 0)
                {
                    var cur = stack.Pop();
                    var target = map[cur];
                    if (!cur.Equals(target))
                    {
                        ret.Add(instructionMaker.Reorder(target, cur));
                    }
                }
            }

            Debug.Assert(stack.Count == 0);
            return ret;
        }

        private int positionFor(Tag tau)
        {
            Debug.Assert(tau.IsEndTag || tau.IsStartTag);

            var r = 2 * tau.Group.Number;
            if (tau.IsEndTag)
            {
                r++;
            }

            return r;
        }

        /**
         * @return an array {@code parentOf} such that for capture group `t`,
         * its parent is {@code parentOf[t]}.
         */
        internal int[] makeParentOf()
        {
            var allTags = tnfa.allTags();
            var ret = new int[allTags.Count / 2];
            foreach (var t in allTags)
            {
                ret[t.Group.Number] = t.Group.parent.Number;
            }

            return ret;
        }
    }
}
