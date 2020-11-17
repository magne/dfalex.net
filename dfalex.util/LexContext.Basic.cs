using System;

namespace dfalex.util
{
    public partial class LexContext
    {
        /// <summary>
        /// Attempts to read whitespace from the current input, capturing it
        /// </summary>
        /// <returns>True if whitespace was read, otherwise false</returns>
        public bool TryReadWhiteSpace()
        {
            EnsureStarted();
            if (-1 == Current || !char.IsWhiteSpace((char) Current))
            {
                return false;
            }

            Capture();
            while (-1 != Advance() && char.IsWhiteSpace((char) Current))
            {
                Capture();
            }

            return true;
        }

        /// <summary>
        /// Attempts to skip whitespace in the current input without capturing it
        /// </summary>
        /// <returns>True if whitespace was skipped, otherwise false</returns>
        public bool TrySkipWhiteSpace()
        {
            EnsureStarted();
            if (-1 == Current || !char.IsWhiteSpace((char) Current))
            {
                return false;
            }

            while (-1 != Advance() && char.IsWhiteSpace((char) Current))
            { }

            return true;
        }

        /// <summary>
        /// Attempts to read up until the specified character, optionally consuming it
        /// </summary>
        /// <param name="character">The character to halt at</param>
        /// <param name="readCharacter">True if the character should be consumed, otherwise false</param>
        /// <returns>True if the character was found, otherwise false</returns>
        public bool TryReadUntil(int character, bool readCharacter = true)
        {
            EnsureStarted();
            if (0 > character)
            {
                character = -1;
            }

            Capture();
            if (Current == character)
            {
                return true;
            }

            while (-1 != Advance() && Current != character)
            {
                Capture();
            }

            //
            if (Current == character)
            {
                if (readCharacter)
                {
                    Capture();
                    Advance();
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
        public bool TrySkipUntil(int character, bool skipCharacter = true)
        {
            EnsureStarted();
            if (0 > character)
            {
                character = -1;
            }

            if (Current == character)
            {
                return true;
            }

            while (-1 != Advance() && Current != character)
            { }

            if (Current == character)
            {
                if (skipCharacter)
                {
                    Advance();
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
        public bool TryReadUntil(int character, int escapeChar, bool readCharacter = true)
        {
            EnsureStarted();
            if (0 > character)
            {
                character = -1;
            }

            if (-1 == Current)
            {
                return false;
            }

            if (Current == character)
            {
                if (readCharacter)
                {
                    Capture();
                    Advance();
                }

                return true;
            }

            do
            {
                if (escapeChar == Current)
                {
                    Capture();
                    if (-1 == Advance())
                    {
                        return false;
                    }

                    Capture();
                }
                else
                {
                    if (character == Current)
                    {
                        if (readCharacter)
                        {
                            Capture();
                            Advance();
                        }

                        return true;
                    }

                    Capture();
                }
            } while (-1 != Advance());

            return false;
        }

        /// <summary>
        /// Attempts to skip up until the specified character, using the specified escape, optionally consuming it
        /// </summary>
        /// <param name="character">The character to halt at</param>
        /// <param name="escapeChar">The escape indicator character to use</param>
        /// <param name="skipCharacter">True if the character should be consumed, otherwise false</param>
        /// <returns>True if the character was found, otherwise false</returns>
        public bool TrySkipUntil(int character, int escapeChar, bool skipCharacter = true)
        {
            EnsureStarted();
            if (0 > character)
            {
                character = -1;
            }

            if (Current == character)
            {
                return true;
            }

            while (-1 != Advance() && Current != character)
            {
                if (character == escapeChar)
                {
                    if (-1 == Advance())
                    {
                        break;
                    }
                }
            }

            if (Current == character)
            {
                if (skipCharacter)
                {
                    Advance();
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
        public bool TryReadUntil(bool readCharacter = true, params char[]? anyOf)
        {
            EnsureStarted();
            anyOf ??= Array.Empty<char>();

            Capture();
            if (-1 != Current && _ContainsChar(anyOf, (char) Current))
            {
                if (readCharacter)
                {
                    Capture();
                    Advance();
                }

                return true;
            }

            while (-1 != Advance() && !_ContainsChar(anyOf, (char) Current))
            {
                Capture();
            }

            if (-1 != Current && _ContainsChar(anyOf, (char) Current))
            {
                if (readCharacter)
                {
                    Capture();
                    Advance();
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
        public bool TrySkipUntil(bool skipCharacter = true, params char[]? anyOf)
        {
            EnsureStarted();
            anyOf ??= Array.Empty<char>();

            if (-1 != Current && _ContainsChar(anyOf, (char) Current))
            {
                if (skipCharacter)
                {
                    Advance();
                }

                return true;
            }

            while (-1 != Advance() && !_ContainsChar(anyOf, (char) Current))
            { }

            if (-1 != Current && _ContainsChar(anyOf, (char) Current))
            {
                if (skipCharacter)
                {
                    Advance();
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
        public bool TryReadUntil(string text)
        {
            EnsureStarted();
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            while (-1 != Current && TryReadUntil(text[0], false))
            {
                var found = true;
                for (var i = 1; i < text.Length; ++i)
                {
                    if (Advance() != text[i])
                    {
                        found = false;
                        break;
                    }

                    Capture();
                }

                if (found)
                {
                    Advance();
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
        public bool TrySkipUntil(string text)
        {
            EnsureStarted();
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            while (-1 != Current && TrySkipUntil(text[0], false))
            {
                var found = true;
                for (var i = 1; i < text.Length; ++i)
                {
                    if (Advance() != text[i])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    Advance();
                    return true;
                }
            }

            return false;
        }
    }
}
