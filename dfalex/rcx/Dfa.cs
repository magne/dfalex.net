using System.Collections.Generic;

namespace CodeHive.DfaLex.rcx
{
    internal static class Dfa
    {
        /// <summary>
        /// Represents a DFA state: a cached NFA state list.
        /// </summary>
        internal class DState
        {
            public          IList<Nfa.State> l;
            public readonly DState[]         next = new DState[256];
        }

        private static readonly IDictionary<IList<Nfa.State>, DState> AllStates = new Dictionary<IList<Nfa.State>, DState>();

        /// <summary>
        /// Return the cached DState for list l, creating a new one if needed.
        /// </summary>
        private static DState dstate(IList<Nfa.State> l)
        {
            ((List<Nfa.State>) l).Sort();
            if (!AllStates.TryGetValue(l, out var d))
            {
                d = new DState { l = l };
                AllStates.Add(l, d);
            }

            return d;
        }

        public static DState StartDState(Nfa.State start)
        {
            return dstate(Nfa.StartList(start));
        }

        private static DState NextState(DState d, int c)
        {
            var l1 = new List<Nfa.State>();
            Nfa.Step(d.l, c, l1);
            return d.next[c] = dstate(l1);
        }

        public static bool Match(DState start, string s)
        {
            var d = start;
            foreach (var c in s)
            {
                var next = d.next[c] ?? NextState(d, c);
                d = next;
            }

            return Nfa.IsMatch(d.l);
        }
    }
}
