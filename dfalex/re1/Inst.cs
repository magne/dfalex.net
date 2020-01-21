namespace CodeHive.DfaLex.re1
{
    internal class Inst
    {
        internal enum Opcode
        {
            Char = 1,
            Match,
            Jmp,
            Split,
            Any,
            Save
        }

        public int gen; // Global state, oooh!

        public Inst(Opcode opcode)
        {
            OpCode = opcode;
        }

        public Opcode OpCode { get; }
        public int C { get; set; }
        public int N { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }
}
