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
    /// Base class for serializable placeholders that construct final-form DFA states and
    /// temporarily assume their places in the DFA.
    ///
    /// In serialized placeholders, target states are identified by their state number in a SerializableDfa.
    /// </summary>
    [Serializable]
    internal abstract class DfaStatePlaceholder<TResult> : DfaStateImpl<TResult>
    {
        protected DfaStateImpl<TResult> Delegate = null;

        /// <summary>
        /// Create a new DfaStatePlaceholder
        ///
        /// The initially constructed stat will accept no strings.
        /// </summary>
        public DfaStatePlaceholder()
        { }

        /// <summary>
        /// Creates the final form delegate state, implementing all the required transitions and matches.
        ///
        /// This is called on all DFA state placeholders after they are constructed
        /// </summary>
        /// <param name="statenum"></param>
        /// <param name="allStates"></param>
        internal abstract void CreateDelegate(int statenum, List<DfaStatePlaceholder<TResult>> allStates);

        internal sealed override void FixPlaceholderReferences()
        {
            Delegate.FixPlaceholderReferences();
        }

        internal sealed override DfaStateImpl<TResult> ResolvePlaceholder()
        {
            return Delegate.ResolvePlaceholder();
        }

        public sealed override DfaState<TResult> GetNextState(char c)
        {
            return Delegate.GetNextState(c);
        }

        public sealed override bool IsAccepting => Delegate.IsAccepting;

        public sealed override TResult Match => Delegate.Match;

        public sealed override void EnumerateTransitions(DfaTransitionConsumer<TResult> consumer)
        {
            Delegate.EnumerateTransitions(consumer);
        }

        public sealed override int StateNumber => Delegate.StateNumber;

        public override bool HasSuccessorStates => Delegate.HasSuccessorStates;

        public override IEnumerable<DfaState<TResult>> SuccessorStates => Delegate.SuccessorStates;
    }
}
