using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace dfalex.util
{
    /// <summary>
    /// Represents a location in the input source.
    /// </summary>
    /// <param name="Line">The one based line number</param>
    /// <param name="Column">The one based column number</param>
    /// <param name="Position">The zero based position</param>
    /// <param name="Source">The file or URL</param>
    public sealed record Location(int Line, int Column, long Position, object? Source)
    {
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("line ").Append(Line).Append(", column ").Append(Column).Append(", position ").Append(Position);
            if (Source != null)
            {
                sb.Append(", in ").Append(Source);
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Represents an exception encountered while lexing or parsing from an input source.
    /// </summary>
    public class ExpectingException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="ExpectingException" />
        /// </summary>
        /// <param name="message">The error message - this will be appended with the location information</param>
        /// <param name="location">The location where the exception occured</param>
        /// <param name="expecting">A list of expected symbols or characters</param>
        public ExpectingException(string message, Location location, params string[] expecting)
            : base(GetMessage(message, location))
        {
            Location = location;
            Expecting = expecting;
        }

        /// <summary>
        /// Indicates the location where the exception occured
        /// </summary>
        public Location Location { get; }

        /// <summary>
        /// Indicates a list of expecting characters or symbols
        /// </summary>
        public string[] Expecting { get; }

        private static string GetMessage(string message, Location location)
        {
            return new StringBuilder().Append(message).Append(" at ").Append(location).ToString();
        }
    }

    /// <summary>
    /// Provides error reporting, location tracking, lifetime and start/end management over an input cursor.
    /// </summary>
    public abstract partial class LexContext : IDisposable
    {
        /// <summary>
        /// Indicates the default tab width of an input device
        /// </summary>
        public const int     DefaultTabWidth = 4;

        /// <summary>
        /// Represents the end of input symbol
        /// </summary>
        public const int     EndOfInput      = -1;

        /// <summary>
        /// Represents the before input symbol
        /// </summary>
        public const int     BeforeInput     = -2;

        /// <summary>
        /// Represents a symbol for the disposed state
        /// </summary>
        public const int     Disposed        = -3;

        private      int     current         = BeforeInput;
        private      int     line;
        private      int     column;
        private      long    position;
        private      string? fileOrUrl;

        /// <summary>
        /// Indicates the tab width of the input device
        /// </summary>
        public int TabWidth { get; set; } = DefaultTabWidth;

        /// <summary>
        /// Indicates the current one based line number
        /// </summary>
        public int Line => line;

        /// <summary>
        /// Indicates the current one based column number
        /// </summary>
        public int Column => column;

        /// <summary>
        /// Indicates the current zero based position
        /// </summary>
        public long Position => position;

        /// <summary>
        /// Indicates the current filename or URL, if any could be discerned
        /// </summary>
        public string? FileOrUrl => fileOrUrl;

        /// <summary>
        /// Provides access to the capture buffer, a <see cref="StringBuilder" />
        /// </summary>
        public StringBuilder CaptureBuffer { get; } = new();

        /// <summary>
        /// Gets the current character under the cursor or <see cref="BeforeInput"/>, <see cref="EndOfInput" />, or <see cref="Disposed" />
        /// </summary>
        public int Current => current;

        internal LexContext()
        {
            line = 1;
            column = 0;
            position = 0L;
        }

        ~LexContext()
        {
            Close();
        }

        /// <summary>
        /// Creates a <see cref="LexContext" /> over an enumeration of characters, which can be a string, character array, or other source
        /// </summary>
        /// <param name="input">The input characters</param>
        /// <returns>A new <see cref="LexContext" /> over the input</returns>
        public static LexContext Create(IEnumerable<char> input)
        {
            return new CharEnumeratorContext(input);
        }

        /// <summary>
        /// Creates a <see cref="LexContext" /> over a <see cref="TextReader"/>
        /// </summary>
        /// <param name="input">The input reader</param>
        /// <returns>A new <see cref="LexContext" /> over the input</returns>
        public static LexContext CreateFrom(TextReader input)
        {
            // try to get a filename off the text reader
            string fn = null;
            var sr = input as StreamReader;
            if (null != sr)
            {
                var fstm = sr.BaseStream as FileStream;
                if (null != fstm)
                {
                    fn = fstm.Name;
                }
            }

            var result = new TextReaderLexContext(input);
            if (!string.IsNullOrEmpty(fn))
            {
                result.fileOrUrl = fn;
            }

            return result;
        }

        /// <summary>
        /// Creates a <see cref="LexContext" /> over a file
        /// </summary>
        /// <param name="filename">The file</param>
        /// <returns>A new <see cref="LexContext" /> over the file</returns>
        public static LexContext CreateFrom(string filename)
        {
            return CreateFrom(new StreamReader(filename));
        }

        /// <summary>
        /// Creates a <see cref="LexContext" /> over an URL
        /// </summary>
        /// <param name="url">The URL</param>
        /// <returns>A new <see cref="LexContext" /> over the URL</returns>
        public static LexContext CreateFromUrl(string url)
        {
            var wreq = WebRequest.Create(url);
            var wrsp = wreq.GetResponse();
            var result = CreateFrom(new StreamReader(wrsp.GetResponseStream()));
            result.fileOrUrl = url;
            return result;
        }

        /// <summary>
        /// Closes the current instance and releases any resources being held
        /// </summary>
        private void Close()
        {
            if (current != Disposed)
            {
                current = Disposed;
                GC.SuppressFinalize(this);
                CloseInner();
                CaptureBuffer.Clear();
            }
        }

        /// <summary>
        /// Sets the location information for this instance
        /// </summary>
        /// <param name="line">The one based line number</param>
        /// <param name="column">The one based column number</param>
        /// <param name="position">The zero based position</param>
        /// <param name="fileOrUrl">The file or URL</param>
        public void SetLocation(int line, int column, long position, string fileOrUrl)
        {
            this.line = line;
            this.column = column;
            this.position = position;
            this.fileOrUrl = fileOrUrl;
        }

        /// <summary>
        /// Gets all or a subset of the current capture buffer
        /// </summary>
        /// <param name="startIndex">The start index</param>
        /// <param name="length">The number of characters to retrieve, or zero to retrieve the remainder of the buffer</param>
        /// <returns>A string containing the specified subset of the capture buffer</returns>
        public string GetCapture(int startIndex = 0, int length = 0)
        {
            _CheckDisposed();
            if (length == 0)
            {
                length = CaptureBuffer.Length - startIndex;
            }

            return CaptureBuffer.ToString(startIndex, length);
        }

        /// <summary>
        /// Clears the capture buffer
        /// </summary>
        public void ClearCapture()
        {
            _CheckDisposed();
            CaptureBuffer.Clear();
        }

        /// <summary>
        /// Captures the current character under the cursor, if any
        /// </summary>
        public void Capture()
        {
            _CheckDisposed();
            if (current != EndOfInput && current != BeforeInput)
            {
                CaptureBuffer.Append((char) current);
            }
        }

        /// <summary>
        /// Verifies that one of the specified characters is under the input cursor. If it isn't, a <see cref="ExpectingException" /> is raised.
        /// </summary>
        /// <param name="expecting">The list of expected characters. If empty, anything but end of input is accepted. If <see cref="EndOfInput" /> is included, end of input is accepted.</param>
        [System.Diagnostics.DebuggerHidden()]
        public void Expecting(params int[] expecting)
        {
            _CheckDisposed();
            if (current == BeforeInput)
            {
                throw new ExpectingException("The cursor is before the beginning of the input", new Location(line, column, position, fileOrUrl));
            }

            switch (expecting.Length)
            {
                case 0:
                    if (current == EndOfInput)
                    {
                        throw new ExpectingException("Unexpected end of input", new Location(line, column, position, fileOrUrl));
                    }

                    break;

                case 1:
                    if (current != expecting[0])
                    {
                        throw new ExpectingException(_GetErrorMessage(expecting), new Location(line, column, position, fileOrUrl), _GetErrorExpecting(expecting));
                    }

                    break;

                default:
                    if (Array.IndexOf(expecting, current) < 0)
                    {
                        throw new ExpectingException(_GetErrorMessage(expecting), new Location(line, column, position, fileOrUrl), _GetErrorExpecting(expecting));
                    }

                    break;
            }
        }

        private string _GetErrorMessage(int[] expecting)
        {
            StringBuilder sb = null;
            switch (expecting.Length)
            {
                case 0:
                    break;
                case 1:
                    sb = new StringBuilder();
                    if (expecting[0] == EndOfInput)
                    {
                        sb.Append("end of input");
                    }
                    else
                    {
                        sb.Append('"').Append((char) expecting[0]).Append('"');
                    }

                    break;
                case 2:
                    sb = new StringBuilder();
                    if (expecting[0] == EndOfInput)
                    {
                        sb.Append("end of input");
                    }
                    else
                    {
                        sb.Append('"').Append((char) expecting[0]).Append('"');
                    }

                    sb.Append(" or ");
                    if (expecting[1] == EndOfInput)
                    {
                        sb.Append("end of input");
                    }
                    else
                    {
                        sb.Append('"').Append((char) expecting[1]).Append('"');
                    }

                    break;
                default: // length > 2
                    sb = new StringBuilder();
                    if (expecting[0] == EndOfInput)
                    {
                        sb.Append("end of input");
                    }
                    else
                    {
                        sb.Append('"').Append((char) expecting[1]).Append('"');
                    }

                    var l = expecting.Length - 1;
                    var i = 1;
                    for (; i < l; ++i)
                    {
                        sb.Append(", ");
                        if (expecting[i] == EndOfInput)
                        {
                            sb.Append("end of input");
                        }
                        else
                        {
                            sb.Append('"').Append((char) expecting[1]).Append('"');
                        }
                    }

                    sb.Append(", or ");
                    if (expecting[i] == EndOfInput)
                    {
                        sb.Append("end of input");
                    }
                    else
                    {
                        sb.Append('"').Append((char) expecting[1]).Append('"');
                    }

                    break;
            }

            if (current == EndOfInput)
            {
                if (expecting.Length == 0)
                {
                    return "Unexpected end of input";
                }

                return string.Concat("Unexpected end of input. Expecting ", sb.ToString());
            }

            if (expecting.Length == 0)
            {
                return string.Concat("Unexpected character \"", (char) current, "\" in input");
            }

            return string.Concat("Unexpected character \"", (char) current, "\" in input. Expecting ", sb.ToString());
        }

        private string[] _GetErrorExpecting(int[] expecting)
        {
            var result = new string[expecting.Length];
            for (var i = 0; i < expecting.Length; ++i)
            {
                if (expecting[i] != EndOfInput)
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

        public void Dispose()
        {
            Close();
        }

        public void EnsureStarted()
        {
            _CheckDisposed();
            if (current == BeforeInput)
            {
                Advance();
            }
        }

        public int Advance()
        {
            _CheckDisposed();
            if (current == EndOfInput)
            {
                return EndOfInput;
            }

            current = AdvanceInner();
            switch (current)
            {
                case '\n':
                    ++line;
                    column = 0;
                    break;
                case '\r':
                    column = 0;
                    break;
                case '\t':
                    column += TabWidth;
                    break;
                default:
                    // since we have to advance to read the second surrogate
                    // we don't increment the column on the first surrogate
                    // and surrogate pairs should only change the column
                    // by one anyway.
                    if (!char.IsHighSurrogate(unchecked((char) current)))
                    {
                        ++column;
                    }

                    break;
            }

            ++position;
            return current;
        }

        private void _CheckDisposed()
        {
            if (current == Disposed)
            {
                throw new ObjectDisposedException(nameof(LexContext));
            }
        }

        protected abstract int AdvanceInner();

        protected abstract void CloseInner();

        private sealed class CharEnumeratorContext : LexContext
        {
            private readonly IEnumerator<char> inner;

            internal CharEnumeratorContext(IEnumerable<char> inner) => this.inner = inner.GetEnumerator();

            protected override int AdvanceInner() => !inner.MoveNext() ? EndOfInput : inner.Current;

            protected override void CloseInner() => inner.Dispose();
        }

        private sealed class TextReaderLexContext : LexContext
        {
            private readonly TextReader inner;

            internal TextReaderLexContext(TextReader inner) => this.inner = inner;

            protected override int AdvanceInner() => inner.Read();

            protected override void CloseInner() => inner.Close();
        }
    }
}
