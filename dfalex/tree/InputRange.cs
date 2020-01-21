using System;

namespace CodeHive.DfaLex.tree
{
    /// <summary>
    /// <see cref="InputRange"/> represents a range of <see cref="char"/> which can be used in
    /// <see cref="TransitionTable"/> of TDFA.
    /// </summary>
    internal abstract class InputRange : IComparable<InputRange>
    {
        // ReSharper disable once InconsistentNaming
        public static readonly InputRange ANY = new Any();

        // ReSharper disable once InconsistentNaming
        public static readonly InputRange EOS = new Eos();

        /// <summary>
        /// Tell if the {@link InputRange} contains a {@link Character} within its range.
        /// </summary>
        /// <param name="ch">A specific <see cref="char"/>.</param>
        /// <returns>if the <see cref="char"/> is contained within the <see cref="InputRange"/>.</returns>
        public abstract bool Contains(char ch);

        /// <summary>
        /// Return the first <see cref="char"/> of the range.
        /// </summary>
        public abstract char From { get; }

        /// <summary>
        /// Return the last <see cref="char"/> of the range.
        /// </summary>
        public abstract char To { get; }

        public int CompareTo(InputRange other)
        {
            var cmp = From.CompareTo(other.From);
            if (cmp != 0)
            {
                return cmp;
            }

            return To.CompareTo(other.To);
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            return obj is InputRange range && From == range.From && To == range.To;
        }

        public override int GetHashCode()
        {
            return (From * 31) ^ To;
        }

        public static InputRange Make(char character)
        {
            return Make(character, character);
        }

        public static InputRange Make(char from, char to)
        {
            return new RealInputRange(from, to);
        }

        // Everything.
        private class Any : RealInputRange
        {
            internal Any()
                : base(char.MinValue, char.MaxValue)
            { }

            public override string ToString() => "ANY";
        }

        // Nothing.
        private class Eos : RealInputRange
        {
            internal Eos()
                : base((char) (char.MinValue + 1), char.MaxValue)
            { }

            public override string ToString() => "$";
        }

        private class SpecialInputRange : RealInputRange
        {
            private SpecialInputRange(char from, char to)
                : base(from, to)
            { }
        }

        private class RealInputRange : InputRange
        {
            protected internal RealInputRange(char from, char to)
            {
                From = from;
                To = to;
            }

            public override bool Contains(char ch)
            {
                return From <= ch && ch <= To;
            }

            public override char From { get; }
            public override char To { get; }

            public override string ToString()
            {
                var printedFrom = From.ToString();
                if (!char.IsLetterOrDigit(From))
                {
                    printedFrom = $"0x{(int) From:x}";
                }

                var printedTo = To.ToString();
                if (!char.IsLetterOrDigit(To))
                {
                    printedTo = $"0x{(int) To:x}";
                }

                return $"{printedFrom}-{printedTo}";
            }
        }
    }
}
