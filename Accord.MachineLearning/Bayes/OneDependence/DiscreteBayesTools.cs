// Accord Machine Learning Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © Jube Holdings Limited, 2026
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Accord.MachineLearning.Bayes
{
    internal static class DiscreteBayesTools
    {
        public static void CheckArguments(double[][] x, int[] y, double[] weights, out int inputs, out int classes)
        {
            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            if (y == null)
            {
                throw new ArgumentNullException("y");
            }

            if (x.Length == 0)
            {
                throw new ArgumentOutOfRangeException("x", "Training algorithm needs at least one training vector.");
            }

            if (x.Length != y.Length)
            {
                throw new DimensionMismatchException("y",
                    "The number of output labels should match the number of training samples.");
            }

            if (weights != null)
            {
                if (weights.Length != x.Length)
                {
                    throw new DimensionMismatchException("weights",
                        "The weights vector must have the same length as the vector of training samples 'x'.");
                }

                for (var i = 0; i < weights.Length; i++)
                {
                    if (double.IsNaN(weights[i]) || double.IsInfinity(weights[i]) || weights[i] < 0)
                    {
                        throw new ArgumentOutOfRangeException("weights",
                            "The weight at index " + i + " must be a finite, non-negative number.");
                    }
                }
            }

            if (x[0] == null)
            {
                throw new ArgumentException("Input vector at position 0 is null.", "x");
            }

            inputs = x[0].Length;
            if (inputs == 0)
            {
                throw new ArgumentOutOfRangeException("x", "The input vectors should have at least one variable.");
            }

            for (var i = 0; i < x.Length; i++)
            {
                if (x[i] == null)
                {
                    throw new ArgumentException("Input vector at position " + i + " is null.", "x");
                }

                if (x[i].Length != inputs)
                {
                    throw new DimensionMismatchException("x",
                        "The size of the input vector at index " + i +
                        " does not match the expected number of inputs. All input vectors must have the same length " +
                        inputs);
                }
            }

            var max = 0;
            for (var i = 0; i < y.Length; i++)
            {
                if (y[i] < 0)
                {
                    throw new ArgumentException(
                        "This learning algorithm does not support negative class labels. The class label passed for instance " +
                        i + " is " + y[i] + ".");
                }

                if (y[i] > max)
                {
                    max = y[i];
                }
            }

            classes = max + 1;
            if (classes < 2)
            {
                throw new ArgumentOutOfRangeException("y",
                    "The number of classes should be higher than 1 (currently detected: " + classes + ").");
            }

            var seen = new bool[classes];
            for (var i = 0; i < y.Length; i++)
            {
                seen[y[i]] = true;
            }

            for (var c = 0; c < classes; c++)
            {
                if (!seen[c])
                {
                    throw new ArgumentException("There are no samples for class label " + c +
                                                ". Please make sure that class labels are contiguous and there is at least one training sample for each label.");
                }
            }
        }

        public static double[] Weights(double[] weights, int length)
        {
            if (weights != null)
            {
                return weights;
            }

            var w = new double[length];
            for (var i = 0; i < w.Length; i++)
            {
                w[i] = 1;
            }

            return w;
        }

        public static double[][] LearnBinEdges(double[][] x, int inputs, int bins, ParallelOptions options)
        {
            var edges = new double[inputs][];
            For(inputs, options, j =>
            {
                var values = new double[x.Length];
                var m = 0;
                for (var i = 0; i < x.Length; i++)
                {
                    var v = x[i][j];
                    if (!double.IsNaN(v))
                    {
                        values[m++] = v;
                    }
                }

                Array.Sort(values, 0, m);

                var cuts = new double[System.Math.Max(bins - 1, 0)];
                var count = 0;
                for (var k = 1; k < bins && m > 0; k++)
                {
                    var position = (int)((long)k * m / bins);
                    if (position >= m)
                    {
                        position = m - 1;
                    }

                    var cut = values[position];
                    if (cut <= values[0])
                    {
                        continue;
                    }

                    if (count > 0 && cut <= cuts[count - 1])
                    {
                        continue;
                    }

                    cuts[count++] = cut;
                }

                var result = new double[count];
                Array.Copy(cuts, result, count);
                edges[j] = result;
            });
            return edges;
        }

        public static int[] NumberOfStates(double[][] edges)
        {
            var states = new int[edges.Length];
            for (var j = 0; j < states.Length; j++)
            {
                states[j] = edges[j].Length + 2;
            }

            return states;
        }

        public static int State(double value, double[] edges)
        {
            if (double.IsNaN(value))
            {
                return edges.Length + 1;
            }

            var lo = 0;
            var hi = edges.Length;
            while (lo < hi)
            {
                var mid = (lo + hi) >> 1;
                if (value >= edges[mid])
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }

            return lo;
        }

        public static int[] States(double[] input, double[][] edges)
        {
            var states = new int[input.Length];
            for (var j = 0; j < states.Length; j++)
            {
                states[j] = State(input[j], edges[j]);
            }

            return states;
        }

        public static int[][] DiscretizeColumns(double[][] x, double[][] edges)
        {
            var columns = new int[edges.Length][];
            for (var j = 0; j < columns.Length; j++)
            {
                var column = new int[x.Length];
                var e = edges[j];
                for (var i = 0; i < column.Length; i++)
                {
                    column[i] = State(x[i][j], e);
                }

                columns[j] = column;
            }

            return columns;
        }

        public static double[] ClassCounts(int[] y, double[] w, int classes)
        {
            var counts = new double[classes];
            for (var i = 0; i < y.Length; i++)
            {
                counts[y[i]] += w[i];
            }

            return counts;
        }

        public static double[] SmoothedPriors(double[] classCounts, double alpha)
        {
            double total = 0;
            for (var c = 0; c < classCounts.Length; c++)
            {
                total += classCounts[c];
            }

            var priors = new double[classCounts.Length];
            var denominator = total + alpha * classCounts.Length;
            for (var c = 0; c < priors.Length; c++)
            {
                priors[c] = (classCounts[c] + alpha) / denominator;
            }

            return priors;
        }

        public static double[] ClassConditional(int[] column, int states, int[] y, double[] w, int classes,
            double alpha)
        {
            var table = new double[classes * states];
            for (var i = 0; i < column.Length; i++)
            {
                table[y[i] * states + column[i]] += w[i];
            }

            Normalize(table, states, alpha);
            return table;
        }

        public static void Normalize(double[] table, int width, double alpha)
        {
            for (var start = 0; start < table.Length; start += width)
            {
                double sum = 0;
                for (var v = 0; v < width; v++)
                {
                    sum += table[start + v];
                }

                var denominator = sum + alpha * width;
                for (var v = 0; v < width; v++)
                {
                    table[start + v] = (table[start + v] + alpha) / denominator;
                }
            }
        }

        public static double[] Log(double[] values)
        {
            var result = new double[values.Length];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = System.Math.Log(values[i]);
            }

            return result;
        }

        public static double[][] Log(double[][] values)
        {
            var result = new double[values.Length][];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = values[i] == null ? null : Log(values[i]);
            }

            return result;
        }

        public static double MutualInformation(int[] a, int statesA, int[] y, int classes, double[] w)
        {
            var joint = new double[classes * statesA];
            var classTotal = new double[classes];
            var valueTotal = new double[statesA];
            double total = 0;

            for (var i = 0; i < a.Length; i++)
            {
                var wi = w[i];
                joint[y[i] * statesA + a[i]] += wi;
                classTotal[y[i]] += wi;
                valueTotal[a[i]] += wi;
                total += wi;
            }

            if (total <= 0)
            {
                return 0;
            }

            double mi = 0;
            for (var c = 0; c < classes; c++)
            {
                for (var v = 0; v < statesA; v++)
                {
                    var n = joint[c * statesA + v];
                    if (n > 0)
                    {
                        mi += n / total * System.Math.Log(n * total / (classTotal[c] * valueTotal[v]));
                    }
                }
            }

            return mi < 0 ? 0 : mi;
        }

        public static double ConditionalMutualInformation(int[] a, int statesA, int[] b, int statesB, int[] y,
            int classes, double[] w)
        {
            var joint = new double[classes * statesA * statesB];
            var ca = new double[classes * statesA];
            var cb = new double[classes * statesB];
            var c0 = new double[classes];
            double total = 0;

            for (var i = 0; i < a.Length; i++)
            {
                var wi = w[i];
                var c = y[i];
                joint[(c * statesA + a[i]) * statesB + b[i]] += wi;
                ca[c * statesA + a[i]] += wi;
                cb[c * statesB + b[i]] += wi;
                c0[c] += wi;
                total += wi;
            }

            if (total <= 0)
            {
                return 0;
            }

            double mi = 0;
            for (var c = 0; c < classes; c++)
            {
                for (var u = 0; u < statesA; u++)
                {
                    for (var v = 0; v < statesB; v++)
                    {
                        var n = joint[(c * statesA + u) * statesB + v];
                        if (n > 0)
                        {
                            mi += n / total * System.Math.Log(n * c0[c] / (ca[c * statesA + u] * cb[c * statesB + v]));
                        }
                    }
                }
            }

            return mi < 0 ? 0 : mi;
        }

        public static long CheckedSize(long size, string what)
        {
            if (size > 1L << 26)
            {
                throw new InvalidOperationException("The conditional probability table for " + what + " would have " +
                                                    size +
                                                    " cells. Reduce the number of bins or the number of parents.");
            }

            return size;
        }

        public static void For(int count, ParallelOptions options, Action<int> body)
        {
            options.CancellationToken.ThrowIfCancellationRequested();

            if (options.MaxDegreeOfParallelism == 1)
            {
                for (var i = 0; i < count; i++)
                {
                    options.CancellationToken.ThrowIfCancellationRequested();
                    body(i);
                }
            }
            else
            {
                try
                {
                    Parallel.For(0, count, options, i =>
                    {
                        options.CancellationToken.ThrowIfCancellationRequested();
                        body(i);
                    });
                }
                catch (AggregateException ex)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerExceptions[0]).Throw();
                }
            }
        }

        public static void Validate(int bins, double smoothing)
        {
            if (bins < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(bins), "The number of bins must be at least 1.");
            }

            if (double.IsNaN(smoothing) || double.IsInfinity(smoothing) || smoothing <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(smoothing),
                    "The smoothing parameter must be a finite number greater than zero.");
            }
        }

        public static void CheckInput(double[] input, int inputs)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (input.Length != inputs)
            {
                throw new DimensionMismatchException("input",
                    "The size of the input vector does not match the expected number of inputs. All input vectors must have the length " +
                    inputs);
            }
        }

        public static double LogSumExp(double[] values, int count)
        {
            var max = double.NegativeInfinity;
            for (var i = 0; i < count; i++)
            {
                if (values[i] > max)
                {
                    max = values[i];
                }
            }

            if (double.IsNegativeInfinity(max))
            {
                return max;
            }

            double sum = 0;
            for (var i = 0; i < count; i++)
            {
                sum += System.Math.Exp(values[i] - max);
            }

            return max + System.Math.Log(sum);
        }
    }
}