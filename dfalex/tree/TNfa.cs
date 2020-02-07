using System.Collections.Generic;
using System.Linq;

namespace CodeHive.DfaLex.tree
{
    // ReSharper disable once InconsistentNaming
    internal class TNfa
    {
        private readonly  IList<NfaTransition>[] stateTransitions;
        internal readonly IList<NfaEpsilon>[]    stateEpsilons;
        internal readonly int                    initialState;
        internal readonly int                    finalState;

        private TNfa(IList<IList<NfaTransition>> stateTransitions,
            IList<IList<NfaEpsilon>> stateEpsilons,
            int initialState,
            int finalState)
        {
            this.stateTransitions = stateTransitions.ToArray();
            this.stateEpsilons = stateEpsilons.ToArray();
            this.initialState = initialState;
            this.finalState = finalState;
            AllInputRanges = FindAllInputRanges();
            AllTags = FindAllTags();
        }

        internal IEnumerable<InputRange> AllInputRanges { get; }

        internal ISet<Tag> AllTags { get; }

        internal IEnumerable<NfaTransition> AvailableTransitionsFor(int q, InputRange range)
        {
            return stateTransitions[q]?.Where(transition => transition.FirstChar == range.From && transition.LastChar == range.To) ?? Enumerable.Empty<NfaTransition>();
        }

        internal IEnumerable<NfaEpsilon> AvailableEpsilonTransitionsFor(int q)
        {
            return stateEpsilons[q] ?? Enumerable.Empty<NfaEpsilon>();
        }

        public IEnumerable<NfaEpsilon> GetStateEpsilons(int state) => stateEpsilons[state] ?? Enumerable.Empty<NfaEpsilon>();

        public IEnumerable<NfaTransition> GetStateTransitions(int state) => stateTransitions[state] ?? Enumerable.Empty<NfaTransition>();

        public override string ToString() => $"{initialState} -> {finalState}, {stateTransitions.AsString()}, {stateEpsilons.AsString()}";

        private IEnumerable<InputRange> FindAllInputRanges()
        {
            return stateTransitions.Where(list => list != null).SelectMany(list => list).Select(transition => InputRange.Make(transition.FirstChar, transition.LastChar)).ToList();
        }

        private ISet<Tag> FindAllTags()
        {
            var tags = stateTransitions.Where(list => list != null).SelectMany(list => list).Select(transition => transition.Tag);
            tags = tags.Concat(stateEpsilons.Where(list => list != null).SelectMany(list => list).Select(transition => transition.Tag));
            return new HashSet<Tag>(tags.Where(tag => tag.IsStartTag || tag.IsEndTag));
        }

        internal class Builder : INfaBuilder
        {
            private readonly IList<IList<NfaTransition>> stateTransitions = new List<IList<NfaTransition>>();
            private readonly IList<IList<NfaEpsilon>>    stateEpsilons    = new List<IList<NfaEpsilon>>();
            private          int                         initialState;
            private          int                         finalState;
            private readonly SortedSet<InputRange>       allInputRanges;

            public Builder(IEnumerable<InputRange> allInputRanges)
            {
                this.allInputRanges = new SortedSet<InputRange>(InputRangeCleanup.CleanUp(allInputRanges));
            }

            public CaptureGroup.Maker CaptureGroupMaker { get; } = new CaptureGroup.Maker();

            public int AddState()
            {
                var state = stateTransitions.Count;
                stateTransitions.Add(null);
                stateEpsilons.Add(null);
                return state;
            }

            public void AddTransition(int from, int to, char firstChar, char lastChar)
            {
                var lower = InputRange.Make(firstChar);
                var upper = InputRange.Make(lastChar);

                var transitions = stateTransitions[from];
                if (transitions == null)
                {
                    transitions = new List<NfaTransition>();
                    stateTransitions[from] = transitions;
                }

                var overlappedRanges = allInputRanges.GetViewBetween(lower, upper);
                foreach (var range in overlappedRanges)
                {
                    transitions.Add(new NfaTransition(range.From, range.To, to, Tag.None));
                }
            }

            public void AddEpsilon(int from, int to, NfaTransitionPriority priority, Tag tag)
            {
                var transitions = stateEpsilons[from];
                if (transitions == null)
                {
                    transitions = new List<NfaEpsilon>();
                    stateEpsilons[from] = transitions;
                }

                transitions.Add(new NfaEpsilon(to, priority, tag));
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
                return CaptureGroupMaker.Next(parent);
            }

            public TNfa Build()
            {
                return new TNfa(stateTransitions, stateEpsilons, initialState, finalState);
            }
        }
    }
}
