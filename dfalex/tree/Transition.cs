namespace CodeHive.DfaLex.tree
{
    internal class Transition
    {
        internal Transition(State state, NfaTransitionPriority priority, Tag tag)
        {
            State = state;
            Priority = priority;
            Tag = tag;
        }

        internal State State { get; }

        internal NfaTransitionPriority Priority { get; }

        internal Tag Tag { get; }

        public override string ToString() => $"{State}, {Priority}, {Tag}";
    }
}
