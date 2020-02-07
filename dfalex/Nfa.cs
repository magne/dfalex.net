/*
 * Copyright 2015 Matthew Timmermans
 * Copyright 2019 Magne Rasmussen
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CodeHive.DfaLex
{
    /// <summary>
    /// Simple non-deterministic finite automaton (NFA) representation.
    ///
    /// A set of <see cref="IMatchable"/> patterns is converted to an NFA as an intermediate step toward creating the DFA.
    /// </summary>
    /// <typeparam name="TResult">The type of result produced by matching a pattern.</typeparam>
    public class Nfa<TResult>
    {
        public class Builder : INfaBuilder
        {
            private readonly CaptureGroup.Maker        captureGroupMaker = new CaptureGroup.Maker();
            private readonly List<List<NfaTransition>> stateTransitions  = new List<List<NfaTransition>>();
            private readonly List<List<NfaEpsilon>>    stateEpsilons     = new List<List<NfaEpsilon>>();

            private readonly List<(bool accepting, TResult accepts)> stateAccepts = new List<(bool, TResult)>();

            /// <summary>
            /// Add a new state to the NFA.
            /// </summary>
            /// <returns>the number of the new state</returns>
            public int AddState()
            {
                return AddState(default, false);
            }

            /// <summary>
            /// Add a new accepting state to the NFA.
            /// </summary>
            /// <param name="accept">Add a new state to the NFA</param>
            /// <returns>the number of the new state</returns>
            public int AddState(TResult accept)
            {
                return AddState(accept, true);
            }

            private int AddState(TResult accept, bool accepting)
            {
                var state = stateAccepts.Count;
                stateAccepts.Add((accepting, accept));
                stateTransitions.Add(null);
                stateEpsilons.Add(null);
                Debug.Assert(stateAccepts.Count == stateTransitions.Count);
                Debug.Assert(stateAccepts.Count == stateEpsilons.Count);
                return state;
            }

            public void SetAccepting(int state, TResult accept)
            {
                stateAccepts[state] = (true, accept);
            }

            /// <summary>
            /// Add a transition to the NFA.
            /// </summary>
            /// <param name="from">The state to transition from</param>
            /// <param name="to">The state to transition to</param>
            /// <param name="firstChar">The first character in the accepted range</param>
            /// <param name="lastChar">The last character in the accepted range</param>
            public void AddTransition(int from, int to, char firstChar, char lastChar)
            {
                var list = stateTransitions[from];
                if (list == null)
                {
                    list = new List<NfaTransition>();
                    stateTransitions[from] = list;
                }

                list.Add(new NfaTransition(firstChar, lastChar, to, Tag.None));
            }

            /// <summary>
            /// Add an epsilon transition to the NFA.
            /// </summary>
            /// <param name="from">The state to transition from</param>
            /// <param name="to">The state to transition to</param>
            /// <param name="priority">The priority of this transition</param>
            /// <param name="tag">The tag of this transition</param>
            public void AddEpsilon(int from, int to, NfaTransitionPriority priority, Tag tag)
            {
                var list = stateEpsilons[from];
                if (list == null)
                {
                    list = new List<NfaEpsilon>();
                    stateEpsilons[from] = list;
                }

                list.Add(new NfaEpsilon(to, priority, tag));
            }


            /// <summary>
            /// Make modified state, if necessary, that doesn't match the empty string.
            ///
            /// If <tt>state</tt> has a non-null result attached, or can reach such a state through epsilon transitions,
            /// then a DFA made from that state would match the empty string.  In that case a new NFA state will be created
            /// that matches all the same strings <i>except</i> the empty string.
            /// </summary>
            /// <param name="state">the number of the state to disemptify</param>
            /// <returns>If <tt>state</tt> matches the empty string, then a new state that does not match the empty string
            /// is returned.  Otherwise <tt>state</tt> is returned.</returns>
            public int Disemptify(int state)
            {
                var reachable = new List<int>();

                //first find all epsilon-reachable states
                var checkSet = new HashSet<int>();
                reachable.Add(state);
                checkSet.Add(reachable[0]); //same Integer instance
                for (var i = 0; i < reachable.Count; ++i)
                {
                    ForStateEpsilons(reachable[i],
                        trans =>
                        {
                            if (checkSet.Add(trans.State))
                            {
                                reachable.Add(trans.State);
                            }
                        });
                }

                //if none of them accept, then we're done
                for (var i = 0;; ++i)
                {
                    if (i >= reachable.Count)
                    {
                        return state;
                    }

                    if (IsAccepting(reachable[i]))
                    {
                        break;
                    }
                }

                //need to make a new disemptified state.  first get all transitions
                var newState = AddState();
                var transSet = new HashSet<NfaTransition>();
                foreach (var src in reachable)
                {
                    ForStateTransitions(src,
                        trans =>
                        {
                            if (transSet.Add(trans))
                            {
                                AddTransition(newState, trans.State, trans.FirstChar, trans.LastChar);
                            }
                        });
                }

                return newState;
            }

            public CaptureGroup MakeCaptureGroup(CaptureGroup parent)
            {
                return captureGroupMaker.Next(parent);
            }

            public Nfa<TResult> Build()
            {
                return new Nfa<TResult>(stateTransitions, stateEpsilons, stateAccepts);
            }

            private bool IsAccepting(int state)
            {
                return stateAccepts[state].accepting;
            }

            private void ForStateEpsilons(int state, Action<NfaEpsilon> dest)
            {
                var list = stateEpsilons[state];
                list?.ForEach(dest);
            }

            private void ForStateTransitions(int state, Action<NfaTransition> dest)
            {
                var list = stateTransitions[state];
                list?.ForEach(dest);
            }
        }

        public static Builder GetBuilder()
        {
            return new Builder();
        }

        private readonly List<List<NfaTransition>> stateTransitions;
        private readonly List<List<NfaEpsilon>>    stateEpsilons;

        private readonly List<(bool accepting, TResult accepts)> stateAccepts;

        private Nfa(List<List<NfaTransition>> stateTransitions, List<List<NfaEpsilon>> stateEpsilons, List<(bool accepting, TResult accepts)> stateAccepts)
        {
            this.stateTransitions = stateTransitions;
            this.stateEpsilons = stateEpsilons;
            this.stateAccepts = stateAccepts;
        }

        /// <summary>
        /// Get the number of states in the NFA
        /// </summary>
        /// <returns>the total number of states that have been added with <see cref="CodeHive.DfaLex.Nfa{TResult}.Builder.AddState()"/> and
        /// <see cref="CodeHive.DfaLex.Nfa{TResult}.Builder.AddState()"/>.</returns>
        public int NumStates => stateAccepts.Count;

        /// <summary>
        /// Is the given state an accepting state?
        /// </summary>
        /// <param name="state">the state number</param>
        /// <returns>True if the given state is accepting</returns>
        public bool IsAccepting(int state)
        {
            return stateAccepts[state].accepting;
        }

        /// <summary>
        /// Get the result attached to the given state
        /// </summary>
        /// <param name="state">the state number</param>
        /// <returns>the result that was provided to <see cref="INfaBuilder.AddState()"/> when the state was created</returns>
        public TResult GetAccept(int state)
        {
            return stateAccepts[state].accepts;
        }

        /// <summary>
        /// Check whether a state has any non-epsilon transitions or has a result attached
        /// </summary>
        /// <param name="state">the state number</param>
        /// <returns>true if the state has any transitions or accepts</returns>
        public bool HasTransitionsOrAccepts(int state)
        {
            return stateAccepts[state].accepting || stateTransitions[state] != null;
        }

        /// <summary>
        /// Get all the epsilon transitions from a state
        /// </summary>
        /// <param name="state">the state number</param>
        /// <returns>An enumerable over all transitions out of the given state</returns>
        public IEnumerable<NfaEpsilon> GetStateEpsilons(int state)
        {
            var list = stateEpsilons[state];
            return list != null ? (IEnumerable<NfaEpsilon>) list : new NfaEpsilon[0];
        }

        /// <summary>
        /// Get all the non-epsilon transitions from a state
        /// </summary>
        /// <param name="state">the state number</param>
        /// <returns>An enumerable over all transitions out of the given state</returns>
        public IEnumerable<NfaTransition> GetStateTransitions(int state)
        {
            var list = stateTransitions[state];
            return list != null ? (IEnumerable<NfaTransition>) list : new NfaTransition[0];
        }

        internal void ForStateEpsilons(int state, Action<NfaEpsilon> dest)
        {
            var list = stateEpsilons[state];
            list?.ForEach(dest);
        }

        internal void ForStateTransitions(int state, Action<NfaTransition> dest)
        {
            var list = stateTransitions[state];
            list?.ForEach(dest);
        }
    }

    public interface INfaBuilder
    {
        int AddState();
        void AddTransition(int from, int to, char firstChar, char lastChar);
        void AddEpsilon(int from, int to, NfaTransitionPriority priority, Tag tag);
        CaptureGroup MakeCaptureGroup(CaptureGroup parent);
    }
}
