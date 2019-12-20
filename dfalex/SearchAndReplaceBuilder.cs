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
using System.Collections.Generic;

namespace CodeHive.DfaLex
{
    /// <summary>
    /// Builds search and replace functions that finds patterns in strings and replaces them.
    ///
    /// Given a set of patterns and associated {@link StringReplacement} functions, you can produce an optimized,
    /// thread-safe Function&lt;String,String&gt; that will find all occurrences of those patterns and replace them
    /// with their replacements.
    ///
    /// The returned function is thread-safe.
    ///
    /// NOTE that building a search and replace function is a relatively complex procedure.  You should typically do it
    /// only once for each pattern set you want to use.  Usually you would do this in a static initializer.
    ///
    /// You can provide a cache that can remember and recall built functions, which allows you to build them during
    /// your build process in various ways, instead of building them at runtime.  Or you can use the cache to store
    /// built functions on the first run of your program so they don't need to be built the next time...  But this
    /// is usually unnecessary, since building them is more than fast enough to do during runtime initialization.
    /// </summary>
    internal class SearchAndReplaceBuilder
    {
        private readonly DfaBuilder<int?>        dfaBuilder;
        private readonly List<StringReplacement> replacements = new List<StringReplacement>();
        private          DfaState<int?>          dfaMemo;
        private          DfaState<bool>          reverseFinderMemo;

        /// <summary>
        /// Create a new SearchAndReplaceBuilder without a <see cref="IBuilderCache"/>
        /// </summary>
        public SearchAndReplaceBuilder()
        {
            dfaBuilder = new DfaBuilder<int?>();
        }

        /// <summary>
        /// Create a new SearchAndReplaceBuilder, with a builder cache to bypass recalculation of pre-built functions.
        /// </summary>
        /// <param name="cache">The BuilderCache to use</param>
        public SearchAndReplaceBuilder(IBuilderCache cache)
        {
            dfaBuilder = new DfaBuilder<int?>(cache);
        }

        /// <summary>
        /// Reset this builder by forgetting all the patterns that have been added
        /// </summary>
        public void Clear()
        {
            ClearMemos();
            dfaBuilder.Clear();
            replacements.Clear();
        }

        /// <summary>
        /// Add a search + string replacement.
        ///
        /// Occurrences of the search pattern will be replaced with the given string.
        ///
        /// This is equivalent to addReplacement(pat, StringReplacements.string(replacement));
        /// </summary>
        /// <param name="pat">The pattern to search for</param>
        /// <param name="replacement">A function to generate the replacement value</param>
        /// <returns>this</returns>
        public SearchAndReplaceBuilder AddStringReplacement(IMatchable pat, string replacement)
        {
            return AddReplacement(pat, StringReplacements.String(replacement));
        }

        /// <summary>
        /// Add a dynamic search + replacement.
        ///
        /// The provided replacement function will be called to generate the replacement value for each
        /// occurrence of the search pattern.
        ///
        /// <see cref="StringReplacements"/> contains commonly used replacement functions
        /// </summary>
        /// <param name="pat">The pattern to search for</param>
        /// <param name="replacement">A function to generate the replacement value</param>
        /// <returns>this</returns>
        public SearchAndReplaceBuilder AddReplacement(IMatchable pat, StringReplacement replacement)
        {
            ClearMemos();
            int result = replacements.Count;
            replacements.Add(replacement);
            dfaBuilder.AddPattern(pat, result);
            return this;
        }

        /// <summary>
        /// Add a pattern to ignore
        ///
        /// Occurrences of the search pattern will be left alone.  This just adds a replacer that replaces occurrences
        /// of the search pattern with the same string.
        ///
        /// With careful attention to match priority rules (see <see cref="BuildStringReplacer"/>}, this can be used
        /// for many special purposes.
        ///
        /// This is equivalent to addReplacement(pat, StringReplacements.IGNORE);
        /// </summary>
        /// <param name="pat">The pattern to search for</param>
        /// <returns>this</returns>
        public SearchAndReplaceBuilder AddIgnorePattern(IMatchable pat)
        {
            return AddReplacement(pat, StringReplacements.Ignore);
        }

        /// <summary>
        /// Build a search and replace function
        ///
        /// The resulting function finds all patterns in the string you give it, and replaces them all with
        /// the associated replacement.
        ///
        /// Matches are found in order of their start positions.  If matches to more than one pattern occur at the same
        /// position, then the <i>longest</i> match will be used.  If there is a tie, then the first one added to this
        /// builder will be used.
        /// </summary>
        /// <returns>The search+replace function</returns>
        public Func<string, string> BuildStringReplacer()
        {
            if (dfaMemo == null)
            {
                dfaMemo = dfaBuilder.Build(AmbiguityResolver);
            }

            if (reverseFinderMemo == null)
            {
                reverseFinderMemo = dfaBuilder.BuildReverseFinder();
            }

            var searcher = new StringSearcher<int?>(dfaMemo, reverseFinderMemo);
            var replacer = new StringSearcherReplacer(replacements).ReplacementSelector;
            return (str => searcher.FindAndReplace(str, replacer));
        }

        /// <summary>
        /// Build a search and replace function from a searcher and replacer
        /// </summary>
        /// <param name="searcher">the searcher</param>
        /// <param name="replacer">the replacer</param>
        /// <typeparam name="TResult"></typeparam>
        /// <returns>The search+replace function</returns>
        public static Func<string, string> BuildFromSearcher<TResult>(StringSearcher<TResult> searcher, ReplacementSelector<TResult> replacer)
        {
            return str => searcher.FindAndReplace(str, replacer);
        }

        private void ClearMemos()
        {
            dfaMemo = null;
            reverseFinderMemo = null;
        }

        private static int? AmbiguityResolver(ISet<int?> candidates)
        {
            int? ret = null;
            foreach (var c in candidates)
            {
                if (ret == null || c < ret)
                {
                    ret = c;
                }
            }

            return ret;
        }

        private class StringSearcherReplacer
        {
            private readonly StringReplacement[] replacements;

            public StringSearcherReplacer(List<StringReplacement> replacements)
            {
                this.replacements = replacements.ToArray();
            }

            public ReplacementSelector<int?> ReplacementSelector => (dest, mr, src, pos, endPos) => replacements[mr.Value](dest, src, pos, endPos);
        }
    }
}
