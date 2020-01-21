using CodeHive.DfaLex.re1;
using static CodeHive.DfaLex.re1.Inst.Opcode;

namespace CodeHive.DfaLex.Tests.re1
{
    internal static class ThompsonVm
    {
        private class Thread
        {
            public int pc;

            public Thread(int pc)
            {
                this.pc = pc;
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

            public void AddThread(Thread t)
            {
                if (prog[t.pc].gen == gen)
                {
                    return; // already on list
                }

                prog[t.pc].gen = gen;
                l[n++] = t;

                switch (prog[t.pc].OpCode)
                {
                    case Jmp:
                        AddThread(new Thread(prog[t.pc].X));
                        break;
                    case Split:
                        AddThread(new Thread(prog[t.pc].X));
                        AddThread(new Thread(prog[t.pc].Y));
                        break;
                    case Save:
                        AddThread(new Thread(t.pc + 1));
                        break;
                }
            }
        }

        private static int gen;

        public static bool Run(Prog prog, string input, int[] subp)
        {
            for (var i = 0; i < subp.Length; i++)
            {
                subp[i] = -1;
            }

            var len = prog.Length;
            var clist = new ThreadList(prog, len);
            var nlist = new ThreadList(prog, len);

            if (subp.Length >= 1)
            {
                subp[0] = 0;
            }

            gen++;
            clist.AddThread(new Thread(0));
            var matched = false;
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
                    switch (prog[pc].OpCode)
                    {
                        case Char:
                            if (sp >= input.Length || input[sp] != prog[pc].C)
                            {
                                break;
                            }

                            nlist.AddThread(new Thread(pc + 1));
                            break;

                        case Any:
                            nlist.AddThread(new Thread(pc + 1));
                            break;

                        case Match:
                            if (subp.Length >= 2)
                            {
                                subp[1] = sp;
                            }

                            matched = true;
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
                if (sp >=  input.Length)
                {
                    break;
                }
            }

            return matched;
        }
    }
}
