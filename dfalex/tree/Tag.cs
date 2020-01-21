namespace CodeHive.DfaLex.tree
{
    internal abstract class Tag
    {
        public static readonly Tag None = new NoTag();

        public abstract CaptureGroup Group { get; }

        public virtual bool IsStartTag => false;

        public virtual bool IsEndTag => false;

        private class NoTag : Tag
        {
            private static readonly CaptureGroup NoGroup = new CaptureGroup(-1, null);

            public override CaptureGroup Group => NoGroup;

            public override string ToString() => "NONE";
        }

        internal abstract class RealTag : Tag
        {
            private RealTag(CaptureGroup captureGroup)
            {
                Group = captureGroup;
            }

            public override CaptureGroup Group { get; }

            public static Tag MakeStartTag(CaptureGroup cg)
            {
                return new StartTag(cg);
            }

            public static Tag MakeEndTag(CaptureGroup cg)
            {
                return new EndTag(cg);
            }

            private class StartTag : RealTag
            {
                internal StartTag(CaptureGroup captureGroup)
                    : base(captureGroup)
                { }

                public override bool IsStartTag => true;

                public override string ToString()
                {
                    return $"➀{Group.Number}";
                }
            }

            private class EndTag : RealTag
            {
                public EndTag(CaptureGroup captureGroup)
                    : base(captureGroup)
                { }

                public override bool IsEndTag => true;

                public override string ToString()
                {
                    return $"➁{Group.Number}";
                }
            }
        }
    }
}
