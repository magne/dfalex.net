namespace CodeHive.DfaLex.tree
{
    internal static class RegexToNfa
    {
        public static TNfa Convert(IMatchable matchable)
        {
            var builder = new TNfa.Builder();
            var target = builder.MakeFinalState();
            var endTagger = builder.AddState();
            builder.AddEpsilon(endTagger, target, NfaTransitionPriority.Normal, Tag.MakeEndTag(builder.CaptureGroupMaker.EntireMatch));
            var match = matchable.AddToNfa(builder, endTagger, builder.CaptureGroupMaker.EntireMatch);
            var start = builder.AddState();
            builder.AddEpsilon(start, match, NfaTransitionPriority.Normal, Tag.MakeStartTag(builder.CaptureGroupMaker.EntireMatch));
            builder.SetAsInitial(start);
            return builder.Build();
        }
    }
}
