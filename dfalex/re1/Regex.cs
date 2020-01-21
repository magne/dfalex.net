using System;
using System.Text;

namespace CodeHive.DfaLex.re1
{
    internal class Regex
    {
        private static readonly IRegexParserActions<Regex> Actions = new RegexParserActions();

        private enum Type
        {
            Alt = 1,
            Cat,
            Lit,
            Dot,
            Paren,
            Quest,
            Star,
            Plus
        }

        private readonly Type  type;
        private readonly Regex left;
        private readonly Regex right;
        private          int   n;
        private          int   ch;

        private Regex(Type type, Regex left, Regex right)
        {
            this.type = type;
            this.left = left;
            this.right = right;
        }

        internal static Regex Parse(string str)
        {
            return new RegexParser(Actions, str).Parse();
        }

        public Prog Compile()
        {
            var prog = new Prog(Count());
            var pc = 0;
            Emit(prog, ref pc);
            return prog;
        }

        /// <summary>
        /// How many instructions does this regex need?
        /// </summary>
        private int Count()
        {
            return type switch
            {
                Type.Alt => (2 + left.Count() + right.Count()),
                Type.Cat => (left.Count() + right.Count()),
                Type.Lit => 1,
                Type.Dot => 1,
                Type.Paren => (2 + left.Count()),
                Type.Quest => (1 + left.Count()),
                Type.Star => (2 + left.Count()),
                Type.Plus => (1 + left.Count()),
                _ => throw new DfaException("bad count")
            };
        }

        private int Emit(Prog prog, ref int pc)
        {
            var oldPc = pc;
            int pc1;
            int pc2;
            switch (type)
            {
                case Type.Alt:
                    pc1 = pc++;
                    prog[pc1] = new Inst(Inst.Opcode.Split);
                    prog[pc1].X = left.Emit(prog, ref pc);
                    pc2 = pc++;
                    prog[pc2] = new Inst(Inst.Opcode.Jmp);
                    prog[pc1].Y = Emit(prog, ref pc);
                    prog[pc2].X = pc;
                    break;

                case Type.Cat:
                    left.Emit(prog, ref pc);
                    right.Emit(prog, ref pc);
                    break;

                case Type.Lit:
                    prog[pc++] = new Inst(Inst.Opcode.Char) { C = ch };
                    break;

                case Type.Dot:
                    prog[pc++] = new Inst(Inst.Opcode.Any);
                    break;

                case Type.Paren:
                    prog[pc++] = new Inst(Inst.Opcode.Save) { N = 2 * n };
                    left.Emit(prog, ref pc);
                    prog[pc++] = new Inst(Inst.Opcode.Save) { N = 2 * n + 1 };
                    break;

                case Type.Quest:
                    pc1 = pc++;
                    prog[pc1] = new Inst(Inst.Opcode.Split) { X = pc };
                    left.Emit(prog, ref pc);
                    prog[pc1].Y = pc;
                    if (n > 0)
                    {
                        // non-greedy
                        var t = prog[pc1].X;
                        prog[pc1].X = prog[pc1].Y;
                        prog[pc1].Y = t;
                    }

                    break;

                case Type.Star:
                    pc1 = pc++;
                    prog[pc1] = new Inst(Inst.Opcode.Split) { X = pc };
                    left.Emit(prog, ref pc);
                    pc2 = pc++;
                    prog[pc2] = new Inst(Inst.Opcode.Jmp) { X = pc1 };
                    prog[pc1].Y = pc;
                    if (n > 0)
                    {
                        // non-greedy
                        var t = prog[pc1].X;
                        prog[pc1].X = prog[pc1].Y;
                        prog[pc1].Y = t;
                    }

                    break;

                case Type.Plus:
                    pc1 = left.Emit(prog, ref pc);
                    prog[pc] = new Inst(Inst.Opcode.Split) { X = pc1 };
                    pc2 = pc++;
                    prog[pc2].Y = pc;
                    if (n > 0)
                    {
                        // non-greedy
                        var t = prog[pc1].X;
                        prog[pc1].X = prog[pc1].Y;
                        prog[pc1].Y = t;
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return oldPc;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            switch (type)
            {
                case Type.Alt:
                    sb.Append("Alt(").Append(left).Append(", ").Append(right).Append(')');
                    break;

                case Type.Cat:
                    sb.Append("Cat(").Append(left).Append(", ").Append(right).Append(')');
                    break;

                case Type.Lit:
                    sb.Append("Lit(").Append((char) ch).Append(')');
                    break;

                case Type.Dot:
                    sb.Append("Dot");
                    break;

                case Type.Paren:
                    sb.Append("Paren(").Append(n).Append(", ").Append(left).Append(')');
                    break;

                case Type.Star:
                    if (n > 0)
                    {
                        sb.Append("Ng");
                    }

                    sb.Append("Star(").Append(left).Append(')');
                    break;

                case Type.Plus:
                    if (n > 0)
                    {
                        sb.Append("Ng");
                    }

                    sb.Append("Plus(").Append(left).Append(')');
                    break;

                case Type.Quest:
                    if (n > 0)
                    {
                        sb.Append("Ng");
                    }

                    sb.Append("Quest(").Append(left).Append(')');
                    break;

                default:
                    sb.Append("???");
                    break;
            }

            return sb.ToString();
        }

        private class RegexParser : RegexParser<Regex>
        {
            public RegexParser(IRegexParserActions<Regex> actions, string str, RegexOptions options = RegexOptions.None)
                : base(actions, str, options)
            { }

            public new Regex Parse() => base.Parse();
        }

        private class RegexParserActions : IRegexParserActions<Regex>
        {
            public Regex Empty(IRegexContext ctx) => null;

            public Regex Literal(IRegexContext ctx, CharRange range)
            {
                if (Equals(range, CharRange.All))
                {
                    return new Regex(Type.Dot, null, null);
                }

                if (range.TryGetSingle(out var codepoint))
                {
                    return new Regex(Type.Lit, null, null) { ch = codepoint };
                }

                throw new DfaException($"Character sets not supported ({range})");
            }

            public Regex Alternate(IRegexContext ctx, Regex p1, Regex p2) => new Regex(Type.Alt, p1, p2);

            public Regex Catenate(IRegexContext ctx, Regex p1, Regex p2)
            {
                if (p1 == null)
                {
                    return p2;
                }

                if (p2 == null)
                {
                    return p1;
                }

                return new Regex(Type.Cat, p1, p2);
            }

            public Regex Repeat(IRegexContext ctx, Regex p, int min = -1, int max = -1, bool lazy = false)
            {
                var lazyN = lazy ? 1 : 0;
                switch (min, max)
                {
                    case (0, 1):
                        return new Regex(Type.Quest, p, null) { n = lazyN };
                    case (0, -1):
                        return new Regex(Type.Star, p, null) { n = lazyN };
                    case (1, -1):
                        return new Regex(Type.Plus, p, null) { n = lazyN };
                    default:
                        var strMin = min == -1 ? string.Empty : min.ToString();
                        var strMax = max == -1 ? string.Empty : max.ToString();
                        throw new DfaException($"Unsupported repeat {{{strMin},{strMax}}}");
                }
            }

            public Regex Group(IRegexContext ctx, Regex p, int no) => new Regex(Type.Paren, p, null) { n = no };
        }
    }
}
