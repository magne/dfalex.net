using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeHive.DfaLex.tree
{
    internal class ParserProvider
    {
        internal interface INode
        {
            IList<INode> Children { get; }
        }

        internal interface IRegex : INode
        {
            // <union> | <simple-RE>
        }

        internal interface IElementary : IBasic
        {
            // <group> | <any> | <eos> | <char> | <set>
        }

        internal interface IBasic : INode
        {
            // <star> | <plus> | <elementary-RE>
        }

        internal class Eos : IElementary
        {
            public IList<INode> Children => new INode[0];
        }

        internal class Any : IElementary
        {
            public IList<INode> Children => new INode[0];

            public override string ToString() => ".";
        }

        // Char || Range
        internal abstract class SetItem : INode
        {
            protected internal readonly InputRange inputRange;

            protected SetItem(InputRange inputRange)
            {
                this.inputRange = inputRange;
            }

            public IList<INode> Children => new INode[0];

            public override string ToString() => throw new DfaException("Overwrite me");
        }

        internal abstract class Char : SetItem, IElementary
        {
            protected Char(InputRange ir)
                : base(ir)
            { }

            public override string ToString() => inputRange.From.ToString();
        }

        internal class SimpleChar : Char
        {
            public SimpleChar(char character)
                : base(InputRange.Make(character))
            { }

            public override string ToString() => $"{inputRange.From.ToString()}";
        }

        internal class EscapedChar : Char
        {
            public EscapedChar(char character)
                : base(InputRange.Make(character))
            { }

            public override string ToString() => $"\\{inputRange.From.ToString()}";
        }

        internal sealed class Group : IElementary
        {
            internal readonly INode body;

            public Group(INode body)
            {
                this.body = body;
            }

            public IList<INode> Children => new[] { body };

            public override string ToString() => $"({body})";
        }

        internal abstract class Set : IElementary
        {
            protected internal readonly IList<SetItem> items;

            protected Set(IList<SetItem> items)
            {
                this.items = items;
            }

            public virtual IList<INode> Children => items.Cast<INode>().ToList();

            public override string ToString() => throw new DfaException("Overwrite me");
        }

        private sealed class NegativeSet : Set
        {
            public NegativeSet(IList<SetItem> items)
                : base(items)
            { }

            public override IList<INode> Children => throw new DfaException("Not implemented");

            public override string ToString()
            {
                var s = new StringBuilder();
                s.Append("[^");
                foreach (var i in items)
                {
                    s.Append(i);
                }

                s.Append("]");
                return s.ToString();
            }
        }

        internal sealed class PositiveSet : Set
        {
            public PositiveSet(IList<SetItem> items)
                : base(items)
            { }

            public override string ToString()
            {
                var s = new StringBuilder();
                s.Append("[");
                foreach (var i in items)
                {
                    s.Append(i);
                }

                s.Append("]");
                return s.ToString();
            }
        }

        internal sealed class Optional : IBasic
        {
            internal readonly IElementary elementary;

            public Optional(IElementary elementary)
            {
                this.elementary = elementary;
            }

            public IList<INode> Children => new INode[] { elementary };

            public override string ToString() => $"{elementary}?";
        }

        internal sealed class Plus : IBasic
        {
            internal readonly IElementary elementary;

            public Plus(IElementary elementary)
            {
                this.elementary = elementary;
            }

            public IList<INode> Children => new INode[] { elementary };

            public override string ToString() => $"{elementary}+";
        }

        private sealed class Range : SetItem
        {
            public Range(char from, char to)
                : base(InputRange.Make(from, to))
            { }

            public override string ToString() => $"{inputRange.From.ToString()}-{inputRange.To.ToString()}";
        }

        internal class Simple : IRegex
        {
            internal readonly IList<IBasic> basics;

            public Simple(List<IBasic> basics)
            {
                this.basics = basics;
            }

            public IList<INode> Children => basics.Cast<INode>().ToList();

            public override string ToString()
            {
                var s = new StringBuilder();
                foreach (var b in basics)
                {
                    s.Append(b);
                }

                return s.ToString();
            }
        }

        internal class Star : IBasic
        {
            internal readonly IElementary elementary;

            public Star(IElementary elementary)
            {
                this.elementary = elementary;
            }

            public IList<INode> Children => new INode[] { elementary };

            public override string ToString() => $"{elementary}*";
        }

        internal class NonGreedyStar : IBasic
        {
            internal readonly IElementary elementary;

            public NonGreedyStar(IElementary elementary)
            {
                this.elementary = elementary;
            }

            public IList<INode> Children => new INode[] { elementary };

            public override string ToString() => $"{elementary}*?";
        }

        internal sealed class Union : IRegex
        {
            internal readonly INode left;
            internal readonly INode right;

            public Union(INode left, INode right)
            {
                this.left = left;
                this.right = right;
            }

            public IList<INode> Children => new[] { left, right };

            public override string ToString() => $"{left}|{right}";
        }

        private static readonly IRegexParserActions<INode> Actions = new RegexParserActions();

        internal INode Parse(string str)
        {
            return new RegexParser(Actions, str).Parse();
        }

        private class RegexParser : RegexParser<INode>
        {
            public RegexParser(IRegexParserActions<INode> actions, string str, RegexOptions options = RegexOptions.None)
                : base(actions, str, options)
            { }

            public new INode Parse() => base.Parse();
        }

        private class RegexParserActions : IRegexParserActions<INode>
        {
            public INode Empty(IRegexContext ctx) => null;

            public INode Literal(IRegexContext ctx, CharRange range)
            {
                const string escaped = "\t\n\r\f\u0007\u001b()[].*?+";
                const string translated = "tnrfae()[].*?+";

                bool IsEscaped(int ch) => escaped.IndexOf((char) ch) != -1;
                char Translate(int ch) => translated[escaped.IndexOf((char) ch)];
                Char MakeChar(char ch) => IsEscaped(ch) ? (Char) new EscapedChar(Translate(ch)) : new SimpleChar(ch);

                IList<SetItem> MakeItems(char[] bounds, int offset)
                {
                    var items = new List<SetItem>();
                    for (var i = offset; i < bounds.Length; i += 2)
                    {
                        var min = bounds[i];
                        var max = (char) (bounds[i + 1] - 1);
                        if (min == max)
                        {
                            items.Add(MakeChar(min));
                        }
                        else
                        {
                            items.Add(new Range(min, max));
                        }
                    }

                    return items;
                }

                if (Equals(range, CharRange.All))
                {
                    return new Any();
                }

                if (range.TryGetSingle(out var codepoint))
                {
                    return MakeChar((char) codepoint);
                }

                if (range.bounds.Length % 2 == 1 && range.bounds[0] == '\u0000')
                {
                    return new NegativeSet(MakeItems(range.bounds, 1));
                }

                return new PositiveSet(MakeItems(range.bounds, 0));
            }

            public INode Alternate(IRegexContext ctx, INode p1, INode p2) => new Union(p1, p2);

            public INode Catenate(IRegexContext ctx, INode p1, INode p2)
            {
                if (p1 == null)
                {
                    return p2;
                }

                if (p2 == null)
                {
                    return p1;
                }

                if (p1 is Simple simple && p2 is IBasic basic)
                {
                    simple.basics.Add(basic);
                    return simple;
                }

                return new Simple(new List<IBasic> { (IBasic) p1, (IBasic) p2 });
            }

            public INode Repeat(IRegexContext ctx, INode p, int min = -1, int max = -1, bool lazy = false)
            {
                switch (min, max)
                {
                    case (0, 1):
                        return new Optional(p as IElementary);

                    case (0, -1):
                        if (lazy)
                        {
                            return new NonGreedyStar(p as IElementary);
                        }

                        return new Star(p as IElementary);

                    case (1, -1):
                        return new Plus(p as IElementary);

                    default:
                        var strMin = min == -1 ? string.Empty : min.ToString();
                        var strMax = max == -1 ? string.Empty : max.ToString();
                        throw new DfaException($"Unsupported repeat {{{strMin},{strMax}}}");
                }
            }

            public INode Group(IRegexContext ctx, INode p, int no) => new Group(p);
        }
    }
}
