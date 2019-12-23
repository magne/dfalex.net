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
    /// For search and replace operations, a functional interface that is called to select replacement text for matches,
    /// based on the TResult.
    ///
    /// This is called by a <see cref="StringSearcher{TResult}.FindAndReplace"/> to replace instances of patterns found
    /// in a string.
    /// </summary>
    /// <param name="dest">The replacement text for the matching substring should be written here</param>
    /// <param name="mr">The TResult produced by the match</param>
    /// <param name="src">The string being searched, or a part of the stream being searched that contains the current match</param>
    /// <param name="startPos">the start index of the current match in src</param>
    /// <param name="endPos">the end index of the current match in src</param>
    /// <returns>if this is &gt;0, then it is the position in the source string at which to continue processing after
    /// replacement.  If you set this &lt;= startPos, a runtime exception will be thrown to abort the infinite loop that
    /// would result.  Almost always return 0.</returns>
    /// <typeparam name="TResult"></typeparam>
    public delegate int ReplacementSelector<in TResult>(IAppendable dest, TResult mr, string src, int startPos, int endPos);
}
