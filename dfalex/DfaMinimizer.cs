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
using System.Diagnostics;

namespace CodeHive.DfaLex
{
    /// <summary>
    /// Converts a DFA into a minimal DFA using a fast variant of Hopcroft's algorithm
    /// </summary>
    internal class DfaMinimizer<TResult>
    {
        private readonly RawDfa<TResult>    origDfa;
        private readonly List<DfaStateInfo> origStates;
        private readonly int[]              newStartStates;

        //map from transition target to all sources, using original state numbers
        private readonly int[][] origBackReferences;

        //for each original state, it's current partition number
        //during partitioning, partition numbers are the partition start position in m_partitionOrderStates
        //when that's done, they are contiguous ints
        private readonly int[] origOrderPartNums;

        //the original state numbers, sorted by partition number
        private readonly int[] partitionOrderStates;

        private readonly int[]              origOrderHashes;
        private readonly int[]              hashBuckets;
        private readonly int[]              scratchSpace;
        private readonly int[]              scratchPartitions;
        private readonly int                hashTableSize;
        private readonly List<DfaStateInfo> minStates = new List<DfaStateInfo>();

        public DfaMinimizer(RawDfa<TResult> dfa)
        {
            origDfa = dfa;
            origStates = dfa.States;
            origBackReferences = _createBackReferences();
            origOrderPartNums = new int[origStates.Count];
            partitionOrderStates = new int[origStates.Count];
            origOrderHashes = new int[origStates.Count];
            scratchSpace = new int[origStates.Count];
            scratchPartitions = new int[origStates.Count];
            hashTableSize = PrimeSizeFinder.FindPrimeSize(origStates.Count);
            hashBuckets = new int[hashTableSize];
            newStartStates = new int[origDfa.StartStates.Length];
            CreateMinimalPartitions();
            CreateNewStates();
        }

        public RawDfa<TResult> GetMinimizedDfa()
        {
            return new RawDfa<TResult>(minStates, origDfa.AcceptSets, newStartStates);
        }

        private void CreateNewStates()
        {
            minStates.Clear();
            var tempTrans = new List<NfaTransition>();
            foreach (var statenum in partitionOrderStates)
            {
                var partnum = origOrderPartNums[statenum];
                if (partnum < minStates.Count)
                {
                    continue;
                }

                Debug.Assert(partnum == minStates.Count);

                //compress and redirect transitions
                tempTrans.Clear();
                var instate = origStates[statenum];
                var inlen = instate.GetTransitionCount();
                var inpos = 0;
                while (inpos < inlen)
                {
                    var trans = instate.GetTransition(inpos++);
                    var startc = trans.FirstChar;
                    var endc = trans.LastChar;
                    var dest = origOrderPartNums[trans.State];
                    for (; inpos < inlen; ++inpos)
                    {
                        trans = instate.GetTransition(inpos);
                        if (trans.FirstChar - endc > 1 || origOrderPartNums[trans.State] != dest)
                        {
                            break;
                        }

                        endc = trans.LastChar;
                    }

                    tempTrans.Add(new NfaTransition(startc, endc, dest));
                }

                minStates.Add(new DfaStateInfo(tempTrans, instate.GetAcceptSetIndex()));
            }

            var origStartStates = origDfa.StartStates;
            for (var i = 0; i < newStartStates.Length; ++i)
            {
                newStartStates[i] = origOrderPartNums[origStartStates[i]];
            }
        }

        private void CreateMinimalPartitions()
        {
            if (partitionOrderStates.Length <= 0)
            {
                return;
            }
            //create initial partitioning
            //States in a partition are contiguous in m_partitionOrderStates
            //m_origOrderPartNums maps from state to partition number

            //initially, we just set these up so that states with different accept
            //sets will not compare equal
            for (var i = 0; i < origOrderPartNums.Length; ++i)
            {
                origOrderPartNums[i] = origStates[i].GetAcceptSetIndex();
                partitionOrderStates[i] = i;
            }

            //Then we repartition the whole state set, which will set m_origOrderPartNums and
            //m_partitionOrderStates properly and do an initial partitioning by previous
            //partition (accept set) AND transitions
            _repartition(0, partitionOrderStates.Length);

            //from now on during partitioning, partition numbers are the partition start position in m_partitionOrderStates

            //We use this queue to keep track of all partitions that might need to split.  When it's
            //empty we are done.  Note that partition numbers are stable, since they are the index
            //of the partition's first state in m_partitionOrderStates
            var closureQ = new IntRangeClosureQueue(partitionOrderStates.Length);

            //Initially, all of our partitions are new and need to be checked
            foreach (var partitionOrderState in partitionOrderStates)
            {
                closureQ.Add(origOrderPartNums[partitionOrderState]);
            }

            //split partitions as necessary until we're done
            int targetPart;
            while ((targetPart = closureQ.Poll()) >= 0)
            {
                //find contiguous range in m_partitionOrderStates conrresponding to the target partition
                var targetEnd = targetPart + 1;
                while (targetEnd < partitionOrderStates.Length && origOrderPartNums[partitionOrderStates[targetEnd]] == targetPart)
                {
                    ++targetEnd;
                }

                //repartition it if necessary
                _repartition(targetPart, targetEnd);

                //queue other partitions.  Two states that were assumed to be equivalent,
                //because they had transitions into targetPart on the same character, might
                //now be recognizable as distict, because those transitions now go to different
                //partitions.
                //Any partition that transitions to two or more of our new partitions
                //needs to be queued for repartitioning

                //STEP 1: for each partition that transitions to the old target, remember
                //ONE of the new partitions it transitions to.
                for (var i = targetPart; i < targetEnd; ++i)
                {
                    var st = partitionOrderStates[i];
                    var partNum = origOrderPartNums[st]; //NEW partition number for st!
                    foreach (var src in origBackReferences[st])
                    {
                        var srcPart = origOrderPartNums[src];
                        scratchSpace[srcPart] = partNum;
                    }
                }

                //STEP 2: for each partition that transitions to the old target, see if any
                //of the new transitions it goes to are different from the one we remembered
                for (var i = targetPart; i < targetEnd; ++i)
                {
                    var st = partitionOrderStates[i];
                    var partNum = origOrderPartNums[st];
                    foreach (var src in origBackReferences[st])
                    {
                        var srcPart = origOrderPartNums[src];
                        if (scratchSpace[srcPart] != partNum)
                        {
                            closureQ.Add(srcPart);
                        }
                    }
                }
            }

            //now renumber the partitions with contiguous ints instead of start positions
            {
                var st = partitionOrderStates[0];
                var prevPartIn = origOrderPartNums[st];
                origOrderPartNums[st] = 0;
                var prevPartOut = 0;
                for (var i = 1; i < partitionOrderStates.Length; ++i)
                {
                    st = partitionOrderStates[i];
                    var partIn = origOrderPartNums[st];
                    if (partIn != prevPartIn)
                    {
                        prevPartIn = partIn;
                        ++prevPartOut;
                    }

                    origOrderPartNums[st] = prevPartOut;
                }
            }
        }

        //given the start and end of a partition in m_partitionOrderStartStates, repartition it
        //into smaller partitions (equivalence classes) according to current information
        //each partition will end up with a number equal to its start position in m_partitionOrderStartStates
        private void _repartition(int start, int end)
        {
            if (end <= start)
            {
                return;
            }

            //hash all the states and initialize negated counts in the hash buckets
            for (var i = start; i < end; i++)
            {
                var state = partitionOrderStates[i];
                var h = _hashOrig(state);
                origOrderHashes[state] = h;
                var bucket = (h & int.MaxValue) % hashTableSize;
                hashBuckets[bucket] = ~0;
            }

            //calculate negated counts
            for (var i = start; i < end; i++)
            {
                var state = partitionOrderStates[i];
                var h = origOrderHashes[state];
                var bucket = (h & int.MaxValue) % hashTableSize;
                hashBuckets[bucket] -= 1;
            }

            //turn counts into start positions
            var totalLen = 0;
            for (var i = start; i < end; i++)
            {
                var state = partitionOrderStates[i];
                var h = origOrderHashes[state];
                var bucket = (h & int.MaxValue) % hashTableSize;
                var oldVal = hashBuckets[bucket];
                if (oldVal < 0)
                {
                    hashBuckets[bucket] = totalLen;
                    totalLen += ~oldVal;
                }
            }

            Debug.Assert(totalLen == end - start);
            //copy states in bucket order, turning start positions into end positions
            for (var i = start; i < end; i++)
            {
                var state = partitionOrderStates[i];
                var h = origOrderHashes[state];
                var bucket = (h & int.MaxValue) % hashTableSize;
                var pos = hashBuckets[bucket]++;
                scratchSpace[pos] = state;
            }

            //copy bucket order back into partition order, separating different states in the same bucket
            var destpos = start;
            for (var bucketStart = 0; bucketStart < totalLen;)
            {
                var state = scratchSpace[bucketStart];
                var hash = origOrderHashes[state];
                var bucket = (hash & int.MaxValue) % hashTableSize;
                var bucketEnd = hashBuckets[bucket];
                Debug.Assert(destpos == bucketStart + start);
                partitionOrderStates[destpos++] = state;
                scratchPartitions[state] = bucketStart + start;
                var missPos = bucketStart;
                var nextPos = bucketStart + 1;
                //add equivalent states in the same bucket
                for (; nextPos < bucketEnd; ++nextPos)
                {
                    var tempst = scratchSpace[nextPos];

                    if (origOrderHashes[tempst] == hash && _compareOrig(tempst, state))
                    {
                        partitionOrderStates[destpos++] = tempst;
                        scratchPartitions[tempst] = bucketStart + start;
                    }
                    else
                    {
                        scratchSpace[missPos++] = tempst;
                    }
                }

                while (missPos > bucketStart)
                {
                    scratchSpace[--nextPos] = scratchSpace[--missPos];
                }

                bucketStart = nextPos;
            }

            //all the counts line up and all states copied
            Debug.Assert(destpos == end);

            //update partition numbers
            for (var i = start; i < end; ++i)
            {
                var state = partitionOrderStates[i];
                origOrderPartNums[state] = scratchPartitions[state];
            }
        }

        //compute back-references for all states, for m_origBackReferences
        private int[][] _createBackReferences()
        {
            var nstates = origStates.Count;
            var backRefCounts = new int[origStates.Count];
            for (var st = 0; st < nstates; ++st)
            {
                origStates[st].ForEachTransition(trans => backRefCounts[trans.State]++);
            }

            var backrefs = new int[nstates][];
            for (var st = 0; st < nstates; ++st)
            {
                backrefs[st] = new int[backRefCounts[st]];
                backRefCounts[st] = 0;
            }

            var captureFix = new int[1]; //avoid making a new consumer for each st
            for (var st = 0; st < nstates; ++st)
            {
                captureFix[0] = st;
                origStates[st].ForEachTransition(trans =>
                {
                    var target = trans.State;
                    backrefs[target][backRefCounts[target]++] = captureFix[0];
                });
            }

            //dedup
            for (var st = 0; st < nstates; ++st)
            {
                var refs = backrefs[st];
                if (refs.Length < 1)
                {
                    continue;
                }

                Array.Sort(refs);
                var newLen = 1;
                for (var s = 1; s < refs.Length; ++s)
                {
                    if (refs[s] != refs[s - 1])
                    {
                        refs[newLen++] = refs[s];
                    }
                }

                if (newLen != refs.Length)
                {
                    backrefs[st] = new int[newLen];
                    Array.Copy(refs, backrefs[st], newLen);
                }
            }

            return backrefs;
        }

        //Make a hash of the original state, using its transitions and
        //the current partition
        private int _hashOrig(int st)
        {
            var h = origOrderPartNums[st] * 5381;
            var nextc = 0;
            var prevtarget = -1;
            var info = origStates[st];
            var len = info.GetTransitionCount();
            for (var i = 0; i < len; i++)
            {
                var trans = info.GetTransition(i);
                var curtarget = origOrderPartNums[trans.State] & 0x7FFFFFFF;
                if (trans.FirstChar != nextc && prevtarget != -1)
                {
                    h *= 65599;
                    h += nextc;
                    h *= 65599;
                    prevtarget = -1;
                }

                if (curtarget != prevtarget)
                {
                    h *= 65599;
                    h += trans.FirstChar;
                    h *= 65599;
                    h += curtarget + 1;
                    prevtarget = curtarget;
                }

                nextc = trans.LastChar + 1;
            }

            if (nextc < 0x10000 && prevtarget != -1)
            {
                h *= 65599;
                h += nextc;
            }

            h *= 65599;
            h ^= (h >> 16);
            h ^= (h >> 8);
            h ^= (h >> 4);
            h ^= (h >> 2);
            return h;
        }

        //Compare two original states to see if they're equivalent, as far as
        //we know based on the current partitioning and transitions
        private bool _compareOrig(int st1, int st2)
        {
            if (origOrderPartNums[st1] != origOrderPartNums[st2])
            {
                return false;
            }

            var info1 = origStates[st1];
            var info2 = origStates[st2];
            var len1 =
                info1.GetTransitionCount();
            var len2 =
                info2.GetTransitionCount();
            if (len2 <= 0)
            {
                return (len1 <= 0);
            }

            if (len1 <= 0)
            {
                return false;
            }

            int pos1 = 1, pos2 = 1;
            var trans1 = info1.GetTransition(0);
            var trans2 = info2.GetTransition(0);
            var nextc = 0;
            for (;;)
            {
                while (trans1.LastChar < nextc)
                {
                    if (pos1 >= len1)
                    {
                        trans1 = null;
                        break;
                    }

                    trans1 = info1.GetTransition(pos1++);
                }

                while (trans2.LastChar < nextc)
                {
                    if (pos2 >= len2)
                    {
                        trans2 = null;
                        break;
                    }

                    trans2 = info2.GetTransition(pos2++);
                }

                if (trans1 == null || trans2 == null)
                {
                    // ReSharper disable once PossibleUnintendedReferenceComparison
                    return trans1 == trans2;
                }

                if (trans1.FirstChar > nextc || trans2.FirstChar > nextc)
                {
                    if (trans1.FirstChar != trans2.FirstChar)
                    {
                        return false;
                    }
                }

                if (origOrderPartNums[trans1.State] != origOrderPartNums[trans2.State])
                {
                    return false;
                }

                nextc = Math.Min(trans1.LastChar + 1, trans2.LastChar + 1);
            }
        }
    }
}
