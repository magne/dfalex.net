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
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;

namespace CodeHive.DfaLex
{
    /// <summary>
    /// Builds deterministic finite automata (google phrase) or DFAs that find patterns in strings.
    ///
    /// Given a set of patterns and the desired result of matching each pattern, you can produce a DFA that will
    /// simultaneously match a sequence of characters against all of those patterns.
    ///
    /// You can also build DFAs for multiple sets of patterns simultaneously. The resulting DFAs will be optimized to
    /// share states wherever possible.
    ///
    /// When you build a DFA to match a set of patterns, you get a "start state" (a <see cref="DfaState{TResult}"/>)
    /// for that pattern set. Each character of a string can be passed in turn to <see cref="DfaState{TResult}.GetNextState"/>,
    /// which will return a new <see cref="DfaState{TResult}"/>.
    ///
    /// <see cref="DfaState{TResult}.Match"/> can be called at any time to get the TResult (if any) for the patterns that
    /// match the characters processed so far.
    ///
    /// A <see cref="DfaState{TResult}"/> can be used with a <see cref="StringMatcher{TResult}"/> to find instances of patterns
    /// in strings, or with other pattern-matching classes.
    ///
    /// NOTE that building a Dfa is a complex procedure.  You should typically do it only once for each pattern set
    /// you want to use.  Usually you would do this in a static initializer.
    ///
    /// You can provide a cache that can remember and recall built DFAs, which allows you to build DFAs during your
    /// build process in various ways, instead of building them at runtime.  Or you can use the cache to store built
    /// DFAs on the first run of your program so they don't need to be built the next time...  But this is usually
    /// unnecessary, since building DFAs is more than fast enough to do during runtime initialization.
    /// </summary>
    /// <typeparam name="TResult">The type of result to produce by matching a pattern.</typeparam>
    public class DfaBuilder<TResult>
    {
        //dfa types for cache keys
        private const int DfaTypeMatcher = 0;
        private const int DfaTypeReverseFinder = 1;

        private readonly IBuilderCache                         cache;
        private readonly Dictionary<TResult, List<IMatchable>> patterns = new Dictionary<TResult, List<IMatchable>>();

        /// <summary>
        /// Create a new DfaBuilder without a <see cref="IBuilderCache"/>.
        /// </summary>
        public DfaBuilder()
        { }

        /// <summary>
        /// Create a new DfaBuilder, with a builder cache to bypass recalculation of pre-built DFAs.
        /// </summary>
        /// <param name="cache">The IBuilderCache to use</param>
        public DfaBuilder(IBuilderCache cache)
        {
            this.cache = cache;
        }

        /// <summary>
        /// Reset this DFA builder by forgetting all the patterns that have been added.
        /// </summary>
        public void Clear()
        {
            patterns.Clear();
        }

        public void AddPattern(IMatchable pattern, TResult accept)
        {
            if (!patterns.TryGetValue(accept, out var list))
            {
                list = new List<IMatchable>();
                patterns.Add(accept, list);
            }

            list.Add(pattern);
        }

        /// <summary>
        /// Build DFA for a single language.
        ///
        /// The resulting DFA matches ALL patterns that have been added to this builder.
        /// </summary>
        /// <param name="ambiguityResolver">When patterns for multiple results match the same string, this is called to
        /// combine the multiple results into one. If this is null, then a <see cref="DfaAmbiguityException{TResult}"/>
        /// will be thrown in that case.</param>
        /// <returns>The start state for a DFA that matches the set of patterns in language</returns>
        public DfaState<TResult> Build(DfaAmbiguityResolver<TResult> ambiguityResolver)
        {
            return Build(new List<ISet<TResult>> {new HashSet<TResult>(patterns.Keys)}, ambiguityResolver)[0];
        }

        /// <summary>
        /// Build DFA for a single language.
        ///
        /// The language is specified as a subset of available <typeparamref name="TResult"/>s, and will include
        /// patterns for each result in its set.
        /// </summary>
        /// <param name="language">Set defining the language to build</param>
        /// <param name="ambiguityResolver">When patterns for multiple results match the same string, this is called to
        /// combine the multiple results into one.  If this is null, then a DfaAmbiguityException will be thrown in that
        /// case.</param>
        /// <returns>The start state for a DFA that matches the set of patterns in language</returns>
        public DfaState<TResult> Build(ISet<TResult> language, DfaAmbiguityResolver<TResult> ambiguityResolver)
        {
            return Build(new List<ISet<TResult>> {language}, ambiguityResolver)[0];
        }

        /// <summary>
        /// Build DFAs for multiple languages simultaneously.
        ///
        /// Each language is specified as a subset of available <typeparamref name="TResult"/>s, and will include
        /// patterns for each result in its set.
        ///
        /// Languages built simultaneously will be globally minimized and will share as many states as possible.
        /// </summary>
        /// <param name="languages">Sets defining the languages to build</param>
        /// <param name="ambiguityResolver">When patterns for multiple results match the same string, this is called to
        /// combine the multiple results into one.	If this is null, then a DfaAmbiguityException will be thrown in that
        /// case.</param>
        /// <returns>Start states for DFAs that match the given languages.  This will have the same length as languages,
        /// with corresponding start states in corresponding positions.</returns>
        public IList<DfaState<TResult>> Build(IList<ISet<TResult>> languages, DfaAmbiguityResolver<TResult> ambiguityResolver)
        {
            if (languages.Count < 1)
            {
                return new List<DfaState<TResult>>();
            }

            SerializableDfa<TResult> serializableDfa;
            if (cache == null)
            {
                serializableDfa = _build(languages, ambiguityResolver);
            }
            else
            {
                var cacheKey = GetCacheKey(DfaTypeMatcher, languages, ambiguityResolver);
                serializableDfa = (SerializableDfa<TResult>) cache.GetCachedItem(cacheKey);
                if (serializableDfa == null)
                {
                    serializableDfa = _build(languages, ambiguityResolver);
                    cache.MaybeCacheItem(cacheKey, serializableDfa);
                }
            }

            return serializableDfa.GetStartStates();
        }

        /// <summary>
        /// Build the reverse finder DFA for all patterns that have been added to this builder.
        ///
        /// The "reverse finder DFA" for a set of patterns is applied to a string backwards from the end, and will
        /// produce a <c>true</c> result at every position where a non-empty string match for one of the
        /// patterns starts. At other positions it will produce null result.
        ///
        /// For searching through an entire string, using a reverse finder with <see cref="StringSearcher{TResult}"/> is faster
        /// than matching with just the DFA for the language, especially for strings that have no matches.
        /// </summary>
        /// <returns>The start state for the reverse finder DFA</returns>
        public DfaState<bool> BuildReverseFinder()
        {
            return BuildReverseFinders(new List<ISet<TResult>> {new HashSet<TResult>(patterns.Keys)})[0];
        }

        /// <summary>
        /// Build the reverse finder DFA for a language.
        ///
        /// The language is specified as a subset of available <typeparamref name="TResult"/>s, and will include patterns
        /// for each result in its set.
        ///
        /// The "reverse finder DFA" for a language is applied to a string backwards from the end, and will produce a
        /// <c>true</c> result at every position where a non-empty string in the language starts. At other positions it
        /// will produce null result.
        ///
        /// For searching through an entire string, using a reverse finder with <see cref="StringSearcher{TResult}"/> is faster
        /// than matching with just the DFA for the language, especially for strings that have no matches.
        /// </summary>
        /// <param name="language">set defining the languages to build</param>
        /// <returns>The start state for the reverse finder DFA</returns>
        public DfaState<bool> BuildReverseFinder(ISet<TResult> language)
        {
            return BuildReverseFinders(new List<ISet<TResult>> {language})[0];
        }

        /// <summary>
        ///Build reverse finder DFAs for multiple languages simultaneously.
        ///
        /// Each language is specified as a subset of available MATCHRESULTs, and will include patterns for each result
        /// in its set.
        ///
        /// The "reverse finder DFA" for a language is applied to a string backwards from the end, and will produce a
        /// <c>true</c> result at every position where a non-empty string in the language starts. At other positions it
        /// will produce null result.
        ///
        /// For searching through an entire string, using a reverse finder with <see cref="StringSearcher{TResult}"/> is faster
        /// than matching with just the DFA for the language, especially for strings that have no matches.
        /// </summary>
        /// <param name="languages">sets defining the languages to build</param>
        /// <returns>Start states for reverse finders for the given languages.  This will have the same length as
        /// languages, with corresponding start states in corresponding positions.</returns>
        public IList<DfaState<bool>> BuildReverseFinders(IList<ISet<TResult>> languages)
        {
            if (languages.Count == 0)
            {
                return new List<DfaState<bool>>();
            }

            SerializableDfa<bool> serializableDfa;
            if (cache == null)
            {
                serializableDfa = _buildReverseFinders(languages);
            }
            else
            {
                var cacheKey = GetCacheKey(DfaTypeReverseFinder, languages, null);
                serializableDfa = (SerializableDfa<bool>) cache.GetCachedItem(cacheKey);
                if (serializableDfa == null)
                {
                    serializableDfa = _buildReverseFinders(languages);
                    cache.MaybeCacheItem(cacheKey, serializableDfa);
                }
            }

            return serializableDfa.GetStartStates();
        }

        /// <summary>
        /// Build a <see cref="StringSearcher{TResult}"/> for all the patterns that have been added to this builder
        /// </summary>
        /// <param name="ambiguityResolver">When patterns for multiple results match the same string, this is called to
        /// combine the multiple results into one.  If this is null, then a <see cref="DfaAmbiguityException{TResult}"/>
        /// will be thrown in that case.</param>
        /// <returns>A <see cref="StringSearcher{TResult}"/> for all the patterns in this builder</returns>
        public StringSearcher<TResult> BuildStringSearcher(DfaAmbiguityResolver<TResult> ambiguityResolver)
        {
            return new StringSearcher<TResult>(Build(ambiguityResolver), BuildReverseFinder());
        }

        /// <summary>
        /// Build DFAs from a provided NFA
        ///
        /// This method is used when you want to build the NFA yourself instead of letting this class do it.
        ///
        /// Languages built simultaneously will be globally minimized and will share as many states as possible.
        /// </summary>
        /// <param name="nfa">The NFA</param>
        /// <param name="nfaStartStates">The return value will include the DFA states corresponding to these NFA states, in the same order</param>
        /// <param name="ambiguityResolver">When patterns for multiple results match the same string, this is called to
        /// combine the multiple results into one.  If this is null, then a DfaAmbiguityException will be thrown in that case.</param>
        /// <param name="cache">If this cache is non-null, it will be checked for a memoized result for this NFA, and will be populated
        /// with a memoized result when the call is complete.</param>
        /// <returns>DFA start states that are equivalent to the given NFA start states.  This will have the same length as nfaStartStates, with
        /// corresponding start states in corresponding positions.</returns>
        public static IList<DfaState<TResult>> BuildFromNfa(Nfa<TResult> nfa, int[] nfaStartStates, DfaAmbiguityResolver<TResult> ambiguityResolver,
            IBuilderCache cache)
        {
            string cacheKey = null;
            SerializableDfa<TResult> serializableDfa = null;
            if (cache != null)
            {
                var hashAlg = new SHA256Managed();
                using (var ms = new MemoryStream())
                {
                    using var cs = new CryptoStream(ms, hashAlg, CryptoStreamMode.Write);
                    var bf = new BinaryFormatter();
                    bf.Serialize(ms, nfaStartStates);
                    bf.Serialize(ms, nfa);
                    bf.Serialize(ms, ambiguityResolver);
                    ms.Flush();
                    cs.FlushFinalBlock();

                    cacheKey = Base32.GetDigest(hashAlg.Hash);
                }

                serializableDfa = (SerializableDfa<TResult>) cache.GetCachedItem(cacheKey);
            }

            if (serializableDfa == null)
            {
                var rawDfa = new DfaFromNfa<TResult>(nfa, nfaStartStates, ambiguityResolver).GetDfa();
                var minimalDfa = new DfaMinimizer<TResult>(rawDfa).GetMinimizedDfa();
                serializableDfa = new SerializableDfa<TResult>(minimalDfa);
                if (cacheKey != null)
                {
                    cache.MaybeCacheItem(cacheKey, serializableDfa);
                }
            }

            return serializableDfa.GetStartStates();
        }

        private string GetCacheKey(int dfaType, IList<ISet<TResult>> languages, DfaAmbiguityResolver<TResult> ambiguityResolver)
        {
            string cacheKey;
            var hashAlg = new SHA256Managed();
            using (var ms = new MemoryStream())
            {
                using var cs = new CryptoStream(ms, hashAlg, CryptoStreamMode.Write);
                var bf = new BinaryFormatter();
                bf.Serialize(ms, dfaType);
                var numLangs = languages.Count;
                bf.Serialize(ms, numLangs);

                //write key stuff out in an order based on our LinkedHashMap, for deterministic serialization
                foreach (var patEntry in patterns)
                {
                    var included = false;
                    var patList = patEntry.Value;
                    if (patList.Count == 0)
                    {
                        continue;
                    }

                    for (var i = 0; i < numLangs; ++i)
                    {
                        if (!languages[i].Contains(patEntry.Key))
                        {
                            continue;
                        }

                        included = true;
                        break;
                    }

                    if (!included)
                    {
                        continue;
                    }

                    bf.Serialize(ms, patList.Count);
                    if (numLangs > 1)
                    {
                        var bits = languages[0].Contains(patEntry.Key) ? 1 : 0;
                        for (var i = 1; i < languages.Count; ++i)
                        {
                            if ((i & 31) == 0)
                            {
                                bf.Serialize(ms, bits);
                                bits = 0;
                            }

                            if (languages[i].Contains(patEntry.Key))
                            {
                                bits |= 1 << (i & 31);
                            }
                        }

                        bf.Serialize(ms, bits);
                    }

                    foreach (var pat in patList)
                    {
                        bf.Serialize(ms, pat);
                    }

                    bf.Serialize(ms, patEntry.Key);
                }

                bf.Serialize(ms, 0); //0-size pattern list terminates pattern map
                bf.Serialize(ms, ambiguityResolver ?? (object) 0);
                ms.Flush();
                cs.FlushFinalBlock();

                cacheKey = Base32.GetDigest(hashAlg.Hash);
            }

            return cacheKey;
        }

        private SerializableDfa<TResult> _build(IList<ISet<TResult>> languages, DfaAmbiguityResolver<TResult> ambiguityResolver)
        {
            var nfa = Nfa<TResult>.GetBuilder();

            var nfaStartStates = new int[languages.Count];
            for (var i = 0; i < languages.Count; ++i)
            {
                nfaStartStates[i] = nfa.AddState();
            }

            ambiguityResolver ??= DefaultAmbiguityResolver;

            foreach (var patEntry in patterns)
            {
                var patList = patEntry.Value;
                if (patList == null || patList.Count < 1)
                {
                    continue;
                }

                var matchState = -1; //start state for matching this token
                for (var i = 0; i < languages.Count; ++i)
                {
                    if (!languages[i].Contains(patEntry.Key))
                    {
                        continue;
                    }

                    if (matchState < 0)
                    {
                        var acceptState = nfa.AddState(patEntry.Key); //final state accepting this token
                        if (patList.Count > 1)
                        {
                            //we have multiple patterns.  Make a union
                            matchState = nfa.AddState();
                            foreach (var pat in patList)
                            {
                                var endState = pat.AddToNfaF(nfa, matchState, CaptureGroup.NoGroup);
                                nfa.AddEpsilon(endState, acceptState, NfaTransitionPriority.Normal, Tag.None);
                            }
                        }
                        else
                        {
                            //only one pattern no union necessary
                            matchState = nfa.AddState();
                            var endState = patList[0].AddToNfaF(nfa, matchState, CaptureGroup.NoGroup);
                            nfa.AddEpsilon(endState, acceptState, NfaTransitionPriority.Normal, Tag.None);
                        }
                    }

                    //language i matches these patterns
                    nfa.AddEpsilon(nfaStartStates[i], matchState, NfaTransitionPriority.Normal, Tag.None);
                }
            }

            var rawDfa = new DfaFromNfa<TResult>(nfa.Build(), nfaStartStates, ambiguityResolver).GetDfa();
            var minimalDfa = new DfaMinimizer<TResult>(rawDfa).GetMinimizedDfa();
            var serializableDfa = new SerializableDfa<TResult>(minimalDfa);
            return serializableDfa;
        }

        private SerializableDfa<bool> _buildReverseFinders(IList<ISet<TResult>> languages)
        {
            var nfa = Nfa<bool>.GetBuilder();

            var startState = nfa.AddState();
            var endState = nfa.AddState(true);
            DfaAmbiguityResolver<bool> ambiguityResolver = DefaultAmbiguityResolver;

            //First, make an NFA that matches the reverse of all the patterns
            foreach (var patEntry in patterns)
            {
                var patList = patEntry.Value;
                if (patList == null || patList.Count < 1)
                {
                    continue;
                }

                foreach (var language in languages)
                {
                    if (!language.Contains(patEntry.Key))
                    {
                        continue;
                    }

                    foreach (var pat in patEntry.Value)
                    {
                        var st = pat.Reversed.AddToNfaF(nfa, startState, CaptureGroup.NoGroup);
                        nfa.AddEpsilon(st, endState, NfaTransitionPriority.Normal, Tag.None);
                    }
                }
            }

            //omit the empty string
            startState = nfa.Disemptify(startState);

            //allow anything first
            var beginState = nfa.AddState();
            nfa.AddEpsilon(Pattern.MaybeRepeat(CharRange.All).AddToNfaF(nfa, beginState, CaptureGroup.NoGroup), startState, NfaTransitionPriority.Normal, Tag.None);

            //build the DFA
            var rawDfa = new DfaFromNfa<bool>(nfa.Build(), new[] { beginState }, ambiguityResolver).GetDfa();
            var minimalDfa = new DfaMinimizer<bool>(rawDfa).GetMinimizedDfa();
            var serializableDfa = new SerializableDfa<bool>(minimalDfa);
            return serializableDfa;
        }

        private static T DefaultAmbiguityResolver<T>(IEnumerable<T> matches)
        {
            throw new DfaAmbiguityException<T>(matches);
        }
    }

    internal static class Base32
    {
        private static readonly char[] Digits36 = "0123456789abcdefghijklmnopqrstuvwxyz".ToCharArray();

        internal static string GetDigest(IEnumerable<byte> messageDigest)
        {
            var sb = new StringBuilder();
            var bits = 0;
            var nbits = 0;
            foreach (var b in messageDigest)
            {
                bits |= (b & 255) << nbits;
                nbits += 8;
                while (nbits >= 5)
                {
                    sb.Append(Digits36[bits & 31]);
                    bits = (int) ((uint) bits >> 5);
                    nbits -= 5;
                }
            }

            return sb.ToString();
        }
    }
}
