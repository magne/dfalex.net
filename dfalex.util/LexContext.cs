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
    public sealed record Location(int Line, int Column, long Position, object? Source = null)
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
    public abstract class LexContext : IDisposable
    {
        /// <summary>
        /// Indicates the default tab width of an input device
        /// </summary>
        public const int DefaultTabWidth = 4;

        /// <summary>
        /// Represents the end of input symbol
        /// </summary>
        public const int EndOfInput = -1;

        /// <summary>
        /// Represents the before input symbol
        /// </summary>
        public const int BeforeInput = -2;

        /// <summary>
        /// Represents a symbol for the disposed state
        /// </summary>
        public const int Disposed = -3;

        private int current = BeforeInput;

        // Indicates the current one based line number
        private int line;

        // Indicates the current one based column number
        private int column;

        // Indicates the current zero based position
        private long position;

        // Indicates the current filename or URL, if any could be discerned
        private object? fileOrUrl;

        /// <summary>
        /// Indicates the tab width of the input device
        /// </summary>
        public int TabWidth { get; set; } = DefaultTabWidth;

        /// <summary>
        /// The location information for this instance.
        /// </summary>
        public Location Location
        {
            get => new(line, column, position, fileOrUrl);
            set => (line, column, position, fileOrUrl) = value;
        }

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
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (current != Disposed)
            {
                // Release unmanaged resources
                if (disposing)
                {
                    // Dispose managed resources
                    CaptureBuffer.Clear(); // ???
                }

                current = Disposed;
            }
        }

        /// <summary>
        /// Closes the current instance and releases any resources being held
        /// </summary>
        public void Close()
        {
            Dispose();
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
            string? fn = null;
            var sr = input as StreamReader;
            if (sr?.BaseStream is FileStream fstm)
            {
                fn = fstm.Name;
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
        /// Gets all or a subset of the current capture buffer
        /// </summary>
        /// <param name="startIndex">The start index</param>
        /// <param name="length">The number of characters to retrieve, or zero to retrieve the remainder of the buffer</param>
        /// <returns>A string containing the specified subset of the capture buffer</returns>
        public string GetCapture(int startIndex = 0, int length = 0)
        {
            CheckDisposed();
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
            CheckDisposed();
            CaptureBuffer.Clear();
        }

        /// <summary>
        /// Captures the current character under the cursor, if any
        /// </summary>
        public void Capture()
        {
            CheckDisposed();
            if (current != EndOfInput && current != BeforeInput)
            {
                CaptureBuffer.Append((char) current);
            }
        }

        public void EnsureStarted()
        {
            CheckDisposed();
            if (current == BeforeInput)
            {
                Advance();
            }
        }

        public int Advance()
        {
            CheckDisposed();
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

        internal void CheckDisposed()
        {
            if (current == Disposed)
            {
                throw new ObjectDisposedException(nameof(LexContext));
            }
        }

        protected abstract int AdvanceInner();

        private sealed class CharEnumeratorContext : LexContext
        {
            private readonly IEnumerator<char> inner;

            internal CharEnumeratorContext(IEnumerable<char> inner) => this.inner = inner.GetEnumerator();

            protected override void Dispose(bool disposing)
            {
                if (current != Disposed && disposing)
                {
                    inner.Dispose();
                }

                base.Dispose(disposing);
            }

            protected override int AdvanceInner() => !inner.MoveNext() ? EndOfInput : inner.Current;
        }

        private sealed class TextReaderLexContext : LexContext
        {
            private readonly TextReader inner;

            internal TextReaderLexContext(TextReader inner) => this.inner = inner;

            protected override void Dispose(bool disposing)
            {
                if (current != Disposed && disposing)
                {
                    inner.Dispose();
                }

                base.Dispose(disposing);
            }

            protected override int AdvanceInner() => inner.Read();
        }
    }
}
