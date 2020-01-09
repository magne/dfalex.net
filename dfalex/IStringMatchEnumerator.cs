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
using System.Collections;
using System.Collections.Generic;

namespace CodeHive.DfaLex
{
    /// <summary>
    /// An <see cref="IEnumerator{T}"/> that provides access to the pattern matches in a string.
    ///
    /// <see cref="StringSearcher{TResult}.SearchString"/> produces these.
    /// </summary>
    public interface IStringMatchEnumerator<out TResult> : IEnumerator<TResult>
    {
        /// <summary>
        /// Get the position of the start of the last match in the string.
        /// </summary>
        /// <returns>the index of the first character in the last match</returns>
        /// <exception cref="InvalidOperationException">unless called after a valid call to <see cref="IEnumerator.MoveNext"/></exception>
        int MatchStartPosition { get; }

        /// <summary>
        /// Get the position of the end of the last match in the string.
        /// </summary>
        /// <returns>the index after the last character in the last match</returns>
        /// <exception cref="InvalidOperationException">unless called after a valid call to <see cref="IEnumerator.MoveNext"/></exception>
        int MatchEndPosition { get; }

        /// <summary>
        /// Get the string value of the last match
        ///
        /// Note that a new string is allocated by the first call to this method for each match.
        /// </summary>
        /// <returns>the source portion of the source string corresponding to the last match</returns>
        /// <exception cref="InvalidOperationException">unless called after a valid call to <see cref="IEnumerator.MoveNext"/></exception>
        string MatchValue { get; }

        /// <summary>
        /// Get the result of the last match.
        /// </summary>
        /// <returns>the TResult returned by the last call to <see cref="IEnumerator.MoveNext"/></returns>
        /// <exception cref="InvalidOperationException">unless called after a valid call to <see cref="IEnumerator.MoveNext"/></exception>
        TResult MatchResult { get; }

        /// <summary>
        /// Rewind (or jump forward) to a given position in the source string
        ///
        /// The next match returned will be the one (if any) that starts at a position &gt;= pos
        ///
        /// IMPORTANT:  If this method returns true, you must call <see cref="IEnumerator.MoveNext"/> to get the result
        /// of the next match.  Until then calls to the the match accessor methods will continue to return information
        /// from the previous call to <see cref="IEnumerator.MoveNext"/>.
        /// </summary>
        /// <param name="pos">new position in the source string to search from</param>
        /// <returns>true if there is a match after the given position.  The same value will be returned from <see cref="IEnumerator.MoveNext"/></returns>
        bool Reposition(int pos);
    }
}
