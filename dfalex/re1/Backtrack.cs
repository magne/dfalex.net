using System.Collections.Generic;
using static CodeHive.DfaLex.re1.Inst.Opcode;

namespace CodeHive.DfaLex.re1
{
    internal static class Backtrack
    {
        internal class Thread
        {
            public int pc;
            public int sp;
            public Sub sub;

            public Thread(int pc, int sp, Sub sub)
            {
                this.pc = pc;
                this.sp = sp;
                this.sub = sub;
            }
        }

        public static bool Run(Prog prog, string str, int[] subp)
        {
            const int maxThreads = 1000;
            var ready = new Stack<Thread>(maxThreads);

            // queue initial thread
            var newSub = new Sub(subp.Length);

            ready.Push(new Thread(0, 0, newSub));

            // run threads in stack order
            while (ready.Count > 0)
            {
                var t = ready.Pop();
                var pc = t.pc;
                var sp = t.sp;
                var sub = t.sub;
                while (true)
                {
                    switch (prog[pc].OpCode)
                    {
                        case Char:
                            if (sp >= str.Length || str[sp] != prog[pc].C)
                            {
                                goto Dead;
                            }

                            pc++;
                            sp++;
                            continue;

                        case Any:
                            if (sp >= str.Length)
                            {
                                goto Dead;
                            }

                            pc++;
                            sp++;
                            continue;

                        case Match:
                            for (var i = 0; i < subp.Length; i++)
                            {
                                subp[i] = sub.sub[i];
                            }

                            sub.DecRef();
                            return true;

                        case Jmp:
                            pc = prog[pc].X;
                            continue;

                        case Split:
                            if (ready.Count >= maxThreads)
                            {
                                throw new DfaException("backtrack overflow");
                            }

                            ready.Push(new Thread(prog[pc].Y, sp, sub.IncRef()));
                            pc = prog[pc].X; /* continue current thread */
                            continue;

                        case Save:
                            sub = sub.Update(prog[pc].N, sp);
                            pc++;
                            continue;
                    }
                }

                Dead:
                sub.DecRef();
            }

            return false;
        }
    }
}
