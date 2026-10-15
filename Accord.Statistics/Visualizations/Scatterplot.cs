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
using Accord.Math;

namespace Accord.Statistics.Visualizations
{
    /// <summary>
    ///     Scatter Plot.
    /// </summary>
    [Serializable]
    public class Scatterplot
    {
        private string labelTitle = "Label";
        private string title = "Scatter plot";

        private string xAxisTitle = "X";
        private string yAxisTitle = "Y";

        /// <summary>
        ///     Constructs an empty Scatter plot.
        /// </summary>
        public Scatterplot()
        {
        }

        /// <summary>
        ///     Constructs an empty Scatter plot with given title.
        /// </summary>
        /// <param name="title">Scatter plot title.</param>
        public Scatterplot(string title)
        {
            this.title = title;
        }

        /// <summary>
        ///     Constructs an empty scatter plot with
        ///     given title and axis names.
        /// </summary>
        /// <param name="title">Scatter Plot title.</param>
        /// <param name="xAxisTitle">Title for the x-axis.</param>
        /// <param name="yAxisTitle">Title for the y-axis.</param>
        public Scatterplot(string title, string xAxisTitle, string yAxisTitle)
        {
            this.title = title;
            this.xAxisTitle = xAxisTitle;
            this.yAxisTitle = yAxisTitle;
        }

        /// <summary>
        ///     Constructs an empty Scatter Plot with
        ///     given title and axis names.
        /// </summary>
        /// <param name="title">Scatter Plot title.</param>
        /// <param name="xAxisTitle">Title for the x-axis.</param>
        /// <param name="yAxisTitle">Title for the y-axis.</param>
        /// <param name="labelTitle">Title for the labels.</param>
        public Scatterplot(string title, string xAxisTitle, string yAxisTitle, string labelTitle)
        {
            this.title = title;
            this.xAxisTitle = xAxisTitle;
            this.yAxisTitle = yAxisTitle;
            this.labelTitle = labelTitle;
        }

        /// <summary>
        ///     Gets the title of the scatter plot.
        /// </summary>
        public string Title
        {
            get => title;
            set => title = value;
        }

        /// <summary>
        ///     Gets the name of the X-axis.
        /// </summary>
        public string XAxisTitle
        {
            get => xAxisTitle;
            set => xAxisTitle = value;
        }

        /// <summary>
        ///     Gets the name of the Y-axis.
        /// </summary>
        public string YAxisTitle
        {
            get => yAxisTitle;
            set => yAxisTitle = value;
        }

        /// <summary>
        ///     Gets the name of the label axis.
        /// </summary>
        public string LabelAxisTitle
        {
            get => labelTitle;
            set => labelTitle = value;
        }

        /// <summary>
        ///     Gets the values associated with the X-axis.
        /// </summary>
        public double[] XAxis { get; private set; }

        /// <summary>
        ///     Gets the corresponding Y values associated with each X.
        /// </summary>
        public double[] YAxis { get; private set; }

        /// <summary>
        ///     Gets the label of each (x,y) pair.
        /// </summary>
        public int[] LabelAxis { get; private set; }

        /// <summary>
        ///     Gets an integer array containing the integer labels
        ///     associated with each of the classes in the scatter plot.
        /// </summary>
        internal int[] LabelValues { get; private set; }

        /// <summary>
        ///     Gets the class labels for each of the classes in the plot.
        /// </summary>
        public string[] LabelNames { get; private set; }

        /// <summary>
        ///     Gets a collection containing information about
        ///     each of the classes presented in the scatter plot.
        /// </summary>
        public ReadOnlyCollection<ScatterplotClassValueCollection> Classes { get; private set; }

        private void initialize(double[] x, double[] y, int[] z)
        {
            XAxis = x;
            YAxis = y;
            LabelAxis = z;

            if (z == null)
            {
                LabelValues = new int[] { };
            }
            else
            {
                LabelValues = LabelAxis.Distinct();
            }

            LabelNames = new string[LabelValues.Length];
            for (var i = 0; i < LabelNames.Length; i++)
            {
                LabelNames[i] = i.ToString();
            }

            var classes = new ScatterplotClassValueCollection[LabelValues.Length];
            for (var i = 0; i < classes.Length; i++)
            {
                classes[i] = new ScatterplotClassValueCollection(this, i);
            }

            Classes = new ReadOnlyCollection<ScatterplotClassValueCollection>(classes);
        }

        /// <summary>
        ///     Computes the scatter plot.
        /// </summary>
        /// <param name="values">Array of values.</param>
        public void Compute(double[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            var idx = Vector.Range(0.0, values.Length);

            initialize(idx, values, null);
        }

        /// <summary>
        ///     Computes the scatter plot.
        /// </summary>
        /// <param name="x">Array of X values.</param>
        /// <param name="y">Array of corresponding Y values.</param>
        public void Compute(double[] x, double[] y)
        {
            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            if (y == null)
            {
                throw new ArgumentNullException("y");
            }

            if (x.Length != y.Length)
            {
                throw new DimensionMismatchException("y", "The x and y arrays should have the same length");
            }

            initialize(x, y, null);
        }

        /// <summary>
        ///     Computes the scatter plot.
        /// </summary>
        /// <param name="x">Array of X values.</param>
        /// <param name="y">Array of corresponding Y values.</param>
        /// <param name="labels">Array of integer labels defining a class for each (x,y) pair.</param>
        public void Compute(double[] x, double[] y, int[] labels)
        {
            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            if (y == null)
            {
                throw new ArgumentNullException("y");
            }

            if (labels == null)
            {
                throw new ArgumentNullException("labels");
            }

            if (x.Length != y.Length)
            {
                throw new DimensionMismatchException("y", "The x and y arrays should have the same length");
            }

            if (x.Length != labels.Length)
            {
                throw new DimensionMismatchException("labels",
                    "The labels array should have the same length as the data array.");
            }

            initialize(x, y, labels);
        }

        /// <summary>
        ///     Computes the scatter plot.
        /// </summary>
        /// <param name="data">Array of { x,y } values.</param>
        public void Compute(double[][] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            for (var i = 0; i < data.Length; i++)
            {
                if (data[i].Length != 2)
                {
                    throw new DimensionMismatchException("data", "The matrix should have two columns.");
                }
            }

            compute(data, null);
        }

        /// <summary>
        ///     Computes the scatter plot.
        /// </summary>
        /// <param name="data">Array of { x,y } values.</param>
        /// <param name="labels">Array of integer labels defining a class for each (x,y) pair.</param>
        public void Compute(double[][] data, int[] labels)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (labels == null)
            {
                throw new ArgumentNullException("labels");
            }

            if (data.Length != labels.Length)
            {
                throw new DimensionMismatchException("labels",
                    "The labels array should have the same length as the data array.");
            }

            for (var i = 0; i < data.Length; i++)
            {
                if (data[i].Length != 2)
                {
                    throw new DimensionMismatchException("data", "The matrix should have two columns.");
                }
            }

            compute(data, labels);
        }

        /// <summary>
        ///     Computes the scatter plot.
        /// </summary>
        /// <param name="data">Array of { x,y } values.</param>
        public void Compute(double[,] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (data.GetLength(1) != 2)
            {
                throw new DimensionMismatchException("data", "The matrix should have two columns.");
            }

            compute(data, null);
        }

        /// <summary>
        ///     Computes the scatter plot.
        /// </summary>
        /// <param name="data">Array of { x,y } values.</param>
        /// <param name="labels">Array of integer labels defining a class for each (x,y) pair.</param>
        public void Compute(double[,] data, int[] labels)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (labels == null)
            {
                throw new ArgumentNullException("labels");
            }

            if (data.GetLength(1) != 2)
            {
                throw new DimensionMismatchException("data", "The matrix should have two columns.");
            }

            if (data.GetLength(0) != labels.Length)
            {
                throw new DimensionMismatchException("labels",
                    "The labels array should have the same length as the data array.");
            }

            compute(data, labels);
        }

        private void compute(double[,] data, int[] labels)
        {
            var rows = data.GetLength(0);
            var x = new double[rows];
            var y = new double[rows];

            for (var i = 0; i < x.Length; i++)
            {
                x[i] = data[i, 0];
                y[i] = data[i, 1];
            }

            initialize(x, y, labels);
        }

        private void compute(double[][] data, int[] labels)
        {
            var x = new double[data.Length];
            var y = new double[data.Length];

            for (var i = 0; i < data.Length; i++)
            {
                x[i] = data[i][0];
                y[i] = data[i][1];
            }

            initialize(x, y, labels);
        }
    }
}