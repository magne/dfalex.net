using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CodeHive.DfaLex.tree
{
    internal class DFAState : IComparable<DFAState>
    {
        internal readonly IList<RThread> threads;
        private readonly  byte[]         comparisonKey;

        // Histories of this state if it is finishing, otherwise null.
        internal readonly Arraylike finalHistories;

        internal DFAState(IList<RThread> threads, byte[] comparisonKey, Arraylike finalHistories)
        {
            this.comparisonKey = comparisonKey ?? throw new ArgumentNullException(nameof(comparisonKey)); // Needed for equals.
            this.threads = threads;
            this.finalHistories = finalHistories;
        }

        internal static byte[] MakeComparisonKey(IList<RThread> innerStates)
        {
            Debug.Assert(innerStates.Any());

            byte[] firstPart = MakeStateComparisonKey(innerStates);
            byte[] secondPart = MakeHistoryComparisonKey(innerStates);

            // concatenate both parts.
            var ret = new byte[firstPart.Length + secondPart.Length];
            Array.Copy(firstPart, 0, ret, 0, firstPart.Length);
            Array.Copy(secondPart, 0, ret, firstPart.Length, secondPart.Length);
            return ret;
        }

        internal static byte[] MakeStateComparisonKey(IList<RThread> threads)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            foreach (var t in threads)
            {
                writer.Write(t.State.Id);
            }

            var hash = MakeMessageDigest();
            return hash.ComputeHash(stream.ToArray());
        }

        private static byte[] MakeHistoryComparisonKey(IList<RThread> threads)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            foreach (var t in threads)
            {
                foreach (var h in t.Histories)
                {
                    writer.Write(h.id);
                }
            }

            var hash = MakeMessageDigest();
            return hash.ComputeHash(stream.ToArray());
        }

        private static HashAlgorithm MakeMessageDigest()
        {
            return MD5.Create();
        }

        public int CompareTo(DFAState other)
        {
            var cmp = comparisonKey.Length.CompareTo(other.comparisonKey.Length);
            if (cmp != 0)
            {
                return cmp;
            }

            for (var i = 0; i < comparisonKey.Length; i++)
            {
                cmp = comparisonKey[i].CompareTo(other.comparisonKey[i]);
                if (cmp != 0)
                {
                    return cmp;
                }
            }

            return 0;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            return obj is DFAState other && comparisonKey.SequenceEqual(other.comparisonKey);
        }

        public override int GetHashCode()
        {
            var hash = 0;
            foreach (var b in comparisonKey)
            {
                hash *= 31;
                hash ^= b;
            }

            return hash;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('(');
            foreach (var el in threads)
            {
                sb.Append(el.State);
                sb.Append("->");
                sb.Append(el.Histories);
                sb.Append(", ");
            }

            sb.Remove(sb.Length - 2, 2);
            sb.Append(')');
            return sb.ToString();
        }
    }
}
