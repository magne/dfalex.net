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
    /// Base interface for the types of patterns that can be used with <see cref="DfaBuilder{TResult}"/> to specify a
    /// set of strings to match.
    ///
    /// The primary implementation classes are <see cref="Pattern"/> and <see cref="CharRange"/>.
    /// </summary>
    public interface IMatchable
    {
        /// <summary>
        /// Add states to an NFA to match the desired pattern.
        ///
        /// New states will be created in the NFA to match the pattern and transitions to the given <paramref name="targetState"/>.
        ///
        /// NO NEW TRANSITIONS will be added to the target state or any other pre-existing states.
        /// </summary>
        /// <param name="nfa">NFA builder to add to.</param>
        /// <param name="targetState">target state after the pattern is matched</param>
        /// <param name="captureGroup">current capture group</param>
        /// <typeparam name="TState">The type of the NFA states.</typeparam>
        /// <returns>a state that transitions to <paramref name="targetState"/> after matching the pattern, and only after
        /// matching the pattern. This may be <paramref name="targetState"/> if the pattern is an empty string.</returns>
        TState AddToNfa<TState>(INfaBuilder<TState> nfa, TState targetState, CaptureGroup captureGroup);

        /// <returns><c>true</c> if this pattern matches the empty string</returns>
        bool MatchesEmpty { get; }

        /// <returns>True if this pattern matches any non-empty string</returns>
        bool MatchesNonEmpty { get; }

        /// <returns>True if this pattern matches anything at all</returns>
        bool MatchesSomething { get; }

        /// <returns>True if this pattern matches an infinite number of strings</returns>
        bool IsUnbounded { get; }

        /// <summary>
        /// Get the reverse of this pattern.
        ///
        /// The reverse of a pattern matches the reverse of all the strings that this pattern matches.
        /// </summary>
        /// <returns>The reverse of this pattern</returns>
        IMatchable Reversed { get; }

        // TODO remove when we get rid of InputRange
        IEnumerable<IMatchable> Children { get; }
    }
}
