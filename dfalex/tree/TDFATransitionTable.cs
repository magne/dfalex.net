using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CodeHive.DfaLex.tree
{
    internal class TDFATransitionTable
    {
        private readonly int size;

        // The following vars are, together a struct of arrays.
        private readonly char[]          froms;
        private readonly Instruction[][] instructions;
        private readonly int[]           newStates;
        private readonly int[]           states;
        private readonly char[]          tos;

        // Position of the last hit in the transition table.
        private int last;

        internal TDFATransitionTable(char[] froms, char[] tos, int[] states, int[] newStates, Instruction[][] instructions)
        {
            size = froms.Length;
            Debug.Assert(tos.Length == size &&
                         states.Length == size &&
                         froms.Length == size &&
                         newStates.Length == size &&
                         instructions.Length == size);
            this.froms = froms;
            this.tos = tos;
            this.states = states;
            this.newStates = newStates;
            this.instructions = instructions;
        }

        internal class NextState
        {
            internal Instruction[] instructions;
            internal int           nextState;
            internal bool          found;
        }

        internal class NextDFAState
        {
            internal readonly Instruction[] instructions;
            public readonly DFAState      nextState;

            internal NextDFAState(Instruction[] instructions, DFAState nextState)
            {
                this.instructions = instructions;
                this.nextState = nextState;
            }

            public override string ToString()
            {
                return $"{nextState} {instructions}";
            }
        }

        internal class Builder
        {
            internal readonly Mapping          mapping     = new Mapping();
            private readonly SortedSet<Entry> transitions = new SortedSet<Entry>();

            internal class Entry : IComparable<Entry>
            {
                public static readonly Entry Head = new HeadEntry();

                internal readonly char          from;
                internal readonly char          to;
                internal readonly Instruction[] instructions;
                internal readonly int           state;
                internal readonly int           newState;
                internal readonly DFAState      toDFA;
                private readonly  int           compareOffset;

                internal Entry(char from, char to, Instruction[] c, int state, int newState, DFAState toDFA)
                {
                    this.from = from;
                    this.to = to;
                    instructions = c;
                    this.state = state;
                    this.newState = newState;
                    this.toDFA = toDFA;
                }

                private Entry(char from, char to, Instruction[] c, int state, int newState, DFAState toDFA, int compareOffset)
                {
                    this.compareOffset = compareOffset;
                }

                public virtual int CompareTo(Entry other)
                {
                    var cmp = state.CompareTo(other.state);
                    if (cmp != 0)
                    {
                        return cmp;
                    }

                    return from.CompareTo((char)(other.from + compareOffset));
                }

                public Entry MakeInclusiveUpper()
                {
                    return new Entry(from, to, instructions, state, newState, toDFA, 1);
                }

                public override string ToString() => $"q{state}-{from}-{to} -> q{newState} {instructions.AsString()}";

                private class HeadEntry : Entry
                {
                    internal HeadEntry()
                        : base('\0', '\0', null, 0, 0, null)
                    { }

                    public override int CompareTo(Entry other)
                    {
                        return -1;
                    }
                }
            }

            internal class Mapping
            {
                /** Map from full DFAState to optimized state (an int) */
                internal readonly IDictionary<DFAState, int> mapping = new Dictionary<DFAState, int>();

                /** Map from optimized state (an integer) to full DFAState. */
                internal readonly IList<DFAState> deoptimized = new List<DFAState>();

                internal int LookupOrMake(DFAState state)
                {
                    if (mapping.TryGetValue(state, out var to))
                    {
                        return to;
                    }

                    var next = deoptimized.Count;

                    mapping.Add(state, next);

                    deoptimized.Add(state);
                    Debug.Assert(deoptimized[next].Equals(state));

                    return next;
                }
            }

            internal void AddTransition(DFAState t, InputRange inputRange, DFAState newState, List<Instruction> c)
            {
                var e = new Entry(inputRange.From, inputRange.To, c.ToArray(), mapping.LookupOrMake(t), mapping.LookupOrMake(newState), newState);
                transitions.Add(e);
            }

            internal NextDFAState AvailableTransition(DFAState t, char a)
            {
                if (!mapping.mapping.TryGetValue(t, out var fromState))
                {
                    return null;
                }

                var probe = new Entry(a, a, null, fromState, -1, null);
                var headSet = transitions.GetViewBetween(Entry.Head, probe.MakeInclusiveUpper());
                if (!headSet.Any())
                {
                    return null;
                }

                var found = headSet.Max;
                if (found.state != probe.state || !(found.from <= a && a <= found.to))
                {
                    return null;
                }

                return new NextDFAState(found.instructions, found.toDFA);
            }

            public TDFATransitionTable build()
            {
                var size = transitions.Count;
                var froms = new char[size];
                var instructions = new Instruction[size][];
                var newStates = new int[size];
                var states = new int[size];
                var tos = new char[size];

                var i = 0;
                foreach (var e in transitions)
                {
                    froms[i] = e.from;
                    tos[i] = e.to;
                    states[i] = e.state;
                    newStates[i] = e.newState;
                    instructions[i] = e.instructions;
                    i++;
                }

                return new TDFATransitionTable(froms, tos, states, newStates, instructions);
            }

            public override string ToString()
            {
                return build().ToString();
            }
        }

        private int Cmp(int state1, int state2, char ch1, char ch2)
        {
            var scmp = state1.CompareTo(state2);
            if (scmp != 0)
            {
                return scmp;
            }

            return ch1.CompareTo(ch2);
        }

        internal void NewStateAndInstructions(int state, char input, NextState @out)
        {
            if (states[last] == state && froms[last] <= input && input <= tos[last])
            {
                @out.nextState = newStates[last];
                @out.instructions = instructions[last];
                @out.found = true;
                return;
            }

            if (size < 20)
            {
                // linear scan for small automata
                for (var y = 0; y < size; y++)
                {
                    if (states[y] == state && froms[y] <= input && input <= tos[y])
                    {
                        @out.nextState = newStates[y];
                        @out.instructions = instructions[y];
                        last = y;
                        @out.found = true;
                        return;
                    }
                }

                @out.found = false;
                return;
            }

            var l = 0;
            var r = size - 1;
            var x = -1;
            while (r >= l)
            {
                x = (int) ((uint) (l + r) >> 1); // average and stays correct if addition overflows.
                var cmp = Cmp(state, states[x], input, froms[x]);
                if (cmp < 0)
                {
                    r = x - 1;
                }
                else if (cmp > 0)
                {
                    l = x + 1;
                }
                else
                {
                    @out.nextState = newStates[x];
                    @out.instructions = instructions[x];
                    last = x;
                    @out.found = true;
                    return;
                }
            }

            Debug.Assert(x != -1);

            for (var i = -1; i <= 1; i++)
            {
                var y = x + i;

                if (0 <= y && y < size)
                {
                    if (states[y] == state && froms[y] <= input && input <= tos[y])
                    {
                        @out.nextState = newStates[y];
                        @out.instructions = instructions[y];
                        last = y;
                        @out.found = true;
                        return;
                    }
                }
            }

            @out.found = false;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (var i = 0; i < size; i++)
            {
                sb.Append(new Builder.Entry(froms[i], tos[i], instructions[i], states[i], newStates[i], null));
                sb.Append('\n');
            }

            return sb.ToString();
        }
    }
}
