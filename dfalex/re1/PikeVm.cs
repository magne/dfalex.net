using static CodeHive.DfaLex.re1.Inst.Opcode;

namespace CodeHive.DfaLex.re1
{
    internal static class PikeVm
    {
        private class Thread
        {
            public int pc;
            public Sub sub;

            public Thread(int pc, Sub sub)
            {
                this.pc = pc;
                this.sub = sub;
            }
        }

        private class ThreadList
        {
            private readonly Prog prog;

            public int      n;
            public Thread[] l;

            public ThreadList(Prog prog, int n)
            {
                this.prog = prog;
                l = new Thread[n];
            }

            public void AddThread(Thread t, int cp)
            {
                if (prog[t.pc].gen == gen)
                {
                    t.sub.DecRef();
                    return; // already on list
                }

                prog[t.pc].gen = gen;

                switch (prog[t.pc].OpCode)
                {
                    case Jmp:
                        AddThread(new Thread(prog[t.pc].X, t.sub), cp);
                        break;

                    case Split:
                        AddThread(new Thread(prog[t.pc].X, t.sub.IncRef()), cp);
                        AddThread(new Thread(prog[t.pc].Y, t.sub),          cp);
                        break;

                    case Save:
                        AddThread(new Thread(t.pc + 1, t.sub.Update(prog[t.pc].N, cp)), cp);
                        break;

                    default:
                        l[n++] = t;
                        break;
                }
            }
        }

        private static int gen;

        public static bool Run(Prog prog, string input, int[] subp)
        {
            Sub matched = null;

            for (var i = 0; i < subp.Length; i++)
            {
                subp[i] = -1;
            }

            var len = prog.Length;
            var clist = new ThreadList(prog, len);
            var nlist = new ThreadList(prog, len);

            gen++;
            clist.AddThread(new Thread(0, new Sub(subp.Length)), input[0]);
            for (var sp = 0;; sp++)
            {
                if (clist.n == 0)
                {
                    break;
                }

                gen++;
                for (var i = 0; i < clist.n; i++)
                {
                    var pc = clist.l[i].pc;
                    var sub = clist.l[i].sub;
                    switch (prog[pc].OpCode)
                    {
                        case Char:
                            if (sp >= input.Length || input[sp] != prog[pc].C)
                            {
                                sub.DecRef();
                                break;
                            }

                            nlist.AddThread(new Thread(pc + 1, sub), sp < input.Length - 2 ? input[sp + 1] : 0);
                            break;

                        case Any:
                            if (sp >= input.Length)
                            {
                                sub.DecRef();
                                break;
                            }

                            nlist.AddThread(new Thread(pc + 1, sub), sp < input.Length - 2 ? input[sp + 1] : 0);
                            break;

                        case Match:
                            matched?.DecRef();
                            matched = sub;
                            for (i++; i < clist.n; i++)
                            {
                                clist.l[i].sub.DecRef();
                            }

                            goto BreakFor;
                        // Jmp, Split, Save handled in addthread, so that
                        // machine execution matches what a backtracker would do.
                        // This is discussed (but not shown as code) in
                        // Regular Expression Matching: the Virtual Machine Approach.
                    }
                }

                BreakFor:
                var tmp = clist;
                clist = nlist;
                nlist = tmp;
                nlist.n = 0;
                if (sp >= input.Length)
                {
                    break;
                }
            }

            if (matched != null)
            {
                for (var i = 0; i < subp.Length; i++)
                {
                    subp[i] = matched.sub[i];
                }

                matched.DecRef();
                return true;
            }

            return false;
        }
    }
}
