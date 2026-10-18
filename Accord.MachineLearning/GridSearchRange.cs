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
using System.Collections.ObjectModel;
using Accord.MachineLearning.Performance;
using Accord.Math;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Range of parameters to be tested in a grid search.
    /// </summary>
    /// <seealso cref="GridSearch" />
    [Serializable]
    public class GridSearchRange
    {
        /// <summary>
        ///     Constructs a new GridsearchRange object.
        /// </summary>
        /// <param name="name">The name for this parameter.</param>
        /// <param name="start">The starting value for this range.</param>
        /// <param name="end">The end value for this range.</param>
        /// <param name="step">The step size for this range.</param>
        public GridSearchRange(string name, double start, double end, double step)
        {
            Name = name;
            Values = Vector.Range(start, end, step);
        }

        /// <summary>
        ///     Constructs a new GridSearchRange object.
        /// </summary>
        /// <param name="name">The name for this parameter.</param>
        /// <param name="values">The array of values to try.</param>
        public GridSearchRange(string name, double[] values)
        {
            Name = name;
            Values = values;
        }

        /// <summary>
        ///     Gets or sets the name of the parameter from which the range belongs to.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Gets or sets the range of values that should be tested for this parameter.
        /// </summary>
        public double[] Values { get; set; }

        /// <summary>
        ///     Gets the array of GridSearchParameters to try.
        /// </summary>
        public GridSearchParameter[] GetParameters()
        {
            var parameters = new GridSearchParameter[Values.Length];
            for (var i = 0; i < Values.Length; i++)
            {
                parameters[i] = new GridSearchParameter(Name, Values[i]);
            }

            return parameters;
        }
    }

    /// <summary>
    ///     GridSearchRange collection.
    /// </summary>
    /// <seealso cref="GridSearch" />
    [Serializable]
    public class GridSearchRangeCollection : KeyedCollection<string, GridSearchRange>
    {
        /// <summary>
        ///     Constructs a new collection of GridsearchRange objects.
        /// </summary>
        public GridSearchRangeCollection(params GridSearchRange[] ranges)
        {
            foreach (var range in ranges)
            {
                Add(range);
            }
        }

        /// <summary>
        ///     Returns the identifying value for an item on this collection.
        /// </summary>
        protected override string GetKeyForItem(GridSearchRange item)
        {
            return item.Name;
        }

        /// <summary>
        ///     Adds a parameter range to the end of the GridsearchRangeCollection.
        /// </summary>
        public void Add(string name, params double[] values)
        {
            Add(new GridSearchRange(name, values));
        }
    }
}