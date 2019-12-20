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

namespace CodeHive.DfaLex
{
    [Serializable]
    internal class SerializableDfa<TResult>
    {
        private readonly List<DfaStatePlaceholder<TResult>> dfaStates;
        private readonly int[]                              startStateNumbers;

        private List<DfaState<TResult>> startStatesMemo;

        public SerializableDfa(RawDfa<TResult> rawDfa)
        {
            var origStates = rawDfa.States;
            var len = origStates.Count;
            dfaStates = new List<DfaStatePlaceholder<TResult>>(len);
            startStateNumbers = rawDfa.StartStates;
            while (dfaStates.Count < len)
            {
                dfaStates.Add(new PackedTreeDfaPlaceholder<TResult>(rawDfa, dfaStates.Count));
            }
        }

        public List<DfaState<TResult>> GetStartStates()
        {
            if (startStatesMemo == null)
            {
                var len = dfaStates.Count;
                for (var i = 0; i < len; ++i)
                {
                    dfaStates[i].CreateDelegate(i, dfaStates);
                }

                for (var i = 0; i < len; ++i)
                {
                    dfaStates[i].FixPlaceholderReferences();
                }

                startStatesMemo = new List<DfaState<TResult>>(startStateNumbers.Length);
                foreach (var startState in startStateNumbers)
                {
                    startStatesMemo.Add(dfaStates[startState].ResolvePlaceholder());
                }
            }

            return startStatesMemo;
        }
    }
}
