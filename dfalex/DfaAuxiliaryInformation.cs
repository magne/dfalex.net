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
using System.Linq;
using System.Runtime.CompilerServices;

namespace CodeHive.DfaLex
{
    /// <summary>
    /// Utility class to calculate various auxiliary information about DFAs.
    ///
    /// An instance of this class is created with a set of start states, which all the other methods of this class will
    /// calculate information about.
    ///
    /// Unless otherwise noted, the methods of this class all operated in linear time or better.
    ///
    /// Use one instance of this class to calculate everything you need.  It will remember results you ask for and reuse
    /// them for other calculations when required.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public class DfaAuxiliaryInformation<TResult>
    {
        private static readonly object                  Sentinel = new object();
        private readonly        List<DfaState<TResult>> startStates;
        private                 List<DfaState<TResult>> statesByNumber;
        private                 int[]                   cycleNumbers;
        private                 List<TResult>           destiniesByNumber;

        /// <summary>
        /// Create a new DfaAuxiliaryInformation.
        /// </summary>
        /// <param name="startStates">A collection of start states returned by a single call to
        /// <see cref="DfaBuilder{TResult}"/>. The states must have been returned by a single call, so that the state
        /// numbers of all states they reach will be unique.  Methods of this class will calculate various information
        /// about these states</param>
        public DfaAuxiliaryInformation(IList<DfaState<TResult>> startStates)
        {
            this.startStates = new List<DfaState<TResult>>(startStates.Count);
            this.startStates.AddRange(startStates);
        }

        /// <summary>
        /// Get a list of all states reachable from the start states.
        ///
        /// Multiple calls to this method will return the same list.
        /// </summary>
        /// <returns>a list that contains every state reachable from the start states, with the index of each state s
        /// equal to s.getStateNumber().  Unused indexes will have null values.</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public IList<DfaState<TResult>> GetStatesByNumber()
        {
            if (statesByNumber == null)
            {
                var statesByNums = new List<DfaState<TResult>>();
                var q = new Queue<DfaState<TResult>>();
                foreach (var state in startStates)
                {
                    if (state != null)
                    {
                        var i = state.GetStateNumber();
                        while (statesByNums.Count <= i)
                        {
                            statesByNums.Add(null);
                        }

                        if (statesByNums[i] == null)
                        {
                            statesByNums[i] = state;
                            q.Enqueue(state);
                        }
                    }
                }

                while (q.Any())
                {
                    var state = q.Dequeue();
                    state.EnumerateTransitions((@in, @out, target) =>
                    {
                        var i = target.GetStateNumber();
                        while (statesByNums.Count <= i)
                        {
                            statesByNums.Add(null);
                        }

                        if (statesByNums[i] == null)
                        {
                            statesByNums[i] = target;
                            q.Enqueue(target);
                        }
                    });
                }

                this.statesByNumber = statesByNums;
            }

            return statesByNumber;
        }

        /// <summary>
        /// Perform a depth first search of all states, starting at the start states
        ///
        /// To avoid stack overflow errors on large DFAs, the implementation uses an auxiliary
        /// stack on the heap instead of recursing
        /// </summary>
        /// <param name="onEnter">called with (parent, child) when a child is entered.  parent == null for roots.</param>
        /// <param name="onSkip">called with (parent, child) when a child is skipped because it has been entered previously.
        /// parent == null for roots.</param>
        /// <param name="onLeave">called with (parent, child) when a child is exited.  parent == null for roots.</param>
        public void DepthFirstSearch(Action<DfaState<TResult>, DfaState<TResult>> onEnter,
                                     Action<DfaState<TResult>, DfaState<TResult>> onSkip,
                                     Action<DfaState<TResult>, DfaState<TResult>> onLeave)
        {
            var iterators = new IEnumerator<DfaState<TResult>>[GetStatesByNumber().Count];
            var stack = new Stack<DfaState<TResult>>();
            for (var rootIndex = 0; rootIndex < startStates.Count; ++rootIndex)
            {
                var st = startStates[rootIndex];
                if (iterators[st.GetStateNumber()] != null)
                {
                    onSkip(null, st);
                    continue;
                }

                iterators[st.GetStateNumber()] = st.GetSuccessorStates().GetEnumerator();
                stack.Push(st);
                onEnter(null, st);
                for (;;)
                {
                    //process the next child of the stack top
                    st = stack.Peek();
                    var sti = st.GetStateNumber();
                    var iter = iterators[sti];
                    if (iter.MoveNext())
                    {
                        var child = iter.Current;
                        if (child == null)
                        {
                            //shouldn't happen, but if it does get the next child
                            continue;
                        }

                        var childi = child.GetStateNumber();
                        if (iterators[childi] != null)
                        {
                            onSkip(st, child);
                        }
                        else
                        {
                            iterators[childi] = child.GetSuccessorStates().GetEnumerator();
                            stack.Push(child);
                            onEnter(st, child);
                        }
                    }
                    else
                    {
                        //top element is done
                        stack.Pop();
                        if (!stack.Any())
                        {
                            onLeave(null, st);
                            break;
                        }

                        onLeave(stack.Peek(), st);
                    }
                }
            }
        }

        /// <summary>
        /// Get an array that maps each state number to the state's 'cycle number', such that:
        /// <UL><LI>States that are not in a cycle have cycle number -1
        /// </LI><LI>States that are in a cycle have cycle number &gt;= 0
        /// </LI><LI>States in cycles have the same cycle number IFF they are in the same cycle
        ///      (i.e., they are reachable from each other)
        /// </LI><LI>Cycles are compactly numbered from 0
        /// </LI></UL>
        /// Note that states with cycle numbers &gt;=0 match an infinite number of different strings, while
        /// states with cycle number -1 match a finite number of strings with lengths &lt;= the size
        /// of this array.
        /// </summary>
        /// <returns>the cycle numbers array</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public int[] GetCycleNumbers()
        {
            if (this.cycleNumbers != null)
            {
                return this.cycleNumbers;
            }

            //Tarjan's algorithm
            var pindex = new[] { 0 };
            var pcycle = new[] { 0 };
            var stack = new Stack<DfaState<TResult>>();
            var orderIndex = new int[GetStatesByNumber().Count];
            var backLink = new int[orderIndex.Length];
            var cycleNums = new int[orderIndex.Length];
            for (var i = 0; i < orderIndex.Length; ++i)
            {
                orderIndex[i] = -1;
                backLink[i] = -1; //not on stack
                cycleNums[i] = -1; //no cycle
            }

            Action<DfaState<TResult>, DfaState<TResult>> onEnter = (parent, child) =>
            {
                stack.Push(child);
                backLink[child.GetStateNumber()] = orderIndex[child.GetStateNumber()] = pindex[0]++;
            };
            Action<DfaState<TResult>, DfaState<TResult>> onSkip = (parent, child) =>
            {
                var childLink = backLink[child.GetStateNumber()];
                if (parent != null && childLink >= 0 && childLink < backLink[parent.GetStateNumber()])
                {
                    backLink[parent.GetStateNumber()] = childLink;
                }
            };
            Action<DfaState<TResult>, DfaState<TResult>> onExit = (parent, child) =>
            {
                var childi = child.GetStateNumber();
                var childLink = backLink[childi];
                if (childLink == orderIndex[childi])
                {
                    //child is a cycle root
                    var cycleNum = -1;
                    if (stack.Peek() != child)
                    {
                        cycleNum = pcycle[0]++;
                    }

                    for (;;)
                    {
                        var st = stack.Pop();
                        var sti = st.GetStateNumber();
                        cycleNums[sti] = cycleNum;
                        backLink[sti] = -1;
                        if (st == child)
                        {
                            break;
                        }
                    }
                }

                if (parent != null && childLink >= 0 && childLink < backLink[parent.GetStateNumber()])
                {
                    backLink[parent.GetStateNumber()] = childLink;
                }
            };

            this.cycleNumbers = cycleNums;
            DepthFirstSearch(onEnter, onSkip, onExit);
            return cycleNums;
        }

        /// <summary>
        /// Get a list that maps each state number to the state's "destiny"
        ///
        /// If all strings accepted by the state produce the same TResult,
        /// then that TResult is the state's destiny.  Otherwise the state's
        /// destiny is null.
        /// </summary>
        /// <returns>The list of destinies by state number</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public IList<TResult> GetDestinies()
        {
            if (destiniesByNumber != null)
            {
                return destiniesByNumber;
            }

            GetCycleNumbers();
            var numCycles = 0;
            for (var i = 0; i < cycleNumbers.Length; ++i)
            {
                if (cycleNumbers[i] >= numCycles)
                {
                    numCycles = cycleNumbers[i] + 1;
                }
            }

            var destinies = new object[GetStatesByNumber().Count];
            var cycleDestinies = new object[numCycles];
            Action<DfaState<TResult>, DfaState<TResult>> onEnter = (parent, child) =>
            {
                var childi = child.GetStateNumber();
                var cycle = cycleNumbers[childi];
                var match = child.IsAccepting ? child.Match : (object) null;
                if (cycle >= 0)
                {
                    cycleDestinies[cycle] = DestinyMerge(cycleDestinies[cycle], match);
                }
                else
                {
                    destinies[childi] = match;
                }
            };
            Action<DfaState<TResult>, DfaState<TResult>> onMerge = (parent, child) =>
                {
                    if (parent != null)
                    {
                        var childi = child.GetStateNumber();
                        var pari = parent.GetStateNumber();
                        var cycle = cycleNumbers[childi];
                        var o = (cycle >= 0 ? cycleDestinies[cycle] : destinies[childi]);
                        cycle = cycleNumbers[pari];
                        if (cycle >= 0)
                        {
                            cycleDestinies[cycle] = DestinyMerge(cycleDestinies[cycle], o);
                        }
                        else
                        {
                            destinies[pari] = DestinyMerge(destinies[pari], o);
                        }
                    }
                }
                ;
            DepthFirstSearch(onEnter, onMerge, onMerge);

            for (var i = 0; i < destinies.Length; ++i)
            {
                var cycleNum = cycleNumbers[i];
                var o = (cycleNum >= 0 ? cycleDestinies[cycleNum] : destinies[i]);
                destinies[i] = (o == Sentinel ? default : (TResult) o);
            }

            destiniesByNumber = new List<TResult>(destinies.Cast<TResult>());
            return destiniesByNumber;
        }


        private static object DestinyMerge(object a, object b)
        {
            if (b == null)
            {
                return a;
            }

            if (a == null)
            {
                return b;
            }

            if (a == Sentinel || b == Sentinel)
            {
                return Sentinel;
            }

            return a.Equals(b) ? a : Sentinel;
        }
    }
}
