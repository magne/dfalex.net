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
using System.Diagnostics;

namespace CodeHive.DfaLex
{
    /// <summary>
    /// Performs fast searches of a whole string for patterns.  When you need to search the entire string for the same
    /// set of patterns, this class is faster than <see cref="StringMatcher{TResult}"/>.
    ///
    /// NOTE: Instances of this class are thread-safe.
    /// </summary>
    /// <typeparam name="TResult">The type of result associated with the patterns being searched for</typeparam>
    public class StringSearcher<TResult>
    {
        private static readonly IStringMatchEnumerator<TResult> NoMatches = new NoMatchEnumerator();
        private readonly        DfaState<TResult>               matcher;
        private readonly        DfaState<bool>                  reverseFinder;

        /// <summary>
        /// Create a new StringSearcher.
        /// </summary>
        /// <param name="matcher">A DFA that matches the patterns being searched for</param>
        /// <param name="reverseFinder">A DFA that can be applied to a string backwards to find all the places where
        /// matches start.  See <see cref="DfaBuilder{TResult}.BuildReverseFinder()"/></param>
        public StringSearcher(DfaState<TResult> matcher, DfaState<bool> reverseFinder)
        {
            this.matcher = matcher;
            this.reverseFinder = reverseFinder;
        }

        /// <summary>
        /// Search the string for all occurrences of the patterns that this searcher finds
        /// </summary>
        /// <param name="src">String to search</param>
        /// <returns>a <see cref="IStringMatchEnumerator{TResult}"/> that returns all (non-overlapping) matches</returns>
        public IStringMatchEnumerator<TResult> SearchString(string src)
        {
            var pos = src.Length;
            var finderState = reverseFinder;
            if (finderState == null)
            {
                return NoMatches;
            }

            //see if the string has at least one match.  If there are
            //no matches, then we don't have to allocate anything
            while (true)
            {
                if (pos <= 0)
                {
                    return NoMatches;
                }

                --pos;
                finderState = finderState.GetNextState(src[pos]);
                if (finderState == null)
                {
                    return NoMatches;
                }

                if (finderState.IsAccepting && finderState.Match)
                {
                    break;
                }
            }

            //found at least one (the last) match
            //make a bit mask of matching positions, starting at the end
            var maskArray = new int[8];
            maskArray[maskArray.Length - 1] = 1 << 31;
            var maskStartPos = pos - (maskArray.Length * 32 - 1);
            while (pos > 0)
            {
                --pos;
                finderState = finderState.GetNextState(src[pos]);
                if (finderState == null)
                {
                    break;
                }

                if (finderState.IsAccepting && finderState.Match)
                {
                    if (pos < maskStartPos)
                    {
                        //need a longer array
                        var toAdd = Math.Max(maskStartPos - pos, maskArray.Length << 5);
                        toAdd = (toAdd + 31) >> 5; //bits to ints
                        var newMask = new int[maskArray.Length + toAdd];
                        for (var i = 0; i < maskArray.Length; ++i)
                        {
                            newMask[i + toAdd] = maskArray[i];
                        }

                        maskArray = newMask;
                        maskStartPos -= toAdd << 5;
                        Debug.Assert(maskStartPos <= pos);
                    }

                    var offset = pos - maskStartPos;
                    maskArray[(int) ((uint) offset >> 5)] |= 1 << (offset & 31);
                }
            }

            return new EnumeratorImpl(src, matcher, maskArray, maskStartPos);
        }

        /// <summary>
        /// Replace all occurrences of patterns in a string
        ///
        /// The string is searched for all (non-overlapping) occurrences of patterns in this searcher,
        /// for each occurrence, the provided replacer is called to supply a replacement value for
        /// that part of the string.  If it returns null, that part of the string remains unchanged.
        /// If it returns a String, then the pattern occurrence will be replaced with the string returned.
        /// </summary>
        /// <param name="src">the String to search</param>
        /// <param name="replacer">the <see cref="ReplacementSelector{TResult}"/> that provides new values for matches
        /// in the string</param>
        /// <returns>the new string with values replaced</returns>
        public string FindAndReplace(string src, ReplacementSelector<TResult> replacer)
        {
            var it = SearchString(src);
            StringReplaceAppendable dest = null;
            var doneTo = 0;
            while (it.MoveNext())
            {
                var mr = it.Current;
                var s = it.MatchStartPosition;
                var e = it.MatchEndPosition;
                if (dest == null)
                {
                    dest = new StringReplaceAppendable(src);
                }

                if (doneTo < s)
                {
                    dest.Append(src, doneTo, s);
                }

                doneTo = replacer(dest, mr, src, s, e);
                if (doneTo <= 0)
                {
                    doneTo = e;
                }
                else
                {
                    if (doneTo <= s)
                    {
                        throw new IndexOutOfRangeException("Replacer tried to rescan matched string");
                    }

                    it.Reposition(doneTo);
                }
            }

            if (dest != null)
            {
                if (doneTo < src.Length)
                {
                    dest.Append(src, doneTo, src.Length);
                }

                return dest.ToString();
            }

            return src;
        }

        private class EnumeratorImpl : IStringMatchEnumerator<TResult>
        {
            private readonly string            src;
            private readonly DfaState<TResult> matcher;
            private readonly int[]             matchMask;
            private readonly int               matchMaskPos;
            private          DfaState<TResult> nextEndState;
            private          int               nextScanStart; //where we started looking for m_next*
            private          int               nextPos;
            private          int               nextEnd;
            private          int               prevPos;
            private          int               prevEnd;
            private          bool              prevAccepting;
            private          TResult           prevResult;
            private          string            prevString;

            internal EnumeratorImpl(string src, DfaState<TResult> matcher, int[] matchMask, int matchMaskPos)
            {
                this.src = src;
                this.matcher = matcher;
                this.matchMask = matchMask;
                this.matchMaskPos = matchMaskPos;
                nextScanStart = 0;
                if (!ScanForNext(0, this.src.Length))
                {
                    nextEndState = null;
                    nextPos = nextEnd = this.src.Length;
                }
            }

            public void Dispose()
            {
                // empty implementation
            }

            public void Reset()
            {
                Reposition(0);
            }

            object IEnumerator.Current => Current;

            public TResult Current => prevResult;

            public bool MoveNext()
            {
                if (nextEndState == null)
                {
                    return false;
                }

                prevPos = nextPos;
                prevEnd = nextEnd;
                prevAccepting = nextEndState.IsAccepting;
                prevResult = nextEndState.Match;
                prevString = null;
                //extend the previously found match as far as possible
                var st = nextEndState;
                var len = src.Length;
                for (var pos = nextEnd; pos < len; pos++)
                {
                    st = st.GetNextState(src[pos]);
                    if (st == null)
                    {
                        break;
                    }

                    if (st.IsAccepting)
                    {
                        prevAccepting = st.IsAccepting;
                        prevResult = st.Match;
                        prevEnd = pos + 1;
                    }
                }

                nextScanStart = prevEnd;
                var last = false;
                if (!ScanForNext(prevEnd, len))
                {
                    nextEndState = null;
                    nextPos = nextEnd = len;
                    last = true;
                }

                return nextEndState != null || last;
            }

            public int MatchStartPosition => IsValid() ? prevPos : throw new InvalidOperationException();

            public int MatchEndPosition => IsValid() ? prevEnd : throw new InvalidOperationException();

            public string MatchValue => prevString ??= IsValid() ? src.Substring(prevPos, prevEnd - prevPos) : throw new InvalidOperationException();

            public TResult MatchResult => IsValid() ? prevResult : throw new InvalidOperationException();

            public bool Reposition(int pos)
            {
                if (pos >= nextScanStart)
                {
                    if (nextEndState == null)
                    {
                        return false;
                    }

                    if (pos <= nextPos)
                    {
                        return true;
                    }

                    nextScanStart = pos;
                    if (!ScanForNext(pos, src.Length))
                    {
                        nextEndState = null;
                        nextPos = nextEnd = src.Length;
                        return false;
                    }

                    return true;
                }

                //the start positions between pos and m_nextScanStart are unchecked.
                //See if there's a match in there.  If not, leave the next* fields alone
                //No need to scan forward into the part we've already scanned
                ScanForNext(pos, nextScanStart);
                nextScanStart = pos;
                return (nextEndState != null);
            }

            private bool ScanForNext(int start, int end)
            {
                if (start < matchMaskPos)
                {
                    start = matchMaskPos;
                }

                //switch from string positions to mask array bit positions
                start -= matchMaskPos;
                end -= matchMaskPos;
                while (start < end)
                {
                    var wi = start >> 5;
                    if (wi >= matchMask.Length)
                    {
                        return false;
                    }

                    //all bits with positions >= start&31
                    var mask = -1 << (start & 31);
                    mask &= matchMask[wi]; //only ones with bits set
                    if (mask == 0)
                    {
                        start = (start | 31) + 1; //next start position is after the current word
                        continue;
                    }

                    //move start position up to next bit set
                    start = (wi << 5) + BitUtils.LowBitIndex(mask);

                    //get corresponding string position and find the _shortest_ match
                    //(it will be expanded to the longest match when next() is called)
                    var tryPos = start + matchMaskPos;
                    var len = src.Length;
                    var st = matcher;
                    for (var pos = tryPos; pos < len; ++pos)
                    {
                        st = st.GetNextState(src[pos]);
                        if (st == null)
                        {
                            break;
                        }

                        if (st.IsAccepting)
                        {
                            //found one!
                            nextPos = tryPos;
                            nextEnd = pos + 1;
                            nextEndState = st;
                            return true;
                        }
                    }

                    //missed (shouldn't happen if the reverse finder is accurate)
                    ++start;
                }

                return false;
            }

            private bool IsValid()
            {
                return prevAccepting;
            }
        }

        private class NoMatchEnumerator : IStringMatchEnumerator<TResult>
        {
            public void Dispose()
            {
                // empty implementation
            }

            public void Reset()
            {
                // empty implementation
            }

            object IEnumerator.Current => Current;

            public TResult Current => default;

            public bool MoveNext() => false;

            public int MatchStartPosition => throw new InvalidOperationException();

            public int MatchEndPosition => throw new InvalidOperationException();

            public string MatchValue => throw new InvalidOperationException();

            public TResult MatchResult => throw new InvalidOperationException();

            public bool Reposition(int pos)
            {
                return false;
            }
        }
    }
}
