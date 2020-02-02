using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CodeHive.DfaLex.tree
{
    // ReSharper disable once InconsistentNaming
    internal class TNfa
    {
        internal readonly IDictionary<(State state, InputRange range), IList<Transition>> transitions;
        internal readonly IDictionary<State, IList<Transition>>                           epsilonTransitions;
        internal readonly State                                                           initialState;
        internal readonly State                                                           finalState;
        private readonly  IList<Tag>                                                      tags;

        private TNfa(IDictionary<(State, InputRange), IList<Transition>> transitions, IDictionary<State, IList<Transition>> epsilonTransitions, State initialState,
            State finalState, IList<Tag> tags)
        {
            this.transitions = transitions;
            this.epsilonTransitions = epsilonTransitions;
            this.initialState = initialState;
            this.finalState = finalState;
            this.tags = tags;
        }

        private IList<InputRange> AllInputRanges => transitions.Keys.Select(key => key.range).ToList();

        private ISet<Tag> AllTags
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

        private IList<Transition> AvailableTransitionsFor(State q, InputRange ir)
        {
            return transitions.TryGetValue((q, ir), out var ret) ? ret : new List<Transition>();
        }

        private IList<Transition> AvailableEpsilonTransitionsFor(State q)
        {
            return epsilonTransitions.TryGetValue(q, out var ret) ? ret : new List<Transition>();
        }

        public override string ToString() => $"{initialState} -> {finalState}, {transitions.AsString()}, {epsilonTransitions.AsString()}";

        internal class Builder : INfaBuilder<State>
        {
            private readonly IDictionary<(State, InputRange), IList<Transition>> inputTransitions   = new Dictionary<(State, InputRange), IList<Transition>>();
            private readonly IDictionary<State, IList<Transition>>               epsilonTransitions = new Dictionary<State, IList<Transition>>();
            private readonly IList<Tag>                                          tags               = new List<Tag>();
            private          State                                               initialState;
            private          State                                               finalState;

            public Builder()
            {
                RegisterCaptureGroup(CaptureGroupMaker.EntireMatch);
            }

            public CaptureGroup.Maker CaptureGroupMaker { get; } = new CaptureGroup.Maker();

            public State AddState()
            {
                return new State();
            }

            public void AddTransition(State from, State to, char firstChar, char lastChar)
            {
                var range = InputRange.Make(firstChar, lastChar);
                if (!inputTransitions.TryGetValue((from, range), out var transitions))
                {
                    transitions = new List<Transition>();
                    inputTransitions[(from, range)] = transitions;
                }

                transitions.Add(new Transition(to, NfaTransitionPriority.Normal, Tag.None));
            }

            public void AddEpsilon(State from, State to, NfaTransitionPriority priority, Tag tag)
            {
                if (!epsilonTransitions.TryGetValue(from, out var transitions))
                {
                    transitions = new List<Transition>();
                    epsilonTransitions[from] = transitions;
                }

                transitions.Add(new Transition(to, priority, tag));
            }

            public void SetAsInitial(State state)
            {
                initialState = state;
            }

            public State MakeFinalState()
            {
                return this.finalState = new State();
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