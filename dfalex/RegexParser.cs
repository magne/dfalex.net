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
using System.Diagnostics;
using System.Globalization;
using System.Text;
using static CodeHive.DfaLex.RegexParserConstants;

namespace CodeHive.DfaLex
{
    /// <summary>
    /// Parses regular expression into <see cref="IMatchable"/> implementations.
    ///
    /// One would normally use <see cref="Pattern.Regex"/> or <see cref="Pattern.RegexI"/> instead of using
    /// this class directly.
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
    internal class RegexParser : RegexParser<IMatchable>
    {
        private static readonly IRegexParserActions<IMatchable> Actions = new MatchableRegexParserActions();

        /// <summary>
        /// Parse a regular expression.
        /// </summary>
        /// <param name="regex">a string containing the expression to parse</param>
        /// <param name="options">options that influence the parse</param>
        /// <returns>a <see cref="IMatchable"/> that implements the regular expression</returns>
        public static IMatchable Parse(string regex, RegexOptions options = RegexOptions.None)
        {
            return new RegexParser(regex, options).Parse();
        }

        /// <summary>
        /// Parse a regular expression.
        /// </summary>
        /// <param name="regex">a string containing the expression to parse</param>
        /// <param name="caseIndependent">true to make it case independent</param>
        /// <returns>a <see cref="IMatchable"/> that implements the regular expression</returns>
        [Obsolete("Use RegexParser.Parse(regex, RegexOptions.IgnoreCase) instead. Will be removed in version 2.0")]
        public static IMatchable Parse(string regex, bool caseIndependent)
        {
            var options = caseIndependent ? RegexOptions.IgnoreCase : RegexOptions.None;
            return Parse(regex, options);
        }

        private RegexParser(string str, RegexOptions options)
            : base(Actions, str, options)
        { }


        private sealed class MatchableRegexParserActions : IRegexParserActions<IMatchable>
        {
            public IMatchable Empty(IRegexContext ctx) => Pattern.Empty;

            public IMatchable Literal(IRegexContext ctx, CharRange range) => range;

            public IMatchable Alternate(IRegexContext ctx, IMatchable p1, IMatchable p2) => Pattern.AnyOf(p1, p2);

            public IMatchable Catenate(IRegexContext ctx, IMatchable p1, IMatchable p2) => Pattern.Match(p1).Then(p2);

            public IMatchable Repeat(IRegexContext ctx, IMatchable p, int min = -1, int max = -1, bool lazy = false)
            {
#pragma warning disable 618
                if (ctx.Option(RegexOptions.Legacy))
#pragma warning restore 618
                {
                    switch (min, max)
                    {
                        case (0, 1):
                            return Pattern.Maybe(p);
                        case (0, -1):
                            return Pattern.MaybeRepeat(p);
                        case (1, -1):
                            return lazy ? Pattern.MaybeRepeat(p) : Pattern.Repeat(p);
                    }
                }

                switch (min, max)
                {
                    case (0, 1):
                        return lazy ? Pattern.MaybeLazy(p) : Pattern.Maybe(p);
                    case (0, -1):
                        return lazy ? Pattern.MaybeRepeatLazy(p) : Pattern.MaybeRepeat(p);
                    case (1, -1):
                        return lazy ? Pattern.RepeatLazy(p) : Pattern.Repeat(p);
                    default:
                        var strMin = min == -1 ? string.Empty : min.ToString();
                        var strMax = max == -1 ? string.Empty : max.ToString();
                        throw new DfaException($"Unsupported repeat {{{strMin},{strMax}}}");
                }
            }

            public IMatchable Group(IRegexContext ctx, IMatchable p, int no) => p;
        }
    }

    internal static class RegexParserConstants
    {
        internal static readonly CharRange DigitChars    = CharRange.Digits;
        internal static readonly CharRange NonDigitChars = DigitChars.Complement();

        internal static readonly CharRange SpaceChars    = CharRange.CreateBuilder().AddChars(" \t\n\r\f\u000B").Build();
        internal static readonly CharRange NonSpaceChars = SpaceChars.Complement();

        internal static readonly CharRange WordChars =
            CharRange.CreateBuilder().AddRange('a', 'z').AddRange('A', 'Z').AddRange('0', '9').AddChars("_").Build();

        internal static readonly CharRange NonWordChars = WordChars.Complement();
    }

    internal class RegexParser<T> where T : class
    {
        private static readonly DfaState<Action> Dfa = BuildParserDfa();

        private readonly IRegexParserActions<T>  actions;
        private readonly IRegexContext           context;
        private readonly string                  str;
        private          int                     readPos;
        private readonly CharRange.Builder       charBuilder = CharRange.CreateBuilder();
        private          char                    cprev;
        private          char                    clast;
        private          int                     nparen;
        private readonly StringBuilder           symStack   = new StringBuilder();
        private readonly Stack<T>                valStack   = new Stack<T>();
        private readonly Stack<DfaState<Action>> stateStack = new Stack<DfaState<Action>>();

        protected RegexParser(IRegexParserActions<T> actions, string str, RegexOptions options)
        {
            this.actions = actions;
            this.str = str;

            context = new RegexContext(options);
        }

        protected T Parse()
        {
            stateStack.Clear();
            valStack.Clear();
            symStack.Clear();
            nparen = 0;
            readPos = 0;
            stateStack.Push(Dfa);
            var srclen = str.Length;
            var maxpos = 0;
            while (true)
            {
                // Match up to the end of the recognized symbol stack.  If we can't do
                // this, then there's a bug and we've reduced something we shouldn't have
                var st = stateStack.Peek();
                while (stateStack.Count - 1 < symStack.Length)
                {
                    st = st.GetNextState(symStack[stateStack.Count - 1]);
                    if (st == null)
                    {
                        throw new DfaException($"Internal bug encountered parsing regular expression: {str}");
                    }

                    stateStack.Push(st);
                }

                //get the reduction action at the end of the symbol stack
                var action = st.IsAccepting ? st.Match : null;

                //if we can lex and then reduce, do that instead
                var lexState = st.GetNextState(':');
                if (lexState != null && readPos < srclen)
                {
                    for (var i = readPos; i < srclen; ++i)
                    {
                        lexState = lexState.GetNextState(str[i]);
                        if (lexState == null)
                        {
                            break;
                        }

                        maxpos = i + 1;
                        if (lexState.IsAccepting)
                        {
                            action = lexState.Match;
                            readPos = i + 1;
                        }
                    }
                }

                if (action == null)
                {
                    //no applicable reduction -- we're either done or there's an error
                    break;
                }

                action(this, actions, context);
            }

            if (!"S".Equals(symStack.ToString()))
            {
                throw new ArgumentException($"Invald regular expression: \"{str}\" has error at position {maxpos}");
            }

            Debug.Assert(valStack.Count == 1);
            return valStack.Pop();
        }

        private void Push(string codes, T pattern)
        {
            symStack.Append(codes);
            while (valStack.Count < symStack.Length)
            {
                valStack.Push(pattern);
            }
        }

        private T Pop(int nonterms)
        {
            T ret = null;
            symStack.Remove(symStack.Length - nonterms, nonterms);
            while (valStack.Count > symStack.Length)
            {
                var pat = valStack.Pop();
                if (ret == null)
                {
                    ret = pat;
                }
            }

            while (stateStack.Count - 1 > symStack.Length)
            {
                stateStack.Pop();
            }

            return ret;
        }

        private char LastChar()
        {
            return str[readPos - 1];
        }

        private char ParseCharEscape()
        {
            var spos = readPos - 1;
            for (; spos > 0 && str[spos - 1] != '\\'; --spos)
            {
                // empty
            }

            try
            {
                var ch = str[spos];
                switch (ch)
                {
                    case 't':
                        return '\t';

                    case 'n':
                        return '\n';

                    case 'r':
                        return '\r';

                    case 'f':
                        return '\f';

                    case 'a':
                        return '\u0007';

                    case 'e':
                        return '\u001B';

                    case 'x':
                    case 'u':
                        return (char) uint.Parse(str.Substring(spos + 1, readPos - spos - 1), NumberStyles.HexNumber);

                    case 'c':
                        if (ch >= 'A' && ch <= 'Z')
                        {
                            return (char) (ch - 'A' + 1);
                        }

                        if (ch >= 'a' && ch <= 'z')
                        {
                            return (char) (ch - 'z' + 1);
                        }

                        break;

                    default:
                        if (ch == '_' || !WordChars.Contains(ch))
                        {
                            return ch;
                        }

                        break;
                }
            }
            catch (Exception)
            {
                // Will be thrown as a new exception below
            }

            throw new DfaException($"Invalid character escape \\{str.Substring(spos, 1)}");
        }

        private CharRange ParseClassEscape()
        {
            var spos = readPos - 1;
            for (; spos > 0 && str[spos - 1] != '\\'; --spos)
            {
                // empty
            }

            switch (str[spos])
            {
                case 'd':
                    return DigitChars;

                case 'D':
                    return NonDigitChars;

                case 's':
                    return SpaceChars;

                case 'S':
                    return NonSpaceChars;

                case 'w':
                    return WordChars;

                case 'W':
                    return NonWordChars;
            }

            throw new DfaException($"Invalid class escape \\{str.Substring(spos, 1)}");
        }

        // Build a DFA that matches a parse stack from the bottom to produce the next LR(1) action to perform.
        // The stack is of the form XXX:ccc, where XXX are previously recognized symbols, and ccc are all the
        // remaining characters in the input.
        private static DfaState<Action> BuildParserDfa()
        {
            var bld = new DfaBuilder<Action>();
            // S can be the whole expression, or group contents
            var sPos = Pattern.MaybeRepeat(Pattern.MaybeRepeat(CharRange.AnyOf("SCA|")).Then("("));

            // S: C | S '|' C
            bld.AddPattern(sPos.Then("C"),   (parser, _, __) => parser.Push("S", parser.Pop(1)));
            bld.AddPattern(sPos.Then("S:|"), (parser, _, __) => parser.Push("|", null));
            bld.AddPattern(sPos.Then("S|C"),
                           (parser, actions, ctx) =>
                           {
                               var p2 = parser.Pop(2);
                               var p1 = parser.Pop(1);
                               parser.Push("S", actions.Alternate(ctx, p1, p2));
                           });
            var cPos = sPos.ThenMaybe("S|");

            // C: e | C A
            bld.AddPattern(cPos, (parser, actions, ctx) => parser.Push("C", actions.Empty(ctx)));
            bld.AddPattern(cPos.Then("CA"),
                           (parser, actions, ctx) =>
                           {
                               var p2 = parser.Pop(1);
                               var p1 = parser.Pop(1);
                               parser.Push("C", actions.Catenate(ctx, p1, p2));
                           });
            var aPos = cPos.Then("C");

            //A: A? | A+ | A*
            bld.AddPattern(aPos.Then("A:?"), (parser, actions, ctx) => parser.Push("A", actions.Repeat(ctx, parser.Pop(1), 0, 1)));
            bld.AddPattern(aPos.Then("A:+"), (parser, actions, ctx) => parser.Push("A", actions.Repeat(ctx, parser.Pop(1), 1)));
            bld.AddPattern(aPos.Then("A:*"), (parser, actions, ctx) => parser.Push("A", actions.Repeat(ctx, parser.Pop(1), 0)));

            // A: A?? | A+? | A*?
            bld.AddPattern(aPos.Then("A:??"), (parser, actions, ctx) => parser.Push("A", actions.Repeat(ctx, parser.Pop(1), 0, 1, true)));
            bld.AddPattern(aPos.Then("A:+?"), (parser, actions, ctx) => parser.Push("A", actions.Repeat(ctx, parser.Pop(1), 1, lazy: true)));
            bld.AddPattern(aPos.Then("A:*?"), (parser, actions, ctx) => parser.Push("A", actions.Repeat(ctx, parser.Pop(1), 0, lazy: true)));

            //A: GROUP
            bld.AddPattern(aPos.Then(":("),   (parser, actions, ctx) => parser.Push("(", null));
            bld.AddPattern(aPos.Then("(S:)"), (parser, actions, ctx) => parser.Push("A", actions.Group(ctx, parser.Pop(2), ++parser.nparen)));

            //A: literal | .
            bld.AddPattern(aPos.Then(":").Then(CharRange.CreateBuilder().AddChars(".()[]+*?|\\").Invert().Build()),
                           (parser, actions, ctx) =>
                           {
                               CharRange range;
                               var c = parser.LastChar();
                               if (!ctx.Option(RegexOptions.IgnoreCase))
                               {
                                   range = CharRange.Single(c);
                               }
                               else
                               {
                                   var lc = char.ToLowerInvariant(c);
                                   var uc = char.ToUpperInvariant(c);
                                   if (lc == uc && lc == c)
                                   {
                                       range = CharRange.Single(c);
                                   }
                                   else
                                   {
                                       range = CharRange.CreateBuilder().AddChar(c).AddChar(lc).AddChar(uc).Build();
                                   }
                               }

                               parser.Push("A", actions.Literal(ctx, range));
                           });
            bld.AddPattern(aPos.Then(":."), (parser, actions, ctx) => parser.Push("A", actions.Literal(ctx, CharRange.All)));

            var charEscape = Pattern.Match(":\\").Then(Pattern.AnyOf(
                                                                     Pattern.Match("x").Then(CharRange.HexDigits).Then(CharRange.HexDigits),
                                                                     Pattern.Match("u").Then(CharRange.HexDigits).Then(CharRange.HexDigits).Then(CharRange.HexDigits)
                                                                            .Then(CharRange.HexDigits),
                                                                     Pattern.Match("c").Then(CharRange.CreateBuilder().AddRange('a', 'z').AddRange('A', 'Z').Build()),
                                                                     CharRange.CreateBuilder().AddChars("xucdDwWsS").Invert().Build()));
            var classEscape = Pattern.Match(":\\").Then(Pattern.AnyCharIn("dDsSwW"));

            bld.AddPattern(aPos.Then(charEscape),  (parser, actions, ctx) => parser.Push("A", actions.Literal(ctx, CharRange.Single(parser.ParseCharEscape()))));
            bld.AddPattern(aPos.Then(classEscape), (parser, actions, ctx) => parser.Push("A", actions.Literal(ctx, parser.ParseClassEscape())));

            //A: [R] | [^R]
            bld.AddPattern(aPos.Then(":[^"),
                           (parser, _, __) =>
                           {
                               parser.charBuilder.Clear();
                               parser.Push("[^", null);
                           });
            bld.AddPattern(aPos.Then(":["),
                           (parser, _, __) =>
                           {
                               parser.charBuilder.Clear();
                               parser.Push("[", null);
                           });
            bld.AddPattern(aPos.Then("[R:]"),
                           (parser, actions, ctx) =>
                           {
                               parser.Pop(2);
                               if (ctx.Option(RegexOptions.IgnoreCase))
                               {
                                   parser.charBuilder.ExpandCases();
                               }

                               parser.Push("A", actions.Literal(ctx, parser.charBuilder.Build()));
                           });
            bld.AddPattern(aPos.Then("[^R:]"),
                           (parser, actions, ctx) =>
                           {
                               parser.Pop(3);
                               if (ctx.Option(RegexOptions.IgnoreCase))
                               {
                                   parser.charBuilder.ExpandCases();
                               }

                               parser.Push("A", actions.Literal(ctx, parser.charBuilder.Invert().Build()));
                           });
            var rPos = aPos.Then(Pattern.AnyOf("[^", "["));

            //R: e | R classEscape | R c | R c - c
            bld.AddPattern(rPos, (parser, _, __) => parser.Push("R", null));
            bld.AddPattern(rPos.Then("R").Then(classEscape),
                           (parser, _, __) =>
                           {
                               parser.charBuilder.AddRange(parser.ParseClassEscape());
                               parser.Pop(0);
                           });
            bld.AddPattern(rPos.Then("Rc"),
                           (parser, _, __) =>
                           {
                               parser.Pop(1);
                               parser.charBuilder.AddRange(parser.clast, parser.clast);
                           });
            bld.AddPattern(rPos.Then("Rc:-"), (parser, _, ctx) => parser.Push("-", null));
            bld.AddPattern(rPos.Then("Rc-c"),
                           (parser, _, __) =>
                           {
                               parser.Pop(3);
                               if (parser.clast < parser.cprev)
                               {
                                   parser.charBuilder.AddRange(parser.clast, parser.cprev);
                               }
                               else
                               {
                                   parser.charBuilder.AddRange(parser.cprev, parser.clast);
                               }
                           });
            var cpos = rPos.Then("R").ThenMaybe("c-");

            //class chars
            bld.AddPattern(cpos.Then(":").Then(CharRange.CreateBuilder().AddChars("-[]\\").Invert().Build()),
                           (parser, _, __) =>
                           {
                               parser.cprev = parser.clast;
                               parser.clast = parser.LastChar();
                               parser.Push("c", null);
                           });
            bld.AddPattern(cpos.Then(charEscape),
                           (parser, _, __) =>
                           {
                               parser.cprev = parser.clast;
                               parser.clast = parser.ParseCharEscape();
                               parser.Push("c", null);
                           });

            return bld.Build(null);
        }

        private delegate void Action(RegexParser<T> parser, IRegexParserActions<T> actions, IRegexContext ctx);

        private sealed class RegexContext : IRegexContext
        {
            private RegexOptions options;

            public RegexContext(RegexOptions options)
            {
                this.options = options;
            }

            public bool Option(RegexOptions option) => (options & option) == option;

            public void ClrOption(RegexOptions option) => options &= ~option;
            public void SetOption(RegexOptions option) => options |= option;
        }
    }
}
