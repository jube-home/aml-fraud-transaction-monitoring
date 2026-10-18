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
using System.Globalization;

namespace Accord.Math
{
    /// <summary>
    ///     Format provider for the matrix format used by Octave.
    /// </summary>
    /// <remarks>
    ///     This class can be used to convert to and from C#
    ///     matrices and their string representation. Please
    ///     see the example for details.
    /// </remarks>
    /// <remarks>
    ///     This class can be used to convert to and from C#
    ///     matrices and their string representation. Please
    ///     see the example for details.
    /// </remarks>
    /// <example>
    ///     <para>
    ///         Converting from a multidimensional matrix to a
    ///         string representation:
    ///     </para>
    ///     <code>
    ///   // Declare a number array
    ///   double[,] x = 
    ///   {
    ///      { 1, 2, 3, 4 },
    ///      { 5, 6, 7, 8 },
    ///   };
    ///   
    ///   // Convert the aforementioned array to a string representation:
    ///   string str = x.ToString(OctaveMatrixFormatProvider.CurrentCulture);
    ///   
    ///   // the final result will be equivalent to
    ///   "[ 1, 2, 3, 4; 5, 6, 7, 8 ]"
    /// </code>
    ///     <para>
    ///         Converting from strings to actual matrices:
    ///     </para>
    ///     <code>
    ///   // Declare an input string
    ///   string str = "[ 1, 2, 3, 4; 5, 6, 7, 8 ]";
    ///   
    ///   // Convert the string representation to an actual number array:
    ///   double[,] matrix = Matrix.Parse(str, OctaveMatrixFormatProvider.InvariantCulture);
    ///   
    ///   // matrix will now contain the actual multidimensional 
    ///   // matrix representation of the given string.
    /// </code>
    /// </example>
    /// <seealso cref="Matrix" />
    /// <seealso cref="CSharpMatrixFormatProvider" />
    /// <seealso cref="CSharpJaggedMatrixFormatProvider" />
    /// <seealso cref="CSharpArrayFormatProvider" />
    /// <seealso cref="OctaveMatrixFormatProvider" />
    /// <seealso cref="OctaveArrayFormatProvider" />
    public sealed class OctaveMatrixFormatProvider : MatrixFormatProviderBase
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="OctaveMatrixFormatProvider" /> class.
        /// </summary>
        public OctaveMatrixFormatProvider(IFormatProvider innerProvider)
            : base(innerProvider)
        {
            FormatMatrixStart = "[";
            FormatMatrixEnd = "]";
            FormatRowStart = string.Empty;
            FormatRowEnd = string.Empty;
            FormatColStart = string.Empty;
            FormatColEnd = string.Empty;
            FormatRowDelimiter = "; ";
            FormatColDelimiter = " ";

            ParseMatrixStart = "[";
            ParseMatrixEnd = "]";
            ParseRowStart = string.Empty;
            ParseRowEnd = string.Empty;
            ParseColStart = string.Empty;
            ParseColEnd = string.Empty;
            ParseRowDelimiter = "; ";
            ParseColDelimiter = " ";
        }

        /// <summary>
        ///     Gets the IMatrixFormatProvider which uses the CultureInfo used by the current thread.
        /// </summary>
        public static OctaveMatrixFormatProvider CurrentCulture { get; } = new(CultureInfo.CurrentCulture);

        /// <summary>
        ///     Gets the IMatrixFormatProvider which uses the invariant system culture.
        /// </summary>
        public static OctaveMatrixFormatProvider InvariantCulture { get; } = new(CultureInfo.InvariantCulture);
    }
}