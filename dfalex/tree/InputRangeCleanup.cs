using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeHive.DfaLex.tree
{
    /// <summary>
    /// Gets a collection of possibly intersecting input ranges, and makes them non-intersecting.
    /// </summary>
    internal static class InputRangeCleanup
    {
        internal static IList<InputRange> CleanUp(IEnumerable<InputRange> ranges)
        {
            var pq = new SortedList<InputRange, object>(ranges.Distinct().ToDictionary(range => range, range => (object) null));
            if (!pq.Any())
            {
                return Array.Empty<InputRange>();
            }

            var ret = new List<InputRange>();

            // Remove duplicates, add to pq.
            while (pq.Count > 1)
            {
                var c = pq.Keys[0];
                pq.RemoveAt(0);
                var n = pq.Keys[0];
                pq.RemoveAt(0);

                char c1 = c.From, c2 = c.To;
                char n1 = n.From, n2 = n.To;

                // Three cases:
                //  1. [c1, c2] and [n1, n2] don't intersect: [c1-----c2]  [n1----n2]
                //     add [c1, c2] unmodified.
                if (c2 < n1)
                {
                    ret.Add(c);
                    pq.Add(n, null);
                }

                //  2. [c1, c2] and [n1, n2] intersect, but n2 >= c2: [c1-------c2]
                //     add [c1, n1-1]                                       [n1---------n2]
                else if (c2 <= n2)
                {
                    if (n1 > c1)
                    {
                        ret.Add(InputRange.Make(c1, (char) (n1 - 1)));
                        pq.Add(InputRange.Make(n1,  c2), null);
                        if (n2 > c2)
                        {
                            pq.Add(InputRange.Make((char) (c2 + 1), n2), null);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(n1 == c1); //      [c1-------c2]
                        System.Diagnostics.Debug.Assert(n2 > c2); //      [n1---------------n2]
                        ret.Add(c);
                        pq.Add(InputRange.Make((char) (c2 + 1), n2), null);
                    }
                }

                //  3. [c1, c2] and [n1, n2] intersect so that n2 < c2 [c1----------------c2]
                //     add [c1, n1-1] and [n1,n2] and continue with              [n1---n2]
                //     [n2+1, c2] as next c.
                else
                {
                    System.Diagnostics.Debug.Assert(n2 < c2);
                    System.Diagnostics.Debug.Assert(c2 >= n1);
                    System.Diagnostics.Debug.Assert(c1 < n1);

                    ret.Add(InputRange.Make(c1, (char) (n1 - 1)));
                    pq.Add(n, null);
                    pq.Add(InputRange.Make((char) (n2 + 1), c2), null);
                }
            }

            if (pq.Any())
            {
                ret.Add(pq.Keys[0]);
                pq.RemoveAt(0);
            }

            System.Diagnostics.Debug.Assert(!pq.Any());

            return ret;
        }
    }
}
