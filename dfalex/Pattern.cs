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
using System.Globalization;
using System.Linq;

namespace CodeHive.DfaLex
{
    /// <summary>
    /// A pattern represents a set of strings. A string in the set is said to "match" the pattern.
    /// </summary>
    [Serializable]
    public abstract class Pattern : IMatchable
    {
        private volatile Pattern reverse;

        /// <summary>
        /// Pattern that matches only the empty string
        /// </summary>
        public static readonly Pattern Empty = new EmptyPattern();

        /// <summary>
        /// Pattern that matches all strings
        /// </summary>
        public static readonly Pattern AllStrings = MaybeRepeat(CharRange.All);

        /// <summary>
        /// Get a Pattern corresponding to a <see cref="CharRange"/> or other <see cref="IMatchable"/>.
        /// </summary>
        /// <param name="tomatch">pattern to match</param>
        /// <returns>a Pattern that matches the given <see cref="IMatchable"/></returns>
        public static Pattern Match(IMatchable tomatch)
        {
            if (tomatch is Pattern pattern)
            {
                return pattern;
            }

            return new WrapPattern(tomatch);
        }

        /// <summary>
        /// Create a pattern that exactly matches a single string, case dependent.
        /// </summary>
        /// <param name="tomatch">string to match</param>
        /// <returns>the pattern that matches the string</returns>
        public static Pattern Match(string tomatch)
        {
            return new StringPattern(tomatch);
        }

        /// <summary>
        /// Create a pattern that exactly matches a single string, case independent.
        /// </summary>
        /// <param name="tomatch"></param>
        /// <returns></returns>
        public static Pattern MatchI(string tomatch)
        {
            return new StringIPattern(tomatch);
        }

        /// <summary>
        /// Parse the given regular expression into a pattern.
        ///
        /// Syntax supported include:
        /// <list type="bullet">
        /// <item>
        /// <description>. (matches ANY character, including newlines)</description>
        /// </item>
        /// <item>
        /// <description>*, ?, +, |, ()</description>
        /// </item>
        /// <item>
        /// <description>[abc][^abc][a-zA-Z0-9], etc., character sets</description>
        /// </item>
        /// <item>
        /// <description>\t, \n, \r, \f, \a, \e, &#92;xXX, &#92;uXXXX, \cX character escapes</description>
        /// </item>
        /// <item>
        /// <description>\\, or \x, where x is any non-alphanumeric character</description>
        /// </item>
        /// <item>
        /// <description>\d, \D, \s, \S, \w, \W class escapes</description>
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="regex">regular expression string to parse</param>
        /// <param name="options"><see cref="RegexOptions"/> that modify the interpretation of <paramref name="regex"/></param>
        /// <returns>a pattern that implements the regular expression</returns>
        public static Pattern Regex(string regex, RegexOptions options = RegexOptions.None)
        {
            return Match(RegexParser.Parse(regex, options));
        }

        /// <summary>
        /// Parse the given regular expression into a pattern, case independent
        ///
        /// See <see cref="Regex"/> for syntax information.
        /// </summary>
        /// <param name="regex">regular expression string to parse</param>
        /// <param name="options"><see cref="RegexOptions"/> that modify the interpretation of <paramref name="regex"/></param>
        /// <returns>a pattern that implements the regular expression</returns>
        public static Pattern RegexI(string regex, RegexOptions options = RegexOptions.None)
        {
            return Match(RegexParser.Parse(regex, options | RegexOptions.IgnoreCase));
        }

        /// <summary>
        /// Create a pattern that matches one or more occurrences of a given pattern.
        /// </summary>
        /// <param name="pattern">given pattern</param>
        /// <returns>the new pattern</returns>
        public static Pattern Repeat(IMatchable pattern)
        {
            return new RepeatingPattern(pattern, true);
        }

        /// <summary>
        /// Create a pattern that matches one or more occurrences of a particular string, case dependent
        /// </summary>
        /// <param name="str"> the string to match</param>
        /// <returns>the new pattern</returns>
        public static Pattern Repeat(string str)
        {
            return Repeat(Match(str));
        }

        /// <summary>
        /// Create a pattern that matches one or more occurrences of a particular string, case independent
        /// </summary>
        /// <param name="str"> the string to match</param>
        /// <returns>the new pattern</returns>
        public static Pattern RepeatI(string str)
        {
            return Repeat(MatchI(str));
        }

        /// <summary>
        /// Create a pattern that lazily matches one or more occurrences of a given pattern.
        /// </summary>
        /// <param name="pattern">given pattern</param>
        /// <returns>the new pattern</returns>
        public static Pattern RepeatLazy(IMatchable pattern)
        {
            return new RepeatingPattern(pattern, true, true);
        }

        /// <summary>
        /// Create a pattern that lazily matches one or more occurrences of a particular string, case dependent
        /// </summary>
        /// <param name="str"> the string to match</param>
        /// <returns>the new pattern</returns>
        public static Pattern RepeatLazy(string str)
        {
            return RepeatLazy(Match(str));
        }

        /// <summary>
        /// Create a pattern that lazily matches one or more occurrences of a particular string, case independent
        /// </summary>
        /// <param name="str"> the string to match</param>
        /// <returns>the new pattern</returns>
        public static Pattern RepeatLazyI(string str)
        {
            return RepeatLazy(MatchI(str));
        }

        /// <summary>
        /// Create a pattern that matches a given pattern or the empty string.
        /// </summary>
        /// <param name="pat">given pattern</param>
        /// <returns>the new pattern</returns>
        public static Pattern Maybe(IMatchable pat)
        {
            return new OptionalPattern(pat);
        }

        /// <summary>
        /// Create a pattern that matches a particular string, or the empty string, case dependent
        /// </summary>
        /// <param name="str">the string to match</param>
        /// <returns>the new pattern</returns>
        public static Pattern Maybe(string str)
        {
            return Maybe(Match(str));
        }

        /// <summary>
        /// Create a pattern that matches a particular string, or the empty string, case independent
        /// </summary>
        /// <param name="str">the string to match</param>
        /// <returns>the new pattern</returns>
        public static Pattern MaybeI(string str)
        {
            return Maybe(MatchI(str));
        }

        /// <summary>
        /// Create a pattern that lazily matches a given pattern or the empty string.
        /// </summary>
        /// <param name="pat">given pattern</param>
        /// <returns>the new pattern</returns>
        public static Pattern MaybeLazy(IMatchable pat)
        {
            return new OptionalPattern(pat, true);
        }

        /// <summary>
        /// Create a pattern that lazily matches a particular string, or the empty string, case dependent
        /// </summary>
        /// <param name="str">the string to match</param>
        /// <returns>the new pattern</returns>
        public static Pattern MaybeLazy(string str)
        {
            return MaybeLazy(Match(str));
        }

        /// <summary>
        /// Create a pattern that lazily matches a particular string, or the empty string, case independent
        /// </summary>
        /// <param name="str">the string to match</param>
        /// <returns>the new pattern</returns>
        public static Pattern MaybeLazyI(string str)
        {
            return MaybeLazy(MatchI(str));
        }

        /// <summary>
        /// Create a pattern that matches zero or more occurrences of a given pattern.
        /// </summary>
        /// <param name="pattern">given pattern</param>
        /// <returns>the new pattern</returns>
        public static Pattern MaybeRepeat(IMatchable pattern)
        {
            return new RepeatingPattern(pattern, false);
        }

        /// <summary>
        /// Create a pattern that matches zero or more occurrences of a particular string, case dependent
        /// </summary>
        /// <param name="str">the string to match</param>
        /// <returns>the new pattern</returns>
        public static Pattern MaybeRepeat(string str)
        {
            return MaybeRepeat(Match(str));
        }

        /// <summary>
        /// Create a pattern that matches zero or more occurrences of a particular string, case independent
        /// </summary>
        /// <param name="str">the string to match</param>
        /// <returns>the new pattern</returns>
        public static Pattern MaybeRepeatI(string str)
        {
            return MaybeRepeat(MatchI(str));
        }

        /// <summary>
        /// Create a pattern that lazily matches zero or more occurrences of a given pattern.
        /// </summary>
        /// <param name="pattern">given pattern</param>
        /// <returns>the new pattern</returns>
        public static Pattern MaybeRepeatLazy(IMatchable pattern)
        {
            return new RepeatingPattern(pattern, false, true);
        }

        /// <summary>
        /// Create a pattern that lazily matches zero or more occurrences of a particular string, case dependent
        /// </summary>
        /// <param name="str">the string to match</param>
        /// <returns>the new pattern</returns>
        public static Pattern MaybeRepeatLazy(string str)
        {
            return MaybeRepeatLazy(Match(str));
        }

        /// <summary>
        /// Create a pattern that lazily matches zero or more occurrences of a particular string, case independent
        /// </summary>
        /// <param name="str">the string to match</param>
        /// <returns>the new pattern</returns>
        public static Pattern MaybeRepeatLazyI(string str)
        {
            return MaybeRepeatLazy(MatchI(str));
        }

        /// <summary>
        /// Create a pattern that creates a capture group for a given pattern.
        /// </summary>
        /// <param name="pattern">given pattern</param>
        /// <returns>the new pattern</returns>
        public static Pattern Group(IMatchable pattern)
        {
            return new GroupPattern(pattern);
        }

        /// <summary>
        /// Create a pattern that matches any of the given patterns.
        /// </summary>
        /// <param name="patterns">patterns to accept</param>
        /// <returns>the new pattern</returns>
        public static Pattern AnyOf(params IMatchable[] patterns)
        {
            return new UnionPattern(patterns);
        }

        /// <summary>
        /// Create a pattern that matches any of the given patterns
        /// </summary>
        /// <param name="patterns">patterns to accept</param>
        /// <returns>the new pattern</returns>
        public static Pattern AnyOf(IEnumerable<IMatchable> patterns)
        {
            return new UnionPattern(patterns.ToArray());
        }

        /// <summary>
        /// Create a pattern that matches any of the given strings.
        /// </summary>
        /// <param name="p0">first possible string</param>
        /// <param name="p1">second possible string</param>
        /// <param name="strings">remaining possible strings, if any</param>
        /// <returns>the new pattern</returns>
        public static Pattern AnyOf(string p0, string p1, params string[] strings)
        {
            var patterns = new IMatchable[strings.Length + 2];
            patterns[0] = Match(p0);
            patterns[1] = Match(p1);
            for (var i = 0; i < strings.Length; ++i)
            {
                patterns[i + 2] = Match(strings[i]);
            }

            return new UnionPattern(patterns);
        }

        /// <summary>
        /// Create a pattern that matches any of the given strings, case independent.
        /// </summary>
        /// <param name="p0">first possible string</param>
        /// <param name="p1">second possible string</param>
        /// <param name="strings">remaining possible strings, if any</param>
        /// <returns>the new pattern</returns>
        public static Pattern AnyOfI(string p0, string p1, params string[] strings)
        {
            var patterns = new IMatchable[strings.Length + 2];
            patterns[0] = MatchI(p0);
            patterns[1] = MatchI(p1);
            for (var i = 0; i < strings.Length; ++i)
            {
                patterns[i + 2] = MatchI(strings[i]);
            }

            return new UnionPattern(patterns);
        }

        /// <summary>
        /// Create a pattern that matches any single character from the given string.
        /// </summary>
        /// <param name="chars">the characters to accept</param>
        /// <returns>the new pattern</returns>
        public static Pattern AnyCharIn(string chars)
        {
            return Match(CharRange.CreateBuilder().AddChars(chars).Build());
        }

        /// <summary>
        /// Create a pattern that matches strings from this pattern, followed by strings from the given pattern.
        /// </summary>
        /// <param name="tocat">pattern to append to this one</param>
        /// <returns>the new pattern</returns>
        public virtual Pattern Then(IMatchable tocat)
        {
            return new CatPattern(this, tocat);
        }

        /// <summary>
        /// Create a pattern that matches strings from this pattern, followed by a given string, case dependent.
        /// </summary>
        /// <param name="str">string to append to this pattern</param>
        /// <returns>the new pattern</returns>
        public Pattern Then(string str)
        {
            return Then(Match(str));
        }

        /// <summary>
        /// Create a pattern that matches strings from this pattern, followed by a given string, case independent.
        /// </summary>
        /// <param name="str">string to append to this pattern</param>
        /// <returns>the new pattern</returns>
        public Pattern ThenI(string str)
        {
            return Then(MatchI(str));
        }

        /// <summary>
        /// Create a pattern that matches strings from this pattern, followed by strings that match a regular expression, case dependent.
        /// </summary>
        /// <param name="regexStr">regular expression to append to this pattern</param>
        /// <returns>the new pattern</returns>
        public Pattern ThenRegex(string regexStr)
        {
            return Then(Regex(regexStr));
        }

        /// <summary>
        /// Create a pattern that matches strings from this pattern, followed by strings that match a regular expression, case independent.
        /// </summary>
        /// <param name="regexStr">regular expression to append to this pattern</param>
        /// <returns>the new pattern</returns>
        public Pattern ThenRegexI(string regexStr)
        {
            return Then(RegexI(regexStr));
        }

        /// <summary>
        /// Create a pattern that matches strings from this pattern, followed by one or more occurrences of a given pattern.
        /// </summary>
        /// <param name="pattern">the given pattern</param>
        /// <returns>the new pattern</returns>
        public Pattern ThenRepeat(IMatchable pattern)
        {
            return Then(Repeat(pattern));
        }

        /// <summary>
        /// Create a pattern that matches strings from this pattern, followed by one or more occurrences of a given string, case dependent.
        /// </summary>
        /// <param name="str">the given string</param>
        /// <returns>the new pattern</returns>
        public Pattern ThenRepeat(string str)
        {
            return Then(Repeat(str));
        }

        /// <summary>
        /// Create a pattern that matches strings from this pattern, followed by one or more occurrences of a given string, case independent.
        /// </summary>
        /// <param name="str">the given string</param>
        /// <returns>the new pattern</returns>
        public Pattern ThenRepeatI(string str)
        {
            return Then(RepeatI(str));
        }

        /// <summary>
        /// Create a pattern that lazily matches strings from this pattern, followed by one or more occurrences of a given pattern.
        /// </summary>
        /// <param name="pattern">the given pattern</param>
        /// <returns>the new pattern</returns>
        public Pattern ThenRepeatLazy(IMatchable pattern)
        {
            return Then(RepeatLazy(pattern));
        }

        /// <summary>
        /// Create a pattern that lazily matches strings from this pattern, followed by one or more occurrences of a given string, case dependent.
        /// </summary>
        /// <param name="str">the given string</param>
        /// <returns>the new pattern</returns>
        public Pattern ThenRepeatLazy(string str)
        {
            return Then(RepeatLazy(str));
        }

        /// <summary>
        /// Create a pattern that lazily matches strings from this pattern, followed by one or more occurrences of a given string, case independent.
        /// </summary>
        /// <param name="str">the given string</param>
        /// <returns>the new pattern</returns>
        public Pattern ThenRepeatLazyI(string str)
        {
            return Then(RepeatLazyI(str));
        }

        /// <summary>
        /// Create a pattern that matches strings from this pattern, maybe followed by a match of the given pattern.
        /// </summary>
        /// <param name="pattern">the given pattern</param>
        /// <returns>the new pattern</returns>
        public Pattern ThenMaybe(IMatchable pattern)
        {
            return Then(Maybe(pattern));
        }

        /// <summary>
        /// Create a pattern that matches strings from this pattern, maybe followed by a match of the given string, case dependent.
        /// </summary>
        /// <param name="str">the given string</param>
        /// <returns>the new pattern</returns>
        public Pattern ThenMaybe(string str)
        {
            return Then(Maybe(str));
        }

        /// <summary>
        /// Create a pattern that matches strings from this pattern, maybe followed by a match of the given string, case independent.
        /// </summary>
        /// <param name="str">the given string</param>
        /// <returns>the new pattern</returns>
        public Pattern ThenMaybeI(string str)
        {
            return Then(MaybeI(str));
        }

        /// <summary>
        /// Create a pattern that lazily matches strings from this pattern, maybe followed by a match of the given pattern.
        /// </summary>
        /// <param name="pattern">the given pattern</param>
        /// <returns>the new pattern</returns>
        public Pattern ThenMaybeLazy(IMatchable pattern)
        {
            return Then(MaybeLazy(pattern));
        }

        /// <summary>
        /// Create a pattern that lazily matches strings from this pattern, maybe followed by a match of the given string, case dependent.
        /// </summary>
        /// <param name="str">the given string</param>
        /// <returns>the new pattern</returns>
        public Pattern ThenMaybeLazy(string str)
        {
            return Then(MaybeLazy(str));
        }

        /// <summary>
        /// Create a pattern that lazily matches strings from this pattern, maybe followed by a match of the given string, case independent.
        /// </summary>
        /// <param name="str">the given string</param>
        /// <returns>the new pattern</returns>
        public Pattern ThenMaybeLazyI(string str)
        {
            return Then(MaybeLazyI(str));
        }

        /// <summary>
        /// Create a pattern that matches strings from this pattern, followed by zero or more occurrences of the given pattern.
        /// </summary>
        /// <param name="pattern">the given pattern</param>
        /// <returns>the new pattern</returns>
        public Pattern ThenMaybeRepeat(IMatchable pattern)
        {
            return Then(MaybeRepeat(pattern));
        }

        /// <summary>
        /// Create a pattern that matches strings from this pattern, followed by zero or more occurrences of the given string, case dependent.
        /// </summary>
        /// <param name="str">the given string</param>
        /// <returns>the new pattern</returns>
        public Pattern ThenMaybeRepeat(string str)
        {
            return Then(MaybeRepeat(str));
        }

        /// <summary>
        /// Create a pattern that matches strings from this pattern, followed by zero or more occurrences of the given string, case dependent.
        /// </summary>
        /// <param name="str">the given string</param>
        /// <returns>the new pattern</returns>
        public Pattern ThenMaybeRepeatI(string str)
        {
            return Then(MaybeRepeatI(str));
        }

        /// <summary>
        /// Create a pattern that lazily matches strings from this pattern, followed by zero or more occurrences of the given pattern.
        /// </summary>
        /// <param name="pattern">the given pattern</param>
        /// <returns>the new pattern</returns>
        public Pattern ThenMaybeRepeatLazy(IMatchable pattern)
        {
            return Then(MaybeRepeatLazy(pattern));
        }

        /// <summary>
        /// Create a pattern that lazily matches strings from this pattern, followed by zero or more occurrences of the given string, case dependent.
        /// </summary>
        /// <param name="str">the given string</param>
        /// <returns>the new pattern</returns>
        public Pattern ThenMaybeRepeatLazy(string str)
        {
            return Then(MaybeRepeatLazy(str));
        }

        /// <summary>
        /// Create a pattern that lazily matches strings from this pattern, followed by zero or more occurrences of the given string, case dependent.
        /// </summary>
        /// <param name="str">the given string</param>
        /// <returns>the new pattern</returns>
        public Pattern ThenMaybeRepeatLazyI(string str)
        {
            return Then(MaybeRepeatLazyI(str));
        }

        /// <summary>
        /// Create a pattern that creates a capture group for a given pattern.
        /// </summary>
        /// <param name="pattern">given pattern</param>
        /// <returns>the new pattern</returns>
        public Pattern ThenGroup(IMatchable pattern)
        {
            return Then(Group(pattern));
        }

        public abstract TState AddToNfa<TState>(INfaBuilder<TState> nfa, TState targetState, CaptureGroup captureGroup);

        public abstract bool MatchesEmpty { get; }

        public abstract bool MatchesNonEmpty { get; }

        public abstract bool MatchesSomething { get; }

        public abstract bool IsUnbounded { get; }

        // TODO remove when we get rid of InputRange
        public abstract IEnumerable<IMatchable> Children { get; }

        public IMatchable Reversed
        {
            get
            {
                var ret = reverse;
                if (ret == null)
                {
                    // thread-safe, but we don't bother stopping 2 threads from doing the same work.
                    // It's unusual and not particulary expensive.
                    ret = CalcReverse();
                    ret.reverse = this;
                    reverse = ret;
                }

                return ret;
            }
        }

        protected abstract Pattern CalcReverse();

        [Serializable]
        private class CatPattern : Pattern
        {
            private readonly IMatchable first;
            private readonly IMatchable then;

            internal CatPattern(IMatchable first, IMatchable then)
            {
                this.first = first;
                this.then = then;
                MatchesEmpty = first.MatchesEmpty && then.MatchesEmpty;
            }

            public override TState AddToNfa<TState>(INfaBuilder<TState> nfa, TState targetState, CaptureGroup captureGroup)
            {
                targetState = then.AddToNfa(nfa, targetState, captureGroup);
                targetState = first.AddToNfa(nfa, targetState, captureGroup);
                return targetState;
            }

            public override bool MatchesEmpty { get; }

            public override bool MatchesNonEmpty => first.MatchesNonEmpty ? then.MatchesSomething : first.MatchesEmpty && then.MatchesNonEmpty;

            public override bool MatchesSomething => MatchesEmpty || first.MatchesSomething && then.MatchesSomething;

            public override bool IsUnbounded => then.IsUnbounded ? first.MatchesSomething : first.IsUnbounded && then.MatchesSomething;

            protected override Pattern CalcReverse()
            {
                return new CatPattern(then.Reversed, first.Reversed);
            }

            // TODO remove when we get rid of InputRange
            public override IEnumerable<IMatchable> Children => new[] {first, then};
        }

        [Serializable]
        private class WrapPattern : Pattern
        {
            private readonly IMatchable tomatch;

            internal WrapPattern(IMatchable tomatch)
            {
                this.tomatch = tomatch;
            }

            public override TState AddToNfa<TState>(INfaBuilder<TState> nfa, TState targetState, CaptureGroup captureGroup)
            {
                return tomatch.AddToNfa(nfa, targetState, captureGroup);
            }

            public override bool MatchesEmpty => tomatch.MatchesEmpty;

            public override bool MatchesNonEmpty => tomatch.MatchesNonEmpty;

            public override bool MatchesSomething => tomatch.MatchesSomething;

            public override bool IsUnbounded => tomatch.IsUnbounded;

            protected override Pattern CalcReverse()
            {
                var revmatch = tomatch.Reversed;
                // ReSharper disable once PossibleUnintendedReferenceComparison
                if (revmatch == tomatch)
                {
                    return this;
                }

                return new WrapPattern(revmatch);
            }

            // TODO remove when we get rid of InputRange
            public override IEnumerable<IMatchable> Children => new[] {tomatch};
        }

        [Serializable]
        private class EmptyPattern : Pattern
        {
            public override TState AddToNfa<TState>(INfaBuilder<TState> nfa, TState targetState, CaptureGroup captureGroup)
            {
                return targetState;
            }

            public override bool MatchesEmpty => true;

            public override bool MatchesNonEmpty => false;

            public override bool MatchesSomething => true;

            public override bool IsUnbounded => false;

            public override Pattern Then(IMatchable tocat)
            {
                return Match(tocat);
            }

            protected override Pattern CalcReverse()
            {
                return this;
            }

            // TODO remove when we get rid of InputRange
            public override IEnumerable<IMatchable> Children => Enumerable.Empty<IMatchable>();
        }

        [Serializable]
        // TODO make private again when we get rid of InputRange
        internal class StringPattern : Pattern
        {
            // TODO make private again when we get rid of InputRange
            internal readonly string tomatch;

            public StringPattern(string tomatch)
            {
                this.tomatch = tomatch;
            }

            public override TState AddToNfa<TState>(INfaBuilder<TState> nfa, TState targetState, CaptureGroup captureGroup)
            {
                for (var i = tomatch.Length - 1; i >= 0; --i)
                {
                    var ch = tomatch[i];
                    var newst = nfa.AddState();
                    nfa.AddTransition(newst, targetState, ch, ch);
                    targetState = newst;
                }

                return targetState;
            }

            public override bool MatchesEmpty => tomatch.Length == 0;

            public override bool MatchesNonEmpty => tomatch.Length > 0;

            public override bool MatchesSomething => true;

            public override bool IsUnbounded => false;

            protected override Pattern CalcReverse()
            {
                if (tomatch.Length < 2)
                {
                    return this;
                }

                return new StringPattern(tomatch.Reverse());
            }

            // TODO remove when we get rid of InputRange
            public override IEnumerable<IMatchable> Children => Enumerable.Empty<IMatchable>();
        }

        [Serializable]
        // TODO make private again when we get rid of InputRange
        internal class StringIPattern : Pattern
        {
            // TODO make private again when we get rid of InputRange
            internal readonly string tomatch;

            public StringIPattern(string tomatch)
            {
                this.tomatch = tomatch;
            }

            public override TState AddToNfa<TState>(INfaBuilder<TState> nfa, TState targetState, CaptureGroup captureGroup)
            {
                for (var i = tomatch.Length - 1; i >= 0; --i)
                {
                    var ch = tomatch[i];
                    var newst = nfa.AddState();
                    var lc = char.ToLowerInvariant(ch);
                    if (lc != ch)
                    {
                        nfa.AddTransition(newst, targetState, lc, lc);
                    }

                    var uc = char.ToUpperInvariant(ch);
                    if (uc != ch)
                    {
                        nfa.AddTransition(newst, targetState, uc, uc);
                    }

                    targetState = newst;
                }

                return targetState;
            }

            public override bool MatchesEmpty => tomatch.Length == 0;

            public override bool MatchesNonEmpty => tomatch.Length > 0;

            public override bool MatchesSomething => true;

            public override bool IsUnbounded => false;

            protected override Pattern CalcReverse()
            {
                if (tomatch.Length < 2)
                {
                    return this;
                }

                return new StringIPattern(tomatch.Reverse());
            }

            // TODO remove when we get rid of InputRange
            public override IEnumerable<IMatchable> Children => Enumerable.Empty<IMatchable>();
        }

        [Serializable]
        private class RepeatingPattern : Pattern
        {
            private readonly IMatchable pattern;
            private readonly bool       needAtLeastOne;
            private readonly bool       lazy;

            public RepeatingPattern(IMatchable pattern, bool needAtLeastOne, bool lazy = false)
            {
                this.pattern = pattern;
                this.needAtLeastOne = needAtLeastOne;
                this.lazy = lazy;
            }

            public override TState AddToNfa<TState>(INfaBuilder<TState> nfa, TState targetState, CaptureGroup captureGroup)
            {
                var repState = nfa.AddState();
                nfa.AddEpsilon(repState, targetState, lazy ? NfaTransitionPriority.Normal : NfaTransitionPriority.Low, Tag.None);
                var startState = pattern.AddToNfa(nfa, repState, captureGroup);
                if (needAtLeastOne) // || pattern.MatchesEmpty)
                {
                    nfa.AddEpsilon(repState, startState, lazy ? NfaTransitionPriority.Low : NfaTransitionPriority.Normal, Tag.None);
                    return startState;
                }

                var skipState = nfa.AddState();
                nfa.AddEpsilon(repState,  skipState,   NfaTransitionPriority.Normal,                                    Tag.None);
                nfa.AddEpsilon(skipState, targetState, lazy ? NfaTransitionPriority.Normal : NfaTransitionPriority.Low, Tag.None);
                nfa.AddEpsilon(skipState, startState,  lazy ? NfaTransitionPriority.Low : NfaTransitionPriority.Normal, Tag.None);
                return skipState;
            }

            public override bool MatchesEmpty => !needAtLeastOne || pattern.MatchesEmpty;

            public override bool MatchesNonEmpty => pattern.MatchesNonEmpty;

            public override bool MatchesSomething => pattern.MatchesSomething;

            public override bool IsUnbounded => pattern.MatchesNonEmpty;

            protected override Pattern CalcReverse()
            {
                var patternReversed = pattern.Reversed;
                // ReSharper disable once PossibleUnintendedReferenceComparison
                if (patternReversed == pattern)
                {
                    return this;
                }

                return new RepeatingPattern(patternReversed, needAtLeastOne, lazy);
            }

            // TODO remove when we get rid of InputRange
            public override IEnumerable<IMatchable> Children => new[] {pattern};
        }

        [Serializable]
        private class OptionalPattern : Pattern
        {
            private readonly IMatchable pattern;
            private readonly bool       lazy;

            public OptionalPattern(IMatchable pattern, bool lazy = false)
            {
                this.pattern = pattern;
                this.lazy = lazy;
            }

            public override TState AddToNfa<TState>(INfaBuilder<TState> nfa, TState targetState, CaptureGroup captureGroup)
            {
                var startState = pattern.AddToNfa(nfa, targetState, captureGroup);
                if (pattern.MatchesEmpty)
                {
                    return startState;
                }

                var skipState = nfa.AddState();
                nfa.AddEpsilon(skipState, targetState, lazy ? NfaTransitionPriority.Normal : NfaTransitionPriority.Low, Tag.None);
                nfa.AddEpsilon(skipState, startState,  lazy ? NfaTransitionPriority.Low : NfaTransitionPriority.Normal, Tag.None);
                return skipState;
            }

            public override bool MatchesEmpty => true;

            public override bool MatchesNonEmpty => pattern.MatchesNonEmpty;

            public override bool MatchesSomething => true;

            public override bool IsUnbounded => pattern.IsUnbounded;

            protected override Pattern CalcReverse()
            {
                var revpat = pattern.Reversed;
                // ReSharper disable once PossibleUnintendedReferenceComparison
                if (revpat == pattern)
                {
                    return this;
                }

                return new OptionalPattern(revpat, lazy);
            }

            // TODO remove when we get rid of InputRange
            public override IEnumerable<IMatchable> Children => new[] {pattern};
        }

        [Serializable]
        private class GroupPattern : Pattern
        {
            private readonly IMatchable pattern;

            public GroupPattern(IMatchable pattern)
            {
                this.pattern = pattern;
            }

            public override TState AddToNfa<TState>(INfaBuilder<TState> nfa, TState targetState, CaptureGroup parentCaptureGroup)
            {
                var cg = nfa.MakeCaptureGroup(parentCaptureGroup);

                var startState = nfa.AddState();
                var endState = nfa.AddState();
                var patternState = pattern.AddToNfa(nfa, endState, cg);

                nfa.AddEpsilon(startState, patternState, NfaTransitionPriority.Normal, cg.StartTag);
                nfa.AddEpsilon(endState,   targetState,  NfaTransitionPriority.Normal, cg.EndTag);

                return startState;
            }

            public override bool MatchesEmpty => pattern.MatchesEmpty;
            public override bool MatchesNonEmpty => pattern.MatchesNonEmpty;
            public override bool MatchesSomething => pattern.MatchesSomething;
            public override bool IsUnbounded => pattern.IsUnbounded;

            protected override Pattern CalcReverse()
            {
                var revpat = pattern.Reversed;
                // ReSharper disable once PossibleUnintendedReferenceComparison
                if (revpat == pattern)
                {
                    return this;
                }

                return new GroupPattern(revpat);
            }

            // TODO remove when we get rid of InputRange
            public override IEnumerable<IMatchable> Children => new[] {pattern};
        }

        [Serializable]
        private class UnionPattern : Pattern
        {
            [Flags]
            private enum Checks
            {
                EmptyChecked    = 1,
                EmptyMatch      = 2,
                NonemptyChecked = 4,
                NonemptyMatch   = 8,
                UnboundChecked  = 16,
                UnboundMatch    = 32
            }

            private readonly IMatchable[] choices;
            private volatile Checks       flags = 0;

            public UnionPattern(IMatchable[] choices)
            {
                this.choices = new IMatchable[choices.Length];
                Array.Copy(choices, this.choices, choices.Length);
            }

            public override TState AddToNfa<TState>(INfaBuilder<TState> nfa, TState targetState, CaptureGroup captureGroup)
            {
                if (choices.Length == 0)
                {
                    return targetState;
                }

                if (choices.Length == 1)
                {
                    return choices[0].AddToNfa(nfa, targetState, captureGroup);
                }

                var startState = nfa.AddState();

                var newChoices = new IMatchable[choices.Length - 1];
                Array.Copy(choices, newChoices, newChoices.Length);
                var pattern = new UnionPattern(newChoices);

                var endState = nfa.AddState();
                nfa.AddEpsilon(endState,   targetState,                                                       NfaTransitionPriority.Low,    Tag.None);
                nfa.AddEpsilon(startState, choices[choices.Length - 1].AddToNfa(nfa, endState, captureGroup), NfaTransitionPriority.Normal, Tag.None);

                endState = nfa.AddState();
                nfa.AddEpsilon(endState,   targetState,                                   NfaTransitionPriority.Normal, Tag.None);
                nfa.AddEpsilon(startState, pattern.AddToNfa(nfa, endState, captureGroup), NfaTransitionPriority.Normal, Tag.None);

                return startState;
            }

            public override bool MatchesEmpty => Check(Checks.EmptyChecked, Checks.EmptyMatch, pattern => pattern.MatchesEmpty);

            public override bool MatchesNonEmpty => Check(Checks.NonemptyChecked, Checks.NonemptyMatch, pattern => pattern.MatchesNonEmpty);

            public override bool MatchesSomething => MatchesEmpty || MatchesNonEmpty;

            public override bool IsUnbounded => Check(Checks.UnboundChecked, Checks.UnboundMatch, pattern => pattern.IsUnbounded);

            // TODO remove when we get rid of InputRange
            public override IEnumerable<IMatchable> Children => choices;

            protected override Pattern CalcReverse()
            {
                var ret = this;
                var newpat = new UnionPattern(choices);
                for (var i = 0; i < choices.Length; ++i)
                {
                    var old = newpat.choices[i];
                    var rev = old.Reversed;
                    // ReSharper disable once PossibleUnintendedReferenceComparison
                    if (old != rev)
                    {
                        newpat.choices[i] = rev;
                        ret = newpat;
                    }
                }

                return ret;
            }

            private bool Check(Checks checkedFlag, Checks matchFlag, Func<IMatchable, bool> matcher)
            {
                var f = flags;
                if ((f & checkedFlag) == 0)
                {
                    foreach (var pattern in choices)
                    {
                        if (matcher(pattern))
                        {
                            f |= matchFlag;
                            break;
                        }
                    }

                    f |= checkedFlag;
                    flags = f;
                }

                return (f & matchFlag) != 0;
            }
        }
    }

    internal static class StringExtensions
    {
        private static IEnumerable<string> GraphemeClusters(this string s)
        {
            var enumerator = StringInfo.GetTextElementEnumerator(s);
            while (enumerator.MoveNext())
            {
                yield return (string) enumerator.Current;
            }
        }

        internal static string ReverseGraphemeClusters(this string s)
        {
            return string.Join("", s.GraphemeClusters().Reverse().ToArray());
        }

        internal static string Reverse(this string s)
        {
            var charArray = s.ToArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
    }
}
