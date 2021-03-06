/*
 * Copyright 2015 Matthew Timmermans
 * Copyright 2019 Magne Rasmussen
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

using System;
using System.Collections.Generic;

namespace CodeHive.DfaLex
{
    internal class DfaStateInfo
    {
        private readonly int             acceptSetIndex;
        private readonly int             transitionCount;
        private readonly DfaTransition[] transitionBuf;

        internal DfaStateInfo(List<DfaTransition> transitions, int acceptSetIndex)
        {
            this.acceptSetIndex = acceptSetIndex;
            transitionCount = transitions.Count;
            transitionBuf = transitions.ToArray();
        }

        public int GetAcceptSetIndex()
        {
            return acceptSetIndex;
        }

        public int GetTransitionCount()
        {
            return transitionCount;
        }

        public DfaTransition GetTransition(int index)
        {
            return transitionBuf[index];
        }

        public void ForEachTransition(Action<DfaTransition> consumer)
        {
            for (var i = 0; i < transitionCount; ++i)
            {
                consumer(transitionBuf[i]);
            }
        }
    }
}
