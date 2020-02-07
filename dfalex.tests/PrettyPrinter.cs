using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CodeHive.DfaLex.tree;

namespace CodeHive.DfaLex.Tests
{
    public static class PrettyPrinter
    {
        public static string Print<T>(Nfa<T> nfa, int state, bool useStateNumbers = false)
        {
            var ctx = new NfaContext<T>(nfa, state, useStateNumbers);
            var printer = new CompactPrinter<int, T>(ctx);

            return printer.Print();
        }

        internal static string Print(TNfa tnfa, bool useStateNumbers = false)
        {
            var ctx = new TNfaContext(tnfa, useStateNumbers);
            var printer = new CompactPrinter<int, int>(ctx);

            return printer.Print();
        }

        internal static string Print<T>(RawDfa<T> dfa, bool useStatefulNumbers = false)
        {
            var ctx = new RawDfaContext<T>(dfa, useStatefulNumbers);
            var printer = new CompactPrinter<int, T>(ctx);

            return printer.Print();
        }

        public static string Print<T>(DfaState<T> state, bool useStateNumbers = false)
        {
            var ctx = new DfaContext<T>(state, useStateNumbers);
            var printer = new CompactPrinter<DfaState<T>, T>(ctx);

            return printer.Print();
        }

        public static string PrintDot<T>(Nfa<T> nfa, int state, bool useStateNumbers = false)
        {
            var ctx = new NfaContext<T>(nfa, state, useStateNumbers);
            var printer = new DotPrinter<int, T>(ctx, "nfa");

            return printer.Print();
        }

        internal static string PrintDot(TNfa tnfa, bool useStateNumbers = false)
        {
            var ctx = new TNfaContext(tnfa, useStateNumbers);
            var printer = new DotPrinter<int, int>(ctx, "tnfa");

            return printer.Print();
        }

        public static string PrintDot<T>(DfaState<T> state, bool useStateNumbers = false)
        {
            var ctx = new DfaContext<T>(state, useStateNumbers);
            var printer = new DotPrinter<DfaState<T>, T>(ctx, "dfa");

            return printer.Print();
        }

        internal static string PrintDot<T>(RawDfa<T> rawDfa, bool useStateNumbers = false)
        {
            var ctx = new RawDfaContext<T>(rawDfa, useStateNumbers);
            var printer = new DotPrinter<int, T>(ctx, "rawdfa");

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
                    return $"${(int) c:x}";
            }
        }

        private abstract class Context<TState, T>
        {
            private readonly IDictionary<TState, string> names     = new Dictionary<TState, string>();
            public readonly  Queue<TState>               ClosureQ  = new Queue<TState>();
            private readonly IDictionary<TState, string> transMemo = new Dictionary<TState, string>();
            private readonly bool                        useStateNumbers;
            private          int                         nextStateNum;

            protected Context(bool useStateNumbers)
            {
                this.useStateNumbers = useStateNumbers;
            }

            public IEnumerable<TState> StartStates { get; protected set; }

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
                    ClosureQ.Enqueue(state);
                }

                return ret;
            }

            protected abstract int GetStateNumber(TState state);

            public abstract bool HasIncomingEpsilon(TState target);

            public abstract bool IsAccepting(TState state);

            public abstract T GetMatch(TState state);

            public abstract TState GetNextState(TState state, char ch);

            public abstract void ForTransitions(TState state, Action empty, Action<TState, bool, Tag> epsilon, Action<TState, char, char, Tag> transition);

            public string GetTransitionChars(TState state)
            {
                if (!transMemo.TryGetValue(state, out var ret))
                {
                    var buf = new StringBuilder();
                    ForTransitions(state,
                        null,
                        null,
                        (target, cMin, cMax, lowPriority) =>
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
            public DfaContext(DfaState<T> startState, bool useStateNumbers)
                : base(useStateNumbers)
            {
                StartStates = new[] {startState};
            }

            protected override int GetStateNumber(DfaState<T> state) => state.StateNumber;

            public override bool HasIncomingEpsilon(DfaState<T> target) => false;

            public override bool IsAccepting(DfaState<T> state) => state.IsAccepting;

            public override T GetMatch(DfaState<T> state) => state.Match;

            public override DfaState<T> GetNextState(DfaState<T> state, char ch) => state.GetNextState(ch);

            public override void ForTransitions(DfaState<T> state,
                Action empty,
                Action<DfaState<T>, bool, Tag> epsilon,
                Action<DfaState<T>, char, char, Tag> transition)
            {
                var isEmpty = true;
                state.EnumerateTransitions((cMin, cMax, target) =>
                {
                    transition?.Invoke(target, cMin, cMax, Tag.None);
                    isEmpty = false;
                });

                if (empty != null && isEmpty)
                {
                    empty();
                }
            }
        }

        private class RawDfaContext<T> : Context<int, T>
        {
            private readonly RawDfa<T> dfa;

            public RawDfaContext(RawDfa<T> dfa, bool useStateNumbers)
                : base(useStateNumbers)
            {
                this.dfa = dfa;
                StartStates = dfa.StartStates;
            }

            protected override int GetStateNumber(int state) => state;

            public override bool HasIncomingEpsilon(int target) => false;

            public override bool IsAccepting(int state) => dfa.AcceptSets[dfa.States[state].GetAcceptSetIndex()].accept;

            public override T GetMatch(int state) => dfa.AcceptSets[dfa.States[state].GetAcceptSetIndex()].match;

            public override int GetNextState(int state, char ch)
            {
                var nextState = -1;
                dfa.States[state].ForEachTransition(trans =>
                {
                    if (trans.FirstChar <= ch && ch <= trans.LastChar)
                    {
                        nextState = trans.State;
                    }
                });
                return nextState;
            }

            public override void ForTransitions(int state, Action empty, Action<int, bool, Tag> epsilon, Action<int, char, char, Tag> transition)
            {
                var isEmpty = true;
                dfa.States[state].ForEachTransition(trans =>
                {
                    transition?.Invoke(trans.State, trans.FirstChar, trans.LastChar, Tag.None);
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

            public NfaContext(Nfa<T> nfa, int startState, bool useStateNumbers)
                : base(useStateNumbers)
            {
                this.nfa = nfa;
                StartStates = new[] {startState};
            }

            protected override int GetStateNumber(int state) => state;

            public override bool HasIncomingEpsilon(int target)
            {
                for (var st = 0; st < nfa.NumStates; ++st)
                {
                    if (nfa.GetStateEpsilons(st).Any(trans => trans.State == target))
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

            public override void ForTransitions(int state, Action empty, Action<int, bool, Tag> epsilon, Action<int, char, char, Tag> transition)
            {
                if (!nfa.GetStateEpsilons(state).Any() && !nfa.GetStateTransitions(state).Any())
                {
                    empty?.Invoke();
                }
                else
                {
                    if (epsilon != null)
                    {
                        nfa.ForStateEpsilons(state, trans => epsilon(trans.State, trans.Priority == NfaTransitionPriority.Low, Tag.None));
                    }

                    if (transition != null)
                    {
                        nfa.ForStateTransitions(state, trans => transition(trans.State, trans.FirstChar, trans.LastChar, trans.Tag));
                    }
                }
            }
        }

        private class TNfaContext : Context<int, int>
        {
            private readonly TNfa tnfa;

            public TNfaContext(TNfa tnfa, bool useStateNumbers)
                : base(useStateNumbers)
            {
                this.tnfa = tnfa;
                StartStates = new[] {tnfa.initialState};
            }

            protected override int GetStateNumber(int state) => state;

            public override bool HasIncomingEpsilon(int target)
            {
                return tnfa.stateEpsilons.Any(transitions => transitions != null && transitions.Any(trans => target.Equals(trans.State)));
            }

            public override bool IsAccepting(int state) => state.Equals(tnfa.finalState);

            public override int GetMatch(int state) => 0;

            public override int GetNextState(int state, char ch)
            {
                foreach (var trans in tnfa.GetStateTransitions(state))
                {
                    if (trans.FirstChar <= ch && ch <= trans.LastChar)
                    {
                        return trans.State;
                    }
                }

                throw new InvalidOperationException($"No transition from {StateName(state)} on '{ch}'");
            }

            public override void ForTransitions(int state, Action empty, Action<int, bool, Tag> epsilon, Action<int, char, char, Tag> transition)
            {
                if (!tnfa.GetStateTransitions(state).Any() && !tnfa.GetStateEpsilons(state).Any())
                {
                    empty?.Invoke();
                }
                else
                {
                    if (epsilon != null)
                    {
                        foreach (var trans in tnfa.GetStateEpsilons(state))
                        {
                            epsilon(trans.State, trans.Priority == NfaTransitionPriority.Low, trans.Tag);
                        }
                    }

                    if (transition != null)
                    {
                        foreach (var trans in tnfa.GetStateTransitions(state))
                        {
                            transition(trans.State, trans.FirstChar, trans.LastChar, trans.Tag);
                        }
                    }
                }
            }
        }

        private abstract class Printer<TState, T>
        {
            private readonly Context<TState, T> ctx;
            private readonly StringBuilder      buf = new StringBuilder(4096);
            private readonly bool               appendMatch;

            protected Printer(Context<TState, T> ctx, bool appendMatch)
            {
                this.ctx = ctx;
                this.appendMatch = appendMatch;
            }

            public string Print()
            {
                foreach (var startState in ctx.StartStates)
                {
                    WriteStartState(startState);
                    while (ctx.ClosureQ.Any())
                    {
                        PrintState(ctx.ClosureQ.Dequeue());
                    }
                }

                return ToString();
            }

            private void PrintState(TState state)
            {
                WriteState(state);

                ctx.ForTransitions(state,
                    WriteEmpty,
                    (target, lowPriority, tag) => WriteEpsilon(state, target, lowPriority, tag),
                    (target, cMin, cMax, tag) => WriteTransition(state, target, cMin, cMax, tag));
            }

            public override string ToString()
            {
                return buf.ToString();
            }

            protected string StateName(TState state) => ctx.StateName(state, appendMatch);

            protected abstract void WriteStartState(TState state);

            protected abstract void WriteState(TState state);

            protected virtual void WriteEmpty()
            { }

            protected virtual void WriteEpsilon(TState state, TState target, bool lowPriority, Tag tag)
            { }

            protected abstract void WriteTransition(TState state, TState target, char cMin, char cMax, Tag tag);

            protected (bool, T) Accepting(TState state)
            {
                var accepts = ctx.IsAccepting(state);
                return (accepts, accepts ? ctx.GetMatch(state) : default);
            }

            protected bool NextSingleTransitionState(TState state, ref TState target, out char cMin, out char cMax)
            {
                var nextTrans = ctx.GetTransitionChars(state);
                if (nextTrans.Length == 2 && !ctx.HasIncomingEpsilon(state) && !ctx.IsAccepting(state))
                {
                    cMin = nextTrans[0];
                    cMax = nextTrans[1];
                    target = ctx.GetNextState(state, cMin);
                    return true;
                }

                cMin = cMax = default;
                return false;
            }

            protected Printer<TState, T> Append(char ch)
            {
                buf.Append(ch);
                return this;
            }

            public void Append(string str)
            {
                buf.Append(str);
            }

            protected void AppendLine(string str)
            {
                buf.AppendLine(str);
            }
        }

        private class CompactPrinter<TState, T> : Printer<TState, T>
        {
            public CompactPrinter(Context<TState, T> ctx)
                : base(ctx, true)
            { }

            protected override void WriteStartState(TState state)
            {
                StateName(state);
            }

            protected override void WriteState(TState state)
            {
                var stateName = StateName(state);
                AppendLine(stateName);
            }

            protected override void WriteEmpty()
            {
                AppendLine("    (done)");
            }

            protected override void WriteEpsilon(TState state, TState target, bool lowPriority, Tag tag)
            {
                AppendLine($"   {(lowPriority ? "-" : " ")}ε{(tag == Tag.None ? string.Empty : $" {tag}")} -> {StateName(target)}");
            }

            protected override void WriteTransition(TState state, TState target, char cMin, char cMax, Tag tag)
            {
                Append("    ");
                while (true)
                {
                    Append(PrintChar(cMin));
                    if (cMin != cMax)
                    {
                        Append('-').Append(PrintChar(cMax));
                    }

                    Append(" -> ");

                    if (NextSingleTransitionState(target, ref target, out cMin, out cMax))
                    {
                        continue;
                    }

                    AppendLine(StateName(target));
                    break;
                }
            }
        }

        private class DotPrinter<TState, T> : Printer<TState, T>
        {
            private readonly string title;

            public DotPrinter(Context<TState, T> ctx, string title)
                : base(ctx, false)
            {
                this.title = title;
            }

            private void Preamble(StringBuilder buf)
            {
                buf.AppendLine($"digraph {title} {{");
                buf.AppendLine("rankdir=LR;");
            }

            private void Appendix(StringBuilder buf)
            {
                buf.AppendLine("}");
            }

            protected override void WriteStartState(TState state)
            {
                var stateName = $"{StateName(state)}_Start";
                AppendLine($"{stateName} [style=invis];"); // Invisible start name
                AppendLine($"{stateName} -> {StateName(state)}"); // Edge into start state
            }

            protected override void WriteState(TState state)
            {
                var (accepts, match) = Accepting(state);
                if (accepts)
                {
                    // Accept states are double circles
                    var stateName = StateName(state);
                    AppendLine($"{stateName}[label=\"{stateName}\n{match}\",peripheries=2]");
                }
            }

            protected override void WriteEpsilon(TState state, TState target, bool lowPriority, Tag tag)
            {
                AppendLine($"{StateName(state)} -> {StateName(target)} [label=\"{(lowPriority ? "-" : string.Empty)}ε{(tag == Tag.None ? string.Empty : $" {tag}")}\"]");
            }

            protected override void WriteTransition(TState state, TState target, char cMin, char cMax, Tag tag)
            {
                var label = new StringBuilder();
                label.Append(PrintChar(cMin, true));
                if (cMin != cMax)
                {
                    label.Append('-').Append(PrintChar(cMax, true));
                }

                AppendLine($"{StateName(state)} -> {StateName(target)} [label=\"{label}{(tag == Tag.None ? string.Empty : $" {tag}")}\"]");
            }

            public override string ToString()
            {
                var sb = new StringBuilder(8192);
                Preamble(sb);
                sb.Append(base.ToString());
                Appendix(sb);
                return sb.ToString();
            }
        }
    }
}
