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
using System.Text;

namespace CodeHive.DfaLex
{
    /// <summary>
    /// Exception thrown by default when patterns for multiple results match the same string in a DFA, and no way has
    /// been provided to combine result.
    /// </summary>
    public class DfaAmbiguityException<TResult> : Exception
    {
        /// <summary>
        /// Create a new AmbiguityException.
        /// </summary>
        /// <param name="results">the multiple results for patters that match the same string</param>
        public DfaAmbiguityException(IEnumerable<TResult> results)
            : this(new Initializer(null, results))
        { }

        /// <summary>
        /// Create a new AmbiguityException.
        /// </summary>
        /// <param name="message">The exception detail message</param>
        /// <param name="results">the multiple results for patters that match the same string</param>
        public DfaAmbiguityException(string message, IEnumerable<TResult> results)
            : this(new Initializer(message, results))
        { }

        private DfaAmbiguityException(Initializer inivals)
            : base(inivals.Message)
        {
            Results = inivals.Results;
        }

        /// <summary>
        /// Get the set of results that can match the same string.
        /// </summary>
        /// <returns>set of conflicting results</returns>
        public IList<TResult> Results { get; }

        private class Initializer
        {
            internal readonly string       Message;
            internal readonly List<TResult> Results;

            internal Initializer(string message, IEnumerable<TResult> results)
            {
                Results = new List<TResult>(results);

                if (message == null)
                {
                    var sb = new StringBuilder();
                    sb.Append("The same string can match multiple patterns for: ");
                    var sep = "";
                    foreach (var result in Results)
                    {
                        sb.Append(sep).Append(result);
                        sep = ", ";
                    }

                    message = sb.ToString();
                }

                Message = message;
            }
        }
    }
}
