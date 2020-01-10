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

using System.Collections.Generic;
using System.Linq;

namespace CodeHive.DfaLex
{
    /// <summary>
    /// Turns an NFA into a non-minimal RawDfa by powerset construction
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    internal class DfaFromNfa<TResult>
    {
        //inputs
        private readonly Nfa<TResult>                  nfa;
        private readonly int[]                         nfaStartStates;
        private readonly int[]                         dfaStartStates;
        private readonly DfaAmbiguityResolver<TResult> ambiguityResolver;

        //utility
        private readonly DfaStateSignatureCodec dfaSigCodec = new DfaStateSignatureCodec();

        //These fields are scratch space
        private readonly IntListKey.Builder tempStateSignature = new IntListKey.Builder();
        private readonly Queue<int>         tempNfaClosureList = new Queue<int>();
        private readonly HashSet<TResult>   tempResultSet      = new HashSet<TResult>();

        //accumulators
        private readonly Dictionary<TResult, int> acceptSetMap = new Dictionary<TResult, int>();
        private readonly List<(bool, TResult)>    acceptSets   = new List<(bool, TResult)>();

        private readonly Dictionary<IntListKey, int> dfaStateSignatureMap = new Dictionary<IntListKey, int>();
        private readonly List<IntListKey>            dfaStateSignatures   = new List<IntListKey>();
        private readonly List<DfaStateInfo>          dfaStates            = new List<DfaStateInfo>();

        public DfaFromNfa(Nfa<TResult> nfa, int[] nfaStartStates, DfaAmbiguityResolver<TResult> ambiguityResolver)
        {
            this.nfa = nfa;
            this.nfaStartStates = nfaStartStates;
            dfaStartStates = new int[nfaStartStates.Length];
            this.ambiguityResolver = ambiguityResolver;
            acceptSets.Add((false, default));
            Build();
        }

        public RawDfa<TResult> GetDfa()
        {
            return new RawDfa<TResult>(dfaStates, acceptSets, dfaStartStates);
        }

        private void Build()
        {
            var nfaStateSet = new CompactIntSubset(nfa.NumStates);
            var dfaStateTransitions = new List<NfaTransition>();
            var transitionQ = new List<NfaTransition>(1000);

            //Create the DFA start states
            for (var i = 0; i < dfaStartStates.Length; ++i)
            {
                nfaStateSet.Clear();
                AddNfaStateAndEpsilonsToSubset(nfaStateSet, nfaStartStates[i]);
                dfaStartStates[i] = GetDfaState(nfaStateSet);
            }

            //Create the transitions and other DFA states.
            //m_dfaStateSignatures grows as we discover new states.
            //m_dfaStates grows as we complete them
            for (var stateNum = 0; stateNum < dfaStateSignatures.Count; ++stateNum)
            {
                var dfaStateSig = dfaStateSignatures[stateNum];

                dfaStateTransitions.Clear();

                //For each DFA state, combine the NFA transitions for each
                //distinct character range into a DFA transiton, appending new DFA states
                //as we discover them.
                transitionQ.Clear();

                //dump all the NFA transitions for the state into the Q
                DfaStateSignatureCodec.Expand(dfaStateSig, state => nfa.ForStateTransitions(state, transitionQ.Add));

                //sort all the transitions by first character
                transitionQ.Sort((arg0, arg1) =>
                {
                    if (arg0.FirstChar != arg1.FirstChar)
                    {
                        return (arg0.FirstChar < arg1.FirstChar ? -1 : 1);
                    }

                    return 0;
                });

                var tqlen = transitionQ.Count;

                //first character we haven't accounted for yet
                var minc = (char) 0;

                //NFA transitions at index < tqstart are no longer relevant
                //NFA transitions at index >= tqstart are in first char order OR have first char <= minc
                //The sequence of NFA transitions contributing the the previous DFA transition starts here
                var tqstart = 0;

                //make a range of NFA transitions corresponding to the next DFA transition
                while (tqstart < tqlen)
                {
                    var trans = transitionQ[tqstart];
                    if (trans.LastChar < minc)
                    {
                        ++tqstart;
                        continue;
                    }

                    //INVAR - trans contributes to the next DFA transition
                    nfaStateSet.Clear();
                    AddNfaStateAndEpsilonsToSubset(nfaStateSet, trans.State);
                    var startc = trans.FirstChar;
                    var endc = trans.LastChar;
                    if (startc < minc)
                    {
                        startc = minc;
                    }

                    //make range of all transitions that include the start character, removing ones
                    //that drop out
                    for (var tqend = tqstart + 1; tqend < tqlen; ++tqend)
                    {
                        trans = transitionQ[tqend];
                        if (trans.LastChar < startc)
                        {
                            //remove this one
                            transitionQ[tqend] = transitionQ[tqstart++];
                            continue;
                        }

                        if (trans.FirstChar > startc)
                        {
                            //this one is for the next transition
                            if (trans.FirstChar <= endc)
                            {
                                endc = (char) (trans.FirstChar - 1);
                            }

                            break;
                        }

                        //this one counts
                        if (trans.LastChar < endc)
                        {
                            endc = trans.LastChar;
                        }

                        AddNfaStateAndEpsilonsToSubset(nfaStateSet, trans.State);
                    }

                    dfaStateTransitions.Add(new NfaTransition(startc, endc, GetDfaState(nfaStateSet)));

                    minc = (char) (endc + 1);
                    if (minc < endc)
                    {
                        //wrapped around
                        break;
                    }
                }

                //INVARIANT: m_dfaStatesOut.size() == stateNum
                dfaStates.Add(CreateStateInfo(dfaStateSig, dfaStateTransitions));
            }
        }

        //Add an NFA state to m_currentNFASubset, along with the transitive
        //closure over its epsilon transitions
        private void AddNfaStateAndEpsilonsToSubset(CompactIntSubset dest, int stateNum)
        {
            tempNfaClosureList.Clear();
            if (dest.Add(stateNum))
            {
                tempNfaClosureList.Enqueue(stateNum);
            }

            while (tempNfaClosureList.Any())
            {
                var newNfaState = tempNfaClosureList.Dequeue();
                nfa.ForStateEpsilons(newNfaState,
                    src =>
                    {
                        if (dest.Add(src))
                        {
                            tempNfaClosureList.Enqueue(src);
                        }
                    });
            }
        }

        //Make a DFA state for a set of simultaneous NFA states
        private int GetDfaState(CompactIntSubset nfaStateSet)
        {
            //dump state combination into compressed form
            tempStateSignature.Clear();
            dfaSigCodec.Start(tempStateSignature.Add, nfaStateSet.Size, nfaStateSet.Range);
            nfaStateSet.DumpInOrder(stateNum =>
            {
                if (nfa.HasTransitionsOrAccepts(stateNum))
                {
                    dfaSigCodec.AcceptInt(stateNum);
                }
            });
            dfaSigCodec.Finish();

            //make sure it's in the map
            var stateSig = tempStateSignature.Build();
            if (!dfaStateSignatureMap.TryGetValue(stateSig, out var dfaStateNum))
            {
                dfaStateNum = dfaStateSignatures.Count;
                dfaStateSignatures.Add(stateSig);
                dfaStateSignatureMap.Add(stateSig, dfaStateNum);
            }

            return dfaStateNum;
        }

        private DfaStateInfo CreateStateInfo(IntListKey sig, List<NfaTransition> transitions)
        {
            //calculate the set of accepts
            tempResultSet.Clear();
            DfaStateSignatureCodec.Expand(sig,
                nfastate =>
                {
                    var accept = nfa.GetAccept(nfastate);
                    if (!EqualityComparer<TResult>.Default.Equals(accept, default))
                    {
                        tempResultSet.Add(accept);
                    }

                    if (nfa.IsAccepting(nfastate))
                    {
                        tempResultSet.Add(nfa.GetAccept(nfastate));
                    }
                });

            //and get an accept set index for it
            (bool accepting, TResult accept) dfaAccept = (false, default);
            if (tempResultSet.Count > 1)
            {
                dfaAccept = (true, ambiguityResolver(tempResultSet));
            }
            else if (tempResultSet.Count != 0)
            {
                dfaAccept = (true, tempResultSet.Single());
            }

            var acceptSetIndex = 0;
            if (dfaAccept.accepting && !acceptSetMap.TryGetValue(dfaAccept.accept, out acceptSetIndex))
            {
                acceptSets.Add(dfaAccept);
                acceptSetIndex = acceptSets.Count - 1;
                acceptSetMap[dfaAccept.accept] = acceptSetIndex;
            }

            return new DfaStateInfo(transitions, acceptSetIndex);
        }
    }
}
