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
    ///     Common interface for Matrix format providers.
    /// </summary>
    public interface IMatrixFormatProvider : IFormatProvider
    {
        /// <summary>
        ///     Gets the culture specific formatting information
        ///     to be used during parsing or formatting.
        /// </summary>
        IFormatProvider InnerProvider { get; }

        #region Formatting specification

        /// <summary>A string denoting the start of the matrix to be used in formatting.</summary>
        string FormatMatrixStart { get; }

        /// <summary>A string denoting the end of the matrix to be used in formatting.</summary>
        string FormatMatrixEnd { get; }

        /// <summary>A string denoting the start of a matrix row to be used in formatting.</summary>
        string FormatRowStart { get; }

        /// <summary>A string denoting the end of a matrix row to be used in formatting.</summary>
        string FormatRowEnd { get; }

        /// <summary>A string denoting the start of a matrix column to be used in formatting.</summary>
        string FormatColStart { get; }

        /// <summary>A string denoting the end of a matrix column to be used in formatting.</summary>
        string FormatColEnd { get; }

        /// <summary>A string containing the row delimiter for a matrix to be used in formatting.</summary>
        string FormatRowDelimiter { get; }

        /// <summary>A string containing the column delimiter for a matrix to be used in formatting.</summary>
        string FormatColDelimiter { get; }

        #endregion

        #region Parsing specification

        /// <summary>A string denoting the start of the matrix to be used in parsing.</summary>
        string ParseMatrixStart { get; }

        /// <summary>A string denoting the end of the matrix to be used in parsing.</summary>
        string ParseMatrixEnd { get; }

        /// <summary>A string denoting the start of a matrix row to be used in parsing.</summary>
        string ParseRowStart { get; }

        /// <summary>A string denoting the end of a matrix row to be used in parsing.</summary>
        string ParseRowEnd { get; }

        /// <summary>A string denoting the start of a matrix column to be used in parsing.</summary>
        string ParseColStart { get; }

        /// <summary>A string denoting the end of a matrix column to be used in parsing.</summary>
        string ParseColEnd { get; }

        /// <summary>A string containing the row delimiter for a matrix to be used in parsing.</summary>
        string ParseRowDelimiter { get; }

        /// <summary>A string containing the column delimiter for a matrix to be used in parsing.</summary>
        string ParseColDelimiter { get; }

        #endregion
    }
}