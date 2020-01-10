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
    /// An <see cref="IAppendable"/> for string replacements that will allocate a new string buffer only when the first difference is written.
    /// </summary>
    internal class StringReplaceAppendable : IAppendable
    {
        private readonly string src;
        private          char[] buf;
        private          int    len;

        public StringReplaceAppendable(string src)
        {
            this.src = src;
        }

        public IAppendable Append(string csq)
        {
            Append(csq, 0, csq.Length);
            return this;
        }

        public IAppendable Append(char c)
        {
            if (buf != null)
            {
                if (len >= buf.Length)
                {
                    var tempBuf = new char[buf.Length * 2];
                    Array.Copy(buf, tempBuf, buf.Length);
                    buf = tempBuf;
                }

                buf[len++] = c;
                return this;
            }

            if (len < src.Length && src[len] == c)
            {
                ++len;
                return this;
            }

            Allocate(1);
            buf[len++] = c;
            return this;
        }

        public IAppendable Append(string csq, int start, int end)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            if (end < start)
            {
                throw new ArgumentOutOfRangeException(nameof(end));
            }

            if (buf == null)
            {
                if (csq == src && start == len)
                {
                    if (end > src.Length)
                    {
                        throw new ArgumentOutOfRangeException(nameof(end));
                    }

                    len = end;
                    return this;
                }

                for (;; ++start, ++len)
                {
                    if (start >= end)
                    {
                        return this;
                    }

                    if (len >= src.Length || src[len] != csq[start])
                    {
                        break;
                    }
                }

                //new data - need to allocate
                Allocate(end - start);
            }
            else if (buf.Length - len < end - start)
            {
                var tempBuf = new char[Math.Max(buf.Length * 2, len + (end -start))];
                Array.Copy(buf, tempBuf, buf.Length);
                buf = tempBuf;
            }

            if (csq is string str)
            {
                str.CopyTo(start, buf, len, end - start);
                len += end - start;
            }
            else
            {
                while (start < end)
                {
                    buf[len++] = csq[start++];
                }
            }
            return this;
        }

        public override string ToString()
        {
            if (buf != null)
            {
                return new string(buf, 0, len);
            }

            if (len == src.Length)
            {
                return src;
            }

            return src.Substring(0, len);
        }

        private void Allocate(int addLen)
        {
            buf = new char[Math.Max(len + addLen, src.Length + 16)];
            src.CopyTo(0, buf, 0, len);
        }
    }
}
