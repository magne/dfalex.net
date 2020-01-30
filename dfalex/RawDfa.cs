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

namespace CodeHive.DfaLex
{
    /// <summary>
    ///A DFA in uncomrpessed form
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    internal class RawDfa<TResult>
    {
        private readonly List<DfaStateInfo>    dfaStates;
        private readonly List<(bool, TResult)> acceptSets;
        private readonly int[]                 startStates;

        /// <summary>
        /// Create a new RawDfa.
        /// </summary>
        public RawDfa(List<DfaStateInfo> dfaStates,
            List<(bool, TResult)> acceptSets,
            int[] startStates)
        {
            this.dfaStates = dfaStates;
            this.acceptSets = acceptSets;
            this.startStates = startStates;
        }

        public List<DfaStateInfo> States => dfaStates;

        public List<(bool accept, TResult match)> AcceptSets => acceptSets;

        public int[] StartStates => startStates;
    }
}
