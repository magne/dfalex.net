using System.Collections.Generic;
using System.Linq;

namespace CodeHive.DfaLex.tree
{
    internal static class RegexToNfa
    {
        public static TNfa Convert(IMatchable matchable)
        {
            var allInputRanges = new List<InputRange>();
            allInputRanges.Add(InputRange.ANY); // All regexes contain this implicitly.
            FindRanges(matchable, allInputRanges);

            var builder = new TNfa.Builder(allInputRanges);
            var target = builder.MakeFinalState();
            var endTagger = builder.AddState();
            builder.AddEpsilon(endTagger, target, NfaTransitionPriority.Normal, Tag.MakeEndTag(builder.CaptureGroupMaker.EntireMatch));
            var match = matchable.AddToNfa(builder, endTagger, builder.CaptureGroupMaker.EntireMatch);
            var start = builder.AddState();
            builder.AddEpsilon(start, match, NfaTransitionPriority.Normal, Tag.MakeStartTag(builder.CaptureGroupMaker.EntireMatch));
            builder.SetAsInitial(start);
            return builder.Build();
        }

        private static void FindRanges(IMatchable matchable, List<InputRange> ranges)
        {
            switch (matchable)
            {
                case CharRange range:
                    for (var i = 0; i < range.bounds.Length; i += 2)
                    {
                        if (i + 1 < range.bounds.Length)
                        {
                            ranges.Add(InputRange.Make(range.bounds[i], (char) (range.bounds[i + 1] - 1)));
                        }
                        else
                        {
                            ranges.Add(InputRange.Make(range.bounds[i], char.MaxValue));
                        }
                    }

                    break;

                case Pattern.StringPattern str:
                    ranges.AddRange(str.tomatch.Select(InputRange.Make));
                    break;

                case Pattern.StringIPattern istr:
                    foreach (var ch in istr.tomatch)
                    {
                        ranges.Add(InputRange.Make(char.ToLowerInvariant(ch)));
                        ranges.Add(InputRange.Make(char.ToUpperInvariant(ch)));
                    }
                    break;
            }

            foreach (var child in matchable.Children)
            {
                FindRanges(child, ranges);
            }
        }
    }
}
