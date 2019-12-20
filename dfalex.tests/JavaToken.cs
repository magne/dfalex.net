using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using static CodeHive.DfaLex.Pattern;

namespace CodeHive.DfaLex.Tests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum JavaToken
    {
        /* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
         * Ported from JFlex 1.6.1 Java Example                                    *
         * Copyright (C) 1998-2015  Gerwin Klein <lsf@jflex.de>                    *
         * All rights reserved.                                                    *
         *                                                                         *
         * License: BSD                                                            *
         *                                                                         *
         * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

        /* keywords */
        ABSTRACT,
        BOOLEAN,
        BREAK,
        BYTE,
        CASE,
        CATCH,
        CHAR,
        CLASS,
        CONST,
        CONTINUE,
        DO,
        DOUBLE,
        ELSE,
        EXTENDS,
        FINAL,
        FINALLY,
        FLOAT,
        FOR,
        DEFAULT,
        IMPLEMENTS,
        IMPORT,
        INSTANCEOF,
        INT,
        INTERFACE,
        LONG,
        NATIVE,
        NEW,
        GOTO,
        IF,
        PUBLIC,
        SHORT,
        SUPER,
        SWITCH,
        SYNCHRONIZED,
        PACKAGE,
        PRIVATE,
        PROTECTED,
        TRANSIENT,
        RETURN,
        VOID,
        STATIC,
        WHILE,
        THIS,
        THROW,
        THROWS,
        TRY,
        VOLATILE,
        STRICTFP,

        /* literals */
        NULL,
        TRUE,
        FALSE,

        /* separators */
        LPAREN,
        RPAREN,
        LBRACE,
        RBRACE,
        LBRACK,
        RBRACK,
        SEMICOLON,
        COMMA,
        DOT,

        /* operators */
        EQ,
        GT,
        LT,
        NOT,
        COMP,
        QUESTION,
        COLON,
        EQEQ,
        LTEQ,
        GTEQ,
        NOTEQ,
        ANDAND,
        OROR,
        PLUSPLUS,
        MINUSMINUS,
        PLUS,
        MINUS,
        MULT,
        DIV,
        AND,
        OR,
        XOR,
        MOD,
        LSHIFT,
        RSHIFT,
        URSHIFT,
        PLUSEQ,
        MINUSEQ,
        MULTEQ,
        DIVEQ,
        ANDEQ,
        OREQ,
        XOREQ,
        MODEQ,
        LSHIFTEQ,
        RSHIFTEQ,
        URSHIFTEQ,

        STRING_LITERAL,

        CHARACTER_LITERAL,

        INTEGER_LITERAL,

        LONG_LITERAL,

        DOUBLE_LITERAL,
        FLOAT_LITERAL
    }

    internal static class JavaTokenExtension
    {
        private static readonly IMatchable StringEscape = Match("\\").Then(AnyOf( //escapes
                                                                                 AnyCharIn("btnfr\"\'\\"), //single char escapes
                                                                                 Regex("[0-3]?[0-7]?[0-7]") //octal escape
                                                                                ));

        private static readonly Pattern DigitsWithDecimal = AnyOf(Repeat(CharRange.Digits).Then(".").ThenMaybeRepeat(CharRange.Digits),
                                                                  Match(".").ThenRepeat(CharRange.Digits)
                                                                 );

        private static readonly Pattern Exponent = AnyCharIn("eE").Then(AnyCharIn("+-")).ThenRepeat(CharRange.Digits);

        private static IDictionary<JavaToken, Pattern> patterns;

        private static bool initialized;

        private static readonly object LockObject = new object();

        private static void Initialize()
        {
            patterns = new Dictionary<JavaToken, Pattern>
            {
                /* keywords */
                { JavaToken.ABSTRACT, Match("abstract") },
                { JavaToken.BOOLEAN, Match("boolean") },
                { JavaToken.BREAK, Match("break") },
                { JavaToken.BYTE, Match("byte") },
                { JavaToken.CASE, Match("case") },
                { JavaToken.CATCH, Match("catch") },
                { JavaToken.CHAR, Match("char") },
                { JavaToken.CLASS, Match("class") },
                { JavaToken.CONST, Match("const") },
                { JavaToken.CONTINUE, Match("continue") },
                { JavaToken.DO, Match("do") },
                { JavaToken.DOUBLE, Match("double") },
                { JavaToken.ELSE, Match("else") },
                { JavaToken.EXTENDS, Match("extends") },
                { JavaToken.FINAL, Match("final") },
                { JavaToken.FINALLY, Match("finally") },
                { JavaToken.FLOAT, Match("float") },
                { JavaToken.FOR, Match("for") },
                { JavaToken.DEFAULT, Match("default") },
                { JavaToken.IMPLEMENTS, Match("implements") },
                { JavaToken.IMPORT, Match("import") },
                { JavaToken.INSTANCEOF, Match("instanceof") },
                { JavaToken.INT, Match("int") },
                { JavaToken.INTERFACE, Match("interface") },
                { JavaToken.LONG, Match("long") },
                { JavaToken.NATIVE, Match("native") },
                { JavaToken.NEW, Match("new") },
                { JavaToken.GOTO, Match("goto") },
                { JavaToken.IF, Match("if") },
                { JavaToken.PUBLIC, Match("public") },
                { JavaToken.SHORT, Match("short") },
                { JavaToken.SUPER, Match("super") },
                { JavaToken.SWITCH, Match("switch") },
                { JavaToken.SYNCHRONIZED, Match("synchronized") },
                { JavaToken.PACKAGE, Match("package") },
                { JavaToken.PRIVATE, Match("private") },
                { JavaToken.PROTECTED, Match("protected") },
                { JavaToken.TRANSIENT, Match("transient") },
                { JavaToken.RETURN, Match("return") },
                { JavaToken.VOID, Match("void") },
                { JavaToken.STATIC, Match("static") },
                { JavaToken.WHILE, Match("while") },
                { JavaToken.THIS, Match("this") },
                { JavaToken.THROW, Match("throw") },
                { JavaToken.THROWS, Match("throws") },
                { JavaToken.TRY, Match("try") },
                { JavaToken.VOLATILE, Match("volatile") },
                { JavaToken.STRICTFP, Match("strictfp") },

                /* literals */
                { JavaToken.NULL, Match("null") },
                { JavaToken.TRUE, Match("true") },
                { JavaToken.FALSE, Match("false") },

                /* separators */
                { JavaToken.LPAREN, Match("(") },
                { JavaToken.RPAREN, Match(")") },
                { JavaToken.LBRACE, Match("{") },
                { JavaToken.RBRACE, Match("}") },
                { JavaToken.LBRACK, Match("[") },
                { JavaToken.RBRACK, Match("]") },
                { JavaToken.SEMICOLON, Match(";") },
                { JavaToken.COMMA, Match(",") },
                { JavaToken.DOT, Match(".") },

                /* operators */
                { JavaToken.EQ, Match("=") },
                { JavaToken.GT, Match(">") },
                { JavaToken.LT, Match("<") },
                { JavaToken.NOT, Match("!") },
                { JavaToken.COMP, Match("~") },
                { JavaToken.QUESTION, Match("?") },
                { JavaToken.COLON, Match(":") },
                { JavaToken.EQEQ, Match("==") },
                { JavaToken.LTEQ, Match("<=") },
                { JavaToken.GTEQ, Match(">=") },
                { JavaToken.NOTEQ, Match("!=") },
                { JavaToken.ANDAND, Match("&&") },
                { JavaToken.OROR, Match("||") },
                { JavaToken.PLUSPLUS, Match("++") },
                { JavaToken.MINUSMINUS, Match("--") },
                { JavaToken.PLUS, Match("+") },
                { JavaToken.MINUS, Match("-") },
                { JavaToken.MULT, Match("*") },
                { JavaToken.DIV, Match("/") },
                { JavaToken.AND, Match("&") },
                { JavaToken.OR, Match("|") },
                { JavaToken.XOR, Match("^") },
                { JavaToken.MOD, Match("%") },
                { JavaToken.LSHIFT, Match("<<") },
                { JavaToken.RSHIFT, Match(">>") },
                { JavaToken.URSHIFT, Match(">>>") },
                { JavaToken.PLUSEQ, Match("+=") },
                { JavaToken.MINUSEQ, Match("-=") },
                { JavaToken.MULTEQ, Match("*=") },
                { JavaToken.DIVEQ, Match("/=") },
                { JavaToken.ANDEQ, Match("&=") },
                { JavaToken.OREQ, Match("|=") },
                { JavaToken.XOREQ, Match("^=") },
                { JavaToken.MODEQ, Match("%=") },
                { JavaToken.LSHIFTEQ, Match("<<=") },
                { JavaToken.RSHIFTEQ, Match(">>=") },
                { JavaToken.URSHIFTEQ, Match(">>>=") },

                {
                    JavaToken.STRING_LITERAL, Match("\"").ThenMaybeRepeat(AnyOf(
                                                                                CharRange.CreateBuilder().AddChars("\r\n\"\\").Invert().Build(), // literal string char
                                                                                StringEscape
                                                                               ))
                                                         .Then("\"")
                },

                {
                    JavaToken.CHARACTER_LITERAL, Match("\'").Then(AnyOf(
                                                                        CharRange.CreateBuilder().AddChars("\r\n\'\\").Invert().Build(), //literal char
                                                                        StringEscape
                                                                       ))
                                                            .Then("\'")
                },

                { JavaToken.INTEGER_LITERAL, Regex(@"[+\-]?(0|[1-9][0-9]*|0[0-7]+|0[xX][0-9a-fA-F]+)") },

                {
                    // JavaToken.LONG_LITERAL, null // JavaToken.INTEGER_LITERAL.Pattern().Then(AnyCharIn("lL"))
                    JavaToken.LONG_LITERAL, Regex(@"[+\-]?(0|[1-9][0-9]*|0[0-7]+|0[xX][0-9a-fA-F]+)").Then(AnyCharIn("lL"))
                },

                {
                    JavaToken.DOUBLE_LITERAL, Maybe(AnyCharIn("+-")).Then(AnyOf(
                                                                                DigitsWithDecimal.ThenMaybe(Exponent),
                                                                                Repeat(CharRange.Digits).Then(Exponent)
                                                                               ))
                                                                    .ThenMaybe(AnyCharIn("dD"))
                },

                {
                    JavaToken.FLOAT_LITERAL, AnyCharIn("+-").Then(AnyOf(
                                                                        DigitsWithDecimal.ThenMaybe(Exponent),
                                                                        Repeat(CharRange.Digits).Then(Exponent)
                                                                       ))
                                                            .Then(AnyCharIn("fF"))
                }
            };
        }

        public static Pattern Pattern(this JavaToken token)
        {
            if (!initialized)
            {
                lock (LockObject)
                {
                    if (!initialized)
                    {
                        Initialize();
                        initialized = true;
                    }
                }
            }

            if (patterns.TryGetValue(token, out var pattern))
            {
                return pattern;
            }

            throw new ArgumentException($"Did not find token {token}");
        }
    }
}
