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
        private readonly int[] buf;
        private readonly int   hash;

        private IntListKey(int[] src)
        {
            buf = src;

            var h = 0;
            foreach (var v in buf)
            {
                h *= 65539;
                h += v;
            }

            h ^= (int) ((uint) h >> 17);
            h ^= (int) ((uint) h >> 11);
            h ^= (int) ((uint) h >> 5);
            hash = h;
        }

        public void ForData(Action<int[], int> target)
        {
            target(buf, buf.Length);
        }

        public override bool Equals(object obj)
        {
            if (obj is IntListKey r)
            {
                if (buf.Length != r.buf.Length || GetHashCode() != r.GetHashCode())
                {
                    return false;
                }

                for (var i = buf.Length - 1; i >= 0; --i)
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
            return hash;
        }

        public class Builder
        {
            private static readonly int[] Empty = new int[0];

            private int[] buf;
            private int   size;

            public Builder(int capacity = 16)
            {
                buf = new int[capacity];
            }

            public void Clear()
            {
                size = 0;
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
            }

            public IntListKey Build()
            {
                var src = Empty;
                if (size > 0)
                {
                    src = new int[size];
                    Array.Copy(buf, src, size);
                }

                return new IntListKey(src);
            }
        }
    }
}
