namespace CodeHive.DfaLex.tree
{
    internal class RThread
    {
        internal RThread(int state, Arraylike histories)
        {
            State = state;
            Histories = histories;
        }

        public int State { get; }

        public Arraylike Histories { get; }

        public override string ToString() => $"({State} {Histories})";
    }
}
