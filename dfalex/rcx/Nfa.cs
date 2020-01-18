using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeHive.DfaLex.rcx
{
    internal static class Nfa
    {
        private struct Paren
        {
            public int nalt;
            public int natom;
        }

        /// <summary>
        /// Convert infix regexp re to postfix notation.
        /// Insert . as explicit concatenation operator.
        /// Cheesy parser, return static buffer.
        /// </summary>
        public static string Re2Post(string re)
        {
            var nalt = 0;
            var natom = 0;
            var buf = new char[8000];
            var dst = 0;
            var paren = new Paren[100];
            var p = 0;

            if (re.Length >= buf.Length / 2)
            {
                return null;
            }

            foreach (var ch in re)
            {
                switch (ch)
                {
                    case '(':
                        if (natom > 1)
                        {
                            --natom;
                            buf[dst++] = '.';
                        }

                        if (p >= paren.Length)
                        {
                            return null;
                        }

                        paren[p].nalt = nalt;
                        paren[p].natom = natom;
                        p++;
                        nalt = 0;
                        natom = 0;
                        break;

                    case '|':
                        if (natom == 0)
                        {
                            return null;
                        }

                        while (--natom > 0)
                        {
                            buf[dst++] = '.';
                        }

                        nalt++;
                        break;

                    case ')':
                        if (p == 0)
                        {
                            return null;
                        }

                        if (natom == 0)
                        {
                            return null;
                        }

                        while (--natom > 0)
                        {
                            buf[dst++] = '.';
                        }

                        for (; nalt > 0; nalt--)
                        {
                            buf[dst++] = '|';
                        }

                        --p;
                        nalt = paren[p].nalt;
                        natom = paren[p].natom;
                        natom++;
                        break;

                    case '*':
                    case '+':
                    case '?':
                        if (natom == 0)
                        {
                            return null;
                        }

                        buf[dst++] = ch;
                        break;

                    default:
                        if (natom > 1)
                        {
                            --natom;
                            buf[dst++] = '.';
                        }

                        buf[dst++] = ch;
                        natom++;
                        break;
                }
            }

            if (p != 0)
            {
                return null;
            }

            while (--natom > 0)
            {
                buf[dst++] = '.';
            }

            for (; nalt > 0; nalt--)
            {
                buf[dst++] = '|';
            }

            return new string(buf, 0, dst);
        }

        /// <summary>
        /// Represents an NFA state plus zero or one or two arrows exiting.
        /// If c == Match, no arrows out; matching state.
        /// If c == Split, unlabeled arrows to out and out1 (if != NULL).
        /// If c < 256, labeled arrow with character c to out.
        /// </summary>
        internal class State : IComparable<State>
        {
            public const int Match = 256;
            public const int Split = 257;

            public static readonly State MatchState = new State(Match, null, null);

            public int   c;
            public State out1;
            public State out2;
            public int   lastList;

            public State(int c, State out1, State out2)
            {
                this.c = c;
                this.out1 = out1;
                this.out2 = out2;
            }

            public int CompareTo(State other)
            {
                var cmp = other.GetHashCode() - GetHashCode();
                if (cmp > 0)
                {
                    return -1;
                }

                if (cmp < 0)
                {
                    return 1;
                }

                return 0;
            }
        }

        private class Patch
        {
            private readonly State                state;
            private readonly Action<State, State> patch;

            public Patch(State state, Action<State, State> patch)
            {
                this.state = state;
                this.patch = patch;
            }

            internal void Do(State s)
            {
                patch(state, s);
            }
        }

        /// <summary>
        /// A partially built NFA without the matching state filled in.
        /// Frag.start points at the start state.
        /// Frag.out is a list of places that need to be set to the
        /// next state for this fragment.
        /// </summary>
        private class Frag
        {
            public readonly State       start;
            public readonly List<Patch> outs;

            public Frag(State start, Patch patch)
            {
                this.start = start;
                outs = new List<Patch> { patch };
            }

            public Frag(State start, List<Patch> outs)
            {
                this.start = start;
                this.outs = outs;
            }

            public Frag(State start, List<Patch> outs1, List<Patch> outs2)
            {
                this.start = start;
                outs = new List<Patch>();
                outs.AddRange(outs1);
                outs.AddRange(outs2);
            }

            public void Patch(State s)
            {
                foreach (var patch in outs)
                {
                    patch.Do(s);
                }
            }
        }

        /// <summary>
        /// Convert postfix regular expression to NFA.
        /// Return start state.
        /// </summary>
        public static State Post2nfa(string postfix)
        {
            var stack = new Frag[1000];
            var stackp = 0;
            Frag e;

            if (postfix == null)
            {
                return null;
            }

            foreach (var p in postfix)
            {
                Frag e1;
                Frag e2;
                State s;
                switch (p)
                {
                    default:
                        s = new State(p, null, null);
                        stack[stackp++] = new Frag(s, new Patch(s, (st, ns) => st.out1 = ns));
                        break;
                    case '.': /* catenate */
                        e2 = stack[--stackp];
                        e1 = stack[--stackp];
                        e1.Patch(e2.start);
                        stack[stackp++] = new Frag(e1.start, e2.outs);
                        break;
                    case '|': /* alternate */
                        e2 = stack[--stackp];
                        e1 = stack[--stackp];
                        s = new State(State.Split, e1.start, e2.start);
                        stack[stackp++] = new Frag(s, e1.outs, e2.outs);
                        break;
                    case '?': /* zero or one */
                        e = stack[--stackp];
                        s = new State(State.Split, e.start, null);
                        stack[stackp++] = new Frag(s, e.outs, new List<Patch> { new Patch(s, (st, ns) => st.out2 = ns) });
                        break;
                    case '*': /* zero or more */
                        e = stack[--stackp];
                        s = new State(State.Split, e.start, null);
                        e.Patch(s);
                        stack[stackp++] = new Frag(s, new Patch(s, (st, ns) => st.out2 = ns));
                        break;
                    case '+': /* one or more */
                        e = stack[--stackp];
                        s = new State(State.Split, e.start, null);
                        e.Patch(s);
                        stack[stackp++] = new Frag(e.start, new Patch(s, (st, ns) => st.out2 = ns));
                        break;
                }
            }

            e = stack[--stackp];
            if (stackp != 0)
            {
                return null;
            }

            e.Patch(State.MatchState);
            return e.start;
        }

        public static bool Match(State start, string s)
        {
            var clist = StartList(start);
            var nlist = (IList<State>) new List<State>();
            foreach (var ch in s)
            {
                Step(clist, ch, nlist);
                var t = clist;
                clist = nlist;
                nlist = t;
            }

            return IsMatch(clist);
        }

        private static int listid = 0;

        internal static IList<State> StartList(State start)
        {
            var list = new List<State>();
            listid++;
            AddState(list, start);
            return list;
        }

        private static void AddState(IList<State> list, State s)
        {
            if (s == null || s.lastList == listid)
            {
                return;
            }
            s.lastList = listid;
            if (s.c == State.Split)
            {
                /* follow unlabeled arrows */
                AddState(list, s.out1);
                AddState(list, s.out2);
                return;
            }

            list.Add(s);
        }

        /// <summary>
        /// Step the NFA from the states in clist past the character c, to create next NFA state set nlist.
        /// </summary>
        internal static void Step(IList<State> clist, int ch, IList<State> nlist)
        {
            listid++;
            foreach (var state in clist)
            {
                if (state.c == ch)
                {
                    AddState(nlist, state.out1);
                }
            }
        }

        internal static bool IsMatch(IList<State> list)
        {
            return list.Any(state => state == State.MatchState);
        }
    }
}
