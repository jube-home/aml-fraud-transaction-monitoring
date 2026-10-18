// Accord.NET Framework (originally)
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
// Relicensed to MIT per the author's own explicit grant made when the
// upstream project was archived (2020-11-19):
// https://github.com/accord-net/framework
// "I am also willing to change the license of any file where I am the
// single author... to MIT so people can reuse individual pieces of code
// more easily." -- Cesar De Souza
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;

namespace Accord
{
    /// <summary>
    ///     Dimension Mismatch Exception.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The dimension mismatch exception is thrown in cases where a method expects
    ///         a matrix or array object having specific or compatible dimensions, such as the inner matrix
    ///         dimensions in matrix multiplication.
    ///     </para>
    /// </remarks>
    [Serializable]
    public class DimensionMismatchException : ArgumentException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DimensionMismatchException" /> class.
        /// </summary>
        public DimensionMismatchException()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DimensionMismatchException" /> class.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the current exception.</param>
        public DimensionMismatchException(string paramName) :
            base("Array dimensions must match.", paramName)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DimensionMismatchException" /> class.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the current exception.</param>
        /// <param name="message">Message providing some additional information.</param>
        public DimensionMismatchException(string paramName, string message) :
            base(message, paramName)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DimensionMismatchException" /> class.
        /// </summary>
        /// <param name="message">Message providing some additional information.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public DimensionMismatchException(string message, Exception innerException) :
            base(message, innerException)
        {
        }
    }
}