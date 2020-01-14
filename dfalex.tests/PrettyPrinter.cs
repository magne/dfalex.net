using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CodeHive.DfaLex.Tests
{
    public static class PrettyPrinter
    {
        public static string Print<T>(Nfa<T> nfa, int state, bool useStateNumbers = false)
        {
            var ctx = new NfaContext<T>(nfa, useStateNumbers);
            var printer = new CompactPrinter<int, T>(ctx, state);

            return printer.Print();
        }

        public static string Print<T>(DfaState<T> state, bool useStateNumbers = false)
        {
            var ctx = new DfaContext<T>(useStateNumbers);
            var printer = new CompactPrinter<DfaState<T>, T>(ctx, state);

            return printer.Print();
        }

        public static string PrintDot<T>(Nfa<T> nfa, int state, bool useStateNumbers = false)
        {
            var ctx = new NfaContext<T>(nfa, useStateNumbers);
            var printer = new DotPrinter<int, T>(ctx, state, "nfa");

            return printer.Print();
        }

        public static string PrintDot<T>(DfaState<T> state, bool useStateNumbers = false)
        {
            var ctx = new DfaContext<T>(useStateNumbers);
            var printer = new DotPrinter<DfaState<T>, T>(ctx, state, "dfa");

            return printer.Print();
        }

        private static string PrintChar(char c, bool dot = false)
        {
            if (dot)
            {
                switch (c)
                {
                    case '"':
                        return "\\\"";

                    case '\\':
                        return "\\\\";
                }
            }

            if (c >= ' ' && c < (char) 128)
            {
                return c.ToString();
            }

            switch (c)
            {
                case '\n':
                    return "\\n";

                case '\r':
                    return "\\r";

                case '\t':
                    return "\\t";

                default:
                    return $"${((int) c):x}";
            }
        }

        private abstract class Context<TState, T>
        {
            private readonly IDictionary<TState, string> names     = new Dictionary<TState, string>();
            public readonly  Queue<TState>               closureQ  = new Queue<TState>();
            private readonly IDictionary<TState, string> transMemo = new Dictionary<TState, string>();
            private readonly bool                        useStateNumbers;
            private          int                         nextStateNum;

            protected Context(bool useStateNumbers)
            {
                this.useStateNumbers = useStateNumbers;
            }

            public string StateName(TState state, bool appendMatch = true)
            {
                if (!names.TryGetValue(state, out var ret))
                {
                    var nameNum = useStateNumbers ? GetStateNumber(state) : nextStateNum++;
                    if (!IsAccepting(state) || !appendMatch)
                    {
                        ret = "S" + nameNum;
                    }
                    else
                    {
                        ret = "S" + nameNum + ":" + GetMatch(state);
                    }

                    names.Add(state, ret);
                    closureQ.Enqueue(state);
                }

                return ret;
            }

            protected abstract int GetStateNumber(TState state);

            public abstract bool HasIncomingEpsilon(TState target);

            public abstract bool IsAccepting(TState state);

            public abstract T GetMatch(TState state);

            public abstract TState GetNextState(TState state, char ch);

            public abstract void ForTransitions(TState state, Action empty, Action<TState> epsilon, Action<TState, char, char> transition);

            public string GetTransitionChars(TState state)
            {
                if (!transMemo.TryGetValue(state, out var ret))
                {
                    var buf = new StringBuilder();
                    ForTransitions(state, null, null, (target, cMin, cMax) =>
                    {
                        buf.Append(cMin).Append(cMax);
                        Debug.Assert(Equals(GetNextState(state, cMin), target));
                    });
                    ret = buf.ToString();
                    transMemo.Add(state, ret);
                }

                return ret;
            }
        }

        private class DfaContext<T> : Context<DfaState<T>, T>
        {
            public DfaContext(bool useStateNumbers)
                : base(useStateNumbers)
            { }

            protected override int GetStateNumber(DfaState<T> state) => state.StateNumber;

            public override bool HasIncomingEpsilon(DfaState<T> target) => false;

            public override bool IsAccepting(DfaState<T> state) => state.IsAccepting;

            public override T GetMatch(DfaState<T> state) => state.Match;

            public override DfaState<T> GetNextState(DfaState<T> state, char ch) => state.GetNextState(ch);

            public override void ForTransitions(DfaState<T> state, Action empty, Action<DfaState<T>> epsilon,
                Action<DfaState<T>, char, char> transition)
            {
                var isEmpty = true;
                state.EnumerateTransitions((cMin, cMax, target) =>
                {
                    transition?.Invoke(target, cMin, cMax);
                    isEmpty = false;
                });

                if (empty != null && isEmpty)
                {
                    empty();
                }
            }
        }

        private class NfaContext<T> : Context<int, T>
        {
            private readonly Nfa<T> nfa;

            public NfaContext(Nfa<T> nfa, bool useStateNumbers)
                : base(useStateNumbers)
            {
                this.nfa = nfa;
            }

            protected override int GetStateNumber(int state) => state;

            public override bool HasIncomingEpsilon(int target)
            {
                for (var st = 0; st < nfa.NumStates; ++st)
                {
                    if (nfa.GetStateEpsilons(st).Contains(target))
                    {
                        return true;
                    }
                }

                return false;
            }

            public override bool IsAccepting(int state) => nfa.IsAccepting(state);

            public override T GetMatch(int state) => nfa.GetAccept(state);

            public override int GetNextState(int state, char ch)
            {
                foreach (var trans in nfa.GetStateTransitions(state))
                {
                    if (trans.FirstChar <= ch && ch <= trans.LastChar)
                    {
                        return trans.State;
                    }
                }

                throw new InvalidOperationException($"No transition from {StateName(state)} on '{ch}'");
            }

            public override void ForTransitions(int state, Action empty, Action<int> epsilon, Action<int, char, char> transition)
            {
                if (!nfa.GetStateEpsilons(state).Any() && !nfa.GetStateTransitions(state).Any())
                {
                    empty?.Invoke();
                }
                else
                {
                    if (epsilon != null)
                    {
                        nfa.ForStateEpsilons(state, epsilon);
                    }

                    if (transition != null)
                    {
                        nfa.ForStateTransitions(state, trans => transition(trans.State, trans.FirstChar, trans.LastChar));
                    }
                }
            }
        }

        private abstract class Printer<TState, T>
        {
            protected readonly Context<TState, T> Ctx;
            protected readonly StringBuilder   Buf = new StringBuilder(4096);

            protected Printer(Context<TState, T> ctx)
            {
                Ctx = ctx;
            }

            public string Print()
            {
                while (Ctx.closureQ.Any())
                {
                    PrintState(Ctx.closureQ.Dequeue());
                }

                return ToString();
            }

            private void PrintState(TState state)
            {
                WriteState(state);

                Ctx.ForTransitions(state,
                    WriteEmpty,
                    target => WriteEpsilon(state, target),
                    (target, cMin, cMax) => WriteTransition(state, target, cMin, cMax));
            }

            public override string ToString()
            {
                return Buf.ToString();
            }

            protected abstract void WriteState(TState state);

            protected virtual void WriteEmpty()
            { }

            protected virtual void WriteEpsilon(TState state, TState target)
            { }

            protected abstract void WriteTransition(TState state, TState target, char cMin, char cMax);
        }

        private class CompactPrinter<TState, T> : Printer<TState, T>
        {
            public CompactPrinter(Context<TState, T> ctx, TState startState)
                : base(ctx)
            {
                ctx.StateName(startState);
            }

            protected override void WriteState(TState state)
            {
                var stateName = Ctx.StateName(state);
                Buf.AppendLine(stateName);
            }

            protected override void WriteEmpty()
            {
                Buf.AppendLine("    (done)");
            }

            protected override void WriteEpsilon(TState state, TState target)
            {
                Buf.Append("    ε -> ").AppendLine(Ctx.StateName(target));
            }

            protected override void WriteTransition(TState state, TState target, char cMin, char cMax)
            {
                var line = new StringBuilder();
                line.Append("    ");
                while (true)
                {
                    line.Append(PrintChar(cMin));
                    if (cMin != cMax)
                    {
                        line.Append('-').Append(PrintChar(cMax));
                    }

                    line.Append(" -> ");

                    var nextTrans = Ctx.GetTransitionChars(target);
                    if (nextTrans.Length == 2 && !Ctx.HasIncomingEpsilon(target) && !Ctx.IsAccepting(target))
                    {
                        cMin = nextTrans[0];
                        cMax = nextTrans[1];
                        target = Ctx.GetNextState(target, cMin);
                    }
                    else
                    {
                        line.Append(Ctx.StateName(target));
                        Buf.AppendLine(line.ToString());
                        break;
                    }
                }
            }
        }

        private class DotPrinter<TState, T> : Printer<TState, T>
        {
            private readonly string title;
            private readonly TState startState;

            public DotPrinter(Context<TState, T> ctx, TState startState, string title)
                : base(ctx)
            {
                ctx.StateName(startState, false);
                this.title = title;
                this.startState = startState;
            }

            private void Preamble(StringBuilder buf)
            {
                buf.AppendLine($"digraph {title} {{");
                buf.AppendLine("rankdir=LR;");
                buf.AppendLine("n999999 [style=invis];"); // Invisible start node
                buf.AppendLine($"n999999 -> {Ctx.StateName(startState)}"); // Edge into start state
            }

            private void Appendix(StringBuilder buf)
            {
                buf.AppendLine("}");
            }

            protected override void WriteState(TState state)
            {
                var stateName = Ctx.StateName(state);
                if (Ctx.IsAccepting(state))
                {
                    // Accept states are double circles
                    Buf.AppendLine($"{stateName}[label=\"{stateName}\n{Ctx.GetMatch(state)}\",peripheries=2]");
                }
            }

            protected override void WriteEpsilon(TState state, TState target)
            {
                Buf.AppendLine($"{Ctx.StateName(state, false)} -> {Ctx.StateName(target, false)} [label=\"ε\"]");
            }

            protected override void WriteTransition(TState state, TState target, char cMin, char cMax)
            {
                var label = new StringBuilder();
                label.Append(PrintChar(cMin, true));
                if (cMin != cMax)
                {
                    label.Append('-').Append(PrintChar(cMax, true));
                }

                Buf.AppendLine($"{Ctx.StateName(state)} -> {Ctx.StateName(target, false)} [label=\"{label}\"]");
            }

            public override string ToString()
            {
                var sb = new StringBuilder(Buf.Length + 1024);
                Preamble(sb);
                sb.Append(Buf);
                Appendix(sb);
                return sb.ToString();
            }
        }
    }
}
