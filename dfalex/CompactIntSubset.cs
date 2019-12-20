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
using System.Text;

namespace CodeHive.DfaLex
{
    /// <summary>
    /// A subset of the integers in [0, range), with compact representation, and add+remove in amortized constant(ish) time
    /// </summary>
    internal class CompactIntSubset
    {
        private readonly int   range; //the set contains integers in [0, m_range)
        private readonly int[] bitmask; //bit mask of set members
        private readonly int[] usemarks; //indexes of non-zero words in m_bitmask. may contain duplicates
        private          int   size; //number of integers in the set == number non-zero bits in m_bitmask
        private          int   marksize; //number of marks in m_usemarks
        private          bool  sorted = true; //true if m_usemarks is sorted and deduped

        public CompactIntSubset(int range)
        {
            this.range = range;
            bitmask = new int[(range + 31) >> 5];
            usemarks = new int[bitmask.Length * 2];
        }

        public int Range => range;

        public int Size => size;

        public void Clear()
        {
            if (marksize > bitmask.Length >> 1)
            {
                for (var i = 0; i < bitmask.Length; ++i)
                {
                    bitmask[i] = 0;
                }
            }
            else
            {
                for (var i = 0; i < marksize; ++i)
                {
                    bitmask[usemarks[i]] = 0;
                }
            }

            marksize = 0;
            size = 0;
            sorted = true;
        }

        public bool Add(int val)
        {
            var bit = 1 << (val & 31);
            var index = (int) ((uint) val >> 5);
            var v = bitmask[index];
            if ((v & bit) != 0)
            {
                return false;
            }

            bitmask[index] = v | bit;
            ++size;
            if (v == 0)
            {
                if (marksize < usemarks.Length)
                {
                    usemarks[marksize++] = index;
                    sorted = false;
                }
                else
                {
                    RegenerateMarks();
                }
            }

            return true;
        }

        public bool Remove(int val)
        {
            var bit = 1 << (val & 31);
            var index = (int) ((uint) val >> 5);
            var v = bitmask[index];
            if ((v & bit) == 0)
            {
                return false;
            }

            bitmask[index] = v & ~bit;
            sorted = false;
            --size;
            return true;
        }

        public void DumpInOrder(Action<int> target)
        {
            SortMarks();
            for (var i = 0; i < marksize; ++i)
            {
                var wordIndex = usemarks[i];
                var bits = bitmask[wordIndex];
                while (bits != 0)
                {
                    target((wordIndex << 5) + BitUtils.LowBitIndex(bits));
                    bits = BitUtils.TurnOffLowBit(bits);
                }
            }
        }

        //for the debugger -- inefficient, but doesn't modify anything
        public override string ToString()
        {
            var first = true;
            var sb = new StringBuilder();
            sb.Append("[");
            for (var wordIndex = 0; wordIndex < bitmask.Length; ++wordIndex)
            {
                var bits = bitmask[wordIndex];
                while (bits != 0)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(",");
                    }

                    sb.Append((wordIndex << 5) + BitUtils.LowBitIndex(bits));
                    bits = BitUtils.TurnOffLowBit(bits);
                }
            }

            sb.Append("]");
            return sb.ToString();
        }


        private void RegenerateMarks()
        {
            marksize = 0;
            for (var i = 0; i < bitmask.Length; ++i)
            {
                if (bitmask[i] != 0)
                {
                    usemarks[marksize++] = i;
                }
            }

            sorted = true;
        }

        private void SortMarks()
        {
            if (sorted)
            {
                return;
            }

            if (size >= bitmask.Length >> 3)
            {
                RegenerateMarks();
                return;
            }

            var newsize = 0;
            for (var i = 0; i < marksize; ++i)
            {
                usemarks[marksize + usemarks[i]] = 0;
            }

            for (var i = 0; i < marksize; ++i)
            {
                var v = usemarks[i];
                if (bitmask[v] != 0 && usemarks[marksize + v] == 0)
                {
                    usemarks[marksize + v] = 1;
                    usemarks[newsize++] = v;
                }
            }

            marksize = newsize;
            Array.Sort(usemarks, 0, marksize);
            sorted = true;
        }
    }
}
