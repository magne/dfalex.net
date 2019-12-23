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
    /// <summary>
    /// An object to which <see cref="char"/>s and <see cref="string"/>s can be appended.
    /// </summary>
    public interface IAppendable
    {
        /// <summary>
        /// Appends the specified character to this IAppendable.
        /// </summary>
        /// <param name="c"></param>
        /// <returns>A reference to this Appendable</returns>
        IAppendable Append(char c);

        /// <summary>
        /// Appends the specified string to this IAppendable.
        /// </summary>
        /// <param name="csq">The character sequence to append. If <paramref name="csq"/> is null, then the four characters
        /// "null" are appended to this Appendable.</param>
        /// <returns>A reference to this Appendable</returns>
        IAppendable Append(string csq);

        /// <summary>
        /// Appends a subsequence of the specified string to this IAppendable.
        /// </summary>
        /// <param name="csq">The string from which a subsequence will be appended. If <paramref name="csq"/> is null,
        /// then characters will be appended as if csq contained the four characters "null".</param>
        /// <param name="start">The index of the first character in the subsequence</param>
        /// <param name="end">The index of the character following the last character in the subsequence</param>
        /// <returns>A reference to this Appendable</returns>
        IAppendable Append(string csq, int start, int end);
    }
}
