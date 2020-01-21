using System;

namespace CodeHive.DfaLex.tree
{
    internal class Transition
    {
        internal readonly Priority priority;
        internal readonly State    state;
        internal readonly Tag      tag;

        internal Transition(State state, Priority priority, Tag tag)
        {
            this.state = state;
            this.priority = priority;
            this.tag = tag;
        }

        internal enum Priority
        {
            LOW,
            NORMAL
        }

        public override string ToString() => $"{state}, {priority}, {tag}";
    }
}
