﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace dfalex.util
{
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

    public class ExpectingException : Exception
    {
        public ExpectingException(string message, Location location, params string[] expecting)
            : base(GetMessage(message, location))
        {
            Location = location;
            Expecting = expecting;
        }

        public Location Location { get; }

        public int Line => Location.Line;

        public int Column => Location.Column;

        public long Position => Location.Position;

        public string[] Expecting { get; }

        private static string? GetMessage(string message, Location location)
        {
            return new StringBuilder().Append(message).Append(" at ").Append(location).ToString();
        }
    }

    public abstract partial class LexContext : IDisposable
    {
        public const int     DefaultTabWidth = 4;
        public const int     EndOfInput      = -1;
        public const int     BeforeInput     = -2;
        public const int     Disposed        = -3;
        private      int     current         = BeforeInput;
        private      int     line;
        private      int     column;
        private      long    position;
        private      string? fileOrUrl;

        public int TabWidth { get; set; } = DefaultTabWidth;

        public int Line => line;

        public int Column => column;

        public long Position => position;

        public string? FileOrUrl => fileOrUrl;

        public StringBuilder CaptureBuffer { get; } = new();

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

        public static LexContext Create(IEnumerable<char> input)
        {
            return new CharEnumeratorContext(input);
        }

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

        public static LexContext CreateFrom(string filename)
        {
            return CreateFrom(new StreamReader(filename));
        }

        public static LexContext CreateFromUrl(string url)
        {
            var wreq = WebRequest.Create(url);
            var wrsp = wreq.GetResponse();
            var result = CreateFrom(new StreamReader(wrsp.GetResponseStream()));
            result.fileOrUrl = url;
            return result;
        }

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

        public void SetLocation(int line, int column, long position, string fileOrUrl)
        {
            this.line = line;
            this.column = column;
            this.position = position;
            this.fileOrUrl = fileOrUrl;
        }

        public string GetCapture(int startIndex = 0, int length = 0)
        {
            _CheckDisposed();
            if (length == 0)
            {
                length = CaptureBuffer.Length - startIndex;
            }

            return CaptureBuffer.ToString(startIndex, length);
        }

        public void ClearCapture()
        {
            _CheckDisposed();
            CaptureBuffer.Clear();
        }

        public void Capture()
        {
            _CheckDisposed();
            if (current != EndOfInput && current != BeforeInput)
            {
                CaptureBuffer.Append((char) current);
            }
        }

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
