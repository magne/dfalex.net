using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CodeHive.DfaLex.Tests
{
    public class PrettyPrinter<T>
    {
        private readonly Dictionary<DfaState<T>, string> names     = new Dictionary<DfaState<T>, string>();
        private readonly Dictionary<DfaState<T>, string> transMemo = new Dictionary<DfaState<T>, string>();
        private readonly Queue<DfaState<T>>              closureQ  = new Queue<DfaState<T>>();
        private          int                             nextStateNum;
        private readonly bool                            useStateNumbers;

        public PrettyPrinter()
        {
            useStateNumbers = false;
        }

        public PrettyPrinter(bool useStateNumbers)
        {
            this.useStateNumbers = useStateNumbers;
        }

        internal void Print(TextWriter w, DfaState<T> state)
        {
            names.Clear();
            transMemo.Clear();
            closureQ.Clear();
            nextStateNum = 0;
            NameState(state);
            while (closureQ.Any())
            {
                PrintState(w, closureQ.Dequeue());
            }
        }

        internal void PrintDot(TextWriter w, DfaState<T> state)
        {
            names.Clear();
            transMemo.Clear();
            closureQ.Clear();
            nextStateNum = 0;
            NameState(state, false);

            w.WriteLine("digraph dfa {");
            w.WriteLine("rankdir=LR;");
            w.WriteLine("n999999 [style=invis];"); // Invisible start node
            w.WriteLine($"n999999 -> {names[state]}"); // Edge into start state
            while (closureQ.Any())
            {
                PrintStateDot(w, closureQ.Dequeue());
            }

            w.WriteLine("}");
        }

        private void PrintState(TextWriter w, DfaState<T> state)
        {
            var stateName = names[state];
            w.WriteLine(stateName);
            var trans = GetTransitionChars(state);
            if (trans.Length < 2)
            {
                w.WriteLine("    (done)");
                return;
            }

            for (var i = 0; i < trans.Length - 1; i += 2)
            {
                w.Write("    ");
                var cmin = trans[i];
                var cmax = trans[i + 1];
                var target = state;

                for (;;)
                {
                    w.Write(PrintChar(cmin));
                    if (cmin != cmax)
                    {
                        w.Write("-");
                        w.Write(PrintChar(cmax));
                    }

                    w.Write(" -> ");

                    target = target.GetNextState(cmin);
                    var nexttrans = GetTransitionChars(target);
                    if (nexttrans.Length == 2 && !target.IsAccepting)
                    {
                        cmin = nexttrans[0];
                        cmax = nexttrans[1];
                    }
                    else
                    {
                        w.WriteLine(NameState(target));
                        break;
                    }
                }
            }
        }

        private void PrintStateDot(TextWriter w, DfaState<T> state)
        {
            var stateName = names[state];
            if (state.IsAccepting)
            {
                // Accept states are double circles
                w.WriteLine($"{stateName}[label=\"{stateName}\n{state.Match}\",peripheries=2]");
            }

            var trans = GetTransitionChars(state);
            if (trans.Length < 2)
            {
                return;
            }

            for (var i = 0; i < trans.Length - 1; i += 2)
            {
                var cmin = trans[i];
                var cmax = trans[i + 1];
                var target = state.GetNextState(cmin);

                var label = new StringBuilder();
                label.Append(PrintChar(cmin, true));
                if (cmin != cmax)
                {
                    label.Append('-').Append(PrintChar(cmax, true));
                }

                w.WriteLine($"{stateName} -> {NameState(target, false)} [label=\"{label}\"]");
            }
        }

        private string NameState(DfaState<T> state, bool appendMatch = true)
        {
            if (!names.TryGetValue(state, out var ret))
            {
                var nameNum = (useStateNumbers ? state.StateNumber : nextStateNum);
                ++nextStateNum;
                if (!state.IsAccepting || !appendMatch)
                {
                    ret = "S" + nameNum;
                }
                else
                {
                    ret = "S" + nameNum + ":" + state.Match;
                }

                names.Add(state, ret);
                closureQ.Enqueue(state);
            }

            return ret;
        }

        private string GetTransitionChars(DfaState<T> state)
        {
            if (!transMemo.TryGetValue(state, out var ret))
            {
                var stb = new StringBuilder();
                // state.EnumerateTransitions((firstChar, lastChar, target) => { stb.Append(firstChar).Append(lastChar); });
                state.EnumerateTransitions((startc, endc, newstate) =>
                {
                    stb.Append(startc);
                    stb.Append(endc);
                    System.Diagnostics.Debug.Assert(Equals(state.GetNextState(startc), newstate));
                });
                ret = stb.ToString();
                transMemo.Add(state, ret);
            }

            return ret;
        }

        private string PrintChar(char c, bool dot =false)
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
    }
}
