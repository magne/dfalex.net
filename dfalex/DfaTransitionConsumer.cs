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

namespace CodeHive.DfaLex
{
    /// <summary>
    /// Accept a DFA transition.
    ///
    /// This call indicates that the current state has a transition to target on every character with code point
    /// &gt;= firstChar and &lt;= lastChar
    /// </summary>
    /// <param name="firstChar">First character that triggers this transition</param>
    /// <param name="lastChar">Last character that triggers this transition</param>
    /// <param name="target">Target state of this transition</param>
    internal delegate void DfaTransitionConsumer<TResult>(char firstChar, char lastChar, DfaState<TResult> target);
}
