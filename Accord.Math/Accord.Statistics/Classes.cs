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
using System.Linq;
using Accord.Math;

namespace Accord.Statistics
{
    /// <summary>
    ///     Methods for operating with categorical data.
    /// </summary>
    public static class Classes
    {
        /// <summary>
        ///     Calculates the prevalence of a class for each variable.
        /// </summary>
        /// <param name="positives">An array of counts detailing the occurrence of the first class.</param>
        /// <param name="negatives">An array of counts detailing the occurrence of the second class.</param>
        /// <returns>An array containing the proportion of the first class over the total of occurrences.</returns>
        public static double[] GetRatio(int[] positives, int[] negatives)
        {
            var r = new double[positives.Length];
            for (var i = 0; i < r.Length; i++)
            {
                r[i] = positives[i] / (double)(positives[i] + negatives[i]);
            }

            return r;
        }

        /// <summary>
        ///     Calculates the prevalence of a class.
        /// </summary>
        /// <param name="data">A matrix containing counted, grouped data.</param>
        /// <param name="positiveColumn">The index for the column which contains counts for occurrence of the first class.</param>
        /// <param name="negativeColumn">The index for the column which contains counts for occurrence of the second class.</param>
        /// <returns>An array containing the proportion of the first class over the total of occurrences.</returns>
        public static double[] GetRatio(this int[][] data, int positiveColumn, int negativeColumn)
        {
            var r = new double[data.Length];
            for (var i = 0; i < r.Length; i++)
            {
                r[i] = (double)data[i][positiveColumn] / (data[i][positiveColumn] + data[i][negativeColumn]);
            }

            return r;
        }

        /// <summary>
        ///     Groups the occurrences contained in data matrix of binary (dichotomous) data.
        ///     This operation can be reversed using the <see cref="Expand(int[][], int, int, int)" /> method.
        /// </summary>
        /// <param name="data">A data matrix containing at least a column of binary data.</param>
        /// <param name="groupIndex">Index of the column which contains the group label name.</param>
        /// <param name="yesNoIndex">Index of the column which contains the binary [0,1] data.</param>
        /// <returns>
        ///     A matrix containing the group label in the first column, the number of occurrences of the first class
        ///     in the second column and the number of occurrences of the second class in the third column.
        /// </returns>
        public static int[][] Summarize(this int[][] data, int groupIndex, int yesNoIndex)
        {
            var groups = new List<int>();
            var groupings = new List<int[]>();

            for (var i = 0; i < data.Length; i++)
            {
                var group = data[i][groupIndex];
                if (!groups.Contains(group))
                {
                    groups.Add(group);

                    int positives = 0, negatives = 0;
                    for (var j = 0; j < data.Length; j++)
                    {
                        if (data[j][groupIndex] == group)
                        {
                            if (data[j][yesNoIndex] == 0)
                            {
                                negatives++;
                            }
                            else
                            {
                                positives++;
                            }
                        }
                    }

                    groupings.Add(new[] { group, positives, negatives });
                }
            }

            return groupings.ToArray();
        }

        /// <summary>
        ///     Divides values into groups given a vector
        ///     containing the group labels for every value.
        /// </summary>
        /// <typeparam name="T">The type of the values.</typeparam>
        /// <param name="values">The values to be separated into groups.</param>
        /// <param name="labels">
        ///     A vector containing the class label associated with each of the
        ///     values. The labels must begin on 0 and its maximum value should
        ///     be the number of groups - 1.
        /// </param>
        /// <returns>The original values divided into groups.</returns>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Statistics\ClassesTest.cs" region="doc_random" />
        /// </example>
        public static T[][] Separate<T>(this T[] values, int[] labels)
        {
            return Separate(values, labels, labels.Max() + 1);
        }

        /// <summary>
        ///     Divides values into groups given a vector
        ///     containing the group labels for every value.
        /// </summary>
        /// <typeparam name="T">The type of the values.</typeparam>
        /// <param name="values">The values to be separated into groups.</param>
        /// <param name="labels">
        ///     A vector containing the class label associated with each of the
        ///     values. The labels must begin on 0 and its maximum value should
        ///     be the number of groups - 1.
        /// </param>
        /// <param name="groups">The number of groups.</param>
        /// <returns>The original values divided into groups.</returns>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Statistics\ClassesTest.cs" region="doc_random" />
        /// </example>
        public static T[][] Separate<T>(this T[] values, int[] labels, int groups)
        {
            if (values.Length != labels.Length)
            {
                throw new DimensionMismatchException("labels");
            }

            var result = new T[groups][];

            for (var i = 0; i < result.Length; i++)
            {
                var group = new List<T>();

                for (var j = 0; j < values.Length; j++)
                {
                    if (labels[j] == i)
                    {
                        group.Add(values[j]);
                    }
                }

                result[i] = group.ToArray();
            }

            return result;
        }

        /// <summary>
        ///     Extends a grouped data into a full observation matrix.
        /// </summary>
        /// <param name="data">The group labels.</param>
        /// <param name="positives">
        ///     An array containing he occurrence of the positive class
        ///     for each of the groups.
        /// </param>
        /// <param name="negatives">
        ///     An array containing he occurrence of the negative class
        ///     for each of the groups.
        /// </param>
        /// <returns>A full sized observation matrix.</returns>
        public static int[][] Expand(this int[] data, int[] positives, int[] negatives)
        {
            if (data.Length != positives.Length)
            {
                throw new DimensionMismatchException("positives",
                    "The array of positive labels must have the same length as the data.");
            }

            if (positives.Length != negatives.Length)
            {
                throw new DimensionMismatchException("negatives",
                    "The array of negative labels must have the same length as the data.");
            }

            var rows = new List<int[]>();

            for (var i = 0; i < data.Length; i++)
            {
                for (var j = 0; j < positives[i]; j++)
                {
                    rows.Add(new[] { data[i], 1 });
                }

                for (var j = 0; j < negatives[i]; j++)
                {
                    rows.Add(new[] { data[i], 0 });
                }
            }

            return rows.ToArray();
        }

        /// <summary>
        ///     Expands a grouped data into a full observation matrix.
        /// </summary>
        /// <param name="data">The grouped data matrix.</param>
        /// <param name="labelColumn">
        ///     Index of the column which contains the labels
        ///     in the grouped data matrix.
        /// </param>
        /// <param name="positiveColumn">
        ///     Index of the column which contains
        ///     the occurrences for the first class.
        /// </param>
        /// <param name="negativeColumn">
        ///     Index of the column which contains
        ///     the occurrences for the second class.
        /// </param>
        /// <returns>A full sized observation matrix.</returns>
        public static int[][] Expand(this int[][] data, int labelColumn, int positiveColumn, int negativeColumn)
        {
            var rows = new List<int[]>();

            for (var i = 0; i < data.Length; i++)
            {
                for (var j = 0; j < data[i][positiveColumn]; j++)
                {
                    rows.Add(new[] { data[i][labelColumn], 1 });
                }

                for (var j = 0; j < data[i][negativeColumn]; j++)
                {
                    rows.Add(new[] { data[i][labelColumn], 0 });
                }
            }

            return rows.ToArray();
        }

        /// <summary>
        ///     Returns a random group assignment for a sample.
        /// </summary>
        /// <param name="samples">The sample size.</param>
        /// <param name="classes">The number of groups.</param>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Statistics\ClassesTest.cs" region="doc_random" />
        /// </example>
        public static int[] Random(int samples, int classes)
        {
            return Random(samples, Vector.Create(classes, 1.0 / classes));
        }

        /// <summary>
        ///     Returns a random group assignment for a sample.
        /// </summary>
        /// <param name="samples">The sample size.</param>
        /// <param name="proportion">The desired proportion for each class.</param>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Statistics\ClassesTest.cs" region="doc_random" />
        /// </example>
        public static int[] Random(int samples, double[] proportion)
        {
            // Create the index vector
            var idx = new int[samples];
            if (proportion.Length == 1)
            {
                return idx;
            }

            proportion = proportion.Divide(proportion.Sum());
            var cumSum = proportion.CumulativeSum();

            var a = 0;
            for (var i = 0; i < proportion.Length; i++)
            {
                var b = (int)System.Math.Round(cumSum[i] * samples, MidpointRounding.AwayFromZero);

                for (var j = a; j < b; j++)
                {
                    idx[j] = i;
                }

                a = b;
            }

            // Shuffle the indices vector
            idx.Shuffle();

            return idx;
        }

        /// <summary>
        ///     Returns a random group assignment for a sample
        ///     into two mutually exclusive groups.
        /// </summary>
        /// <param name="samples">The sample size.</param>
        /// <param name="proportion">The proportion of samples between the groups.</param>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Statistics\ClassesTest.cs" region="doc_random" />
        /// </example>
        public static int[] Random(int samples, double proportion)
        {
            return Random(samples, new[] { proportion, 1.0 - proportion });
        }

        /// <summary>
        ///     Returns a random group assignment for a sample, making
        ///     sure different class labels are distributed evenly among
        ///     the groups.
        /// </summary>
        /// <param name="labels">A vector containing class labels.</param>
        /// <param name="categories">The number of groups.</param>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Statistics\ClassesTest.cs" region="doc_random" />
        /// </example>
        public static int[] Random(int[] labels, int categories)
        {
            return Random(labels, labels.Max() + 1, categories);
        }

        /// <summary>
        ///     Returns a random group assignment for a sample, making
        ///     sure different class labels are distributed evenly among
        ///     the groups.
        /// </summary>
        /// <param name="labels">A vector containing class labels.</param>
        /// <param name="classes">The number of different classes in <paramref name="labels" />.</param>
        /// <param name="categories">The number of groups.</param>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Statistics\ClassesTest.cs" region="doc_random" />
        /// </example>
        public static int[] Random(int[] labels, int classes, int categories)
        {
            var buckets = new List<Tuple<int, int>>[classes];
            for (var i = 0; i < buckets.Length; i++)
            {
                buckets[i] = new List<Tuple<int, int>>();
            }

            for (var i = 0; i < labels.Length; i++)
            {
                buckets[labels[i]].Add(Tuple.Create(i, labels[i]));
            }

            for (var i = 0; i < buckets.Length; i++)
            {
                buckets.Shuffle();
            }

            var partitions = new List<Tuple<int, int>>[categories];
            for (var i = 0; i < partitions.Length; i++)
            {
                partitions[i] = new List<Tuple<int, int>>();
            }

            // We are going to take samples from the buckets and assign to 
            // groups. For this, we will be following the buckets in order,
            // such that new samples are drawn equally from each bucket.

            var allEmpty = true;
            var bucketIndex = 0;
            var partitionIndex = 0;

            do
            {
                for (var i = 0; i < partitions.Length; i++)
                {
                    allEmpty = true;

                    var currentPartition = partitions[partitionIndex];
                    partitionIndex = (partitionIndex + 1) % partitions.Length;

                    for (var j = 0; j < buckets.Length; j++)
                    {
                        var currentBucket = buckets[bucketIndex];
                        bucketIndex = (bucketIndex + 1) % buckets.Length;

                        if (currentBucket.Count == 0)
                        {
                            continue;
                        }

                        allEmpty = false;

                        var next = currentBucket[currentBucket.Count - 1];
                        currentBucket.RemoveAt(currentBucket.Count - 1);
                        currentPartition.Add(next);
                    }
                }
            } while (!allEmpty);

            for (var i = 0; i < partitions.Length; i++)
            {
                partitions[i].Shuffle();
            }

            var splittings = new int[labels.Length];
            for (var i = 0; i < partitions.Length; i++)
            {
                foreach (var index in partitions[i])
                {
                    splittings[index.Item1] = i;
                }
            }

            return splittings;
        }

        /// <summary>
        ///     Returns a random group assignment for a sample, making
        ///     sure different class labels are distributed evenly among
        ///     the groups.
        /// </summary>
        /// <param name="labels">A vector containing class labels.</param>
        /// <param name="proportion">The proportion of positive and negative samples.</param>
        /// <example>
        ///     <code source="Unit Tests\Accord.Tests.Statistics\ClassesTest.cs" region="doc_random" />
        /// </example>
        public static int[] Random(int[] labels, double proportion)
        {
            if (labels.DistinctCount() != 2)
            {
                throw new ArgumentException("Only two classes are supported.", "labels");
            }

            var negative = labels.Min();
            var positive = labels.Max();

            var negativeIndices = labels.Find(i => i == negative).ToList();
            var positiveIndices = labels.Find(i => i == positive).ToList();

            var positiveCount = positiveIndices.Count;
            var negativeCount = negativeIndices.Count;

            var firstGroupPositives = (int)(positiveCount / 2.0 * proportion);
            var firstGroupNegatives = (int)(negativeCount / 2.0 * proportion);

            var testing = new List<int>();

            for (var j = 0; j < firstGroupNegatives; j++)
            {
                negativeIndices.RemoveAt(0);
            }

            for (var j = 0; j < firstGroupPositives; j++)
            {
                positiveIndices.RemoveAt(0);
            }

            testing.AddRange(negativeIndices);
            testing.AddRange(positiveIndices);

            var indices = new int[labels.Length];
            for (var i = 0; i < testing.Count; i++)
            {
                indices[testing[i]] = 1;
            }

            return indices;
        }

        /// <summary>
        ///     Gets the percentage of positive samples in a set of class labels.
        /// </summary>
        /// <param name="y">The class labels.</param>
        /// <param name="positives">The number of positive samples in <paramref name="y" />.</param>
        /// <param name="negatives">The number of negatives samples in <paramref name="y" />.</param>
        /// <returns>The percentage of positive samples in <paramref name="y" />.</returns>
        public static double GetRatio(int[] y, out int positives, out int negatives)
        {
            return GetRatio(y.ToBoolean(), out positives, out negatives);
        }

        /// <summary>
        ///     Gets the percentage of positive samples in a set of class labels.
        /// </summary>
        /// <param name="y">The class labels.</param>
        /// <param name="positives">The number of positive samples in <paramref name="y" />.</param>
        /// <param name="negatives">The number of negatives samples in <paramref name="y" />.</param>
        /// <returns>The percentage of positive samples in <paramref name="y" />.</returns>
        public static double GetRatio(this bool[] y, out int positives, out int negatives)
        {
            positives = 0;
            for (var i = 0; i < y.Length; i++)
            {
                if (y[i])
                {
                    positives++;
                }
            }

            negatives = y.Length - positives;
            return positives / (double)(positives + negatives);
        }

        /// <summary>
        ///     Converts a boolean variable into a 0-or-1 representation (0 is false, 1 is true).
        /// </summary>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static int ToZeroOne(this bool p)
        {
            return p ? 1 : 0;
        }

        /// <summary>
        ///     Converts a boolean variable into a 0-or-1 representation (0 is false, 1 is true).
        /// </summary>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static int ToZeroOne(this int p)
        {
            return Decide(p) ? 1 : 0;
        }

        /// <summary>
        ///     Converts a boolean variable into a 0-or-1 representation (0 is false, 1 is true).
        /// </summary>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static int ToZeroOne(this double p)
        {
            return Decide(p) ? 1 : 0;
        }

        /// <summary>
        ///     Converts boolean variables into a 0-or-1 representation (0 is false, 1 is true).
        /// </summary>
        public static int[] ToZeroOne(this bool[] p)
        {
            var result = new int[p.Length];
            for (var i = 0; i < p.Length; i++)
            {
                result[i] = p[i] ? 1 : 0;
            }

            return result;
        }

        /// <summary>
        ///     Converts boolean variables into a 0-or-1 representation (0 is false, 1 is true).
        /// </summary>
        public static int[] ToZeroOne(this int[] p)
        {
            var result = new int[p.Length];
            for (var i = 0; i < p.Length; i++)
            {
                result[i] = Decide(p[i]) ? 1 : 0;
            }

            return result;
        }

        /// <summary>
        ///     Converts boolean variables into a 0-or-1 representation (0 is false, 1 is true).
        /// </summary>
        public static int[] ToZeroOne(this double[] p)
        {
            var result = new int[p.Length];
            for (var i = 0; i < p.Length; i++)
            {
                result[i] = Decide(p[i]) ? 1 : 0;
            }

            return result;
        }

        /// <summary>
        ///     Converts a boolean variable into a -1 or +1 representation (-1 is false, +1 is true).
        /// </summary>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static int ToMinusOnePlusOne(this bool p)
        {
            return p ? 1 : -1;
        }

        /// <summary>
        ///     Converts a boolean variable into a -1 or +1 representation (-1 is false, +1 is true).
        /// </summary>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static int ToMinusOnePlusOne(this int p)
        {
            return Decide(p) ? 1 : -1;
        }

        /// <summary>
        ///     Converts a boolean variable into a -1 or +1 representation (-1 is false, +1 is true).
        /// </summary>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static int ToMinusOnePlusOne(this double p)
        {
            return Decide(p) ? 1 : -1;
        }

        /// <summary>
        ///     Converts boolean variables into a -1 or +1 representation (-1 is false, +1 is true).
        /// </summary>
        public static int[] ToMinusOnePlusOne(this bool[] p)
        {
            var result = new int[p.Length];
            for (var i = 0; i < p.Length; i++)
            {
                result[i] = p[i] ? 1 : -1;
            }

            return result;
        }

        /// <summary>
        ///     Converts boolean variables into a -1 or +1 representation (-1 is false, +1 is true).
        /// </summary>
        public static int[] ToMinusOnePlusOne(this int[] p)
        {
            var result = new int[p.Length];
            for (var i = 0; i < p.Length; i++)
            {
                result[i] = Decide(p[i]) ? 1 : -1;
            }

            return result;
        }

        /// <summary>
        ///     Converts boolean variables into a -1 or +1 representation (-1 is false, +1 is true).
        /// </summary>
        public static int[] ToMinusOnePlusOne(this double[] p)
        {
            var result = new int[p.Length];
            for (var i = 0; i < p.Length; i++)
            {
                result[i] = Decide(p[i]) ? 1 : -1;
            }

            return result;
        }

        /// <summary>
        ///     Converts boolean variables into a -1 or +1 representation (-1 is false, +1 is true).
        /// </summary>
        public static double[][] ToMinusOnePlusOne(this bool[][] p)
        {
            return ToMinusOnePlusOne<double>(p);
        }

        /// <summary>
        ///     Converts boolean variables into a -1 or +1 representation (-1 is false, +1 is true).
        /// </summary>
        public static T[][] ToMinusOnePlusOne<T>(this bool[][] p)
        {
            var positive = (T)Convert.ChangeType(+1, typeof(T));
            var negative = (T)Convert.ChangeType(-1, typeof(T));

            var result = new T[p.Length][];
            for (var i = 0; i < p.Length; i++)
            {
                result[i] = new T[p[i].Length];
                for (var j = 0; j < p[i].Length; j++)
                {
                    result[i][j] = p[i][j] ? positive : negative;
                }
            }

            return result;
        }

        /// <summary>
        ///     Converts double variables into class labels, starting at zero.
        /// </summary>
        public static int[] ToMulticlass(this double[] p)
        {
            return ToMulticlass(p, (int)p.Min());
        }

        /// <summary>
        ///     Converts double variables into class labels, starting at zero.
        /// </summary>
        public static int[] ToMulticlass(this int[] p)
        {
            return ToMulticlass(p, p.Min());
        }

        /// <summary>
        ///     Converts double variables into class labels, starting at zero.
        /// </summary>
        public static int[] ToMulticlass(this double[] p, int min)
        {
            var result = new int[p.Length];
            for (var i = 0; i < p.Length; i++)
            {
                result[i] = (int)p[i] - min;
            }

            return result;
        }

        /// <summary>
        ///     Converts double variables into class labels, starting at zero.
        /// </summary>
        public static int[] ToMulticlass(this int[] p, int min)
        {
            var result = new int[p.Length];
            for (var i = 0; i < p.Length; i++)
            {
                result[i] = p[i] - min;
            }

            return result;
        }

        /// <summary>
        ///     Determines whether the class labels contains only zero and ones.
        /// </summary>
        public static bool IsZeroOne(this int[] p)
        {
            for (var i = 0; i < p.Length; i++)
            {
                if (p[i] != 0 && p[i] != +1)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Determines whether the class labels contains only -1 and +1.
        /// </summary>
        public static bool IsMinusOnePlusOne(this int[] p)
        {
            for (var i = 0; i < p.Length; i++)
            {
                if (p[i] != -1 && p[i] != +1)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Determines whether the class labels contains only (-1 and +1) or (0 and +1).
        /// </summary>
        public static bool IsBinary(this int[] p)
        {
            return IsMinusOnePlusOne(p) ^ IsZeroOne(p);
        }

        /// <summary>
        ///     Determines whether the class labels contains one true value per sample.
        /// </summary>
        public static bool IsMultilabel(this bool[][] y)
        {
            return !IsMulticlass(y);
        }

        /// <summary>
        ///     Determines whether the class labels contains one true value per sample.
        /// </summary>
        public static bool IsMulticlass(this bool[][] y)
        {
            return y.Sum(1).IsEqual(1);
        }

        /// <summary>
        ///     Hyperplane decision function. Return true if distance
        ///     is higher than zero, and false otherwise.
        /// </summary>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool Decide(double distance)
        {
            return distance > 0;
        }

        /// <summary>
        ///     Hyperplane decision function. Return true if distance
        ///     is higher than zero, and false otherwise.
        /// </summary>
#if NET45 || NET46 || NET462 || NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool Decide(int label)
        {
            if (label == 1)
            {
                return true;
            }

            if (label == 0 || label == -1)
            {
                return false;
            }

            throw new ArgumentOutOfRangeException("label", "Label must be 1, 0 or -1");
        }

        /// <summary>
        ///     Hyperplane decision function. Return true if distance
        ///     is higher than zero, and false otherwise.
        /// </summary>
        public static bool[] Decide(double[] values)
        {
            var result = new bool[values.Length];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = Decide(values[i]);
            }

            return result;
        }

        /// <summary>
        ///     Hyperplane decision function. Return true if distance
        ///     is higher than zero, and false otherwise.
        /// </summary>
        public static bool[] Decide(int[] values)
        {
            var result = new bool[values.Length];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = Decide(values[i]);
            }

            return result;
        }

        /// <summary>
        ///     Hyperplane decision function. Return true if distance
        ///     is higher than zero, and false otherwise.
        /// </summary>
        public static bool[][] Decide(double[][] values)
        {
            var result = new bool[values.Length][];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = Decide(values[i]);
            }

            return result;
        }

        /// <summary>
        ///     Hyperplane decision function. Return true if distance
        ///     is higher than zero, and false otherwise.
        /// </summary>
        public static bool[][] Decide(int[][] values)
        {
            var result = new bool[values.Length][];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = Decide(values[i]);
            }

            return result;
        }
    }
}