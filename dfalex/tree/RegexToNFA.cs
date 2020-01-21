using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CodeHive.DfaLex.tree
{
    internal class RegexToNFA
    {
        public TNFA convert(ParserProvider.INode node)
        {
            IList<InputRange> allInputRanges = new List<InputRange>();
            allInputRanges.Add(InputRange.ANY); // All regexes contain this implicitly.
            findRanges(node, allInputRanges);
            var builder = TNFA.Builder.make(allInputRanges);

            builder.registerCaptureGroup(builder.captureGroupMaker.entireMatch);

            var m = makeInitialMiniAutomaton(builder, builder.captureGroupMaker.entireMatch);

            var a = make(m, builder, node, builder.captureGroupMaker.entireMatch);

            var endTagger = builder.makeState();
            builder.addEndTagTransition(a.finishing,
                                        endTagger,
                                        builder.captureGroupMaker.entireMatch,
                                        Transition.Priority.NORMAL);

            builder.setAsAccepting(endTagger);
            return builder.build();
        }

        private void findRanges(ParserProvider.INode n, IList<InputRange> @out)
        {
            if (n is ParserProvider.SetItem item)
            {
                @out.Add(item.inputRange);
            }

            foreach (var c in n.Children)
            {
                findRanges(c, @out);
            }
        }

        MiniAutomaton makeInitialMiniAutomaton(TNFA.Builder builder, CaptureGroup entireMatch)
        {
            var init = builder.makeInitialState();
            var startTagger = builder.makeState();
            builder.addStartTagTransition(singleton(init), startTagger, entireMatch, Transition.Priority.NORMAL);
            return new MiniAutomaton(singleton(init), singleton(startTagger));
        }

        private class MiniAutomaton
        {
            internal readonly IList<State> finishing;
            internal readonly IList<State> initial;

            internal MiniAutomaton(IList<State> initial, IList<State> finishing)
            {
                if (!initial.Any())
                {
                    throw new DfaException("No initial state");
                }

                this.initial = initial;
                this.finishing = finishing;
            }

            internal MiniAutomaton(IList<State> initial, State finishing)
                : this(initial, new List<State> { finishing })
            { }

            public override string ToString() => $"{initial} -> {finishing}";
        }

        MiniAutomaton make(MiniAutomaton last, TNFA.Builder builder, ParserProvider.INode node, CaptureGroup captureGroup)
        {
            MiniAutomaton ret;
            switch (node)
            {
                case ParserProvider.Any _:
                    ret = makeAny(last, builder);
                    break;
                case ParserProvider.Char chr:
                    ret = makeChar(last, builder, chr);
                    break;
                case ParserProvider.Simple simple:
                    ret = makeSimple(last, builder, simple, captureGroup);
                    break;
                case ParserProvider.Optional optional:
                    ret = makeOptional(last, builder, optional, captureGroup);
                    break;
                case ParserProvider.NonGreedyStar lazyStar:
                    ret = makeNonGreedyStar(last, builder, lazyStar, captureGroup);
                    break;
                case ParserProvider.Star star:
                    ret = makeStar(last, builder, star, captureGroup);
                    break;
                case ParserProvider.Plus plus:
                    ret = makePlus(last, builder, plus, captureGroup);
                    break;
                case ParserProvider.Group group:
                    ret = makeGroup(last, builder, group, captureGroup);
                    break;
                case ParserProvider.Eos _:
                    ret = makeEos(last, builder);
                    break;
                case ParserProvider.PositiveSet positiveSet:
                    ret = makePositiveSet(last, builder, positiveSet);
                    break;
                case ParserProvider.Union union:
                    ret = makeUnion(last, builder, union, captureGroup);
                    break;
                default:
                    throw new DfaException($"Unknown node type: {node.GetType().FullName}");
            }

            Debug.Assert(!ret.initial.Contains(null));
            Debug.Assert(!ret.finishing.Contains(null));
            return ret;
        }

        MiniAutomaton makeAny(MiniAutomaton last, TNFA.Builder builder)
        {
            var a = builder.makeState();

            builder.addUntaggedTransition(InputRange.ANY, last.finishing, a);

            return new MiniAutomaton(last.finishing, a);
        }

        MiniAutomaton makeChar(MiniAutomaton last, TNFA.Builder b, ParserProvider.Char character)
        {
            var a = b.makeState();
            var ret = new MiniAutomaton(last.finishing, a);

            b.addUntaggedTransition(character.inputRange, ret.initial, a);

            return ret;
        }

        MiniAutomaton makeEos(MiniAutomaton last, TNFA.Builder builder)
        {
            var a = builder.makeState();
            builder.addUntaggedTransition(InputRange.EOS, last.finishing, a);
            return new MiniAutomaton(last.finishing, a);
        }

        MiniAutomaton makeGroup(MiniAutomaton last, TNFA.Builder builder, ParserProvider.Group group, CaptureGroup parentCaptureGroup)
        {
            var cg = builder.makeCaptureGroup(parentCaptureGroup);
            builder.registerCaptureGroup(cg);
            var startGroup = builder.makeState();
            builder.addStartTagTransition(last.finishing, startGroup, cg, Transition.Priority.NORMAL);
            var startGroupAutomaton = new MiniAutomaton(singleton(startGroup), singleton(startGroup));
            var body = make(startGroupAutomaton, builder, group.body, cg);

            var endTag = builder.makeState();
            builder.addEndTagTransition(body.finishing, endTag, cg, Transition.Priority.NORMAL);

            return new MiniAutomaton(last.finishing, endTag);
        }

        MiniAutomaton makeOptional(MiniAutomaton last, TNFA.Builder builder, ParserProvider.Optional optional, CaptureGroup captureGroup)
        {
            var ma = make(last, builder, optional.elementary, captureGroup);

            var f = new List<State>(last.finishing);
            f.AddRange(ma.finishing);

            return new MiniAutomaton(last.finishing, f);
        }

        MiniAutomaton makePlus(MiniAutomaton last, TNFA.Builder builder, ParserProvider.Plus plus, CaptureGroup captureGroup)
        {
            var inner = make(last, builder, plus.elementary, captureGroup);

            IList<State> @out = singleton(builder.makeState());
            builder.makeUntaggedEpsilonTransitionFromTo(inner.finishing, @out, Transition.Priority.LOW);

            var ret = new MiniAutomaton(last.finishing, @out);

            builder.makeUntaggedEpsilonTransitionFromTo(inner.finishing,
                                                        inner.initial,
                                                        Transition.Priority.NORMAL);
            return ret;
        }

        MiniAutomaton makeUnion(MiniAutomaton last, TNFA.Builder builder, ParserProvider.Union union, CaptureGroup captureGroup)
        {
            var left = make(last,  builder, union.left,  captureGroup);
            var right = make(last, builder, union.right, captureGroup);

            var @out = singleton(builder.makeState());
            builder.makeUntaggedEpsilonTransitionFromTo(left.finishing,  @out, Transition.Priority.NORMAL);
            builder.makeUntaggedEpsilonTransitionFromTo(right.finishing, @out, Transition.Priority.LOW);

            return new MiniAutomaton(last.finishing, @out);
        }

        MiniAutomaton makePositiveSet(MiniAutomaton last, TNFA.Builder builder, ParserProvider.PositiveSet set)
        {
            var @is = set.items;
            var ranges = new SortedSet<InputRange>();
            foreach (var i in @is)
            {
                ranges.Add(i.inputRange);
            }

            var rangesList = new List<InputRange>(ranges);
            var cleanedRanges = InputRangeCleanup.CleanUp(rangesList);
            var a = builder.makeState();
            foreach (var range in cleanedRanges)
            {
                builder.addUntaggedTransition(range, last.finishing, a);
            }

            return new MiniAutomaton(last.finishing, a);
        }

        MiniAutomaton makeSimple(MiniAutomaton last, TNFA.Builder b, ParserProvider.Simple simple, CaptureGroup captureGroup)
        {
            var bs = simple.basics;

            var lm = bs.Aggregate(last, (current, e) => make(current, b, e, captureGroup));

            return new MiniAutomaton(last.finishing, lm.finishing);
        }

        MiniAutomaton makeNonGreedyStar(MiniAutomaton last, TNFA.Builder builder, ParserProvider.NonGreedyStar nonGreedyStar, CaptureGroup captureGroup)
        {
            // Make start state and connect.
            var start = builder.makeState();
            builder.makeUntaggedEpsilonTransitionFromTo(last.finishing, singleton(start), Transition.Priority.NORMAL);

            // Make inner machine.
            var innerLast = new MiniAutomaton(last.finishing, start);
            var inner = make(innerLast, builder, nonGreedyStar.elementary, captureGroup);

            // Connect inner machine back to start.
            builder.makeUntaggedEpsilonTransitionFromTo(inner.finishing, singleton(start), Transition.Priority.LOW);

            // Make and connect `@out` state.
            var @out = builder.makeState();
            builder.makeUntaggedEpsilonTransitionFromTo(singleton(start), singleton(@out), Transition.Priority.NORMAL);

            return new MiniAutomaton(last.finishing, @out);
        }

        MiniAutomaton makeStar(MiniAutomaton last, TNFA.Builder builder, ParserProvider.Star star, CaptureGroup captureGroup)
        {
            // Make start state and connect.
            var start = builder.makeState();
            builder.makeUntaggedEpsilonTransitionFromTo(last.finishing, singleton(start), Transition.Priority.NORMAL);

            // Make inner machine.
            var innerLast = new MiniAutomaton(singleton(start), start);
            var inner = make(innerLast, builder, star.elementary, captureGroup);

            // Connect inner machine back to start.
            builder.makeUntaggedEpsilonTransitionFromTo(inner.finishing, singleton(start), Transition.Priority.NORMAL);

            // Make and connect `@out` state.
            var @out = builder.makeState();
            builder.makeUntaggedEpsilonTransitionFromTo(singleton(start), singleton(@out), Transition.Priority.LOW);

            return new MiniAutomaton(last.finishing, @out);
        }

        private IList<State> singleton(State state)
        {
            return new List<State> { state };
        }
    }
}
