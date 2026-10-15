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
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Accord.Math
{
    /// <summary>
    ///     Defines how matrices are formatted and displayed, depending on the
    ///     chosen format representation.
    /// </summary>
    public class MatrixFormatter : ICustomFormatter
    {
        /// <summary>
        ///     Converts the value of a specified object to an equivalent string
        ///     representation using specified formatting information.
        /// </summary>
        /// <param name="format">A format string containing formatting specifications.</param>
        /// <param name="arg">An object to format.</param>
        /// <param name="formatProvider">
        ///     An <see cref="T:System.IFormatProvider" /> object that supplies
        ///     format information about the current instance.
        /// </param>
        /// <returns>
        ///     The string representation of the value of <paramref name="arg" />,
        ///     formatted as specified by <paramref name="format" /> and
        ///     <paramref name="formatProvider" />.
        /// </returns>
        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            var provider = formatProvider as IMatrixFormatProvider;
            var obj = arg as Array;

            // Check if the user has provided the correct format provider
            // for a matrix or if the argument is indeed an array (matrix)
            if (provider != null && obj != null)
            {
                return Format(format, obj, provider);
            }

            // No, it has not. Try to provide standard formatting for the type
            return handleOtherFormats(format, arg, CultureInfo.CurrentCulture);
        }

        #region Static methods for output formatting

        /// <summary>
        ///     Converts a jagged or multidimensional array into a <a cref="System.String">System.String</a> representation.
        /// </summary>
        public static string Format(string format, Array matrix, IMatrixFormatProvider formatProvider)
        {
            // Initial argument checking
            if (matrix.Rank > 2)
            {
                throw new NotSupportedException("Matrices with more than two dimensions are not supported.");
            }

            // Try to parse the format options passed through the "format" argument
            string newline, elementFormat;
            if (!parseOptions(format, out newline, out elementFormat))
            {
                throw new FormatException(string.Format("The format of '{0}' is invalid.", format));
            }

            var culture = formatProvider.InnerProvider;


            // Retrieve matrix dimensions. If the matrix is a jagged array,
            //  we will compute the columns for each of the rows.
            var rows = matrix.GetLength(0);
            var cols = matrix.Rank == 2 ? matrix.GetLength(1) : 0;


            // Initialize the matrix construction
            var sb = new StringBuilder();
            sb.Append(formatProvider.FormatMatrixStart);


            // For each row
            for (var i = 0; i < rows; i++)
            {
                // Start constructing the row
                sb.Append(formatProvider.FormatRowStart);

                // Construct the columns for the row
                if (matrix.Rank == 1)
                {
                    var obj = matrix.GetValue(i);
                    var row = obj as Array;

                    if (row == null)
                    {
                        sb.Append(handleOtherFormats(elementFormat, obj, culture));
                    }
                    else
                    {
                        #region Process row for jagged arrays

                        cols = row.Length;

                        // For each column
                        for (var j = 0; j < cols; j++)
                        {
                            sb.Append(handleOtherFormats(elementFormat, row.GetValue(j), culture));

                            if (j < cols - 1)
                            {
                                sb.Append(formatProvider.FormatColDelimiter);
                            }
                        }

                        #endregion
                    }
                }
                else
                {
                    #region Process row for multidimensional arrays

                    // For each column
                    for (var j = 0; j < cols; j++)
                    {
                        sb.Append(handleOtherFormats(elementFormat, matrix.GetValue(i, j), culture));

                        if (j < cols - 1)
                        {
                            sb.Append(formatProvider.FormatColDelimiter);
                        }
                    }

                    #endregion
                }

                // Finalize constructing the row
                sb.Append(formatProvider.FormatRowEnd);

                // Check if we are still in the middle of the row
                if (i < rows - 1)
                {
                    sb.Append(formatProvider.FormatRowDelimiter);
                }
            }

            // Finalize constructing the matrix
            sb.Append(formatProvider.FormatMatrixEnd);


            // Finally, perform post-processing such as replacing user
            // selected newlines or presenting the output in just one line.
            var str = sb.ToString();
            str = str.Replace("\n", newline);

            if (string.IsNullOrEmpty(newline))
            {
                str = Regex.Replace(str, " +", " ");
            }

            return str;
        }

        /// <summary>
        ///     Parses a format string containing the format options for the matrix representation.
        /// </summary>
        private static bool parseOptions(string format, out string newline, out string elementFormat)
        {
            // "{0,g}"      -> multiline with system new line character and number format g
            // "{0:Mr,g}"   -> multiline with \r new line character (old Mac) and number format g
            // "{0:Mn,g}"   -> multiline with \n new line character (Unix-like) and number format g
            // "{0:Mrn,g}"  -> multiline with \r\n new line character (Windows) and number format g
            // "{0:Ms,g}"   -> single line and number format g

            if (string.IsNullOrEmpty(format))
            {
                newline = Environment.NewLine;
                elementFormat = string.Empty;
                return true;
            }

            var options = format.Split(',');

            if (options.Length == 1)
            {
                elementFormat = string.Empty;

                switch (options[0])
                {
                    case "Mr":
                        newline = "\r";
                        return true;

                    case "Mn":
                        newline = "\n";
                        return true;

                    case "Mrn":
                        newline = "\r\n";
                        return true;

                    case "Ms":
                        newline = string.Empty;
                        return true;

                    default:
                        newline = Environment.NewLine;
                        elementFormat = format;
                        return true;
                }
            }

            if (options.Length == 2)
            {
                elementFormat = options[1];

                switch (options[0])
                {
                    case "Mr":
                        newline = "\r"; break;

                    case "Mn":
                        newline = "\n"; break;

                    case "Mrn":
                        newline = "\r\n"; break;

                    case "Ms":
                        newline = string.Empty; break;

                    default:
                        newline = string.Empty;
                        return false;
                }

                return true;
            }

            newline = string.Empty;
            elementFormat = format;
            return false;
        }

        /// <summary>
        ///     Handles formatting for objects other than matrices.
        /// </summary>
        private static string handleOtherFormats(string format, object arg, IFormatProvider culture)
        {
            try
            {
                var obj = arg as IFormattable;
                if (obj != null)
                {
                    return obj.ToString(format, culture);
                }

                if (arg != null)
                {
                    return arg.ToString();
                }
            }
            catch (FormatException e)
            {
                throw new FormatException(string.Format("The format of '{0}' is invalid.", format), e);
            }

            return string.Empty;
        }

        #endregion

        #region Static methods for input parsing

        /// <summary>
        ///     Converts a matrix represented in a System.String into a jagged array.
        /// </summary>
        public static double[][] ParseJagged(string str, IMatrixFormatProvider provider)
        {
            // remove excess spaces
            str = Regex.Replace(str.Replace('\t', ' '), @" +", " ");

            // First remove starting and trailing tokens
            str = str.Remove(0, provider.ParseMatrixStart.Length);
            str = str.Remove(str.Length - provider.ParseMatrixEnd.Length, provider.ParseMatrixEnd.Length);

            // Now split rows
            var strRows = str.Split(new[] { provider.ParseRowDelimiter },
                StringSplitOptions.RemoveEmptyEntries);
            var rows = new List<double[]>();

            foreach (var strRow in strRows)
            {
                var row = strRow.Trim();

                // Remove starting and trailing tokens
                if (row.StartsWith(provider.ParseRowStart, StringComparison.Ordinal))
                {
                    row = row.Remove(0, provider.ParseRowStart.Length);
                }

                if (row.EndsWith(provider.ParseRowEnd, StringComparison.Ordinal))
                {
                    row = row.Remove(row.Length - provider.ParseRowEnd.Length, provider.ParseRowEnd.Length);
                }

                // Now split rows values
                var strCols = row.Split(new[] { provider.ParseColDelimiter },
                    StringSplitOptions.RemoveEmptyEntries);
                var values = new List<double>();

                foreach (var strCol in strCols)
                {
                    var col = Regex.Replace(strCol, @"\s", string.Empty);

                    // Remove starting and trailing tokens
                    if (col.StartsWith(provider.ParseColStart, StringComparison.Ordinal))
                    {
                        col = col.Remove(0, provider.ParseColStart.Length);
                    }

                    if (col.EndsWith(provider.ParseColEnd, StringComparison.Ordinal))
                    {
                        col = col.Remove(col.Length - provider.ParseColEnd.Length, provider.ParseColEnd.Length);
                    }

                    // finally, parse the value and store
                    values.Add(double.Parse(col, provider.InnerProvider));
                }

                rows.Add(values.ToArray());
            }

            return rows.ToArray();
        }

        /// <summary>
        ///     Converts a matrix represented in a System.String into a multi-dimensional array.
        /// </summary>
        public static double[,] ParseMultidimensional(string str, IMatrixFormatProvider provider)
        {
            return ParseJagged(str, provider).ToMatrix();
        }

        #endregion
    }
}