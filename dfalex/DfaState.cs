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
    /// <summary>
    /// A state in a char-matching deterministic finite automaton (that's the google phrase) or DFA.
    /// </summary>
    /// <typeparam name="TResult">The type of result produced by matching a pattern.</typeparam>
    [Serializable]
    public abstract class DfaState<TResult>
    {
        /// <summary>
        /// Process a character and get the next state.
        /// </summary>
        /// <param name="ch">input character</param>
        /// <returns>The DfaState that ch transitions to from this one, or null if there is no such state</returns>
        public abstract DfaState<TResult> GetNextState(char ch);

        /// <summary>
        /// If the sequence of characters that led to this state match a pattern in the language being processed,
        /// <c>true</c> is returned. Otherwise <c>false</c>.
        /// </summary>
        public abstract bool IsAccepting { get; }

        /// <summary>
        /// If <see cref="IsAccepting"/> is <c>true</c>, return the match result for that pattern.
        /// </summary>
        /// <exception cref="InvalidOperationException">If called when <see cref="IsAccepting"/> is <c>false</c>.</exception>
        public abstract TResult Match { get; }

        /// <summary>
        /// Get the state number.  All states reachable from the output of a single call to a <see cref="DfaBuilder{TResult}"/>
        /// build method will be compactly numbered starting at 0.
        ///
        /// These state numbers can be used to maintain auxiliary information about a DFA.
        ///
        /// See <see cref="DfaAuxiliaryInformation{TResult}"/>
        /// </summary>
        /// <returns>this state's state number</returns>
        public abstract int StateNumber { get; }

        /// <summary>
        /// Enumerate all the transitions out of this state
        /// </summary>
        /// <param name="consumer">each DFA transition will be sent here</param>
        public abstract void EnumerateTransitions(DfaTransitionConsumer<TResult> consumer);

        /// <summary>
        ///
        /// </summary>
        /// <returns>true if this state has any successor states</returns>
        public abstract bool HasSuccessorStates { get; }

        /// <summary>
        /// Get an <see cref="IEnumerable{T}"/> of all the successor states of this state.
        ///
        /// Note that the same successor state may appear more than once in the iteration
        /// </summary>
        /// <returns>an iterable of successor states.</returns>
        public abstract IEnumerable<DfaState<TResult>> SuccessorStates { get; }
    }
}
