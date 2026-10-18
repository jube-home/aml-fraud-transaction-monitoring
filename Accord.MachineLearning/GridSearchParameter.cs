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
using Accord.MachineLearning.Performance;

namespace Accord.MachineLearning
{
    /// <summary>
    ///     Contains the name and value of a parameter that should be used during fitting.
    /// </summary>
    /// <seealso cref="GridSearch" />
    [Serializable]
    public struct GridSearchParameter
    {
        /// <summary>
        ///     Gets the name of the parameter
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Gets the value of the parameter.
        /// </summary>
        public double Value { get; }

        /// <summary>
        ///     Constructs a new parameter.
        /// </summary>
        /// <param name="name">The name for the parameter.</param>
        /// <param name="value">The value for the parameter.</param>
        public GridSearchParameter(string name, double value)
        {
            this.Name = name;
            this.Value = value;
        }

        /// <summary>
        ///     Determines whether the specified object is equal
        ///     to the current GridSearchParameter object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is GridSearchParameter)
            {
                var g = (GridSearchParameter)obj;
                if (g.Name != Name || g.Value != Value)
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Returns the hash code for this GridSearchParameter
        /// </summary>
        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ Value.GetHashCode();
        }

        /// <summary>
        ///     Compares two GridSearchParameters for equality.
        /// </summary>
        public static bool operator ==(GridSearchParameter parameter1, GridSearchParameter parameter2)
        {
            return parameter1.Name == parameter2.Name && parameter1.Value == parameter2.Value;
        }

        /// <summary>
        ///     Compares two GridSearchParameters for inequality.
        /// </summary>
        public static bool operator !=(GridSearchParameter parameter1, GridSearchParameter parameter2)
        {
            return parameter1.Name != parameter2.Name || parameter1.Value != parameter2.Value;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0}: {1}", Name, Value);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="GridSearchParameter" /> to <see cref="System.Double" />.
        /// </summary>
        /// <param name="param">The parameter to be converted.</param>
        /// <returns>The value of the parameter's <see cref="Value" />.</returns>
        public static implicit operator double(GridSearchParameter param)
        {
            return param.Value;
        }
    }

    /// <summary>
    ///     Grid search parameter collection.
    /// </summary>
    /// <seealso cref="GridSearch" />
    [Serializable]
    public class GridSearchParameterCollection : KeyedCollection<string, GridSearchParameter>
    {
        /// <summary>
        ///     Constructs a new collection of GridsearchParameter objects.
        /// </summary>
        public GridSearchParameterCollection(params GridSearchParameter[] parameters)
        {
            foreach (var param in parameters)
            {
                Add(param);
            }
        }

        /// <summary>
        ///     Constructs a new collection of GridsearchParameter objects.
        /// </summary>
        public GridSearchParameterCollection(IEnumerable<GridSearchParameter> parameters)
        {
            foreach (var param in parameters)
            {
                Add(param);
            }
        }

        /// <summary>
        ///     Returns the identifying value for an item on this collection.
        /// </summary>
        protected override string GetKeyForItem(GridSearchParameter item)
        {
            return item.Name;
        }
    }
}