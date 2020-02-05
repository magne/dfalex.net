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
    public sealed class CaptureGroup
    {
        internal static readonly CaptureGroup NoGroup = new CaptureGroup(null, -1);

        private CaptureGroup(CaptureGroup parent, int number)
        {
            Parent = parent ?? this;
            Number = number;
        }

        public CaptureGroup Parent { get; }

        public int Number { get; }

        public Tag StartTag { get; private set; }

        public Tag EndTag { get; private set; }

        public override string ToString() => $"g{Number}";

        internal sealed class Maker
        {
            private          CaptureGroup last;

            public Maker()
            {
                EntireMatch = Make(null, 0);
                last = EntireMatch;
            }

            internal CaptureGroup EntireMatch { get; }

            public CaptureGroup Next(CaptureGroup parent)
            {
                last = Make(parent, last.Number + 1);
                return last;
            }

            private static CaptureGroup Make(CaptureGroup parent, int number)
            {
                var cg = new CaptureGroup(parent, number);
                cg.StartTag = Tag.MakeStartTag(cg);
                cg.EndTag = Tag.MakeEndTag(cg);
                return cg;
            }
        }
    }
}
