/*
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
using System.Runtime.Serialization;

#nullable enable
namespace CodeHive.DfaLex
{
    /// <summary>
    /// Base exception class for DfaLex library.
    /// </summary>
    [Serializable]
    public class DfaException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DfaException"/> class.
        /// </summary>
        public DfaException()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DfaException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DfaException(string? message)
            : base(message)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DfaException"/> class with a specified error message and
        /// a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference
        /// if no inner exception is specified.</param>
        public DfaException(string? message, Exception? innerException)
            : base(message, innerException)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DfaException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">he <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="info"/> parameter is null.</exception>
        /// <exception cref="SerializationException">The class name is null or <see cref="Exception.HResult"/> is zero (0).</exception>
        protected DfaException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
