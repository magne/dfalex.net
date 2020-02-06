using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace CodeHive.DfaLex.tree
{
    // ReSharper disable once InconsistentNaming
    internal class TNfa
    {
        internal readonly IDictionary<(int state, InputRange range), IList<Transition>> transitions;
        internal readonly IDictionary<int, IList<Transition>>                           epsilonTransitions;
        internal readonly int                                                           initialState;
        internal readonly int                                                           finalState;
        private readonly  IList<Tag>                                                    tags;

        private TNfa(IDictionary<(int, InputRange), IList<Transition>> transitions,
                     IDictionary<int, IList<Transition>> epsilonTransitions,
                     int initialState,
                     int finalState,
                     IList<Tag> tags)
        {
            this.transitions = transitions;
            this.epsilonTransitions = epsilonTransitions;
            this.initialState = initialState;
            this.finalState = finalState;
            this.tags = tags;
        }

        internal IList<InputRange> AllInputRanges => transitions.Keys.Select(key => key.range).ToList();

        internal ISet<Tag> AllTags
        {
            get
            {
                var ret = new HashSet<Tag>();
                var all = transitions.Values.Concat(epsilonTransitions.Values).ToList();
                foreach (var tag in from triples in all from triple in triples select triple.Tag into tag where tag.IsStartTag || tag.IsEndTag select tag)
                {
                    ret.Add(tag);
                }

                return ret;
            }
        }

        internal IList<Transition> AvailableTransitionsFor(int q, InputRange ir)
        {
            return transitions.TryGetValue((q, ir), out var ret) ? ret : new List<Transition>();
        }

        internal IList<Transition> AvailableEpsilonTransitionsFor(int q)
        {
            return epsilonTransitions.TryGetValue(q, out var ret) ? ret : new List<Transition>();
        }

        public override string ToString() => $"{initialState} -> {finalState}, {transitions.AsString()}, {epsilonTransitions.AsString()}";

        internal class Builder : INfaBuilder
        {
            private readonly IDictionary<(int, InputRange), IList<Transition>> inputTransitions   = new Dictionary<(int, InputRange), IList<Transition>>();
            private readonly IDictionary<int, IList<Transition>>               epsilonTransitions = new Dictionary<int, IList<Transition>>();
            private readonly IList<Tag>                                        tags               = new List<Tag>();
            private          int                                               initialState;
            private          int                                               finalState;
            private readonly SortedSet<InputRange>                             allInputRanges;
            private          int                                               currentState = -1;

            public Builder(IEnumerable<InputRange> allInputRanges)
            {
                this.allInputRanges = new SortedSet<InputRange>(InputRangeCleanup.CleanUp(allInputRanges));
                RegisterCaptureGroup(CaptureGroupMaker.EntireMatch);
            }

            public CaptureGroup.Maker CaptureGroupMaker { get; } = new CaptureGroup.Maker();

            public int AddState()
            {
                return Interlocked.Increment(ref currentState);
            }

            public void AddTransition(int from, int to, char firstChar, char lastChar)
            {
                var lower = InputRange.Make(firstChar);
                var upper = InputRange.Make(lastChar);
                var overlappedRanges = allInputRanges.GetViewBetween(lower, upper);
                foreach (var key in overlappedRanges.Select(ir => (from, ir)))
                {
                    if (!inputTransitions.TryGetValue(key, out var transitions))
                    {
                        transitions = new List<Transition>();
                        inputTransitions[key] = transitions;
                    }

                    transitions.Add(new Transition(to, NfaTransitionPriority.Normal, Tag.None));
                }
            }

            public void AddEpsilon(int from, int to, NfaTransitionPriority priority, Tag tag)
            {
                if (!epsilonTransitions.TryGetValue(from, out var transitions))
                {
                    transitions = new List<Transition>();
                    epsilonTransitions[from] = transitions;
                }

                transitions.Add(new Transition(to, priority, tag));
            }

            public int MakeInitialState()
            {
                return initialState = AddState();
            }

            public void SetAsFinal(int state)
            {
                finalState = state;
            }

            public CaptureGroup MakeCaptureGroup(CaptureGroup parent)
            {
                var cg = CaptureGroupMaker.Next(parent);
                RegisterCaptureGroup(cg);
                return cg;
            }

            public void RegisterCaptureGroup(CaptureGroup cg)
            {
                Debug.Assert(tags.Count / 2 == cg.Number);
                tags.Add(cg.StartTag);
                tags.Add(cg.EndTag);
            }

            public TNfa Build()
            {
                return new TNfa(inputTransitions, epsilonTransitions, initialState, finalState, tags);
            }
        }
    }
}
