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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CodeHive.DfaLex
{
    /// <summary>
    /// Serializable placeholder for DFA states implemented as packed binary search trees.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    [Serializable]
    internal class PackedTreeDfaPlaceholder<TResult> : DfaStatePlaceholder<TResult>
    {
        private static readonly char[]                  NoChars      = new char[0];
        private static readonly DfaStateImpl<TResult>[] NoSuccStates = new DfaStateImpl<TResult>[1];

        // Array-packed binary search tree
        // The BST contains an internal node for char c if the the transition on c is
        // different from the transition on c-1
        // Internal nodes are packed heap-style:
        // the root node is at [0], the children of [x] are at [2x+1] and [2x+2]
        private readonly char[] internalNodes;

        // The leaves of the packed tree, holding the state numbers transitioned to
        // the children of m_internalNodes[x] are at [2x+1-m_internalNodes.length] and [2x+2-m_internalNodes.length]
        // target number -1 means no transition
        private readonly int[]   targetStateNumbers;
        private readonly bool    accepting;
        private readonly TResult match;

        internal PackedTreeDfaPlaceholder(RawDfa<TResult> rawDfa, int stateNum)
        {
            var info = rawDfa.States[stateNum];
            (accepting, match) = rawDfa.AcceptSets[info.GetAcceptSetIndex()];

            var rawTransCount = info.GetTransitionCount();
            if (rawTransCount <= 0)
            {
                internalNodes = NoChars;
                targetStateNumbers = new[] {-1};
                return;
            }

            //Find all characters c such that the transition for c
            //is different from the transition for c-1
            var tempChars = new char[rawTransCount * 2];

            var len = 0;
            var trans = info.GetTransition(0);
            if (trans.FirstChar != '\0')
            {
                tempChars[len++] = trans.FirstChar;
            }

            for (var i = 1; i < rawTransCount; ++i)
            {
                var nextTrans = info.GetTransition(i);
                if (nextTrans.FirstChar > trans.LastChar + 1)
                {
                    //there's a gap between transitions
                    tempChars[len++] = (char) (trans.LastChar + 1);
                    tempChars[len++] = nextTrans.FirstChar;
                }
                else if (nextTrans.State != trans.State)
                {
                    tempChars[len++] = nextTrans.FirstChar;
                }

                trans = nextTrans;
            }

            if (trans.LastChar != char.MaxValue)
            {
                tempChars[len++] = (char) (trans.LastChar + 1);
            }

            if (len < 1)
            {
                //all characters same transition
                internalNodes = NoChars;
                targetStateNumbers = new[] {trans.State};
                return;
            }

            //make the packed tree
            internalNodes = new char[len];
            targetStateNumbers = new int[len + 1];
            _transcribeSubtree(0, new TranscriptionSource(tempChars, info));
        }

        internal override void CreateDelegate(int statenum, List<DfaStatePlaceholder<TResult>> allStates)
        {
            var targetStates = new DfaStateImpl<TResult>[targetStateNumbers.Length];
            for (var i = 0; i < targetStates.Length; ++i)
            {
                var num = targetStateNumbers[i];
                targetStates[i] = num < 0 ? null : allStates[num];
            }

            Delegate = new StateImpl(internalNodes, targetStates, accepting, match, statenum);
        }

        //generate the tree by inorder traversal
        private void _transcribeSubtree(int root, TranscriptionSource ts)
        {
            if (root < internalNodes.Length)
            {
                _transcribeSubtree(root * 2 + 1, ts);
                internalNodes[root] = ts.NextChar();
                _transcribeSubtree(root * 2 + 2, ts);
            }
            else
            {
                targetStateNumbers[root - internalNodes.Length] = ts.GetCurrentTarget();
            }
        }

        //Maintains a cursor in the list of transition characters
        private class TranscriptionSource
        {
            private readonly DfaStateInfo stateInfo;

            private readonly char[] srcChars;

            //cursor position is just before m_srcChars[m_srcPos]
            private int srcPos;

            //transitions an indexes less than this are no longer relvant
            private int currentTrans;

            internal TranscriptionSource(char[] srcChars, DfaStateInfo stateInfo)
            {
                this.srcChars = srcChars;
                srcPos = 0;
                this.stateInfo = stateInfo;
                currentTrans = 0;
            }

            //get the next character and increment the cursor
            internal char NextChar()
            {
                return srcChars[srcPos++];
            }

            internal int GetCurrentTarget()
            {
                //get a representative character
                var c = (srcPos > 0 ? srcChars[srcPos - 1] : '\0');
                //and find the effective transition if any
                for (;; ++currentTrans)
                {
                    if (currentTrans >= stateInfo.GetTransitionCount())
                    {
                        return -1;
                    }

                    var trans = stateInfo.GetTransition(currentTrans);
                    if (trans.LastChar >= c)
                    {
                        return (c >= trans.FirstChar ? trans.State : -1);
                    }
                }
            }
        }

        private class StateImpl : DfaStateImpl<TResult>, IEnumerable<DfaState<TResult>>
        {
            private readonly char[]                  internalNodes;
            private readonly DfaStateImpl<TResult>[] targetStates;
            private readonly TResult                 match;

            internal StateImpl(char[] internalNodes, DfaStateImpl<TResult>[] targetStates, bool accepting, TResult match, int stateNum)
            {
                var haveSucc = targetStates.Any(st => st != null);

                if (!haveSucc)
                {
                    internalNodes = NoChars;
                    targetStates = NoSuccStates;
                }

                this.internalNodes = internalNodes;
                this.targetStates = targetStates;
                this.match = match;
                IsAccepting = accepting;
                StateNumber = stateNum;
            }

            internal override void FixPlaceholderReferences()
            {
                for (var i = 0; i < targetStates.Length; ++i)
                {
                    if (targetStates[i] != null)
                    {
                        targetStates[i] = targetStates[i].ResolvePlaceholder();
                    }
                }
            }

            internal override DfaStateImpl<TResult> ResolvePlaceholder()
            {
                return this;
            }

            public override DfaState<TResult> GetNextState(char ch)
            {
                var i = 0;
                while (i < internalNodes.Length)
                {
                    i = i * 2 + (ch < internalNodes[i] ? 1 : 2);
                }

                return targetStates[i - internalNodes.Length];
            }

            public override bool IsAccepting { get; }

            public override TResult Match
            {
                get
                {
                    if (!IsAccepting)
                    {
                        throw new InvalidOperationException("State is not accepting");
                    }

                    return match;
                }
            }

            public override int StateNumber { get; }

            public override bool HasSuccessorStates =>  targetStates != NoSuccStates;

            public override IEnumerable<DfaState<TResult>> SuccessorStates => this;

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public IEnumerator<DfaState<TResult>> GetEnumerator()
            {
                return new TransitionArrayIterator(targetStates);
            }

            public override void EnumerateTransitions(DfaTransitionConsumer<TResult> consumer)
            {
                if (internalNodes.Length < 1)
                {
                    if (targetStates[0] != null)
                    {
                        consumer('\0', Char.MaxValue, targetStates[0]);
                    }

                    return;
                }

                var lastinternal = EnumInternal(consumer, 0, -1);
                var lastc = internalNodes[lastinternal];
                if (lastc >= char.MaxValue)
                {
                    return;
                }

                DfaState<TResult> state = targetStates[lastinternal * 2 + 2 - internalNodes.Length];
                if (state != null)
                {
                    consumer(lastc, char.MaxValue, state);
                }
            }

            private int EnumInternal(DfaTransitionConsumer<TResult> consumer, int target, int previnternal)
            {
                var child = target * 2 + 1; //left child of target
                if (child < internalNodes.Length)
                {
                    previnternal = EnumInternal(consumer, child, previnternal);
                }

                DfaState<TResult> state;
                var cfrom = (previnternal < 0 ? 0 : internalNodes[previnternal]);
                var cto = internalNodes[target] - 1;
                //between adjacent internal nodes is a leaf
                if (previnternal > target)
                {
                    state = targetStates[previnternal * 2 + 2 - internalNodes.Length];
                }
                else
                {
                    state = targetStates[target * 2 + 1 - internalNodes.Length];
                }

                if (state != null && cfrom <= cto)
                {
                    consumer((char) cfrom, (char) cto, state);
                }

                previnternal = target;
                ++child; // right child of target
                if (child < internalNodes.Length)
                {
                    previnternal = EnumInternal(consumer, child, previnternal);
                }

                return previnternal;
            }
        }

        private sealed class TransitionArrayIterator : IEnumerator<DfaState<TResult>>
        {
            private readonly DfaState<TResult>[] array;
            private          int                 pos;

            internal TransitionArrayIterator(DfaState<TResult>[] array)
            {
                this.array = array;
                for (pos = 0; pos < this.array.Length && this.array[pos] == null; ++pos)
                {
                    // empty
                }
            }

            public bool MoveNext()
            {
                for (; pos < array.Length && array[pos] == null; ++pos)
                {
                    // empty
                }

                return pos < array.Length;
            }

            public void Reset()
            {
                pos = -1;
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                // empty implementation
            }

            public DfaState<TResult> Current => array[pos++];
        }
    }
}
