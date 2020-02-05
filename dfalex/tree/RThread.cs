namespace CodeHive.DfaLex.tree
{
    internal class RThread
    {
        internal RThread(State state, Arraylike histories)
        {
            State = state;
            Histories = histories;
        }

        public State State { get; }

        public Arraylike Histories { get; }

        public override string ToString() => $"({State} {Histories})";
    }
}
