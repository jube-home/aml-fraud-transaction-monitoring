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
using System.Data;
using Accord.MachineLearning;
using Accord.Math;

namespace Accord.Statistics.Filters
{
    /// <summary>
    ///     Codification type.
    /// </summary>
    public enum CodificationVariable
    {
        /// <summary>
        ///     Returns <see cref="Ordinal" />.
        /// </summary>
        Default = Ordinal,

        /// <summary>
        ///     The variable should be codified as an ordinal variable, meaning they will be
        ///     translated to symbols 0, 1, 2, ... n, where n is the total number of distinct
        ///     symbols this variable can assume. This is the default encoding in the
        ///     <see cref="Codification" /> filter.
        /// </summary>
        Ordinal = 0,

        /// <summary>
        ///     This variable should be codified as a 1-of-n vector by creating
        ///     one column for each symbol this variable can assume, and marking
        ///     the column corresponding to the current symbol as 1 and the rest
        ///     as zero.
        /// </summary>
        Categorical = 1,

        /// <summary>
        ///     This variable should be codified as a 1-of-(n-1) vector by creating
        ///     one column for each symbol this variable can assume, except the
        ///     first. This is the same as as <see cref="Categorical" />,
        ///     but the first symbol is handled as a baseline (and should be indicated by
        ///     a zero in every column).
        /// </summary>
        CategoricalWithBaseline = 2,

        /// <summary>
        ///     This variable is continuous and should be not be codified.
        /// </summary>
        Continuous = 3,

        /// <summary>
        ///     This variable is discrete and should be not be codified.
        /// </summary>
        Discrete = 4
    }

    /// <summary>
    ///     Codification Filter class.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The codification filter performs an integer codification of classes in
    ///         given in a string form. An unique integer identifier will be assigned
    ///         for each of the string classes.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         When handling data tables, often there will be cases in which a single
    ///         table contains both numerical variables and categorical data in the form
    ///         of text labels. Since most machine learning and statistics algorithms
    ///         expect their data to be numeric, the codification filter can be used
    ///         to create mappings between text labels and discrete symbols.
    ///     </para>
    ///     <code>
    /// // Show the start data
    /// DataGridBox.Show(table);
    /// </code>
    ///     <img src="..\images\filters\input-table.png" />
    ///     <code>
    /// // Create a new data projection (column) filter
    /// var filter = new Codification(table, "Category");
    /// 
    /// // Apply the filter and get the result
    /// DataTable result = filter.Apply(table);
    /// 
    /// // Show it
    /// DataGridBox.Show(result);
    /// </code>
    ///     <img src="..\images\filters\output-codification.png" />
    ///     <para>
    ///         The following more elaborated examples show how to use the <see cref="Codification" /> filter without
    ///         necessarily handling <c>System.Data.DataTable</c>s.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Statistics\CodificationFilterSvmTest.cs" region="doc_learn_1" />
    ///     <para>
    ///         After we have created the codebook, we can use it to feed data with
    ///         categorical variables to method which would otherwise not know how
    ///         to handle text labels data. Continuing with our example, the next
    ///         code section shows how to convert an entire data table into a numerical
    ///         matrix.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Statistics\CodificationFilterSvmTest.cs" region="doc_learn_2" />
    ///     <para>
    ///         Finally, by expressing our data in terms of a simple numerical
    ///         matrix we will be able to feed it to any machine learning algorithm.
    ///         The following code section shows how to create a
    ///         <see cref="Kernels.Linear">
    ///             linear
    ///         </see>
    ///         multi-class Support Vector Machine to classify ages into any
    ///         of the previously considered text labels ("child", "adult" or "elder").
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\Statistics\CodificationFilterSvmTest.cs" region="doc_learn_3" />
    ///     <para>
    ///         Every Learn() method in the framework expects the class labels to be contiguous and zero-indexed,
    ///         meaning that if there is a classification problem with n classes, all class labels must be numbers
    ///         ranging from 0 to n-1. However, not every dataset might be in this format and sometimes we will
    ///         have to pre-process the data to be in this format. The example below shows how to use the
    ///         Codification class to perform such pre-processing.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\VectorMachines\MulticlassSupportVectorLearningTest.cs"
    ///         region="doc_learn_codification" />
    ///     <para>
    ///         The codification filter can also work with missing values. The example below shows how a codification codebook
    ///         can be created from a dataset that includes missing values and how to use this codebook to replace missing
    ///         values
    ///         by some other representation (in the case below, replacing <c>null</c> by <c>NaN</c> double numbers.
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.MachineLearning\DecisionTrees\C45LearningTest.cs" region="doc_missing" />
    ///     <para>
    ///         The codification can also support more advanced scenarios where it is necessary to use different categorical
    ///         representations for different variables, such as one-hot-vectors and categorical-with-baselines, as shown
    ///         in the example below:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\MultinomialLogisticRegressionAnalysisTest.cs"
    ///         region="doc_learn_1" />
    ///     <para>
    ///         Another examples of an advanced scenario where the source dataset contains both symbolic and
    ///         discrete/continuous
    ///         variables are shown below:
    ///     </para>
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Models\Regression\MultipleLinearRegressionTest.cs"
    ///         region="doc_learn_2" />
    ///     <code source="Unit Tests\Accord.Tests.Statistics\Analysis\MultipleLinearRegressionAnalysisTest.cs"
    ///         region="doc_learn_database" />
    /// </example>
    /// <seealso cref="Codification{T}" />
    [Serializable]
#if NETSTANDARD2_0
    [SurrogateSelector(typeof(Codification.Selector))]
#endif
    public class Codification : Codification<string>, IAutoConfigurableFilter, ITransform<string[], double[]>
    {
        // TODO: Mark redundant methods as obsolete

        /// <summary>
        ///     Creates a new Codification Filter.
        /// </summary>
        public Codification()
        {
        }

#if !NETSTANDARD1_4
        /// <summary>
        ///     Creates a new Codification Filter.
        /// </summary>
        public Codification(DataTable data)
            : base(data)
        {
        }

        /// <summary>
        ///     Creates a new Codification Filter.
        /// </summary>
        public Codification(DataTable data, params string[] columns)
            : base(data, columns)
        {
        }
#endif

        /// <summary>
        ///     Creates a new Codification Filter.
        /// </summary>
        public Codification(string columnName, params string[] values)
            : base(columnName, values)
        {
        }

        /// <summary>
        ///     Creates a new Codification Filter.
        /// </summary>
        public Codification(string[] columnNames, string[][] values)
            : base(columnNames, values)
        {
        }

        /// <summary>
        ///     Creates a new Codification Filter.
        /// </summary>
        public Codification(string columnName, string[][] values)
            : base(columnName, values)
        {
        }

        /// <summary>
        ///     Transforms a matrix of key-value pairs (where the first column
        ///     denotes a key, and the second column a value) into their integer
        ///     vector representation.
        /// </summary>
        /// <param name="values">
        ///     A 2D matrix with two columns, where the first column contains
        ///     the keys (i.e. "Date") and the second column the values (i.e. "14/05/1988").
        /// </param>
        /// <returns>
        ///     A vector of integers where each element contains the translation
        ///     of each respective row in the given <paramref name="values" /> matrix.
        /// </returns>
        public int[] Transform(string[,] values)
        {
            var rows = values.Rows();
            var cols = values.Columns();
            if (cols != 2)
            {
                throw new DimensionMismatchException("values",
                    "The matrix should contain two columns. The first column should contain the key, and the second should contain the value.");
            }

            var result = new int[rows];
            for (var i = 0; i < rows; i++)
            {
                result[i] = Columns[values[i, 0]].Transform(values[i, 1]);
            }

            return result;
        }

        /// <summary>
        ///     Translates a value of a given variable
        ///     into its integer (codeword) representation.
        /// </summary>
        /// <param name="columnName">The name of the variable's data column.</param>
        /// <param name="value">The value to be translated.</param>
        /// <returns>
        ///     An integer which uniquely identifies the given value
        ///     for the given variable.
        /// </returns>
        [Obsolete("Please use Transform(columnName, value) instead.")]
        public int Translate(string columnName, string value)
        {
            return Transform(columnName, value);
        }

        /// <summary>
        ///     Translates an array of values into their
        ///     integer representation, assuming values
        ///     are given in original order of columns.
        /// </summary>
        /// <param name="data">The values to be translated.</param>
        /// <returns>
        ///     An array of integers in which each value
        ///     uniquely identifies the given value for each of
        ///     the variables.
        /// </returns>
        [Obsolete("Please use Transform(data) instead.")]
        public int[] Translate(params string[] data)
        {
            return Transform(data);
        }

#if !NETSTANDARD1_4
        /// <summary>
        ///     Translates an array of values into their
        ///     integer representation, assuming values
        ///     are given in original order of columns.
        /// </summary>
        /// <param name="row">A <see cref="DataRow" /> containing the values to be translated.</param>
        /// <param name="columnNames">
        ///     The columns of the <paramref name="row" /> containing the
        ///     values to be translated.
        /// </param>
        /// <returns>
        ///     An array of integers in which each value
        ///     uniquely identifies the given value for each of
        ///     the variables.
        /// </returns>
        [Obsolete("Please use Transform(row, columnNames) instead.")]
        public int[] Translate(DataRow row, params string[] columnNames)
        {
            return Transform(row, columnNames);
        }
#endif

        /// <summary>
        ///     Translates a value of the given variables
        ///     into their integer (codeword) representation.
        /// </summary>
        /// <param name="columnNames">The names of the variable's data column.</param>
        /// <param name="values">The values to be translated.</param>
        /// <returns>
        ///     An array of integers in which each integer
        ///     uniquely identifies the given value for the given
        ///     variables.
        /// </returns>
        [Obsolete("Please use Transform(columnNames, values) instead.")]
        public int[] Translate(string[] columnNames, string[] values)
        {
            return Transform(columnNames, values);
        }

        /// <summary>
        ///     Translates a value of the given variables
        ///     into their integer (codeword) representation.
        /// </summary>
        /// <param name="columnName">The variable name.</param>
        /// <param name="values">The values to be translated.</param>
        /// <returns>
        ///     An array of integers in which each integer
        ///     uniquely identifies the given value for the given
        ///     variables.
        /// </returns>
        [Obsolete("Please use Transform(columnName, value) instead.")]
        public int[] Translate(string columnName, string[] values)
        {
            return Transform(columnName, values);
        }

        /// <summary>
        ///     Translates a value of the given variables
        ///     into their integer (codeword) representation.
        /// </summary>
        /// <param name="columnName">The variable name.</param>
        /// <param name="values">The values to be translated.</param>
        /// <returns>
        ///     An array of integers in which each integer
        ///     uniquely identifies the given value for the given
        ///     variables.
        /// </returns>
        [Obsolete("Please use Transform(columnName, values) instead.")]
        public int[][] Translate(string columnName, string[][] values)
        {
            return Transform(columnName, values);
        }

        /// <summary>
        ///     Translates an integer (codeword) representation of
        ///     the value of a given variable into its original
        ///     value.
        /// </summary>
        /// <param name="columnName">The variable name.</param>
        /// <param name="codeword">The codeword to be translated.</param>
        /// <returns>The original meaning of the given codeword.</returns>
        [Obsolete("Please use Revert(columnName, codeword) instead.")]
        public string Translate(string columnName, int codeword)
        {
            return Revert(columnName, codeword);
        }

        /// <summary>
        ///     Translates an integer (codeword) representation of
        ///     the value of a given variable into its original
        ///     value.
        /// </summary>
        /// <param name="columnName">The name of the variable's data column.</param>
        /// <param name="codewords">The codewords to be translated.</param>
        /// <returns>The original meaning of the given codeword.</returns>
        [Obsolete("Please use Revert(columnName, codewords) instead.")]
        public string[] Translate(string columnName, int[] codewords)
        {
            return Revert(columnName, codewords);
        }

        /// <summary>
        ///     Translates the integer (codeword) representations of
        ///     the values of the given variables into their original
        ///     values.
        /// </summary>
        /// <param name="columnNames">The name of the variables' columns.</param>
        /// <param name="codewords">The codewords to be translated.</param>
        /// <returns>The original meaning of the given codewords.</returns>
        [Obsolete("Please use Revert(columnNames, codewords) instead.")]
        public string[] Translate(string[] columnNames, int[] codewords)
        {
            return Revert(columnNames, codewords);
        }

#if !NETSTANDARD1_4
        /// <summary>
        ///     Auto detects the filter options by analyzing a given <see cref="DataTable" />.
        /// </summary>
        public void Detect(DataTable data, string[] columns)
        {
            for (var i = 0; i < columns.Length; i++)
            {
                Add(new Options(columns[i]).Learn(data));
            }
        }

        /// <summary>
        ///     Auto detects the filter options by analyzing a given <see cref="DataTable" />.
        /// </summary>
        public void Detect(DataTable data)
        {
            Learn(data);
        }
#endif

        /// <summary>
        ///     Auto detects the filter options by analyzing a set of string labels.
        /// </summary>
        /// <param name="columnName">The variable name.</param>
        /// <param name="values">A set of values that this variable can assume.</param>
        public void Detect(string columnName, string[] values)
        {
            Add(new Options(columnName).Learn(values));
        }

        /// <summary>
        ///     Auto detects the filter options by analyzing a set of string labels.
        /// </summary>
        /// <param name="columnName">The variable name.</param>
        /// <param name="values">A set of values that this variable can assume.</param>
        public void Detect(string columnName, string[][] values)
        {
            Detect(columnName, values.Reshape(0));
        }

        /// <summary>
        ///     Auto detects the filter options by analyzing a set of string labels.
        /// </summary>
        /// <param name="columnNames">The variable names.</param>
        /// <param name="values">
        ///     A set of values that those variable can assume.
        ///     The first element of the array is assumed to be related to the first
        ///     <paramref name="columnNames">column name</paramref> parameter.
        /// </param>
        public void Detect(string[] columnNames, string[][] values)
        {
            for (var i = 0; i < columnNames.Length; i++)
            {
                Add(new Options(columnNames[i]).Learn(values.GetColumn(i)));
            }
        }

        #region Serialization backwards compatibility

#if NETSTANDARD2_0
        internal class Selector : SurrogateSelector
        {
            sealed class DBNullSerializationSurrogate : ISerializationSurrogate
            {
                const string representation = "__System.DBNull__";

                public void GetObjectData(Object obj, SerializationInfo info, StreamingContext context)
                {
                    if (obj as System.DBNull != null)
                        info.AddValue("value", representation);
                }

                public Object SetObjectData(Object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
                {
                    if (info.GetString("value") == representation)
                        return System.DBNull.Value;
                    return null;
                }
            }

            public Selector()
            {
                AddSurrogate(typeof(System.DBNull), new StreamingContext(StreamingContextStates.All), new DBNullSerializationSurrogate());
            }
        }
#endif

        #endregion
    }
}