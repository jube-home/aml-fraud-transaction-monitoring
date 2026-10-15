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

namespace Accord.Math
{
    /// <summary>
    ///     Base class for IMatrixFormatProvider implementers.
    /// </summary>
    public abstract class MatrixFormatProviderBase : IMatrixFormatProvider
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MatrixFormatProviderBase" /> class.
        /// </summary>
        /// <param name="innerProvider">The inner format provider.</param>
        protected MatrixFormatProviderBase(IFormatProvider innerProvider)
        {
            InnerProvider = innerProvider;
        }

        /// <summary>
        ///     Gets the culture specific formatting information
        ///     to be used during parsing or formatting.
        /// </summary>
        public IFormatProvider InnerProvider { get; protected set; }

        /// <summary>
        ///     Returns an object that provides formatting services for the specified
        ///     type. Currently, only <see cref="IMatrixFormatProvider" /> is supported.
        /// </summary>
        /// <param name="formatType">
        ///     An object that specifies the type of format
        ///     object to return.
        /// </param>
        /// <returns>
        ///     An instance of the object specified by formatType, if the
        ///     <see cref="IFormatProvider">IFormatProvider</see> implementation
        ///     can supply that type of object; otherwise, null.
        /// </returns>
        public object GetFormat(Type formatType)
        {
            // Determine whether custom formatting object is requested.

            if (formatType == typeof(ICustomFormatter))
            {
                return new MatrixFormatter();
            }

            return null;
        }

        #region Formatting specification

        /// <summary>
        ///     A string denoting the start of the matrix to be used in formatting.
        /// </summary>
        public string FormatMatrixStart { get; protected set; }

        /// <summary>
        ///     A string denoting the end of the matrix to be used in formatting.
        /// </summary>
        public string FormatMatrixEnd { get; protected set; }

        /// <summary>
        ///     A string denoting the start of a matrix row to be used in formatting.
        /// </summary>
        public string FormatRowStart { get; protected set; }

        /// <summary>
        ///     A string denoting the end of a matrix row to be used in formatting.
        /// </summary>
        public string FormatRowEnd { get; protected set; }

        /// <summary>
        ///     A string denoting the start of a matrix column to be used in formatting.
        /// </summary>
        public string FormatColStart { get; protected set; }

        /// <summary>
        ///     A string denoting the end of a matrix column to be used in formatting.
        /// </summary>
        public string FormatColEnd { get; protected set; }

        /// <summary>
        ///     A string containing the row delimiter for a matrix to be used in formatting.
        /// </summary>
        public string FormatRowDelimiter { get; protected set; }

        /// <summary>
        ///     A string containing the column delimiter for a matrix to be used in formatting.
        /// </summary>
        public string FormatColDelimiter { get; protected set; }

        #endregion

        #region Parsing specification

        /// <summary>
        ///     A string denoting the start of the matrix to be used in parsing.
        /// </summary>
        public string ParseMatrixStart { get; protected set; }

        /// <summary>
        ///     A string denoting the end of the matrix to be used in parsing.
        /// </summary>
        public string ParseMatrixEnd { get; protected set; }

        /// <summary>
        ///     A string denoting the start of a matrix row to be used in parsing.
        /// </summary>
        public string ParseRowStart { get; protected set; }

        /// <summary>
        ///     A string denoting the end of a matrix row to be used in parsing.
        /// </summary>
        public string ParseRowEnd { get; protected set; }

        /// <summary>
        ///     A string denoting the start of a matrix column to be used in parsing.
        /// </summary>
        public string ParseColStart { get; protected set; }

        /// <summary>
        ///     A string denoting the end of a matrix column to be used in parsing.
        /// </summary>
        public string ParseColEnd { get; protected set; }

        /// <summary>
        ///     A string containing the row delimiter for a matrix to be used in parsing.
        /// </summary>
        public string ParseRowDelimiter { get; protected set; }

        /// <summary>
        ///     A string containing the column delimiter for a matrix to be used in parsing.
        /// </summary>
        public string ParseColDelimiter { get; protected set; }

        #endregion
    }
}