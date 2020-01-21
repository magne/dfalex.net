using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CodeHive.DfaLex.tree
{
    /// <summary>
    /// A fized-size copy-on-write data structure that supports accessing histories.
    /// </summary>
    internal abstract class Arraylike : IEnumerable<History>
    {
        public abstract int Size { get; }

        public abstract History Get(int index);

        public abstract Arraylike Set(int index, History h);

        public abstract IEnumerator<History> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal static Arraylike Make(int size)
        {
            // TODO back heuristic up with data
            if (size < 20)
            {
                return new HistoryArray(size);
            }

            return new TreeArray(size);
        }

        /// <summary>
        /// A fixed-size copy-on-write left-complete binary tree structure.
        /// </summary>
        internal class TreeArray : Arraylike
        {
            internal  TreeArray left;
            internal  TreeArray right;
            internal History   payload;

            /// <summary>
            /// Tree creation ensures left-completeness structure.
            /// It has log_2(n) depth, the order is left-first.
            /// </summary>
            /// <param name="n">number of elements.</param>
            internal TreeArray(int n)
            {
                System.Diagnostics.Debug.Assert(n > 0);
                Size = n;
                payload = new History();

                // we fill maximal power of two smaller than n to the left.
                var rest = n - 1;
                var rightSize = rest / 2;
                var leftSize = rest - rightSize;
                System.Diagnostics.Debug.Assert(leftSize >= rightSize);

                left = leftSize < 1 ? null : new TreeArray(leftSize);
                right = rightSize < 1 ? null : new TreeArray(rightSize);
            }

            private TreeArray(TreeArray ta)
            {
                Size = ta.Size;
                payload = ta.payload;
                left = ta.left;
                right = ta.right;
            }

            public override int Size { get; }

            public override History Get(int index)
            {
                System.Diagnostics.Debug.Assert(0 <= index);
                System.Diagnostics.Debug.Assert(index < Size);

                var current = this;
                while (index > 0)
                {
                    index--;
                    if (current.left.Size > index)
                    {
                        current = current.left;
                    }
                    else
                    {
                        index -= current.left.Size;
                        current = current.right;
                    }
                }

                return current.payload;
            }

            public override Arraylike Set(int index, History h)
            {
                System.Diagnostics.Debug.Assert(0 <= index);
                System.Diagnostics.Debug.Assert(index < Size);

                var top = new TreeArray(this);
                var current = top;
                while (index > 0)
                {
                    index--;
                    if (current.left.Size > index)
                    {
                        current.left = new TreeArray(current.left);
                        current = current.left;
                    }
                    else
                    {
                        index -= current.left.Size;
                        current.right = new TreeArray(current.right);
                        current = current.right;
                    }
                }

                current.payload = h;
                return top;
            }

            public override IEnumerator<History> GetEnumerator()
            {
                return new TreeArrayEnumerator(this);
            }

            public override string ToString()
            {
                var ret = new StringBuilder().Append('[');
                var first = true;
                foreach (var history in this)
                {
                    if (!first)
                    {
                        ret.Append(", ");
                    }

                    ret.Append(history);
                    first = false;
                }

                ret.Append(']');
                return ret.ToString();
            }

            private sealed class TreeArrayEnumerator : IEnumerator<History>
            {
                private readonly TreeArray        orig;
                private readonly Stack<TreeArray> descentStack;

                public TreeArrayEnumerator(TreeArray treeArray)
                {
                    orig = treeArray;
                    descentStack = new Stack<TreeArray>();
                    descentStack.Push(treeArray);
                }

                public bool MoveNext()
                {
                    if (descentStack.Count < 1)
                    {
                        Current = null;
                        return false;
                    }

                    // First current
                    var current = descentStack.Pop();
                    Current = current.payload;

                    // then left, then right (but we fill a stack).
                    if (current.right != null)
                    {
                        descentStack.Push(current.right);
                    }

                    if (current.left != null)
                    {
                        descentStack.Push(current.left);
                    }

                    return true;
                }

                public void Reset()
                {
                    descentStack.Clear();
                    descentStack.Push(orig);
                    Current = null;
                }

                public History Current { get; private set; }

                object IEnumerator.Current => Current;

                public void Dispose()
                { }
            }
        }

        private class HistoryArray : Arraylike
        {
            private readonly History[] histories;

            internal HistoryArray(int n)
            {
                histories = new History[n];
                for (var i = 0; i < n; i++)
                {
                    histories[i] = new History();
                }
            }

            private HistoryArray(HistoryArray orig)
            {
                histories = new History[orig.histories.Length];
                Array.Copy(orig.histories, histories, histories.Length);
            }

            public override int Size => histories.Length;

            public override History Get(int index)
            {
                return histories[index];
            }

            public override Arraylike Set(int index, History h)
            {
                var newHistories = new HistoryArray(this);
                newHistories.histories[index] = h;
                return newHistories;
            }

            public override IEnumerator<History> GetEnumerator()
            {
                return new ArrayIterator(histories);
            }

            public override string ToString() => histories.AsString();

            private sealed class ArrayIterator : IEnumerator<History>
            {
                private readonly History[] histories;
                private          int       index;

                public ArrayIterator(History[] histories)
                {
                    this.histories = histories;
                    index = -1;
                }

                public bool MoveNext()
                {
                    return ++index < histories.Length;
                }

                public void Reset()
                {
                    index = -1;
                }

                public History Current => histories[index];

                object IEnumerator.Current => Current;

                public void Dispose()
                { }
            }
        }
    }
}
