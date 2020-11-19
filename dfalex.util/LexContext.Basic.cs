using System;
using System.Text;

namespace dfalex.util
{
    public static class LexContextBasic
    {
        /// <summary>
        /// Verifies that one of the specified characters is under the input cursor. If it isn't, a <see cref="ExpectingException" /> is raised.
        /// </summary>
        /// <param name="expecting">The list of expected characters. If empty, anything but end of input is accepted. If <see cref="LexContext.EndOfInput" /> is included, end of input is accepted.</param>
        [System.Diagnostics.DebuggerHidden()]
        public static void Expecting(this LexContext lc, params int[] expecting)
        {
            lc.CheckDisposed();
            if (lc.Current == LexContext.BeforeInput)
            {
                throw new ExpectingException("The cursor is before the beginning of the input", lc.Location);
            }

            switch (expecting.Length)
            {
                case 0:
                    if (lc.Current == LexContext.EndOfInput)
                    {
                        throw new ExpectingException("Unexpected end of input", lc.Location);
                    }

                    break;

                case 1:
                    if (lc.Current != expecting[0])
                    {
                        throw new ExpectingException(_GetErrorMessage(lc, expecting), lc.Location, _GetErrorExpecting(expecting));
                    }

                    break;

                default:
                    if (Array.IndexOf(expecting, lc.Current) < 0)
                    {
                        throw new ExpectingException(_GetErrorMessage(lc, expecting), lc.Location, _GetErrorExpecting(expecting));
                    }

                    break;
            }
        }

        private static string _GetErrorMessage(LexContext lc, int[] expecting)
        {
            StringBuilder sb;
            switch (expecting.Length)
            {
                case 0:
                    if (lc.Current == LexContext.EndOfInput)
                    {
                        return "Unexpected end of input";
                    }
                    return string.Concat("Unexpected character \"", (char) lc.Current, "\" in input");

                case 1:
                    sb = new StringBuilder()
                        .AppendExpectedChar(expecting[0]);
                    break;

                case 2:
                    sb = new StringBuilder()
                         .AppendExpectedChar(expecting[0])
                         .Append(" or ")
                         .AppendExpectedChar(expecting[1]);
                    break;

                default: // length > 2
                    sb = new StringBuilder()
                        .AppendExpectedChar(expecting[0]);
                    var l = expecting.Length - 1;
                    for (var i = 1; i < l; ++i)
                    {
                        sb.Append(", ").AppendExpectedChar(expecting[i]);
                    }

                    sb.Append(", or ").AppendExpectedChar(expecting[^1]);
                    break;
            }

            if (lc.Current == LexContext.EndOfInput)
            {
                return string.Concat("Unexpected end of input. Expecting ", sb.ToString());
            }

            if (expecting.Length == 0)
            {
                return string.Concat("Unexpected character \"", (char) lc.Current, "\" in input");
            }

            return string.Concat("Unexpected character \"", (char) lc.Current, "\" in input. Expecting ", sb.ToString());
        }

        private static StringBuilder AppendExpectedChar(this StringBuilder sb, int expecting)
        {
            if (expecting == LexContext.EndOfInput)
            {
                sb.Append("end of input");
            }
            else
            {
                sb.Append('"').Append((char) expecting).Append('"');
            }

            return sb;
        }

        private static string[] _GetErrorExpecting(int[] expecting)
        {
            var result = new string[expecting.Length];
            for (var i = 0; i < expecting.Length; ++i)
            {
                if (expecting[i] != LexContext.EndOfInput)
                {
                    result[i] = Convert.ToString(expecting[i]);
                }
                else
                {
                    result[i] = "end of input";
                }
            }

            return result;
        }

        /// <summary>
        /// Attempts to read whitespace from the lc.Current input, capturing it
        /// </summary>
        /// <returns>True if whitespace was read, otherwise false</returns>
        public static bool TryReadWhiteSpace(this LexContext lc)
        {
            lc.EnsureStarted();
            if (-1 == lc.Current || !char.IsWhiteSpace((char) lc.Current))
            {
                return false;
            }

            lc.Capture();
            while (-1 != lc.Advance() && char.IsWhiteSpace((char) lc.Current))
            {
                lc.Capture();
            }

            return true;
        }

        /// <summary>
        /// Attempts to skip whitespace in the lc.Current input without capturing it
        /// </summary>
        /// <returns>True if whitespace was skipped, otherwise false</returns>
        public static bool TrySkipWhiteSpace(this LexContext lc)
        {
            lc.EnsureStarted();
            if (-1 == lc.Current || !char.IsWhiteSpace((char) lc.Current))
            {
                return false;
            }

            while (-1 != lc.Advance() && char.IsWhiteSpace((char) lc.Current))
            { }

            return true;
        }

        /// <summary>
        /// Attempts to read up until the specified character, optionally consuming it
        /// </summary>
        /// <param name="character">The character to halt at</param>
        /// <param name="readCharacter">True if the character should be consumed, otherwise false</param>
        /// <returns>True if the character was found, otherwise false</returns>
        public static bool TryReadUntil(this LexContext lc, int character, bool readCharacter = true)
        {
            lc.EnsureStarted();
            if (0 > character)
            {
                character = -1;
            }

            lc.Capture();
            if (lc.Current == character)
            {
                return true;
            }

            while (-1 != lc.Advance() && lc.Current != character)
            {
                lc.Capture();
            }

            //
            if (lc.Current == character)
            {
                if (readCharacter)
                {
                    lc.Capture();
                    lc.Advance();
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to skip up until the specified character, optionally consuming it
        /// </summary>
        /// <param name="character">The character to halt at</param>
        /// <param name="skipCharacter">True if the character should be consumed, otherwise false</param>
        /// <returns>True if the character was found, otherwise false</returns>
        public static bool TrySkipUntil(this LexContext lc, int character, bool skipCharacter = true)
        {
            lc.EnsureStarted();
            if (0 > character)
            {
                character = -1;
            }

            if (lc.Current == character)
            {
                return true;
            }

            while (-1 != lc.Advance() && lc.Current != character)
            { }

            if (lc.Current == character)
            {
                if (skipCharacter)
                {
                    lc.Advance();
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to read up until the specified character, using the specified escape, optionally consuming it
        /// </summary>
        /// <param name="character">The character to halt at</param>
        /// <param name="escapeChar">The escape indicator character to use</param>
        /// <param name="readCharacter">True if the character should be consumed, otherwise false</param>
        /// <returns>True if the character was found, otherwise false</returns>
        public static bool TryReadUntil(this LexContext lc, int character, int escapeChar, bool readCharacter = true)
        {
            lc.EnsureStarted();
            if (0 > character)
            {
                character = -1;
            }

            if (-1 == lc.Current)
            {
                return false;
            }

            if (lc.Current == character)
            {
                if (readCharacter)
                {
                    lc.Capture();
                    lc.Advance();
                }

                return true;
            }

            do
            {
                if (escapeChar == lc.Current)
                {
                    lc.Capture();
                    if (-1 == lc.Advance())
                    {
                        return false;
                    }

                    lc.Capture();
                }
                else
                {
                    if (character == lc.Current)
                    {
                        if (readCharacter)
                        {
                            lc.Capture();
                            lc.Advance();
                        }

                        return true;
                    }

                    lc.Capture();
                }
            } while (-1 != lc.Advance());

            return false;
        }

        /// <summary>
        /// Attempts to skip up until the specified character, using the specified escape, optionally consuming it
        /// </summary>
        /// <param name="character">The character to halt at</param>
        /// <param name="escapeChar">The escape indicator character to use</param>
        /// <param name="skipCharacter">True if the character should be consumed, otherwise false</param>
        /// <returns>True if the character was found, otherwise false</returns>
        public static bool TrySkipUntil(this LexContext lc, int character, int escapeChar, bool skipCharacter = true)
        {
            lc.EnsureStarted();
            if (0 > character)
            {
                character = -1;
            }

            if (lc.Current == character)
            {
                return true;
            }

            while (-1 != lc.Advance() && lc.Current != character)
            {
                if (character == escapeChar)
                {
                    if (-1 == lc.Advance())
                    {
                        break;
                    }
                }
            }

            if (lc.Current == character)
            {
                if (skipCharacter)
                {
                    lc.Advance();
                }

                return true;
            }

            return false;
        }

        private static bool _ContainsChar(char[] chars, char ch)
        {
            foreach (var cmp in chars)
            {
                if (cmp == ch)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to read until any of the specified characters, optionally consuming it
        /// </summary>
        /// <param name="readCharacter">True if the character should be consumed, otherwise false</param>
        /// <param name="anyOf">A list of characters that signal the end of the scan</param>
        /// <returns>True if one of the characters was found, otherwise false</returns>
        public static bool TryReadUntil(this LexContext lc, bool readCharacter = true, params char[]? anyOf)
        {
            lc.EnsureStarted();
            anyOf ??= Array.Empty<char>();

            lc.Capture();
            if (-1 != lc.Current && _ContainsChar(anyOf, (char) lc.Current))
            {
                if (readCharacter)
                {
                    lc.Capture();
                    lc.Advance();
                }

                return true;
            }

            while (-1 != lc.Advance() && !_ContainsChar(anyOf, (char) lc.Current))
            {
                lc.Capture();
            }

            if (-1 != lc.Current && _ContainsChar(anyOf, (char) lc.Current))
            {
                if (readCharacter)
                {
                    lc.Capture();
                    lc.Advance();
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to skip until any of the specified characters, optionally consuming it
        /// </summary>
        /// <param name="skipCharacter">True if the character should be consumed, otherwise false</param>
        /// <param name="anyOf">A list of characters that signal the end of the scan</param>
        /// <returns>True if one of the characters was found, otherwise false</returns>
        public static bool TrySkipUntil(this LexContext lc, bool skipCharacter = true, params char[]? anyOf)
        {
            lc.EnsureStarted();
            anyOf ??= Array.Empty<char>();

            if (-1 != lc.Current && _ContainsChar(anyOf, (char) lc.Current))
            {
                if (skipCharacter)
                {
                    lc.Advance();
                }

                return true;
            }

            while (-1 != lc.Advance() && !_ContainsChar(anyOf, (char) lc.Current))
            { }

            if (-1 != lc.Current && _ContainsChar(anyOf, (char) lc.Current))
            {
                if (skipCharacter)
                {
                    lc.Advance();
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Reads up to the specified text string, consuming it
        /// </summary>
        /// <param name="text">The text to read until</param>
        /// <returns>True if the text was found, otherwise false</returns>
        public static bool TryReadUntil(this LexContext lc, string text)
        {
            lc.EnsureStarted();
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            while (-1 != lc.Current && TryReadUntil(lc, text[0], false))
            {
                var found = true;
                for (var i = 1; i < text.Length; ++i)
                {
                    if (lc.Advance() != text[i])
                    {
                        found = false;
                        break;
                    }

                    lc.Capture();
                }

                if (found)
                {
                    lc.Advance();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Skips up to the specified text string, consuming it
        /// </summary>
        /// <param name="text">The text to skip until</param>
        /// <returns>True if the text was found, otherwise false</returns>
        public static bool TrySkipUntil(this LexContext lc, string text)
        {
            lc.EnsureStarted();
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            while (-1 != lc.Current && TrySkipUntil(lc, text[0], false))
            {
                var found = true;
                for (var i = 1; i < text.Length; ++i)
                {
                    if (lc.Advance() != text[i])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    lc.Advance();
                    return true;
                }
            }

            return false;
        }
    }
}
