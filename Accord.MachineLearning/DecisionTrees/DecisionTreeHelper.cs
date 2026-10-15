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
using Accord.Math;

namespace Accord.MachineLearning.DecisionTrees
{
    internal static class DecisionTreeHelper
    {
        public static void CheckArgs(DecisionTree tree, int[][] inputs, int[] outputs, double[] weights = null)
        {
            checkArgs(tree, inputs, outputs, weights);

            for (var i = 0; i < inputs.Length; i++)
            {
                for (var j = 0; j < inputs[i].Length; j++)
                {
                    if (tree.Attributes[j].Nature != DecisionVariableKind.Discrete)
                    {
                        throw new Exception("The tree expects real-valued inputs.");
                    }

                    var min = tree.Attributes[j].Range.Min;
                    var max = tree.Attributes[j].Range.Max;

                    if (inputs[i][j] < min || inputs[i][j] > max)
                    {
                        throw new ArgumentOutOfRangeException("inputs",
                            string.Format(
                                "The input vector at index {0} contains an invalid entry ({1}) at column {2}. The tree expected this value to be between [{3}, {4}].",
                                i, inputs[i][j], j, min, max));
                    }
                }
            }
        }

        public static void CheckArgs(DecisionTree tree, double[][] inputs, int[] outputs, double[] weights = null)
        {
            checkArgs(tree, inputs, outputs, weights);

            for (var i = 0; i < inputs.Length; i++)
            {
                for (var j = 0; j < inputs[i].Length; j++)
                {
                    if (tree.Attributes[j].Nature != DecisionVariableKind.Discrete)
                    {
                        continue;
                    }

                    var min = tree.Attributes[j].Range.Min;
                    var max = tree.Attributes[j].Range.Max;

                    if (inputs[i][j] < min || inputs[i][j] > max)
                    {
                        throw new ArgumentOutOfRangeException("inputs",
                            string.Format(
                                "The input vector at index {0} contains an invalid entry ({1}) at column {2}. The tree expected this value to be between [{3}, {4}].",
                                i, inputs[i][j], j, min, max));
                    }
                }
            }
        }

        private static void checkArgs(DecisionTree tree, Array[] inputs, int[] outputs, double[] weights = null)
        {
            if (inputs == null)
            {
                throw new ArgumentNullException("inputs");
            }

            if (outputs == null)
            {
                throw new ArgumentNullException("outputs");
            }

            if (inputs.Length != outputs.Length)
            {
                throw new DimensionMismatchException("outputs",
                    "The number of input vectors and output labels does not match.");
            }

            if (inputs.Length == 0)
            {
                throw new ArgumentOutOfRangeException("inputs",
                    "Training algorithm needs at least one training vector.");
            }

            if (weights != null)
            {
                if (inputs.Length != weights.Length)
                {
                    throw new ArgumentOutOfRangeException("weights",
                        "The number of input vectors and weights does not match.");
                }
            }

            for (var i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] == null)
                {
                    throw new ArgumentNullException("inputs",
                        "The input vector at index " + i + " is null.");
                }

                if (inputs[i].Length != tree.NumberOfInputs)
                {
                    throw new DimensionMismatchException("inputs",
                        string.Format("The input vector at index {0} has {1} dimensions but the tree expects {2}.",
                            i, inputs[i].Length, tree.NumberOfInputs));
                }
            }

            for (var i = 0; i < outputs.Length; i++)
            {
                if (outputs[i] < 0 || outputs[i] >= tree.NumberOfOutputs)
                {
                    throw new ArgumentOutOfRangeException("outputs",
                        string.Format(
                            "The output label {0} at index {1} should be >= 0 and < {2} (the number of output classes expected by the tree).",
                            i, outputs[i], tree.NumberOfOutputs));
                }
            }
        }

        public static DecisionTree Create(double[][] x, int[] y, IList<DecisionVariable> attributes)
        {
            if (attributes == null || attributes.Count == 0)
            {
                attributes = DecisionVariable.FromData(x);
            }

            return Create(y, attributes);
        }

        public static DecisionTree Create(int[][] x, int[] y, IList<DecisionVariable> attributes)
        {
            if (attributes == null || attributes.Count == 0)
            {
                attributes = DecisionVariable.FromData(x);
            }

            return Create(y, attributes);
        }

        public static DecisionTree Create(int?[][] x, int[] y, IList<DecisionVariable> attributes)
        {
            if (attributes == null || attributes.Count == 0)
            {
                attributes = DecisionVariable.FromData(x);
            }

            return Create(y, attributes);
        }

        private static DecisionTree Create(int[] y, IList<DecisionVariable> attributes)
        {
            var classes = y.Max() + 1;
            var tree = new DecisionTree(attributes, classes);
            return tree;
        }
    }
}