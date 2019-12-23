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
    /// A CharRange is a <see cref="Pattern"/> that matches a single character from some set or range of characters. In
    /// regular expressions, such a pattern is written with [ ...stuff... ]
    ///
    /// Several commonly used ranges are provided as constants (e.g., <see cref="Digits"/>) and the <see cref="Builder"/>
    /// class can be used to construct simple and complex ranges.
    /// </summary>
    [Serializable]
    public class CharRange : IMatchable
    {
        private static readonly char[] NoChars = new char[0];

        /// <summary>
        /// A <see cref="CharRange"/> that matches any single character.
        /// </summary>
        public static readonly CharRange All = new CharRange(char.MinValue, char.MaxValue);

        /// <summary>
        /// A <see cref="CharRange"/> that matches no characters. It's not very useful, but how could I have ALL
        /// without NONE?
        /// </summary>
        public static readonly CharRange None = new CharRange(NoChars);

        /// <summary>
        /// A <see cref="CharRange"/> that matches any decimal digit (0-9).
        /// </summary>
        public static readonly CharRange Digits = new CharRange('0', '9');

        /// <summary>
        /// A <see cref="CharRange"/> that matches any octal digit (0-7).
        /// </summary>
        public static readonly CharRange OctalDigits = new CharRange('0', '7');

        /// <summary>
        /// A <see cref="CharRange"/> that matches any hexadecimal digit (0-9, A-F, and a-f).
        /// </summary>
        public static readonly CharRange HexDigits = CreateBuilder().AddRange('0', '9').AddRange('A', 'F').AddRange('a', 'f').Build();

        // characters in here are in value order and are unique
        // a character c is in this CharRange iff m_bounds contains an ODD number of
        // characters <= c
        private readonly char[] bounds;

        private CharRange(char[] bounds)
        {
            this.bounds = bounds;
        }

        public CharRange(char first, char last)
        {
            if (last < first)
            {
                Swap(ref first, ref last);
            }

            if (last >= char.MaxValue)
            {
                bounds = new[] {first};
            }
            else
            {
                bounds = new[] {first, (char) (last + 1)};
            }
        }

        /// <summary>
        /// Check whether or not this range contains a character ch
        /// </summary>
        /// <param name="ch">character to test</param>
        /// <returns>true iff this CharRange contains ch</returns>
        public bool Contains(char ch)
        {
            var lo = 0;
            var hi = bounds.Length;
            while (hi > lo)
            {
                var test = lo + ((hi - lo) >> 1);
                if (bounds[test] <= ch)
                {
                    lo = test + 1;
                }
                else
                {
                    hi = test;
                }
            }

            return (lo & 1) != 0;
        }

        public CharRange Complement()
        {
            if (bounds.Length == 0)
            {
                return All;
            }

            if (bounds[0] == '\u0000')
            {
                // range includes 0
                if (bounds.Length == 1)
                {
                    return None; // this == All
                }

                var ar = new char[bounds.Length - 1];
                Array.Copy(bounds, 1, ar, 0, ar.Length);
                return new CharRange(ar);
            }
            else
            {
                var ar = new char[bounds.Length + 1];
                Array.Copy(bounds, 0, ar, 1, bounds.Length);
                ar[0] = '\u0000';
                return new CharRange(ar);
            }
        }

        public int AddToNfa<TResult>(Nfa<TResult> nfa, int targetState)
        {
            var startState = nfa.AddState();
            for (var i = 0; i < bounds.Length; i += 2)
            {
                var first = bounds[i];
                char last;
                if (i + 1 < bounds.Length)
                {
                    last = (char) (bounds[i + 1] - 1);
                }
                else
                {
                    last = char.MaxValue;
                }

                nfa.AddTransition(startState, targetState, first, last);
            }

            return startState;
        }

        public bool MatchesEmpty => false;

        public bool MatchesNonEmpty => bounds.Length > 0;

        public bool MatchesSomething => bounds.Length > 0;

        public bool IsUnbounded => false;

        public IMatchable Reversed => this;

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            if (obj is CharRange cr && bounds.Length == cr.bounds.Length)
            {
                for (var i = 0; i < bounds.Length; ++i)
                {
                    if (bounds[i] != cr.bounds[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            //FNV-1a, except we xor in chars instead of bytes
            var hash = unchecked((int) 2166136261L);
            foreach (var c in bounds)
            {
                hash = (hash ^ c) * 16777619;
            }

            return hash;
        }

        /// <summary>
        /// Create a pattern that matches a single character.
        /// </summary>
        /// <param name="ch">character to match</param>
        /// <returns>the pattern that matches the character</returns>
        public static CharRange Single(char ch)
        {
            return new CharRange(ch, ch);
        }

        /// <summary>
        /// Create a pattern that matches all single characters with a range of values.
        ///
        /// The pattern will match a character ch if from &lt;= c &lt;= to.
        /// </summary>
        /// <param name="from">inclusive lower bound</param>
        /// <param name="to">inclusive upper bound</param>
        /// <returns>the pattern that matches the range</returns>
        public static CharRange Range(char from, char to)
        {
            return new CharRange(from, to);
        }

        /// <summary>
        /// Create a CharRange that matches any of the characters in the given string.
        /// </summary>
        /// <param name="chars">characters to match</param>
        /// <returns>the new CharRange, or <see cref="None"/> if that's appropriate</returns>
        public static CharRange AnyOf(string chars)
        {
            if (string.IsNullOrEmpty(chars))
            {
                return None;
            }

            return CreateBuilder().AddChars(chars).Build();
        }

        /// <summary>
        /// Create a CharRange that matches any characters EXCEPT the characters in the given string.
        /// </summary>
        /// <param name="chars">characters to exclude</param>
        /// <returns>the new CharRange, or All if that's appropriate</returns>
        public static CharRange NotAnyOf(string chars)
        {
            if (string.IsNullOrEmpty(chars))
            {
                return All;
            }

            return CreateBuilder().AddChars(chars).Invert().Build();
        }

        /// <summary>
        /// Create a new <see cref="CharRange.Builder"/>
        /// </summary>
        /// <returns>new Builder</returns>
        public static Builder CreateBuilder()
        {
            return new Builder();
        }

        private static void Swap(ref char first, ref char last)
        {
            var tmp = last;
            last = first;
            first = tmp;
        }

        /// <summary>
        /// Instances of this class are used to incrementally build <see cref="CharRange"/>s.
        ///
        /// Initially it contains an empty set of matching characters (<see cref="CharRange.None"/>). Methods like
        /// <see cref="AddChars"/>, <see cref="AddRange(char,char)"/>, <see cref="Exclude"/>, etc. are called to add and remove
        /// characters from the set, and then <see cref="Build"/> is called to produce an immutable <see cref="CharRange"/>
        /// object that matches characters in the set.
        /// </summary>
        public class Builder
        {
            // Intermediate representation for a set of characters. There's an 'in' at char c when the array contains c*2.
            // There's an 'out' at char c when the array contains c*2+1. A char c is in the set then the number of ins at
            // or before c exceeds the number of outs at or before c.
            private int[] inouts;

            // number of inouts in the inout array
            private int size;

            // True if the inout array is normalized.  In normalized form, the ints are all in order, and alternate in-out-in-out
            private bool normalized;

            /// <summary>
            /// Create a new <see cref="Builder"/>.
            ///
            /// It's usually a little more conventient to use <see cref="CharRange.CreateBuilder"/>
            /// </summary>
            public Builder()
            {
                inouts = new int[8];
                normalized = true;
                size = 0;
            }

            /// <summary>
            /// Clears the current range.
            ///
            /// After this call, <see cref="Build"/> will return <see cref="CharRange.None"/>.
            /// </summary>
            /// <returns>this</returns>
            public Builder Clear()
            {
                size = 0;
                normalized = true;
                return this;
            }

            /// <summary>
            /// Add a character to the current set.
            /// </summary>
            /// <param name="ch">this character will bi added to the set</param>
            /// <returns>this</returns>
            public Builder AddChar(char ch)
            {
                AddRange(ch, ch);
                return this;
            }

            /// <summary>
            /// Add characters to the current set.
            /// </summary>
            /// <param name="chars">All characters in this string will be added to the set</param>
            /// <returns>this</returns>
            public Builder AddChars(string chars)
            {
                Reserve(chars.Length * 2);
                foreach (var ch in chars)
                {
                    AddRange(ch, ch);
                }

                return this;
            }

            /// <summary>
            /// Add a range of characters to the current set.
            ///
            /// Adds all characters x such that first &lt;= x and x &lt;= last
            /// </summary>
            /// <param name="first">least-valued character to add</param>
            /// <param name="last">greatest-valued character to add</param>
            /// <returns></returns>
            public Builder AddRange(char first, char last)
            {
                Reserve(2);
                normalized = false;
                if (first > last)
                {
                    Swap(ref first, ref last);
                }

                inouts[size++] = first << 1;
                if (last < char.MaxValue)
                {
                    inouts[size++] = (last << 1) + 3;
                }

                return this;
            }

            /// <summary>
            /// Add characters from another <see cref="CharRange"/>.
            /// </summary>
            /// <param name="cr">All characters matched by this CharRange will be added to the current set</param>
            /// <returns>this</returns>
            public Builder AddRange(CharRange cr)
            {
                Reserve(size + cr.bounds.Length);
                for (int i = 0; i < cr.bounds.Length; ++i)
                {
                    normalized = false;
                    inouts[size++] = (cr.bounds[i] << 1) | (i & 1);
                }

                return this;
            }

            /// <summary>
            /// Remove characters from another <see cref="CharRange"/>.
            ///
            /// This is implemented using <see cref="Invert"/> and <see cref="AddRange(CharRange)"/>.
            /// </summary>
            /// <param name="cr">All characters matched by this CharRange will be removed from the current set</param>
            /// <returns></returns>
            public Builder Exclude(CharRange cr)
            {
                Invert();
                AddRange(cr);
                Invert();
                return this;
            }

            /// <summary>
            /// Intersect with another <see cref="CharRange"/>.
            ///
            /// This is implemented by <see cref="Exclude"/>ing cr.Complement()
            /// </summary>
            /// <param name="cr">All characters that are NOT matched by this CharRange will be removed from the current set</param>
            /// <returns>this</returns>
            public Builder Intersect(CharRange cr)
            {
                Exclude(cr.Complement());
                return this;
            }

            /// <summary>
            /// Make the current range case independent.
            ///
            /// For every char ch in the range, car.ToUpperInvariant(ch) and char.ToLowerInvariant(ch) are added.
            /// </summary>
            /// <returns>this</returns>
            public Builder ExpandCases()
            {
                Normalize();
                var len = size;
                var src = new int[len];
                Array.Copy(inouts, src, src.Length);
                for (var i = 0; i < src.Length; i += 2)
                {
                    var s = (char) (src[i] >> 1);
                    var e = i + 1 >= len ? char.MaxValue : (char) ((src[i + 1] >> 1) - 1);
                    RangeDecaser.ExpandRange(s, e, this);
                }

                return this;
            }

            public Builder Invert()
            {
                Normalize();

                if (size <= 0)
                {
                    AddRange(All);
                    return this;
                }

                if (inouts[0] == 0)
                {
                    // current range includes 0
                    --size;
                    for (var i = 0; i < size; ++i)
                    {
                        inouts[i] = inouts[i + 1] ^ 1;
                    }
                }
                else
                {
                    // current range !includes 0
                    Reserve(1);
                    ++size;
                    for (var i = size - 1; i > 0; --i)
                    {
                        inouts[i] = inouts[i - 1] ^ 1;
                    }

                    inouts[0] = 0;
                }

                return this;
            }

            /// <summary>
            /// Produce a <see cref="CharRange"/> for the current set.
            ///
            /// This method does not alter the current set in any way -- it may be further modified and used to
            /// produce more <see cref="CharRange"/> objects.
            /// </summary>
            /// <returns></returns>
            public CharRange Build()
            {
                Normalize();
                if (size <= 0)
                {
                    return None;
                }

                if (size == 1 && inouts[0] == 0)
                {
                    return All;
                }

                var ar = new char[size];
                for (var i = 0; i < size; ++i)
                {
                    ar[i] = (char) (inouts[i] >> 1);
                }

                return new CharRange(ar);
            }

            private void Normalize()
            {
                if (size > 0 && !normalized)
                {
                    Array.Sort(inouts, 0, size);
                    var d = 0;
                    var depth = 0;
                    for (var s = 0; s < size;)
                    {
                        var olddepth = depth;
                        var inout = inouts[s++];
                        depth += (inout & 1) == 0 ? 1 : -1;
                        while (s < size && (inouts[s] >> 1) == inout >> 1)
                        {
                            depth += (inouts[s++] & 1) == 0 ? 1 : -1;
                        }

                        if (depth > 0)
                        {
                            if (olddepth <= 0)
                            {
                                inouts[d++] = inout & -1;
                            }
                        }
                        else if (olddepth > 0)
                        {
                            inouts[d++] = inout | 1;
                        }
                    }

                    size = d;
                }

                normalized = true;
            }

            // Make sure we have room to add n ints to inouts
            private void Reserve(int n)
            {
                if (inouts.Length < size + n)
                {
                    Normalize();
                    if (inouts.Length >> 1 > size + n)
                    {
                        return;
                    }

                    var na = new int[Math.Max(inouts.Length * 2, size + n)];
                    Array.Copy(inouts, na, size);
                    inouts = na;
                }
            }

            // Helper class for calculating case-independent character ranges
            // We make this a separate class to defer its initialization until the first time we use it
            private static class RangeDecaser
            {
                private static readonly char[] DeltaTable = BuildDeltaTable();

                public static void ExpandRange(char s, char e, Builder target)
                {
                    var tableSize = DeltaTable.Length >> 2;
                    var lo = 0;
                    var hi = 1;
                    // finger search to find the first range with an end >= s
                    while (DeltaTable[(hi << 2) + 1] < s)
                    {
                        // hi is too lo
                        lo = hi + 1;
                        hi <<= 1;
                        if (hi >= tableSize)
                        {
                            hi = tableSize;
                            break;
                        }
                    }

                    while (hi > lo)
                    {
                        var test = lo + ((hi - lo) >> 1);
                        if (DeltaTable[(test << 2) + 1] < s)
                        {
                            lo = test + 1;
                        }
                        else
                        {
                            hi = test;
                        }
                    }

                    for (; lo < tableSize; ++lo)
                    {
                        var subs = DeltaTable[lo << 2];
                        var sube = DeltaTable[(lo << 2) + 1];
                        if (subs > e)
                        {
                            break;
                        }

                        if (s > subs)
                        {
                            subs = s;
                        }

                        if (e < sube)
                        {
                            sube = e;
                        }

                        if (sube < subs)
                        {
                            continue;
                        }

                        var delta = DeltaTable[(lo << 2) + 2];
                        if (delta != '\0')
                        {
                            target.AddRange((char) ((subs + delta) & 65535), (char) ((sube + delta) & 65535));
                        }

                        delta = DeltaTable[(lo << 2) + 3];
                        if (delta != '\0')
                        {
                            target.AddRange((char) ((subs + delta) & 65535), (char) ((sube + delta) & 65535));
                        }
                    }
                }

                private static char[] BuildDeltaTable()
                {
                    var s = 0;
                    var dpos = 0;
                    var ar = new char[256];
                    while (s < 65535)
                    {
                        var lcd = char.ToLowerInvariant((char) s) - s;
                        var ucd = char.ToUpperInvariant((char) s) - s;
                        if (lcd == 0 && ucd == 0)
                        {
                            ++s;
                            continue;
                        }

                        var e = s + 1;
                        for (; e < 65536; ++e)
                        {
                            var lcd2 = char.ToLowerInvariant((char) e) - e;
                            var ucd2 = char.ToUpperInvariant((char) e) - e;
                            if (lcd2 != lcd || ucd2 != ucd)
                            {
                                break;
                            }
                        }

                        while (dpos + 4 > ar.Length)
                        {
                            var na = new char[ar.Length * 2];
                            Array.Copy(ar, na, ar.Length);
                            ar = na;
                        }

                        ar[dpos++] = (char) s;
                        ar[dpos++] = (char) (e - 1);
                        ar[dpos++] = (char) (lcd & 65535);
                        ar[dpos++] = (char) (ucd & 65535);
                        s = e;
                    }

                    var res = new char[dpos];
                    Array.Copy(ar, res, dpos);
                    return res;
                }
            }
        }
    }
}
