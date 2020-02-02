namespace CodeHive.DfaLex.tree
{
    internal static class RegexToNfa
    {
        public static TNfa Convert(IMatchable matchable)
        {
            var builder = new TNfa.Builder();
            var target = builder.MakeFinalState();
            var start = matchable.AddToNfa(builder, target, builder.CaptureGroupMaker.EntireMatch);
            builder.SetAsInitial(start);
            return builder.Build();
        }
    }
}
