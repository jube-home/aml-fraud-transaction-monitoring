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
using System.Collections;
using System.Collections.Generic;
using Accord.Math;

namespace Accord.Statistics.Visualizations
{
    /// <summary>
    ///     Scatter Plot class.
    /// </summary>
    [Serializable]
    public class ScatterplotClassValueCollection : IEnumerable<DoublePoint>
    {
        private int index;
        private Scatterplot parent;

        internal ScatterplotClassValueCollection(Scatterplot parent, int index)
        {
            this.parent = parent;
            this.index = index;
        }

        /// <summary>
        ///     Gets the integer label associated with this class.
        /// </summary>
        public int Label => parent.LabelValues[index];

        /// <summary>
        ///     Gets the indices of all points of this class.
        /// </summary>
        public int[] Indices
        {
            get { return parent.LabelAxis.Find(x => x == Label); }
        }

        /// <summary>
        ///     Gets all X values of this class.
        /// </summary>
        public double[] XAxis => parent.XAxis.Get(Indices);

        /// <summary>
        ///     Gets all Y values of this class.
        /// </summary>
        public double[] YAxis => parent.YAxis.Get(Indices);

        /// <summary>
        ///     Gets or sets the class' text label.
        /// </summary>
        public string Text
        {
            get => parent.LabelNames[index];
            set => parent.LabelNames[index] = value;
        }

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<DoublePoint> GetEnumerator()
        {
            for (var i = 0; i < parent.LabelAxis.Length; i++)
            {
                if (parent.LabelAxis[i] == Label)
                {
                    yield return new DoublePoint(parent.XAxis[i], parent.YAxis[i]);
                }
            }
        }

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}