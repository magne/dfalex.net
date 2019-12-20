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
    internal static class BitUtils
    {
        private static readonly int[] DebruijnWindowToBitPosition =
        {
            -1, 0, 1, 1, 28, 28, 2, 2, 29, 29, 14, 14, 24, 24, 3, 3,
            30, 30, 22, 22, 20, 20, 15, 15, 25, 25, 17, 17, 4, 4, 8, 8,
            31, 31, 27, 27, 13, 13, 23, 23, 21, 21, 19, 19, 16, 16, 7, 7,
            26, 26, 12, 12, 18, 18, 6, 6, 11, 11, 5, 5, 10, 10, 9, 9
        };

        /// <summary>
        /// Get the lowest bit set in X.
        /// </summary>
        /// <param name="x">integer to test</param>
        /// <returns>smallest bit=1&lt;&lt;i, such that (x&amp;bit)!=0, or 0 if x==0</returns>
        public static int LowBit(int x)
        {
            return x & -x; //== x & ~(x-1)
        }

        /// <summary>
        /// Get the index of the lowest bit set in x (from 0 to 31)
        /// </summary>
        /// <param name="x">must not be zero, to get a meaningful result</param>
        /// <returns>least i, such that (x &amp; (1&lt;&lt;i)) != 0, or -1 if x==0</returns>
        public static int LowBitIndex(int x) //undefined if x==0
        {
            x &= -x;
            return DebruijnWindowToBitPosition[((int) ((uint) (x * 0x077CB531) >> 26)) & 63];
        }

        /// <summary>
        /// Turn off the lowest bit in in integer.
        /// </summary>
        /// <param name="x">an integer</param>
        /// <returns>x - lowBit(x);</returns>
        public static int TurnOffLowBit(int x)
        {
            return x & (x - 1);
        }
    }
}
