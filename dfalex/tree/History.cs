using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CodeHive.DfaLex.tree
{
    /// <summary>
    /// Flyweight for the shared history of the memory cells. Singly-linked list where the
    /// head (and it's <c>cur</c>) is mutable, but the rest (everything visible from <c>prev</c>
    /// is immutable.
    /// </summary>
    internal class History : IEnumerable<int>
    {
        private static long _nextId = -1;

        internal readonly long    id;
        internal          int     cur;
        internal          History prev;

        internal History()
            : this(Interlocked.Increment(ref _nextId), 0, null)
        { }

        internal History(long id, int head, History history)
        {
            this.id = id;
            cur = head;
            prev = history;
        }

        public int Size => prev?.Size + 1 ?? 1;

        public IEnumerator<int> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            return obj is History other && id == other.id;
        }

        public override int GetHashCode()
        {
            return (int) id;
        }

        public override string ToString()
        {
            var ret = new StringBuilder().Append(id).Append('(');
            var first = true;
            foreach (var i in this)
            {
                if (!first)
                {
                    ret.Append(' ');
                }

                ret.Append(i);
                first = false;
            }

            ret.Append(')');
            return ret.ToString();
        }

        /// <summary>
        /// Testing only.
        /// </summary>
        internal static void ResetCount()
        {
            Interlocked.Exchange(ref _nextId, -1);
        }

        private sealed class Enumerator : IEnumerator<int>
        {
            private readonly History history;

            private History current;
            private bool    done;

            public Enumerator(History history)
            {
                this.history = history;
            }

            public int Current => current.cur;

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (done)
                {
                    return false;
                }

                current = current == null ? history : current.prev;
                return !(done = current == null);
            }

            public void Reset()
            {
                current = null;
                done = false;
            }

            public void Dispose()
            { }
        }
    }
}
