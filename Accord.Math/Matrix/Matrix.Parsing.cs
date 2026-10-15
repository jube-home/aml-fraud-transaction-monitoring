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
    public static partial class Matrix
    {
        #region ToString

        /// <summary>
        ///     Returns a <see cref="System.String" /> representation of a given matrix.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// <example>
        ///     Please see <see cref="CSharpMatrixFormatProvider" />,
        ///     <see cref="CSharpJaggedMatrixFormatProvider" />, <see cref="CSharpArrayFormatProvider" />,
        ///     <see cref="OctaveMatrixFormatProvider" />, or <see cref="OctaveArrayFormatProvider" />
        ///     for more details.
        /// </example>
        public static string ToString<T>(this T[,] matrix)
        {
            return ToString(matrix, DefaultMatrixFormatProvider.CurrentCulture);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> representation of a given matrix.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// <example>
        ///     Please see <see cref="CSharpMatrixFormatProvider" />,
        ///     <see cref="CSharpJaggedMatrixFormatProvider" />, <see cref="CSharpArrayFormatProvider" />,
        ///     <see cref="OctaveMatrixFormatProvider" />, or <see cref="OctaveArrayFormatProvider" />
        ///     for more details.
        /// </example>
        public static string ToCSharp<T>(this T[,] matrix)
        {
            return ToString(matrix, CSharpMatrixFormatProvider.InvariantCulture);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> representation of a given matrix.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// <example>
        ///     Please see <see cref="CSharpMatrixFormatProvider" />,
        ///     <see cref="CSharpJaggedMatrixFormatProvider" />, <see cref="CSharpArrayFormatProvider" />,
        ///     <see cref="OctaveMatrixFormatProvider" />, or <see cref="OctaveArrayFormatProvider" />
        ///     for more details.
        /// </example>
        public static string ToOctave<T>(this T[,] matrix)
        {
            return ToString(matrix, OctaveMatrixFormatProvider.InvariantCulture);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> representation of a given matrix.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <param name="multiline">
        ///     If set to <c>true</c>, the matrix will be written using multiple
        ///     lines. If set to <c>false</c>, the matrix will be written in a
        ///     single line.
        /// </param>
        /// <param name="provider">
        ///     The <see cref="IMatrixFormatProvider" /> to be used
        ///     when creating the resulting string. Default is to use
        ///     <see cref="DefaultMatrixFormatProvider.CurrentCulture" />.
        /// </param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// <example>
        ///     Please see <see cref="CSharpMatrixFormatProvider" />,
        ///     <see cref="CSharpJaggedMatrixFormatProvider" />, <see cref="CSharpArrayFormatProvider" />,
        ///     <see cref="OctaveMatrixFormatProvider" />, or <see cref="OctaveArrayFormatProvider" />
        ///     for more details.
        /// </example>
        public static string ToString<T>(this T[,] matrix, bool multiline, IMatrixFormatProvider provider)
        {
            if (multiline)
            {
                return ToString(matrix, Environment.NewLine, provider);
            }

            return ToString(matrix, null, provider);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> representation of a given matrix.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <param name="provider">
        ///     The <see cref="IMatrixFormatProvider" /> to be used
        ///     when creating the resulting string. Default is to use
        ///     <see cref="DefaultMatrixFormatProvider.CurrentCulture" />.
        /// </param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// <example>
        ///     Please see <see cref="CSharpMatrixFormatProvider" />,
        ///     <see cref="CSharpJaggedMatrixFormatProvider" />, <see cref="CSharpArrayFormatProvider" />,
        ///     <see cref="OctaveMatrixFormatProvider" />, or <see cref="OctaveArrayFormatProvider" />
        ///     for more details.
        /// </example>
        public static string ToString<T>(this T[,] matrix, IMatrixFormatProvider provider)
        {
            return ToString(matrix, null, provider);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> representation of a given matrix.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <param name="format">
        ///     The format to use when creating the resulting string.
        /// </param>
        /// <param name="provider">
        ///     The <see cref="IMatrixFormatProvider" /> to be used
        ///     when creating the resulting string. Default is to use
        ///     <see cref="DefaultMatrixFormatProvider.CurrentCulture" />.
        /// </param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// <example>
        ///     Please see <see cref="CSharpMatrixFormatProvider" />,
        ///     <see cref="CSharpJaggedMatrixFormatProvider" />, <see cref="CSharpArrayFormatProvider" />,
        ///     <see cref="OctaveMatrixFormatProvider" />, or <see cref="OctaveArrayFormatProvider" />
        ///     for more details.
        /// </example>
        public static string ToString<T>(this T[,] matrix, string format, IMatrixFormatProvider provider)
        {
            return MatrixFormatter.Format(format, matrix, provider);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> representation of a given matrix.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <param name="format">
        ///     The format to use when creating the resulting string.
        /// </param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// <example>
        ///     Please see <see cref="CSharpMatrixFormatProvider" />,
        ///     <see cref="CSharpJaggedMatrixFormatProvider" />, <see cref="CSharpArrayFormatProvider" />,
        ///     <see cref="OctaveMatrixFormatProvider" />, or <see cref="OctaveArrayFormatProvider" />
        ///     for more details.
        /// </example>
        public static string ToString<T>(this T[,] matrix, string format)
        {
            return MatrixFormatter.Format(format, matrix, DefaultMatrixFormatProvider.CurrentCulture);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> representation of a given matrix.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// <example>
        ///     Please see <see cref="CSharpMatrixFormatProvider" />,
        ///     <see cref="CSharpJaggedMatrixFormatProvider" />, <see cref="CSharpArrayFormatProvider" />,
        ///     <see cref="OctaveMatrixFormatProvider" />, or <see cref="OctaveArrayFormatProvider" />
        ///     for more details.
        /// </example>
        public static string ToCSharp<T>(this T[][] matrix)
        {
            return ToString(matrix, CSharpJaggedMatrixFormatProvider.InvariantCulture);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> representation of a given matrix.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// <example>
        ///     Please see <see cref="CSharpMatrixFormatProvider" />,
        ///     <see cref="CSharpJaggedMatrixFormatProvider" />, <see cref="CSharpArrayFormatProvider" />,
        ///     <see cref="OctaveMatrixFormatProvider" />, or <see cref="OctaveArrayFormatProvider" />
        ///     for more details.
        /// </example>
        public static string ToOctave<T>(this T[][] matrix)
        {
            return ToString(matrix, OctaveMatrixFormatProvider.InvariantCulture);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> representation of a given matrix.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// <example>
        ///     Please see <see cref="CSharpMatrixFormatProvider" />,
        ///     <see cref="CSharpJaggedMatrixFormatProvider" />, <see cref="CSharpArrayFormatProvider" />,
        ///     <see cref="OctaveMatrixFormatProvider" />, or <see cref="OctaveArrayFormatProvider" />
        ///     for more details.
        /// </example>
        public static string ToString<T>(this T[][] matrix)
        {
            return ToString(matrix, DefaultMatrixFormatProvider.CurrentCulture);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> representation of a given matrix.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <param name="provider">
        ///     The <see cref="IMatrixFormatProvider" /> to be used
        ///     when creating the resulting string. Default is to use
        ///     <see cref="DefaultMatrixFormatProvider.CurrentCulture" />.
        /// </param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// <example>
        ///     Please see <see cref="CSharpMatrixFormatProvider" />,
        ///     <see cref="CSharpJaggedMatrixFormatProvider" />, <see cref="CSharpArrayFormatProvider" />,
        ///     <see cref="OctaveMatrixFormatProvider" />, or <see cref="OctaveArrayFormatProvider" />
        ///     for more details.
        /// </example>
        public static string ToString<T>(this T[][] matrix, IMatrixFormatProvider provider)
        {
            return ToString(matrix, null, provider);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> representation of a given matrix.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <param name="format">
        ///     The format to use when creating the resulting string.
        /// </param>
        /// <param name="provider">
        ///     The <see cref="IMatrixFormatProvider" /> to be used
        ///     when creating the resulting string. Default is to use
        ///     <see cref="DefaultMatrixFormatProvider.CurrentCulture" />.
        /// </param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// <example>
        ///     Please see <see cref="CSharpMatrixFormatProvider" />,
        ///     <see cref="CSharpJaggedMatrixFormatProvider" />, <see cref="CSharpArrayFormatProvider" />,
        ///     <see cref="OctaveMatrixFormatProvider" />, or <see cref="OctaveArrayFormatProvider" />
        ///     for more details.
        /// </example>
        public static string ToString<T>(this T[][] matrix, string format, IMatrixFormatProvider provider)
        {
            return MatrixFormatter.Format(format, matrix, provider);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> representation of a given matrix.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <param name="format">
        ///     The format to use when creating the resulting string.
        /// </param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// <example>
        ///     Please see <see cref="CSharpMatrixFormatProvider" />,
        ///     <see cref="CSharpJaggedMatrixFormatProvider" />, <see cref="CSharpArrayFormatProvider" />,
        ///     <see cref="OctaveMatrixFormatProvider" />, or <see cref="OctaveArrayFormatProvider" />
        ///     for more details.
        /// </example>
        public static string ToString<T>(this T[][] matrix, string format)
        {
            return MatrixFormatter.Format(format, matrix, DefaultMatrixFormatProvider.CurrentCulture);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> representation of a a given array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// <example>
        ///     Please see <see cref="CSharpMatrixFormatProvider" />,
        ///     <see cref="OctaveArrayFormatProvider" /> or <see cref="DefaultArrayFormatProvider" />
        ///     for examples and more details.
        /// </example>
        public static string ToString<T>(this T[] array)
        {
            return ToString(array, DefaultArrayFormatProvider.CurrentCulture);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> representation of a a given array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// <example>
        ///     Please see <see cref="CSharpMatrixFormatProvider" />,
        ///     <see cref="OctaveArrayFormatProvider" /> or <see cref="DefaultArrayFormatProvider" />
        ///     for examples and more details.
        /// </example>
        public static string ToCSharp<T>(this T[] array)
        {
            return ToString(array, CSharpArrayFormatProvider.InvariantCulture);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> representation of a a given array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// <example>
        ///     Please see <see cref="CSharpMatrixFormatProvider" />,
        ///     <see cref="OctaveArrayFormatProvider" /> or <see cref="DefaultArrayFormatProvider" />
        ///     for examples and more details.
        /// </example>
        public static string ToOctave<T>(this T[] array)
        {
            return ToString(array, OctaveArrayFormatProvider.InvariantCulture);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> representation of a a given array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="provider">
        ///     The <see cref="IMatrixFormatProvider" /> to be used
        ///     when creating the resulting string. Default is to use
        ///     <see cref="DefaultMatrixFormatProvider.CurrentCulture" />.
        /// </param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// <example>
        ///     Please see <see cref="CSharpMatrixFormatProvider" />,
        ///     <see cref="OctaveArrayFormatProvider" /> or <see cref="DefaultArrayFormatProvider" />
        ///     for examples and more details.
        /// </example>
        public static string ToString<T>(this T[] array, IMatrixFormatProvider provider)
        {
            return ToString(array, null, provider);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> representation of a a given array.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <param name="format">
        ///     The format to use when creating the resulting string.
        /// </param>
        /// <param name="provider">
        ///     The <see cref="IMatrixFormatProvider" /> to be used
        ///     when creating the resulting string. Default is to use
        ///     <see cref="DefaultMatrixFormatProvider.CurrentCulture" />.
        /// </param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// <example>
        ///     Please see <see cref="CSharpMatrixFormatProvider" />,
        ///     <see cref="OctaveArrayFormatProvider" /> or <see cref="DefaultArrayFormatProvider" />
        ///     for examples and more details.
        /// </example>
        public static string ToString<T>(this T[] matrix, string format, IMatrixFormatProvider provider)
        {
            return MatrixFormatter.Format(format, matrix, provider);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> representation of a a given array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="format">
        ///     The format to use when creating the resulting string.
        /// </param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// <example>
        ///     Please see <see cref="CSharpMatrixFormatProvider" />,
        ///     <see cref="OctaveArrayFormatProvider" /> or <see cref="DefaultArrayFormatProvider" />
        ///     for examples and more details.
        /// </example>
        public static string ToString<T>(this T[] array, string format)
        {
            return MatrixFormatter.Format(format, array, DefaultArrayFormatProvider.CurrentCulture);
        }

        #endregion

        #region Parse

        /// <summary>
        ///     Converts the string representation of a matrix to its
        ///     double-precision floating-point number matrix equivalent.
        /// </summary>
        /// <param name="str">The string representation of the matrix.</param>
        /// <returns>
        ///     A double-precision floating-point number matrix parsed
        ///     from the given string using the given format provider.
        /// </returns>
        public static double[,] Parse(string str)
        {
            return MatrixFormatter.ParseMultidimensional(str, DefaultMatrixFormatProvider.CurrentCulture);
        }

        /// <summary>
        ///     Converts the string representation of a matrix to its
        ///     double-precision floating-point number matrix equivalent.
        /// </summary>
        /// <param name="str">The string representation of the matrix.</param>
        /// <param name="provider">
        ///     The format provider to use in the conversion. Default is to use
        ///     <see cref="DefaultMatrixFormatProvider.CurrentCulture" />.
        /// </param>
        /// <returns>
        ///     A double-precision floating-point number matrix parsed
        ///     from the given string using the given format provider.
        /// </returns>
        public static double[,] Parse(string str, IMatrixFormatProvider provider)
        {
            return MatrixFormatter.ParseMultidimensional(str, provider);
        }

        /// <summary>
        ///     Converts the string representation of a matrix to its
        ///     double-precision floating-point number matrix equivalent.
        /// </summary>
        /// <param name="s">The string representation of the matrix.</param>
        /// <param name="provider">
        ///     The format provider to use in the conversion. Default is to use
        ///     <see cref="DefaultMatrixFormatProvider.CurrentCulture" />.
        /// </param>
        /// <returns>
        ///     A double-precision floating-point number matrix parsed
        ///     from the given string using the given format provider.
        /// </returns>
        [Obsolete("Please use the Jagged.Parse() method instead.")]
        public static double[][] ParseJagged(string s, IMatrixFormatProvider provider)
        {
            return Jagged.Parse(s, provider);
        }

        /// <summary>
        ///     Converts the string representation of a matrix to its
        ///     double-precision floating-point number matrix equivalent.
        ///     A return value indicates whether the conversion succeeded or failed.
        /// </summary>
        /// <param name="s">The string representation of the matrix.</param>
        /// <param name="provider">
        ///     The format provider to use in the conversion. Default is to use
        ///     <see cref="DefaultMatrixFormatProvider.CurrentCulture" />.
        /// </param>
        /// <param name="matrix">
        ///     A double-precision floating-point number matrix parsed
        ///     from the given string using the given format provider.
        /// </param>
        /// <result>
        ///     When this method returns, contains the double-precision floating-point
        ///     number matrix equivalent to the <see param="s" /> parameter, if the conversion succeeded,
        ///     or null if the conversion failed. The conversion fails if the <see param="s" /> parameter
        ///     is null, is not a matrix in a valid format, or contains elements which represent
        ///     a number less than MinValue or greater than MaxValue. This parameter is passed
        ///     uninitialized.
        /// </result>
        public static bool TryParse(string s, IMatrixFormatProvider provider, out double[,] matrix)
        {
            // TODO: Create a proper TryParse method without
            //       resorting to a underlying try-catch block.
            try
            {
                matrix = Parse(s, provider);
            }
            catch (FormatException)
            {
                matrix = null;
            }
            catch (ArgumentNullException)
            {
                matrix = null;
            }

            return matrix != null;
        }

        /// <summary>
        ///     Converts the string representation of a matrix to its
        ///     double-precision floating-point number matrix equivalent.
        ///     A return value indicates whether the conversion succeeded or failed.
        /// </summary>
        /// <param name="s">The string representation of the matrix.</param>
        /// <param name="provider">
        ///     The format provider to use in the conversion. Default is to use
        ///     <see cref="DefaultMatrixFormatProvider.CurrentCulture" />.
        /// </param>
        /// <param name="matrix">
        ///     A double-precision floating-point number matrix parsed
        ///     from the given string using the given format provider.
        /// </param>
        /// <result>
        ///     When this method returns, contains the double-precision floating-point
        ///     number matrix equivalent to the <see param="s" /> parameter, if the conversion succeeded,
        ///     or null if the conversion failed. The conversion fails if the <see param="s" /> parameter
        ///     is null, is not a matrix in a valid format, or contains elements which represent
        ///     a number less than MinValue or greater than MaxValue. This parameter is passed
        ///     uninitialized.
        /// </result>
        public static bool TryParse(string s, IMatrixFormatProvider provider, out double[][] matrix)
        {
            return Jagged.TryParse(s, provider, out matrix);
        }

        #endregion
    }
}