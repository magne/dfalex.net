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

namespace CodeHive.DfaLex
{
    public static class StringReplacements
    {
        /// <summary>
        /// Replacement that leaves the matching substring unmodified
        /// </summary>
        public static readonly StringReplacement Ignore = (dest, src, startPos, endPos) =>
        {
            dest.Append(src, startPos, endPos);
            return 0;
        };

        /// <summary>
        /// Replacement that deletes the matching substring
        /// </summary>
        public static readonly StringReplacement Delete = (dest, src, startPos, endPos) => 0;

        /// <summary>
        /// Replacement that converts the matching substring to upper case
        /// </summary>
        public static readonly StringReplacement ToUpper = (dest, src, startPos, endPos) =>
        {
            for (var i = startPos; i < endPos; ++i)
            {
                dest.Append(char.ToUpperInvariant(src[i]));
            }

            return 0;
        };

        /// <summary>
        /// Replacement that converts the matching substring to lower case
        /// </summary>
        public static readonly StringReplacement ToLower = (dest, src, startPos, endPos) =>
        {
            for (var i = startPos; i < endPos; ++i)
            {
                dest.Append(char.ToLowerInvariant(src[i]));
            }

            return 0;
        };

        /// <summary>
        /// Replacement that converts the matching substring to a single space (if it does not contain any newlines) or a
        /// newline (if it does contain a newline)
        /// </summary>
        public static readonly StringReplacement SpaceOrNewline = (dest, src, startPos, endPos) =>
        {
            for (var i = startPos; i < endPos; ++i)
            {
                if (src[i] == '\n')
                {
                    dest.Append('\n');
                    return 0;
                }
            }

            dest.Append(' ');
            return 0;
        };

        /// <summary>
        /// Make a replacement that replaces matching substrings with a given string
        /// </summary>
        /// <param name="str">replacement string</param>
        /// <returns>new StringReplacement</returns>
        public static StringReplacement String(string str)
        {
            return (dest, src, startPos, endPos) =>
            {
                dest.Append(str);
                return 0;
            };
        }

        /// <summary>
        /// Make a replacement that surrounds matches with a given prefix and suffix, and applies the given replacer
        /// to the match itself
        /// </summary>
        /// <param name="prefix">to put before matches</param>
        /// <param name="replacement">for the match itself</param>
        /// <param name="suffix">suffix to put after matches</param>
        /// <returns>new StringReplacement</returns>
        public static StringReplacement Surround(string prefix, StringReplacement replacement, string suffix)
        {
            return (dest, src, startPos, endPos) =>
            {
                dest.Append(prefix);
                var ret = replacement(dest, src, startPos, endPos);
                dest.Append(suffix);
                return ret;
            };
        }
    }
}
