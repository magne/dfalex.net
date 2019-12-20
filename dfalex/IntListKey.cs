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
    /// A simple list of integers that can be used as a hash map key.
    /// </summary>
    internal class IntListKey
    {
        private static readonly int[] NoInts = new int[0];

        private int[] buf = NoInts;
        private int   size;
        private int   hash;
        private bool  hashValid;

        public IntListKey()
        { }

        public IntListKey(IntListKey src)
        {
            if (src != null && src.size > 0)
            {
                buf = new int[src.size];
                Array.Copy(src.buf, buf, src.size);
                size = src.size;
                if (src.hashValid)
                {
                    hash = src.hash;
                    hashValid = true;
                }
            }
        }

        public void Clear()
        {
            size = 0;
            hashValid = false;
        }

        public void Add(int v)
        {
            if (size >= buf.Length)
            {
                var tmp = new int[size + (size >> 1) + 16];
                Array.Copy(buf, tmp, buf.Length);
                buf = tmp;
            }

            buf[size++] = v;
            hashValid = false;
        }

        public void ForData(Action<int[], int> target)
        {
            target(buf, size);
        }

        public override bool Equals(object obj)
        {
            if (obj is IntListKey r)
            {
                if (size != r.size || GetHashCode() != r.GetHashCode())
                {
                    return false;
                }

                for (int i = size - 1; i >= 0; --i)
                {
                    if (buf[i] != r.buf[i])
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
            if (!hashValid)
            {
                int h = 0;
                for (int i = 0; i < size; ++i)
                {
                    h *= 65539;
                    h += buf[i];
                }

                h ^= (int) ((uint) h >> 17);
                h ^= (int) ((uint) h >> 11);
                h ^= (int) ((uint) h >> 5);
                hash = h;
                hashValid = true;
            }

            return hash;
        }
    }
}
