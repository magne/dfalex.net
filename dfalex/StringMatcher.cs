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

namespace CodeHive.DfaLex
{
    /// <summary>
    /// This class implements fast matching in a string using DFAs.
    ///
    /// Substrings matching patterns are discovered with the <see cref="FindNext"/> and
    /// <see cref="MatchAt"/> methods, both of which take a DFA start state for the patterns to find.
    ///
    /// NOTE that you don't have to pass the same state every time -- different calls with the
    /// same matcher can search for different patterns and return different kinds of results.
    ///
    /// 3 pointers are maintained in the string:
    /// <list type="bulled">
    /// <item>
    ///  The LastMatchStart position is the position in the source string of the start of the
    ///  last successful match, or if no match has been performed yet.
    /// </item><item>
    ///  The LastMatchEnd position is the position in the source string of the end of the last
    ///  successful match, or 0 of no match has been performed yet
    /// </item><item>
    ///  The SearchLimit is highest position to search.  This is initially set to the source string
    ///  length.  No characters at positions &gt;= SearchLimit will be included in matches
    /// </item>
    /// </list>
    /// </summary>
    public class StringMatcher<TResult>
    {
        private const    int    NmmSize = 40;
        private readonly string src;
        private          int    limit;

        //non-matching memo
        //For all x >= m_nmmStart, whenever you're in m_nmmState[x] at position m_nmmPositions[x],
        //you will fail to find a match
        private          int                 nmmStart     = NmmSize;
        private readonly int[]               nmmPositions = new int[NmmSize];
        private readonly DfaState<TResult>[] nmmStates    = new DfaState<TResult>[NmmSize];

        /// <summary>
        /// Create a new StringMatcher.
        ///
        /// The LastMatchStart and LastMatchEnd positions are initialized to zero
        /// </summary>
        /// <param name="src">the source string to be searched</param>
        public StringMatcher(string src)
        {
            this.src = src;
            limit = this.src.Length;
        }

        /// <summary>
        ///Set the LastMatchStart, LastMatchEnd, and SearchLimit positions explicitly.
        /// </summary>
        /// <param name="lastMatchStart">the new lastMatchStartPosition</param>
        /// <param name="lastMatchEnd">the new lastMatchEnd position</param>
        /// <param name="searchLimit">the new searchLimit.  This will be limited to the source
        /// string length, so you can pass Integer.MAX_VALUE to set it to the string length explicitly.</param>
        /// <exception cref="IndexOutOfRangeException">if (lastMatchStart &lt; 0 || lastMatchEnd &lt; lastMatchStart
        /// || searchLimit &lt; lastMatchEnd)</exception>
        public void SetPositions(int lastMatchStart, int lastMatchEnd, int searchLimit)
        {
            searchLimit = Math.Min(searchLimit, src.Length);
            if (lastMatchStart < 0 || lastMatchEnd < lastMatchStart || searchLimit < lastMatchEnd)
            {
                throw new IndexOutOfRangeException("Invalid positions in StringMatcher.setPositions");
            }

            LastMatchStart = lastMatchStart;
            LastMatchEnd = lastMatchEnd;
            limit = searchLimit;
            nmmStart = NmmSize;
        }

        /// <summary>
        /// Resets the matcher to its initial state
        ///
        /// This is equivalent to setPositions(0,0,int.MaxValue);
        /// </summary>
        public void Reset()
        {
            SetPositions(0, 0, int.MaxValue);
        }

        /// <summary>
        /// Get the start position of the last successful match, or 0 if there isn't one.
        /// </summary>
        public int LastMatchStart { get; private set; }

        /// <summary>
        /// Get the end position of the last successful match, or 0 if there isn't one
        /// </summary>
        public int LastMatchEnd { get; private set; }

        /// <summary>
        /// Get the last successful matching substring, or "" if there isn't one.
        /// </summary>
        public string LastMatch
        {
            get
            {
                if (LastMatchEnd <= LastMatchStart)
                {
                    return string.Empty;
                }

                return src.Substring(LastMatchStart, LastMatchEnd - LastMatchStart);
            }
        }

        /// <summary>
        /// Find the next non-empty match.
        ///
        /// The string is searched from <see cref="LastMatchEnd"/> to the search limit to find a
        /// substring that matches a pattern in the given DFA.
        ///
        /// If there is a match, then the LastMatchStart and LastMatchEnd positions are set to the
        /// start and end of the first match, and the <typeparamref name="TResult" /> that the
        /// DFA produces for that match is returned.
        ///
        /// If there is more than one match starting at the same position, the longest one is selected.
        /// </summary>
        /// <param name="state">The start state of the DFA for the patterns you want to find</param>
        /// <param name="val">The <typeparamref name="TResult"/> that the DFA produces for the match</param>
        /// <returns>True if there is a next non-empty match in the string, or false if there isn't one</returns>
        public bool FindNext(DfaState<TResult> state, out TResult val)
        {
            val = default;
            for (var pos = LastMatchEnd; pos < limit; ++pos)
            {
                if (MatchAt(state, pos, out val))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Find the longest match starting at a given position.
        ///
        /// If there is a non-empty match for the DFA in the source string starting at startPos,
        /// then the LastMatchStart position is set to startPos, the LastMatchEnd position is set
        /// to the end of the longest such match, and the TResult from that match is returned.
        /// </summary>
        /// <param name="state">The start state of the DFA for the patterns you want to match</param>
        /// <param name="startPos">the position in the source string to test for a match</param>
        /// <param name="val">The TResult that the pattern matches</param>
        /// <returns>True if the source string matches a pattern in the DFA at startPos. Otherwise false.</returns>
        public bool MatchAt(DfaState<TResult> state, int startPos, out TResult val)
        {
            val = default;
            var ret = false;
            var newNmmSize = 0;
            var writeNmmNext = startPos + 4;

            for (var pos = startPos; pos < limit;)
            {
                state = state.GetNextState(src[pos]);
                pos++;
                if (state == null)
                {
                    break;
                }

                if (state.IsAccepting)
                {
                    ret = true;
                    val = state.Match;
                    LastMatchEnd = pos;
                    newNmmSize = 0;
                    continue;
                }

                //Check and update the non-matching memo, to accelerate processing long sequences
                //of non-accepting states at multiple positions
                //Many DFAs simply don't have long sequences of non-accepting states, so we only
                //want to incur this overhead when we're actually in a non-accepting state
                bool exitPosLoop = false;
                while (!exitPosLoop && nmmStart < NmmSize && nmmPositions[nmmStart] <= pos)
                {
                    if (nmmPositions[nmmStart] == pos && nmmStates[nmmStart] == state)
                    {
                        //hit the memo -- we won't find a match.
                        exitPosLoop = true;
                    }

                    //we passed this memo entry without using it -- remove it.
                    ++nmmStart;
                }

                if (exitPosLoop)
                {
                    break;
                }

                if (pos >= writeNmmNext && newNmmSize < NmmSize)
                {
                    nmmPositions[newNmmSize] = pos;
                    nmmStates[newNmmSize] = state;
                    ++newNmmSize;
                    writeNmmNext = pos + (2 << newNmmSize);
                    if (nmmStart < newNmmSize)
                    {
                        nmmStart = newNmmSize;
                    }
                }
            }

            //successful or not, we're done.  Merge in our new entries for the non-matching memo
            while (nmmStart < NmmSize && nmmPositions[nmmStart] < writeNmmNext)
            {
                ++nmmStart;
            }

            while (newNmmSize > 0)
            {
                --newNmmSize;
                --nmmStart;
                nmmPositions[nmmStart] = nmmPositions[newNmmSize];
                nmmStates[nmmStart] = nmmStates[newNmmSize];
            }

            if (ret)
            {
                LastMatchStart = startPos;
            }

            return ret;
        }

        /// <summary>
        /// See if a whole string matches a DFA
        /// </summary>
        /// <param name="state">DFA start state</param>
        /// <param name="str">string to test</param>
        /// <param name="val">The TResult the match result produced</param>
        /// <returns>True if the whole string matches the DFA.  Otherwise false.</returns>
        public static bool MatchWholeString(DfaState<TResult> state, string str, out TResult val)
        {
            val = default;
            var len = str.Length;
            for (var i = 0; i < len; i++)
            {
                if (state == null)
                {
                    return false;
                }

                state = state.GetNextState(str[i]);
            }

            if (state == null || !state.IsAccepting)
            {
                return false;
            }

            val = state.Match;
            return true;
        }
    }
}
