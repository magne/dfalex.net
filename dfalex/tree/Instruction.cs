using System;

namespace CodeHive.DfaLex.tree
{
    /// <summary>
    /// Immutable instruction for interpretation in tagged automata.
    /// </summary>
    internal abstract class Instruction
    {
        public abstract void Execute(int pos);

        /** Not threadsafe. */
        internal class InstructionMaker
        {
            public static InstructionMaker Get => new InstructionMaker();

            internal Instruction OpeningCommit(History tdash)
            {
                return new OpeningCommitInstruction(tdash);
            }

            internal Instruction ClosingCommit(History newHistory)
            {
                return new ClosingCommitInstruction(newHistory);
            }

            internal Instruction Reorder(History target, History source)
            {
                return new ReorderInstruction(target, source);
            }

            internal Instruction StorePos(History newHistory)
            {
                return new SetInstruction(newHistory, 0);
            }

            internal Instruction StorePosPlusOne(History newHistory)
            {
                return new SetInstruction(newHistory, 1);
            }
        }

        private class OpeningCommitInstruction : Instruction
        {
            private readonly History history;

            public OpeningCommitInstruction(History newHistory)
            {
                history = newHistory ?? throw new ArgumentNullException(nameof(newHistory));
            }

            public override void Execute(int _)
            {
                history.prev = new History(-1L, history.cur, history.prev);
            }

            public override string ToString() => $"c↑({history.id})";
        }

        private class ClosingCommitInstruction : Instruction
        {
            private readonly History history;

            public ClosingCommitInstruction(History newHistory)
            {
                history = newHistory ?? throw new ArgumentNullException(nameof(newHistory));
            }

            public override void Execute(int _)
            {
                history.prev = new History(-1L, history.cur, history.prev);
            }

            public override string ToString() => $"c↓({history.id})";
        }

        private class ReorderInstruction : Instruction
        {
            private readonly History to;
            private readonly History from;

            public ReorderInstruction(History to, History from)
            {
                this.to = to ?? throw new ArgumentNullException(nameof(to));
                this.from = from ?? throw new ArgumentNullException(nameof(from));
            }

            public override void Execute(int pos)
            {
                to.cur = from.cur;
                to.prev = from.prev;
            }

            public override string ToString() => $"{from.id}->{to.id}";
        }

        private class SetInstruction : Instruction
        {
            private readonly History history;
            private readonly int     offset;

            public SetInstruction(History newHistory, int offset)
            {
                history = newHistory ?? throw new ArgumentNullException(nameof(newHistory));
                this.offset = offset;
            }

            public override void Execute(int pos)
            {
                history.cur = pos + offset;
            }

            public override string ToString()
            {
                if (offset == 0)
                {
                    return $"{history.id}<- pos";
                }

                return $"{history.id}<- pos+{offset}";
            }
        }
    }
}
