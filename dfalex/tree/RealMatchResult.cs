using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeHive.DfaLex.tree
{
    internal class RealMatchResult : MatchResultTree
    {
        readonly Arraylike captureGroupPositions;

        /** The parent capture group number `t` is parentOf[t]. */
        readonly int[] parentOf;

        readonly string input;

        internal RealMatchResult(Arraylike fin, string input, int[] parentOf)
        {
            this.captureGroupPositions = fin;
            this.input = input;
            this.parentOf = parentOf;
        }

        internal sealed class NoMatchResult : MatchResultTree
        {
            public static readonly MatchResultTree SINGLETON = new NoMatchResult();

            private NoMatchResult()
            { }

            public int end()
            {
                return -1;
            }

            public int end(int group)
            {
                if (group == 0)
                {
                    return end();
                }

                throw new InvalidOperationException();
            }

            public string group()
            {
                throw new InvalidOperationException();
            }

            public string group(int group)
            {
                throw new InvalidOperationException();
            }

            public int groupCount()
            {
                return -1;
            }

            public int start()
            {
                return -1;
            }

            public int start(int group)
            {
                throw new InvalidOperationException();
            }

            public override string ToString() => "NO_MATCH";

            public TreeNode getRoot()
            {
                throw new InvalidOperationException("There was no match!");
            }
        }

        internal class RealTreeNode : TreeNode, IComparable<RealTreeNode>
        {
            private readonly RealMatchResult parent;
            private readonly int captureGroup;

            internal readonly List<TreeNode> children = new List<TreeNode>();
            readonly int            from;
            readonly int            to;

            internal RealTreeNode(RealMatchResult parent, int captureGroup, int @from, int to)
            {
                this.parent = parent;
                this.captureGroup = captureGroup;
                this.from = from;
                this.to = to;
            }

            public IEnumerable<TreeNode> getChildren()
            {
                return children;
            }

            public int getGroup()
            {
                return captureGroup;
            }

            public override string ToString()
            {
                return parent.input.Substring(from, to - from);
            }

            public int CompareTo(RealTreeNode other)
            {
                return from.CompareTo(other.from);
            }
        }

        public int end()
        {
            return end(0);
        }

        public int end(int group)
        {
            return captureGroupPositions.Get(group * 2 + 1).FirstOrDefault();
        }

        public string group()
        {
            return group(0);
        }

        public string group(int group)
        {
            return input.Substring(start(group), end(group) - start(group));
        }

        public int groupCount()
        {
            return captureGroupPositions.Size / 2;
        }

        public int start()
        {
            return start(0);
        }

        public int start(int group)
        {
            return captureGroupPositions.Get(group * 2).FirstOrDefault();
        }

        public override string ToString()
        {
            return "" + start() + "-" + end();
        }

        public TreeNode getRoot()
        {
            // copy captureGroupPositions into hs, then move all histories one step down,
            // to see only committed values.
            var hs = new History[captureGroupPositions.Size];

            var i = 0;
            foreach (var h in captureGroupPositions)
            {
                if (h != null)
                {
                    hs[i] = h.prev;
                }

                i++;
            }

            // Copy the input, which is an array of linked lists into an array of arrays.
            var cols = new List<List<RealTreeNode>>(parentOf.Length);
            for (var col = 0; col < parentOf.Length; col++)
            {
                var curCol = new List<RealTreeNode>();
                while (hs[2 * col] != null)
                {
                    curCol.Add(new RealTreeNode(this, col, hs[2 * col].cur, hs[2 * col + 1].cur + 1));

                    // Forward
                    hs[2 * col] = hs[2 * col].prev;
                    hs[2 * col + 1] = hs[2 * col + 1].prev;
                }

                curCol.Reverse(); // Prefer ascending order.
                cols.Add(curCol);
            }

            for (var col = 1; col < cols.Count; col++)
            {
                // in parent column, find only(!) matching parent
                foreach (var n in cols[col])
                {
                    var parentCol = cols[parentOf[col]];
                    var idx = parentCol.BinarySearch(n);
                    if (idx < 0)
                    {
                        // the `from` index of n isn't the same as the `from` index of parent.
                        idx = ~idx - 1; // One before insertion point.
                    }

                    parentCol[idx].children.Add(n);
                }
            }

            return cols[0][0]; // The root is in capture group 0, which has only one entry.
        }

        /**
         * Testing only!
         * @return a string dump of all matched positions for all groups, in reverse.
         */
        internal string matchPositionsDebugString()
        {
            var ret = new StringBuilder();
            foreach (var h in captureGroupPositions)
            {
                ret.Append('(');
                // Ignore uncommitted (first item)
                foreach (var i in h.Skip(1))
                {
                    ret.Append(i);
                    ret.Append(", ");
                }

                ret.Append(") ");
            }

            return ret.ToString();
        }
    }
}
