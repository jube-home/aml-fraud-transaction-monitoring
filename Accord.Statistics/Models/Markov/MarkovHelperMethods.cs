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
using Accord.Math;
using Accord.Statistics.Distributions;

namespace Accord.Statistics.Models.Markov
{
    /// <summary>
    ///     Internal methods for validation and other shared functions.
    /// </summary>
    internal static class MarkovHelperMethods
    {
        /// <summary>
        ///     Converts a univariate or multivariate array
        ///     of observations into a two-dimensional jagged array.
        /// </summary>
        internal static double[][] convertNoCheck(Array array, int dimension)
        {
            var multivariate = array as double[][];
            if (multivariate != null)
            {
                return multivariate;
            }

            var univariate = array as double[];
            if (univariate != null)
            {
                return univariate.Split(dimension);
            }

            throw new ArgumentException("Invalid array argument type.", "array");
        }

        /// <summary>
        ///     Converts a univariate or multivariate array
        ///     of observations into a two-dimensional jagged array.
        /// </summary>
        internal static double[][] checkAndConvert(Array observations, int dimension)
        {
            if (observations == null)
            {
                throw new ArgumentNullException("observations");
            }

            // Test if the observations are multivariate
            {
                var multivariate = observations as double[][];
                if (multivariate != null)
                {
                    for (var i = 0; i < multivariate.Length; i++)
                    {
                        if (multivariate[i].Length != dimension)
                        {
                            throw new DimensionMismatchException("observations",
                                "This model expects observations of length " + dimension);
                        }
                    }

                    return multivariate;
                }

                // Test if the observations are univariate
                var univariate = observations as double[];
                if (univariate != null)
                {
                    if (dimension != 1)
                    {
                        throw new DimensionMismatchException("observations",
                            "This model expects univariate observations");
                    }

                    return univariate.Split(dimension);
                }
            }

            {
                // Test if the observations are multivariate integers
                var multivariate = observations as int[][];
                if (multivariate != null)
                {
                    for (var i = 0; i < multivariate.Length; i++)
                    {
                        if (multivariate[i].Length != dimension)
                        {
                            throw new DimensionMismatchException("observations",
                                "This model expects observations of length " + dimension);
                        }
                    }

                    return multivariate.ToDouble();
                }

                // Test if the observations are univariate
                var univariate = observations as int[];
                if (univariate != null)
                {
                    if (dimension != 1)
                    {
                        throw new DimensionMismatchException("observations",
                            "This model expects univariate observations");
                    }

                    return univariate.Split(dimension).ToDouble();
                }
            }

            // else
            throw new ArgumentException("Argument should be either of type " +
                                        "double[] (for univariate observation) or double[][] (for " +
                                        "multivariate observation).", "observations");
        }

        internal static void CheckObservationDimensions<TObservation, TDistribution>(TObservation[][] x,
            HiddenMarkovModel<TDistribution, TObservation> hmm)
            where TDistribution : IFittableDistribution<TObservation>
        {
            var expected = GetObservationDimensions(x);
            var actual = hmm.NumberOfInputs;

            if (actual != expected)
            {
                throw new InvalidOperationException(string.Format(
                    "The specified emission distributions do not model observations with the same name of dimensions " +
                    "as the training data. The training data has {0}-dimensional observations, but the emissions can model up to {1} dimensions.",
                    expected, actual));
            }
        }

        internal static void checkArgs(int[][] observations, int symbols)
        {
            if (observations == null)
            {
                throw new ArgumentNullException("observations");
            }

            for (var i = 0; i < observations.Length; i++)
            {
                for (var j = 0; j < observations[i].Length; j++)
                {
                    var symbol = observations[i][j];

                    if (symbol < 0 || symbol >= symbols)
                    {
                        var message = "Observation sequences should only contain symbols that are " +
                                      "greater than or equal to 0, and lesser than the number of symbols passed to " +
                                      "the HiddenMarkovModel. This model is expecting at most {0} symbols.";

                        throw new ArgumentOutOfRangeException("observations", string.Format(message, symbols));
                    }
                }
            }
        }

        public static int GetObservationDimensions<T>(T[][] x)
        {
            var arr = x[0][0] as IList;
            if (arr != null)
            {
                return arr.Count;
            }

            return 1;
        }
    }
}