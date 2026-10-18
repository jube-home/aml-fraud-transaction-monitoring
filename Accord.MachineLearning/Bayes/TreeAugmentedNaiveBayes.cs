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

namespace Accord.MachineLearning.Bayes
{
    [Serializable]
    public class TreeAugmentedNaiveBayes : MulticlassLikelihoodClassifierBase<double[]>
    {
        private double[][] intercepts;
        private double[][] mutualInformation;
        private int[] parents;
        private double[] priors;
        private double[][] slopes;
        private double[][] variances;

        public TreeAugmentedNaiveBayes(int classes, int inputs)
        {
            if (classes < 2)
            {
                throw new ArgumentOutOfRangeException("classes",
                    "The number of classses should be higher than 1 (currently detected: {0}).".Format(classes));
            }

            if (inputs <= 0)
            {
                throw new ArgumentOutOfRangeException("inputs",
                    "The number of inputs should be higher than 0 (currently detected: {0}).".Format(inputs));
            }

            NumberOfOutputs = classes;
            NumberOfClasses = classes;
            NumberOfInputs = inputs;

            priors = new double[classes];
            parents = new int[inputs];
            intercepts = new double[classes][];
            slopes = new double[classes][];
            variances = new double[classes][];
            mutualInformation = new double[inputs][];

            for (var i = 0; i < classes; i++)
            {
                priors[i] = 1.0 / classes;
                intercepts[i] = new double[inputs];
                slopes[i] = new double[inputs];
                variances[i] = new double[inputs];
                for (var j = 0; j < inputs; j++)
                {
                    variances[i][j] = 1;
                }
            }

            for (var j = 0; j < inputs; j++)
            {
                parents[j] = -1;
                mutualInformation[j] = new double[inputs];
            }
        }

        public int ClassCount => NumberOfOutputs;

        public int InputCount => NumberOfInputs;

        public double[] Priors
        {
            get => priors;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (value.Length != NumberOfOutputs)
                {
                    throw new DimensionMismatchException("value");
                }

                priors = value;
            }
        }

        public int[] Parents
        {
            get => parents;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (value.Length != NumberOfInputs)
                {
                    throw new DimensionMismatchException("value");
                }

                for (var i = 0; i < value.Length; i++)
                {
                    if (value[i] < -1 || value[i] >= value.Length || value[i] == i)
                    {
                        throw new ArgumentOutOfRangeException("value",
                            "The parent of input {0} is invalid ({1}).".Format(i, value[i]));
                    }

                    var steps = 0;
                    for (var p = value[i]; p >= 0; p = value[p])
                    {
                        if (++steps > value.Length)
                        {
                            throw new ArgumentException("The parents contain a cycle at input {0}.".Format(i), "value");
                        }
                    }
                }

                parents = value;
            }
        }

        public double[][] Intercepts
        {
            get => intercepts;
            set => intercepts = check(value);
        }

        public double[][] Slopes
        {
            get => slopes;
            set => slopes = check(value);
        }

        public double[][] Variances
        {
            get => variances;
            set
            {
                check(value);
                for (var i = 0; i < value.Length; i++)
                {
                    for (var j = 0; j < value[i].Length; j++)
                    {
                        if (!(value[i][j] > 0) || double.IsInfinity(value[i][j]))
                        {
                            throw new ArgumentOutOfRangeException("value",
                                "The variance at class {0}, input {1} should be positive and finite.".Format(i, j));
                        }
                    }
                }

                variances = value;
            }
        }

        public double[][] MutualInformation
        {
            get => mutualInformation;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (value.Length != NumberOfInputs)
                {
                    throw new DimensionMismatchException("value");
                }

                for (var i = 0; i < value.Length; i++)
                {
                    if (value[i] == null || value[i].Length != NumberOfInputs)
                    {
                        throw new DimensionMismatchException("value");
                    }
                }

                mutualInformation = value;
            }
        }

        private double[][] check(double[][] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (value.Length != NumberOfOutputs)
            {
                throw new DimensionMismatchException("value");
            }

            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] == null || value[i].Length != NumberOfInputs)
                {
                    throw new DimensionMismatchException("value");
                }
            }

            return value;
        }

        public override double LogLikelihood(double[] input, int classIndex)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (input.Length != NumberOfInputs)
            {
                throw new DimensionMismatchException("input");
            }

            if (classIndex < 0 || classIndex >= NumberOfOutputs)
            {
                throw new ArgumentOutOfRangeException("classIndex");
            }

            var a = intercepts[classIndex];
            var b = slopes[classIndex];
            var v = variances[classIndex];
            var parent = parents;

            var sum = System.Math.Log(priors[classIndex]);

            for (var i = 0; i < input.Length; i++)
            {
                var xi = input[i];
                if (double.IsNaN(xi))
                {
                    continue;
                }

                var mean = a[i];
                var p = parent[i];
                if (p >= 0)
                {
                    var xp = input[p];
                    if (double.IsNaN(xp))
                    {
                        continue;
                    }

                    mean += b[i] * xp;
                }

                var d = xi - mean;
                sum += -0.5 * (System.Math.Log(2 * System.Math.PI * v[i]) + d * d / v[i]);
            }

            return sum;
        }
    }
}