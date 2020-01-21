using System;
using System.Threading;

namespace CodeHive.DfaLex.tree
{
    /// <summary>
    /// Immutable NFA state.
    ///
    /// The NFA keeps track of whether or not the state is final.
    /// </summary>
    internal class State : IComparable<State>
    {
        private static int _lastId;

        internal State()
        {
            Id = Interlocked.Increment(ref _lastId);
        }

        public int Id { get; }

        public int CompareTo(State other)
        {
            return Id.CompareTo(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            return obj is State other && Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public override string ToString()
        {
            return $"q{Id}";
        }

        /// <summary>
        /// Testing only.
        /// </summary>
        internal static void ResetCount()
        {
            Interlocked.Exchange(ref _lastId, 0);
        }
    }
}
