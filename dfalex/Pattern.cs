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
        /// <returns>a pattern that implements the regular expression</returns>
        public static Pattern Regex(string regex)
        {
            return Match(RegexParser.Parse(regex));
        }

        /// <summary>
        /// Parse the given regular expression into a pattern, case independent
        ///
        /// See <see cref="Regex"/> for syntax information.
        /// </summary>
        /// <param name="regex">regular expression string to parse</param>
        /// <returns>a pattern that implements the regular expression</returns>
        public static Pattern RegexI(string regex)
        {
            return Match(RegexParser.Parse(regex, true));
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
            for (var i = 1; i < strings.Length; ++i)
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
            for (var i = 1; i < strings.Length; ++i)
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

        public abstract int AddToNfa<TResult>(Nfa<TResult> nfa, int targetState);

        public abstract bool MatchesEmpty { get; }

        public abstract bool MatchesNonEmpty { get; }

        public abstract bool MatchesSomething { get; }

        public abstract bool IsUnbounded { get; }

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

            public override int AddToNfa<TResult>(Nfa<TResult> nfa, int targetState)
            {
                targetState = then.AddToNfa(nfa, targetState);
                targetState = first.AddToNfa(nfa, targetState);
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
        }

        [Serializable]
        private class WrapPattern : Pattern
        {
            private readonly IMatchable tomatch;

            internal WrapPattern(IMatchable tomatch)
            {
                this.tomatch = tomatch;
            }

            public override int AddToNfa<TResult>(Nfa<TResult> nfa, int targetState)
            {
                return tomatch.AddToNfa(nfa, targetState);
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
        }

        [Serializable]
        private class EmptyPattern : Pattern
        {
            public override int AddToNfa<TResult>(Nfa<TResult> nfa, int targetState)
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
        }

        [Serializable]
        private class StringPattern : Pattern
        {
            private readonly string tomatch;

            public StringPattern(string tomatch)
            {
                this.tomatch = tomatch;
            }

            public override int AddToNfa<TResult>(Nfa<TResult> nfa, int targetState)
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
        }

        [Serializable]
        private class StringIPattern : Pattern
        {
            private readonly string tomatch;

            public StringIPattern(string tomatch)
            {
                this.tomatch = tomatch;
            }

            public override int AddToNfa<TResult>(Nfa<TResult> nfa, int targetState)
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
        }

        [Serializable]
        private class RepeatingPattern : Pattern
        {
            private readonly IMatchable pattern;
            private readonly bool       needAtLeastOne;

            public RepeatingPattern(IMatchable pattern, bool needAtLeastOne)
            {
                this.pattern = pattern;
                this.needAtLeastOne = needAtLeastOne;
            }

            public override int AddToNfa<TResult>(Nfa<TResult> nfa, int targetState)
            {
                var repState = nfa.AddState();
                nfa.AddEpsilon(repState, targetState);
                var startState = pattern.AddToNfa(nfa, repState);
                nfa.AddEpsilon(repState, startState);
                if (needAtLeastOne || pattern.MatchesEmpty)
                {
                    return startState;
                }

                var skipState = nfa.AddState();
                nfa.AddEpsilon(skipState, targetState);
                nfa.AddEpsilon(skipState, startState);
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

                return new RepeatingPattern(patternReversed, needAtLeastOne);
            }
        }

        [Serializable]
        private class OptionalPattern : Pattern
        {
            private readonly IMatchable pattern;

            public OptionalPattern(IMatchable pattern)
            {
                this.pattern = pattern;
            }

            public override int AddToNfa<TResult>(Nfa<TResult> nfa, int targetState)
            {
                var startState = pattern.AddToNfa(nfa, targetState);
                if (pattern.MatchesEmpty)
                {
                    return startState;
                }

                var skipState = nfa.AddState();
                nfa.AddEpsilon(skipState, targetState);
                nfa.AddEpsilon(skipState, startState);
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

                return new OptionalPattern(revpat);
            }
        }

        [Serializable]
        private class UnionPattern : Pattern
        {
            [Flags]
            private enum Flags
            {
                EmptyChecked    = 1,
                EmptyMatch      = 2,
                NonemptyChecked = 4,
                NonemptyMatch   = 8,
                UnboundChecked  = 16,
                UnboundMatch    = 32
            }

            private readonly IMatchable[] choices;
            private volatile Flags        flags = 0;

            public UnionPattern(IMatchable[] choices)
            {
                this.choices = new IMatchable[choices.Length];
                Array.Copy(choices, this.choices, choices.Length);
            }

            public override int AddToNfa<TResult>(Nfa<TResult> nfa, int targetState)
            {
                var startState = nfa.AddState();
                foreach (var pattern in choices)
                {
                    nfa.AddEpsilon(startState, pattern.AddToNfa(nfa, targetState));
                }

                return startState;
            }

            public override bool MatchesEmpty => Check(Flags.EmptyChecked, Flags.EmptyMatch, pattern => pattern.MatchesEmpty);

            public override bool MatchesNonEmpty => Check(Flags.NonemptyChecked, Flags.NonemptyMatch, pattern => pattern.MatchesNonEmpty);

            public override bool MatchesSomething => MatchesEmpty || MatchesNonEmpty;

            public override bool IsUnbounded => Check(Flags.UnboundChecked, Flags.UnboundMatch, pattern => pattern.IsUnbounded);

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

            private bool Check(Flags checkedFlag, Flags matchFlag, Func<IMatchable, bool> matcher)
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
        private static IEnumerable<string> GraphemeClusters(this string s) {
            var enumerator = StringInfo.GetTextElementEnumerator(s);
            while(enumerator.MoveNext()) {
                yield return (string)enumerator.Current;
            }
        }
        internal static string ReverseGraphemeClusters(this string s) {
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
