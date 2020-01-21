using System.Collections.Generic;
using System.Text;
using static CodeHive.DfaLex.re1.Inst.Opcode;

namespace CodeHive.DfaLex.re1
{
    internal class Prog
    {
        private readonly Inst[] prog;

        public Prog(int size)
        {
            prog = new Inst[size + 1];
            prog[prog.Length - 1] = new Inst(Match);
        }

        public Inst this[int index]
        {
            get => prog[index];
            set => prog[index] = value;
        }

        public int Length => prog.Length;

        public void Reset()
        {
            foreach (var inst in prog)
            {
                inst.gen = -1;
            }
        }

        public bool Recursive(string str, IList<int> sub)
        {
            return Recursive(0, str, 0, sub);
        }

        private bool Recursive(int pc, string str, int sp, IList<int> sub)
        {
            switch (this[pc].OpCode)
            {
                case Char:
                    if (sp >= str.Length || str[sp] != this[pc].C)
                    {
                        return false;
                    }

                    return Recursive(pc + 1, str, sp + 1, sub);

                case Any:
                    if (sp >= str.Length)
                    {
                        return false;
                    }

                    return Recursive(pc + 1, str, sp + 1, sub);

                case Match:
                    return true;

                case Jmp:
                    return Recursive(this[pc].X, str, sp, sub);

                case Split:
                    if (Recursive(this[pc].X, str, sp, sub))
                    {
                        return true;
                    }

                    return Recursive(this[pc].Y, str, sp, sub);

                case Save:
                    while (this[pc].N >= sub.Count)
                    {
                        sub.Add(-1);
                    }

                    var old = sub[this[pc].N];
                    sub[this[pc].N] = sp;
                    if (Recursive(pc + 1, str, sp, sub))
                    {
                        return true;
                    }

                    sub[this[pc].N] = old;
                    return false;
            }

            throw new DfaException("recursive");
        }

        public bool RecursiveLoop(string str, IList<int> sub)
        {
            return RecursiveLoop(0, str, 0, sub);
        }

        private bool RecursiveLoop(int pc, string str, int sp, IList<int> sub)
        {
            while (true)
            {
                switch (this[pc].OpCode)
                {
                    case Char:
                        if (sp >= str.Length || str[sp] != this[pc].C)
                        {
                            return false;
                        }

                        pc++;
                        sp++;
                        continue;

                    case Any:
                        if (sp >= str.Length)
                        {
                            return false;
                        }

                        pc++;
                        sp++;
                        continue;

                    case Match:
                        return true;

                    case Jmp:
                        pc = this[pc].X;
                        continue;

                    case Split:
                        if (RecursiveLoop(this[pc].X, str, sp, sub))
                        {
                            return true;
                        }

                        pc = this[pc].Y;
                        continue;

                    case Save:
                        while (this[pc].N >= sub.Count)
                        {
                            sub.Add(-1);
                        }

                        var old = sub[this[pc].N];
                        sub[this[pc].N] = sp;
                        if (RecursiveLoop(pc + 1, str, sp, sub))
                        {
                            return true;
                        }

                        sub[this[pc].N] = old;
                        return false;
                }

                throw new DfaException("recursiveloop");
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (var pc = 0; pc < prog.Length; pc++)
            {
                sb.AppendFormat("{0,2}. ", pc);
                var inst = prog[pc];
                switch (inst.OpCode)
                {
                    case Split:
                        sb.AppendLine($"split {inst.X}, {inst.Y}");
                        break;
                    case Jmp:
                        sb.AppendLine($"jmp {inst.X}");
                        break;
                    case Char:
                        sb.Append($"char ").Append((char) inst.C).AppendLine();
                        break;
                    case Any:
                        sb.AppendLine("any");
                        break;
                    case Match:
                        sb.AppendLine("match");
                        break;
                    case Save:
                        sb.AppendLine($"save {inst.N}");
                        break;
                    default:
                        throw new DfaException("printprog");
                }
            }

            return sb.ToString();
        }
    }
}
