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
using System.Collections.ObjectModel;
using System.Linq;
using Accord.Math;
using Accord.Statistics.Filters;

namespace Accord.MachineLearning.DecisionTrees
{
    /// <summary>
    ///     Attribute category.
    /// </summary>
    public enum DecisionVariableKind
    {
        /// <summary>
        ///     Attribute is discrete-valued.
        /// </summary>
        Discrete,

        /// <summary>
        ///     Attribute is continuous-valued.
        /// </summary>
        Continuous
    }

    /// <summary>
    ///     Decision attribute.
    /// </summary>
    [Serializable]
    public class DecisionVariable
    {
        /// <summary>
        ///     Creates a new <see cref="DecisionVariable" />.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="range">The range of valid values for this attribute. Default is [0;1].</param>
        public DecisionVariable(string name, DoubleRange range)
        {
            Name = name;
            Nature = DecisionVariableKind.Continuous;
            Range = range;
        }

        /// <summary>
        ///     Creates a new <see cref="DecisionVariable" />.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="nature">The attribute's nature (i.e. real-valued or discrete-valued).</param>
        public DecisionVariable(string name, DecisionVariableKind nature)
        {
            Name = name;
            Nature = nature;
            Range = new DoubleRange(0, 1);
        }

        /// <summary>
        ///     Creates a new <see cref="DecisionVariable" />.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="range">The range of valid values for this attribute.</param>
        public DecisionVariable(string name, IntRange range)
        {
            Name = name;
            Nature = DecisionVariableKind.Discrete;
            Range = new DoubleRange(range.Min, range.Max);
        }

        /// <summary>
        ///     Creates a new discrete-valued <see cref="DecisionVariable" />.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="symbols">The number of possible values for this attribute.</param>
        public DecisionVariable(string name, int symbols)
            : this(name, new IntRange(0, symbols - 1))
        {
        }

        /// <summary>
        ///     Gets the name of the attribute.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Gets the nature of the attribute (i.e. real-valued or discrete-valued).
        /// </summary>
        public DecisionVariableKind Nature { get; set; }

        /// <summary>
        ///     Gets the valid range of the attribute.
        /// </summary>
        public DoubleRange Range { get; set; }

        /// <summary>
        ///     Creates a new continuous <see cref="DecisionVariable" />.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        public static DecisionVariable Continuous(string name)
        {
            return new DecisionVariable(name, DecisionVariableKind.Continuous);
        }

        /// <summary>
        ///     Creates a new continuous <see cref="DecisionVariable" />.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="range">The range of valid values for this attribute. Default is [0;1].</param>
        public static DecisionVariable Continuous(string name, DoubleRange range)
        {
            return new DecisionVariable(name, range);
        }

        /// <summary>
        ///     Creates a new discrete <see cref="DecisionVariable" />.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="range">The range of valid values for this attribute.</param>
        public static DecisionVariable Discrete(string name, IntRange range)
        {
            return new DecisionVariable(name, range);
        }

        /// <summary>
        ///     Creates a new discrete-valued <see cref="DecisionVariable" />.
        /// </summary>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="symbols">The number of possible values for this attribute.</param>
        public static DecisionVariable Discrete(string name, int symbols)
        {
            return new DecisionVariable(name, symbols);
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return string.Format("{0} : {1} ({2})", Name, Nature, Range);
        }

        /// <summary>
        ///     Creates a set of decision variables from a <see cref="System.Collections.Generic.OrderedDictionary{TKey,TValue}" />
        ///     codebook.
        /// </summary>
        /// <param name="columns">The ordered dictionary containing information about the variables.</param>
        /// <returns>
        ///     An array of <see cref="DecisionVariable" /> objects
        ///     initialized with the values from the codebook.
        /// </returns>
        public static DecisionVariable[] FromDictionary(Collections.OrderedDictionary<string, string[]> columns)
        {
            if (columns.Count == 0)
            {
                throw new ArgumentException("List of columns is empty.");
            }

            var variables = new DecisionVariable[columns.Count];

            for (var i = 0; i < variables.Length; i++)
            {
                var name = columns.GetKeyByIndex(i);
                variables[i] = new DecisionVariable(name, columns[name].Length);
            }

            return variables;
        }

        /// <summary>
        ///     Creates a set of decision variables from a <see cref="Codification" /> codebook.
        /// </summary>
        /// <param name="codebook">The codebook containing information about the variables.</param>
        /// <param name="columns">The columns to consider as decision variables.</param>
        /// <returns>
        ///     An array of <see cref="DecisionVariable" /> objects
        ///     initialized with the values from the codebook.
        /// </returns>
        public static DecisionVariable[] FromCodebook(Codification<string> codebook, params string[] columns)
        {
            if (columns.Length == 0)
            {
                throw new ArgumentException("List of columns is empty.");
            }

            var variables = new DecisionVariable[columns.Length];

            for (var i = 0; i < variables.Length; i++)
            {
                var name = columns[i];

                Codification<string>.Options col;

                if (codebook.Columns.TryGetValue(name, out col))
                {
                    variables[i] = new DecisionVariable(name, col.NumberOfSymbols);
                }
                else
                {
                    variables[i] = new DecisionVariable(name, DecisionVariableKind.Continuous);
                }
            }

            return variables;
        }

        /// <summary>
        ///     Creates a set of decision variables from input data.
        /// </summary>
        /// <param name="inputs">The input data.</param>
        /// <returns>
        ///     An array of <see cref="DecisionVariable" /> objects
        ///     initialized with the values from the codebook.
        /// </returns>
        public static DecisionVariable[] FromData(double[][] inputs)
        {
            if (inputs == null)
            {
                throw new ArgumentNullException("inputs");
            }

            var cols = inputs.Columns();
            var variables = new DecisionVariable[cols];
            for (var i = 0; i < variables.Length; i++)
            {
                variables[i] = new DecisionVariable(i.ToString(), inputs.GetColumn(i)
                    .Where(x => !double.IsNaN(x)).ToArray().GetRange());
            }

            return variables;
        }

        /// <summary>
        ///     Creates a set of decision variables from input data.
        /// </summary>
        /// <param name="inputs">The input data.</param>
        /// <returns>
        ///     An array of <see cref="DecisionVariable" /> objects
        ///     initialized with the values from the codebook.
        /// </returns>
        public static DecisionVariable[] FromData(int[][] inputs)
        {
            var cols = inputs.Columns();
            var variables = new DecisionVariable[cols];
            for (var i = 0; i < variables.Length; i++)
            {
                variables[i] = new DecisionVariable(i.ToString(), inputs.GetColumn(i).GetRange());
            }

            return variables;
        }

        /// <summary>
        ///     Creates a set of decision variables from input data.
        /// </summary>
        /// <param name="inputs">The input data.</param>
        /// <returns>
        ///     An array of <see cref="DecisionVariable" /> objects
        ///     initialized with the values from the codebook.
        /// </returns>
        public static DecisionVariable[] FromData(int?[][] inputs)
        {
            var cols = inputs.Columns();
            var variables = new DecisionVariable[cols];
            for (var i = 0; i < variables.Length; i++)
            {
                variables[i] = new DecisionVariable(i.ToString(), inputs.GetColumn(i)
                    .Where(x => x.HasValue).Select(x => x.Value).ToArray().GetRange());
            }

            return variables;
        }
    }

    /// <summary>
    ///     Collection of decision attributes.
    /// </summary>
    [Serializable]
    public class DecisionVariableCollection : ReadOnlyCollection<DecisionVariable>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DecisionVariableCollection" /> class.
        /// </summary>
        /// <param name="list">The list to initialize the collection.</param>
        public DecisionVariableCollection(IList<DecisionVariable> list)
            : base(list)
        {
        }
    }
}