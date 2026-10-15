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
    ///     Gets the default matrix representation, where each row
    ///     is separated by a new line, and columns are separated by spaces.
    /// </summary>
    /// <remarks>
    ///     This class can be used to convert to and from C#
    ///     arrays and their string representation. Please
    ///     see the example for details.
    /// </remarks>
    /// <example>
    ///     <para>
    ///         Converting from an array matrix to a
    ///         string representation:
    ///     </para>
    ///     <code>
    ///   // Declare a number array
    ///   double[] x = { 5, 6, 7, 8 };
    ///   
    ///   // Convert the aforementioned array to a string representation:
    ///   string str = x.ToString(DefaultArrayFormatProvider.CurrentCulture);
    ///   
    ///   // the final result will be equivalent to
    ///   "5, 6, 7, 8"
    /// </code>
    ///     <para>
    ///         Converting from strings to actual matrices:
    ///     </para>
    ///     <code>
    ///   // Declare an input string
    ///   string str = "5, 6, 7, 8";
    ///   
    ///   // Convert the string representation to an actual number array:
    ///   double[] array = Matrix.Parse(str, DefaultArrayFormatProvider.InvariantCulture);
    ///   
    ///   // array will now contain the actual number 
    ///   // array representation of the given string.
    /// </code>
    /// </example>
    /// <seealso cref="Matrix" />
    /// <seealso cref="CSharpMatrixFormatProvider" />
    /// <seealso cref="CSharpJaggedMatrixFormatProvider" />
    /// <seealso cref="CSharpArrayFormatProvider" />
    /// <seealso cref="OctaveMatrixFormatProvider" />
    /// <seealso cref="OctaveArrayFormatProvider" />
    public sealed class DefaultArrayFormatProvider : MatrixFormatProviderBase
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DefaultArrayFormatProvider" /> class.
        /// </summary>
        public DefaultArrayFormatProvider(IFormatProvider innerProvider)
            : base(innerProvider)
        {
            FormatMatrixStart = string.Empty;
            FormatMatrixEnd = string.Empty;
            FormatRowStart = string.Empty;
            FormatRowEnd = string.Empty;
            FormatColStart = string.Empty;
            FormatColEnd = string.Empty;
            FormatRowDelimiter = " ";
            FormatColDelimiter = string.Empty;

            ParseMatrixStart = string.Empty;
            ParseMatrixEnd = string.Empty;
            ParseRowStart = string.Empty;
            ParseRowEnd = string.Empty;
            ParseColStart = string.Empty;
            ParseColEnd = string.Empty;
            ParseRowDelimiter = " ";
            ParseColDelimiter = string.Empty;
        }

        /// <summary>
        ///     Gets the IMatrixFormatProvider which uses the CultureInfo used by the current thread.
        /// </summary>
        public static DefaultArrayFormatProvider CurrentCulture { get; } = new(CultureInfo.CurrentCulture);

        /// <summary>
        ///     Gets the IMatrixFormatProvider which uses the invariant system culture.
        /// </summary>
        public static DefaultArrayFormatProvider InvariantCulture { get; } = new(CultureInfo.InvariantCulture);
    }
}