using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CodeHive.DfaLex.tree
{
    // ReSharper disable once InconsistentNaming
    internal class TNFA
    {
        // TODO Should be private
        internal readonly IDictionary<KeyValuePair<State, InputRange>, List<Transition>> transitions;
        private readonly  IDictionary<State, List<Transition>>                           epsilonTransitions;
        internal readonly State                                                          initialState;
        internal readonly State                                                          finalState;
        internal readonly List<Tag>                                                      tags;

        internal TNFA(IDictionary<KeyValuePair<State, InputRange>, List<Transition>> transitions,
                      IDictionary<State, List<Transition>> epsilonTransitions,
                      State initialState,
                      State finalState,
                      List<Tag> tags)
        {
            this.transitions = transitions;
            this.epsilonTransitions = epsilonTransitions;
            this.initialState = initialState;
            this.finalState = finalState;
            this.tags = tags;
        }

        internal class Builder
        {
            internal readonly CaptureGroup.CaptureGroupMaker                                 captureGroupMaker = new CaptureGroup.CaptureGroupMaker();
            State                                                                   finalState;
            State                                                                   initialState;
            readonly List<Tag>                                                      tags = new List<Tag>();
            readonly SortedSet<InputRange>                                          allInputRanges;
            readonly IDictionary<KeyValuePair<State, InputRange>, List<Transition>> transitions        = new Dictionary<KeyValuePair<State, InputRange>, List<Transition>>();
            readonly IDictionary<State, List<Transition>>                           epsilonTransitions = new Dictionary<State, List<Transition>>();

            private Builder(SortedSet<InputRange> allInputRanges)
            {
                this.allInputRanges = allInputRanges;
            }

            internal static Builder make(IList<InputRange> uncleanInputRanges)
            {
                return new Builder(new SortedSet<InputRange>(InputRangeCleanup.CleanUp(uncleanInputRanges)));
            }

            private void putEpsilon(State state, State endingState, Transition.Priority priority, Tag tag)
            {
                if (!epsilonTransitions.ContainsKey(state))
                {
                    epsilonTransitions.Add(state, new List<Transition>());
                }

                epsilonTransitions[state].Add(new Transition(endingState, priority, tag));
            }

            internal void addEndTagTransition(IList<State> froms, State to, CaptureGroup captureGroup, Transition.Priority priority)
            {
                foreach (var from in froms)
                {
                    putEpsilon(from, to, priority, captureGroup.endTag);
                }
            }

            internal void addStartTagTransition(IList<State> froms, State to, CaptureGroup cg, Transition.Priority priority)
            {
                foreach (var from in froms)
                {
                    putEpsilon(from, to, priority, cg.startTag);
                }
            }

            /** Add untagged transitions for all input that are overlapped by {@code overlappedBy}. Priority: normal. */
            internal void addUntaggedTransition(InputRange overlappedBy, IList<State> froms, State to)
            {
                foreach (var from in froms)
                {
                    var lower = InputRange.Make(overlappedBy.From);
                    var upper = InputRange.Make(overlappedBy.To);
                    var overlappedRanges = allInputRanges.GetViewBetween(lower, upper);
                    foreach (var ir in overlappedRanges)
                    {
                        var key = new KeyValuePair<State, InputRange>(from, ir);
                        if (!transitions.ContainsKey(key))
                        {
                            transitions.Add(key, new List<Transition>());
                        }

                        transitions[key].Add(new Transition(to, Transition.Priority.NORMAL, Tag.None));
                    }
                }
            }

            internal void makeUntaggedEpsilonTransitionFromTo(IList<State> froms, IList<State> tos, Transition.Priority priority)
            {
                foreach (var from in froms)
                {
                    foreach (var to in tos)
                    {
                        putEpsilon(from, to, priority, Tag.None);
                    }
                }
            }

            internal TNFA build()
            {
                return new TNFA(transitions, epsilonTransitions, initialState, finalState, tags);
            }

            internal CaptureGroup makeCaptureGroup(CaptureGroup parent)
            {
                return captureGroupMaker.Next(parent);
            }

            internal State makeInitialState()
            {
                initialState = new State();
                return initialState;
            }

            /** @return a new non-final state */
            internal State makeState()
            {
                return new State();
            }

            /**
             * Sets the argument to be the single final state of the automaton. Must be called exactly once.
             */
            internal void setAsAccepting(State finalState)
            {
                if (this.finalState != null)
                {
                    throw new InvalidOperationException($"Only one final state can be handled.\nOld final state was {this.finalState}\n New final state is {finalState}");
                }

                this.finalState = finalState;
            }

            internal void registerCaptureGroup(CaptureGroup cg)
            {
                Debug.Assert(tags.Count / 2 == cg.Number);
                tags.Add(cg.startTag);
                tags.Add(cg.endTag);
            }
        }

        /** @return all input ranges as they are, possibly with duplicates. */
        internal IList<InputRange> allInputRanges()
        {
            return transitions.Keys.Select(range => range.Value).ToList();
        }

        /** @return all tags used anywhere, in any order, without duplicates. */
        internal ISet<Tag> allTags()
        {
            ISet<Tag> ret = new HashSet<Tag>();

            var all = new List<List<Transition>>(transitions.Count + epsilonTransitions.Count);
            all.AddRange(transitions.Values);
            all.AddRange(epsilonTransitions.Values);

            foreach (var triples in all)
            {
                foreach (var triple in triples)
                {
                    var tag = triple.tag;
                    if (tag.IsEndTag || tag.IsStartTag)
                    {
                        ret.Add(tag);
                    }
                }
            }

            return ret;
        }

        internal List<Transition> availableTransitionsFor(State key, InputRange ir)
        {
            if (!transitions.TryGetValue(new KeyValuePair<State, InputRange>(key, ir), out var ret))
            {
                return new List<Transition>();
            }

            return ret;
        }

        internal List<Transition> availableEpsilonTransitionsFor(State q)
        {
            if (!epsilonTransitions.TryGetValue(q, out var ret))
            {
                return new List<Transition>();
            }

            return ret;
        }

        public override string ToString() => $"{initialState} -> {finalState}, {transitions.AsString()}, {epsilonTransitions.AsString()}";
    }
}
