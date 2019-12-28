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

using System.Collections.Generic;

namespace CodeHive.DfaLex
{
    /// <summary>
    /// Implementations of this interface are used to resolve ambiguities in <see cref="DfaBuilder{TResult}"/>.
    ///
    /// When it's possible for a single string to match patterns that produce different results, the ambiguity resolver
    /// is called to determine what the result should be.
    ///
    /// The implementation can throw a <see cref="DfaAmbiguityException{TResult}"/> in this case, or can combine the multiple
    /// result objects into a single object if its type (e.g., EnumSet) permits.
    /// </summary>
    /// <param name="accepts">The accept results ambiguities to resolve</param>
    /// <typeparam name="TResult">The type of result to produce by matching a pattern.</typeparam>
    public delegate TResult DfaAmbiguityResolver<TResult>(ISet<TResult> accepts);
}
