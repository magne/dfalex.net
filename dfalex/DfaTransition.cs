/*
 * Copyright 2020 Magne Rasmussen
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

namespace CodeHive.DfaLex
{
    /// <summary>
    /// A transition between <see cref="DfaStateInfo"/>
    /// </summary>
    internal sealed class DfaTransition
    {
        /// <summary>
        /// The first character that triggers this transition.
        /// </summary>
        public char FirstChar { get; }

        /// <summary>
        /// The last character that triggers this transition.
        /// </summary>
        public char LastChar { get; }

        /// <summary>
        /// The target state of this transition.
        /// </summary>
        public int State { get; }

        /// <summary>
        /// Creates a new immutable NFA transtition.
        /// </summary>
        /// <param name="firstChar">The first character that triggers this transition.</param>
        /// <param name="lastChar">The last character that triggers this transition.</param>
        /// <param name="state">The target state of this transition.</param>
        public DfaTransition(char firstChar, char lastChar, int state)
        {
            FirstChar = firstChar;
            LastChar = lastChar;
            State = state;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj is DfaTransition t)
            {
                return FirstChar == t.FirstChar && LastChar == t.LastChar && State == t.State;
            }

            return false;
        }

        public override int GetHashCode()
        {
            var hash = unchecked((int) 2166136261L);
            hash = (hash ^ FirstChar) * 16777619;
            hash = (hash ^ LastChar) * 16777619;
            hash = (hash ^ State) * 16777619;
            return hash ^ (hash >> 16);
        }
    }
}
