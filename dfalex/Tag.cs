/*
 * Copyright 2013 Niko Schwarz
 * Copyright 2020 Magne Rasmussen
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace CodeHive.DfaLex
{
    public abstract class Tag
    {
        public static readonly Tag None = new NoTag();

        public abstract CaptureGroup Group { get; }

        public virtual bool IsStartTag => false;

        public virtual bool IsEndTag => false;

        public static Tag MakeStartTag(CaptureGroup cg) => new RealTag.StartTag(cg);

        public static Tag MakeEndTag(CaptureGroup cg) => new RealTag.EndTag(cg);

        private sealed class NoTag : Tag
        {
            public override CaptureGroup Group => CaptureGroup.NoGroup;

            public override string ToString() => "NONE";
        }

        internal abstract class RealTag : Tag
        {
            private RealTag(CaptureGroup group)
            {
                Group = group;
            }

            public override CaptureGroup Group { get; }

            internal sealed class StartTag : RealTag
            {
                internal StartTag(CaptureGroup group)
                    : base(group)
                { }

                public override bool IsStartTag => true;

                public override string ToString() => $"➀{Group.Number}";
            }

            internal sealed class EndTag : RealTag
            {
                internal EndTag(CaptureGroup group)
                    : base(group)
                { }

                public override bool IsEndTag => true;

                public override string ToString() => $"➁{Group.Number}";
            }
        }
    }
}
