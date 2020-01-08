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
using System.Text;

namespace CodeHive.DfaLex
{
    /// <summary>
    /// Parses regular expression into <see cref="IMatchable"/> implementations.
    ///
    /// One would normally use <see cref="Pattern.Regex(string)"/> or <see cref="Pattern.RegexI(string)"/> instead of using
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
    internal class RegexParser
    {
        private static readonly DfaState<Action> DFA = BuildParserDfa();

        private static readonly CharRange DigitChars    = CharRange.Digits;
        private static readonly CharRange NonDigitChars = DigitChars.Complement();

        private static readonly CharRange SpaceChars    = CharRange.CreateBuilder().AddChars(" \t\n\r\f\u000B").Build();
        private static readonly CharRange NonSpaceChars = SpaceChars.Complement();

        private static readonly CharRange WordChars =
            CharRange.CreateBuilder().AddRange('a', 'z').AddRange('A', 'Z').AddRange('0', '9').AddChars("_").Build();

        private static readonly CharRange NonWordChars = WordChars.Complement();

        private readonly string                  src;
        private readonly bool                    caseI;
        private          int                     readPos;
        private readonly CharRange.Builder       charBuilder = CharRange.CreateBuilder();
        private          char                    cprev;
        private          char                    clast;
        private readonly StringBuilder           symStack   = new StringBuilder();
        private readonly Stack<IMatchable>       valStack   = new Stack<IMatchable>();
        private readonly Stack<DfaState<Action>> stateStack = new Stack<DfaState<Action>>();

        /// <summary>
        /// Parse a regular expression.
        /// </summary>
        /// <param name="str">a string containing the expression to parse</param>
        /// <param name="caseIndependent">true to make it case independent</param>
        /// <returns>a <see cref="IMatchable"/> that implements the regular expression</returns>
        public static IMatchable Parse(string str, bool caseIndependent = false)
        {
            return new RegexParser(str, caseIndependent).Parse();
        }

        private RegexParser(string str, bool caseI)
        {
            src = str;
            this.caseI = caseI;
        }

        private IMatchable Parse()
        {
            stateStack.Clear();
            valStack.Clear();
            symStack.Clear();
            readPos = 0;
            stateStack.Push(DFA);
            var srclen = src.Length;
            var maxpos = 0;
            for (;;)
            {
                // Match up to the end of the recognized symbol stack.  If we can't do
                // this, then there's a bug and we've reduced something we shouldn't have
                var st = stateStack.Peek();
                while (stateStack.Count - 1 < symStack.Length)
                {
                    st = st.GetNextState(symStack[stateStack.Count - 1]);
                    if (st == null)
                    {
                        throw new Exception($"Internal bug encountered parsing regular expression: {src}");
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
                        lexState = lexState.GetNextState(src[i]);
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

                action(this);
            }

            if (!"S".Equals(symStack.ToString()))
            {
                throw new ArgumentException($"Invald regular expression: \"{src}\" has error at position {maxpos}");
            }

            System.Diagnostics.Debug.Assert(valStack.Count == 1);
            return valStack.Pop();
        }

        private void Push(string codes, IMatchable pattern)
        {
            symStack.Append(codes);
            while (valStack.Count < symStack.Length)
            {
                valStack.Push(pattern);
            }
        }

        private IMatchable Pop(int nonterms)
        {
            IMatchable ret = null;
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
            return src[readPos - 1];
        }

        private char ParseCharEscape()
        {
            var spos = readPos - 1;
            for (; spos > 0 && src[spos - 1] != '\\'; --spos)
            { }

            try
            {
                var ch = src[spos];
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
                        return (char) uint.Parse(src.Substring(spos + 1, readPos - spos - 1), NumberStyles.HexNumber);

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

            throw new Exception($"Invalid character escape \\{src.Substring(spos, 1)}");
        }

        private CharRange ParseClassEscape()
        {
            var spos = readPos - 1;
            for (; spos > 0 && src[spos - 1] != '\\'; --spos)
            { }

            switch (src[spos])
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

            throw new Exception($"Invalid class escape \\{src.Substring(spos, 1)}");
        }

        // Build a DFA that matches a parse stack from the bottom to produce the next LR(1) action to perform.
        // The stack is of the form XXX:ccc, where XXX are previously recognized symbols, and ccc are all the
        // remaining characters in the input.
        private static DfaState<Action> BuildParserDfa()
        {
            var bld = new DfaBuilder<Action>();
            // S can be the whole expression, or group contents
            var Spos = Pattern.MaybeRepeat(Pattern.MaybeRepeat(CharRange.AnyOf("SCA|")).Then("("));

            // S: C | S '|' C
            bld.AddPattern(Spos.Then("C"), parser => parser.Push("S", parser.Pop(1)));
            bld.AddPattern(Spos.Then("S:|"), parser => parser.Push("|", null));
            bld.AddPattern(Spos.Then("S|C"),
                parser =>
                {
                    var p1 = parser.Pop(2);
                    var p2 = parser.Pop(1);
                    parser.Push("S", Pattern.AnyOf(p1, p2));
                });
            var Cpos = Spos.ThenMaybe("S|");

            // C: e | C A
            bld.AddPattern(Cpos, parser => parser.Push("C", Pattern.Empty));
            bld.AddPattern(Cpos.Then("CA"),
                parser =>
                {
                    var p2 = parser.Pop(1);
                    var p1 = parser.Pop(1);
                    parser.Push("C", Pattern.Match(p1).Then(p2));
                });
            var Apos = Cpos.Then("C");

            //A: A? | A+ | A*
            bld.AddPattern(Apos.Then("A:?"), parser => parser.Push("A", Pattern.Maybe(parser.Pop(1))));
            bld.AddPattern(Apos.Then("A:+"), parser => parser.Push("A", Pattern.Repeat(parser.Pop(1))));
            bld.AddPattern(Apos.Then("A:*"), parser => parser.Push("A", Pattern.MaybeRepeat(parser.Pop(1))));

            //A: GROUP
            bld.AddPattern(Apos.Then(":("), parser => parser.Push("(", null));
            bld.AddPattern(Apos.Then("(S:)"), parser => parser.Push("A", parser.Pop(2)));

            //A: literal | .
            bld.AddPattern(Apos.Then(":").Then(CharRange.CreateBuilder().AddChars(".()[]+*?|\\").Invert().Build()),
                parser =>
                {
                    CharRange range;
                    var c = parser.LastChar();
                    if (!parser.caseI)
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

                    parser.Push("A", range);
                });
            bld.AddPattern(Apos.Then(":."), parser => parser.Push("A", CharRange.All));

            var charEscape = Pattern.Match(":\\").Then(Pattern.AnyOf(
                Pattern.Match("x").Then(CharRange.HexDigits).Then(CharRange.HexDigits),
                Pattern.Match("u").Then(CharRange.HexDigits).Then(CharRange.HexDigits).Then(CharRange.HexDigits)
                    .Then(CharRange.HexDigits),
                Pattern.Match("c").Then(CharRange.CreateBuilder().AddRange('a', 'z').AddRange('A', 'Z').Build()),
                CharRange.CreateBuilder().AddChars("xucdDwWsS").Invert().Build()));
            var classEscape = Pattern.Match(":\\").Then(Pattern.AnyCharIn("dDsSwW"));

            bld.AddPattern(Apos.Then(charEscape), parser => parser.Push("A", CharRange.Single(parser.ParseCharEscape())));
            bld.AddPattern(Apos.Then(classEscape), parser => parser.Push("A", parser.ParseClassEscape()));

            //A: [R] | [^R]
            bld.AddPattern(Apos.Then(":[^"),
                parser =>
                {
                    parser.charBuilder.Clear();
                    parser.Push("[^", null);
                });
            bld.AddPattern(Apos.Then(":["),
                parser =>
                {
                    parser.charBuilder.Clear();
                    parser.Push("[", null);
                });
            bld.AddPattern(Apos.Then("[R:]"),
                parser =>
                {
                    parser.Pop(2);
                    if (parser.caseI)
                    {
                        parser.charBuilder.ExpandCases();
                    }

                    parser.Push("A", parser.charBuilder.Build());
                });
            bld.AddPattern(Apos.Then("[^R:]"),
                parser =>
                {
                    parser.Pop(3);
                    if (parser.caseI)
                    {
                        parser.charBuilder.ExpandCases();
                    }

                    parser.Push("A", parser.charBuilder.Invert().Build());
                });
            var Rpos = Apos.Then(Pattern.AnyOf("[^", "["));

            //R: e | R classEscape | R c | R c - c
            bld.AddPattern(Rpos, parser => parser.Push("R", null));
            bld.AddPattern(Rpos.Then("R").Then(classEscape),
                parser =>
                {
                    parser.charBuilder.AddRange(parser.ParseClassEscape());
                    parser.Pop(0);
                });
            bld.AddPattern(Rpos.Then("Rc"),
                parser =>
                {
                    parser.Pop(1);
                    parser.charBuilder.AddRange(parser.clast, parser.clast);
                });
            bld.AddPattern(Rpos.Then("Rc:-"), parser => parser.Push("-", null));
            bld.AddPattern(Rpos.Then("Rc-c"),
                parser =>
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
            var cpos = Rpos.Then("R").ThenMaybe("c-");

            //class chars
            bld.AddPattern(cpos.Then(":").Then(CharRange.CreateBuilder().AddChars("-[]\\").Invert().Build()),
                parser =>
                {
                    parser.cprev = parser.clast;
                    parser.clast = parser.LastChar();
                    parser.Push("c", null);
                });
            bld.AddPattern(cpos.Then(charEscape),
                parser =>
                {
                    parser.cprev = parser.clast;
                    parser.clast = parser.ParseCharEscape();
                    parser.Push("c", null);
                });

            return bld.Build(null);
        }

        private delegate void Action(RegexParser parser);
    }
}
