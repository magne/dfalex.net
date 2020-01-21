using System.Runtime.CompilerServices;

namespace CodeHive.DfaLex.tree
{
    internal class CaptureGroup
    {
        internal readonly CaptureGroup parent;
        internal          Tag          startTag;
        internal          Tag          endTag;

        internal CaptureGroup(int number, CaptureGroup parent)
        {
            this.parent = parent ?? this;
            Number = number;
        }

        internal int Number { get; }

        public override string ToString()
        {
            return $"g{Number}";
        }

        internal class CaptureGroupMaker
        {
            internal readonly CaptureGroup entireMatch;
            private           CaptureGroup last;

            internal CaptureGroupMaker()
            {
                var match = Make(0, null);
                entireMatch = match;

                last = entireMatch;
            }

            private CaptureGroup Make(int number, CaptureGroup parent)
            {
                var cg = new CaptureGroup(number, parent);
                cg.startTag = Tag.RealTag.MakeStartTag(cg);
                cg.endTag = Tag.RealTag.MakeEndTag(cg);
                return cg;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public CaptureGroup Next(CaptureGroup parent)
            {
                last = Make(last.Number + 1, parent);
                return last;
            }
        }
    }
}
