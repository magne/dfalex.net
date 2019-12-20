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
    /// Handles translating a CompactIntSubset representing a combination of NFA states to/from a compact DFA state
    /// signature representation
    /// </summary>
    internal class DfaStateSignatureCodec
    {
        private Action<int> currentTarget;
        private int         fieldLength = 32;
        private int         fieldMask;
        private int         pendingBits;
        private int         pendingSize; // always in [1,32]
        private int         minval;

        /// <summary>
        /// To build a signature, call this first, then call <see cref="AcceptInt"/> for all the NFA states, in order,
        /// then call <see cref="Finish"/>.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="size">number of NFA states in this signature</param>
        /// <param name="range">total number of NFA states in the NFA</param>
        public void Start(Action<int> target, int size, int range)
        {
            currentTarget = target;
            fieldLength = GetCompactEncodingLengthForSize(size, range);
            if (fieldLength >= 31)
            {
                fieldMask = int.MaxValue;
                fieldLength = 31;
            }
            else
            {
                if (fieldLength < 1)
                {
                    fieldLength = 1;
                }

                fieldMask = (1 << fieldLength) - 1;
            }

            pendingBits = fieldLength - 1;
            pendingSize = 5;
            minval = 0;
        }

        public void AcceptInt(int value)
        {
            var gap = value - minval;
            if (gap < 0)
            {
                throw new ArgumentException("values negative or out of order");
            }

            minval = value + 1;
            if (fieldLength < 31)
            {
                while (gap >= fieldMask)
                {
                    _putField(fieldMask);
                    gap -= fieldMask;
                }
            }

            _putField(gap);
        }

        private void _putField(int val)
        {
            if (pendingSize >= 32)
            {
                currentTarget(pendingBits);
                pendingBits = val;
                pendingSize = fieldLength;
                return;
            }

            pendingBits |= val << pendingSize;
            if ((pendingSize += fieldLength) > 32)
            {
                currentTarget(pendingBits);
                pendingSize -= 32;
                pendingBits = (int) ((uint) val >> (fieldLength - pendingSize));
            }
        }

        public void Finish()
        {
            if (pendingSize < 32)
            {
                pendingBits |= (~0) << pendingSize;
            }

            currentTarget(pendingBits);
            currentTarget = null;
        }

        public static void Expand(IntListKey key, Action<int> target)
        {
            key.ForData((buf, len) => Expand(buf, len, target));
        }

        public static void Expand(int[] sigbuf, int siglen, Action<int> target)
        {
            if (siglen <= 0)
            {
                return;
            }

            var bits = sigbuf[0];
            var nextpos = 1;
            var fieldLen = (bits & 31) + 1;
            int fieldMask;
            if (fieldLen >= 31)
            {
                fieldMask = int.MaxValue;
            }
            else
            {
                fieldMask = (1 << fieldLen) - 1;
            }

            bits = (int) ((uint) bits >> 5);
            var bitsleft = 32 - 5;
            var minval = 0;
            for (;;)
            {
                var val = bits;
                if (bitsleft < fieldLen)
                {
                    if (nextpos >= siglen)
                    {
                        break;
                    }

                    bits = sigbuf[nextpos++];
                    val |= (bits << bitsleft);
                    bits = (int) ((uint) bits >> (fieldLen - bitsleft));
                    bitsleft += 32 - fieldLen;
                }
                else
                {
                    bits = (int) ((uint) bits >> (fieldLen));
                    bitsleft -= fieldLen;
                }

                val &= fieldMask;
                minval += val;
                if (val != fieldMask || fieldLen >= 31)
                {
                    target(minval++);
                }
            }
        }

        //Given Psym = 1_count/total_count
        //Probability that an extra word will be required with symbol length len
        //Px = (1-Psym)^(2^len-1)
        //Expected total encoding length per 1
        //BPS = len/(1-Px) = len/(1-(1-Psym)^(2^len-1))

        private static readonly int[] LengthProgression =
        {
            /* len <= 1 until size/range >= 1/ */ 3,
            /* len <= 2 until size/range >= 1/ */ 5,
            /* len <= 3 until size/range >= 1/ */ 7,
            /* len <= 4 until size/range >= 1/ */ 12,
            /* len <= 5 until size/range >= 1/ */ 20,
            /* len <= 6 until size/range >= 1/ */ 36,
            /* len <= 7 until size/range >= 1/ */ 66,
            /* len <= 8 until size/range >= 1/ */ 124,
            /* len <= 9 until size/range >= 1/ */ 234,
            /* len <= 10 until size/range >= 1/ */ 445,
            /* len <= 11 until size/range >= 1/ */ 855,
            /* len <= 12 until size/range >= 1/ */ 1649,
            /* len <= 13 until size/range >= 1/ */ 3194,
            /* len <= 14 until size/range >= 1/ */ 6209,
            /* len <= 15 until size/range >= 1/ */ 12101,
            /* len <= 16 until size/range >= 1/ */ 23638,
            /* len <= 17 until size/range >= 1/ */ 46263,
            /* len <= 18 until size/range >= 1/ */ 90696,
            /* len <= 19 until size/range >= 1/ */ 178061,
            /* len <= 20 until size/range >= 1/ */ 350024,
            /* len <= 21 until size/range >= 1/ */ 688829,
            /* len <= 22 until size/range >= 1/ */ 1356923,
            /* len <= 23 until size/range >= 1/ */ 2675371,
            /* len <= 24 until size/range >= 1/ */ 5279086,
            /* len <= 25 until size/range >= 1/ */ 10424271,
            /* len <= 26 until size/range >= 1/ */ 20597568,
            /* len <= 27 until size/range >= 1/ */ 40723415,
            /* len <= 28 until size/range >= 1/ */ 80557921,
            /* len <= 29 until size/range >= 1/ */ 159436815,
            /* len <= 30 until size/range >= 1/ */ 315695254
        };

        public static int GetCompactEncodingLengthForSize(int size, int range)
        {
            if (range < 1)
            {
                return 32;
            }

            var ratedivisor = range / size;
            var min = 0;
            var lim = LengthProgression.Length;
            while (min < lim)
            {
                var t = min + ((lim - min) >> 1);
                if (ratedivisor >= LengthProgression[t])
                {
                    min = t + 1;
                }
                else
                {
                    lim = t;
                }
            }

            return min + 1;
        }

        /*
         * This was used to calculate LENGTH_PROGRESSION
         *
        static void Main(string[] argv)
        {
            var divisor = 1;
            for (var len = 1; len <= 30; len++)
            {
                while (ExpectedBitsPerEntry(1.0 / divisor, len) <= ExpectedBitsPerEntry(1.0 / divisor, len + 1))
                {
                    divisor += (divisor >> 24) + 1;
                }

                Console.WriteLine($"/* len <= {len} until size/range >= 1/ *{string.Empty}/ {divisor},");
            }
        }

        private static double ExpectedBitsPerEntry(double rate, double len)
        {
            var pexceed = Math.Pow(1.0 - rate, Math.Pow(2.0, len) - 1);
            return len / (1.0 - pexceed);
        }
        */
    }
}
