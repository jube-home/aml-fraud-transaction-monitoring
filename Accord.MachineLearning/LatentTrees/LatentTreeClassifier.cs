// Accord Machine Learning Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © Jube, 2026
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
using System.Collections.Generic;

namespace Accord.MachineLearning.LatentTrees
{
    [Serializable]
    public class LatentTreeClassifier : MulticlassLikelihoodClassifierBase<double[]>
    {
        private readonly LatentTreeShape shape;

        public LatentTreeClassifier(int[] parents, int[] states, double[][][] conditionals,
            int[] featureNodes, double[][] binEdges, bool[] missingStates)
        {
            if (parents == null)
            {
                throw new ArgumentNullException("parents");
            }

            if (states == null)
            {
                throw new ArgumentNullException("states");
            }

            if (conditionals == null)
            {
                throw new ArgumentNullException("conditionals");
            }

            if (featureNodes == null)
            {
                throw new ArgumentNullException("featureNodes");
            }

            if (binEdges == null)
            {
                throw new ArgumentNullException("binEdges");
            }

            if (missingStates == null)
            {
                throw new ArgumentNullException("missingStates");
            }

            var nodes = parents.Length;
            if (nodes < 2)
            {
                throw new ArgumentException("The tree must have a class node and at least one other node.", "parents");
            }

            if (states.Length != nodes)
            {
                throw new DimensionMismatchException("states", "The number of states must match the number of nodes.");
            }

            if (conditionals.Length != nodes)
            {
                throw new DimensionMismatchException("conditionals",
                    "The number of tables must match the number of nodes.");
            }

            if (binEdges.Length != featureNodes.Length)
            {
                throw new DimensionMismatchException("binEdges", "There must be one set of bin edges per feature.");
            }

            if (missingStates.Length != featureNodes.Length)
            {
                throw new DimensionMismatchException("missingStates", "There must be one flag per feature.");
            }

            if (parents[0] != -1)
            {
                throw new ArgumentException("Node 0 must be the root class node with parent -1.", "parents");
            }

            if (states[0] < 2)
            {
                throw new ArgumentOutOfRangeException("states", "The class node must have at least two states.");
            }

            for (var i = 1; i < nodes; i++)
            {
                if (parents[i] < 0 || parents[i] >= i)
                {
                    throw new ArgumentException("Every node must have a parent with a lower index.", "parents");
                }

                if (states[i] < 1)
                {
                    throw new ArgumentOutOfRangeException("states", "Every node must have at least one state.");
                }
            }

            if (conditionals[0] == null || conditionals[0].Length != 1 || conditionals[0][0].Length != states[0])
            {
                throw new DimensionMismatchException("conditionals",
                    "The root table must hold one row with the class priors.");
            }

            CheckDistribution(conditionals[0][0], "conditionals");

            for (var i = 1; i < nodes; i++)
            {
                var table = conditionals[i];
                if (table == null || table.Length != states[parents[i]])
                {
                    throw new DimensionMismatchException("conditionals",
                        "Table " + i + " must have one row per parent state.");
                }

                for (var s = 0; s < table.Length; s++)
                {
                    if (table[s] == null || table[s].Length != states[i])
                    {
                        throw new DimensionMismatchException("conditionals",
                            "Table " + i + " must have one column per state.");
                    }

                    CheckDistribution(table[s], "conditionals");
                }
            }

            var hasChild = new bool[nodes];
            for (var i = 1; i < nodes; i++)
            {
                hasChild[parents[i]] = true;
            }

            var nodeFeatures = new int[nodes];
            for (var i = 0; i < nodes; i++)
            {
                nodeFeatures[i] = -1;
            }

            for (var f = 0; f < featureNodes.Length; f++)
            {
                var node = featureNodes[f];
                if (node < 1 || node >= nodes)
                {
                    throw new ArgumentOutOfRangeException("featureNodes",
                        "Feature nodes must be non-root nodes of the tree.");
                }

                if (nodeFeatures[node] != -1)
                {
                    throw new ArgumentException("Two features refer to the same node.", "featureNodes");
                }

                if (hasChild[node])
                {
                    throw new ArgumentException("A feature node must be a leaf.", "featureNodes");
                }

                nodeFeatures[node] = f;
                if (binEdges[f] == null)
                {
                    throw new ArgumentNullException("binEdges");
                }

                var expected = binEdges[f].Length + 1 + (missingStates[f] ? 1 : 0);
                if (states[node] != expected)
                {
                    throw new DimensionMismatchException("states",
                        "Feature " + f + " must have one state per bin plus a missing state when flagged.");
                }
            }

            for (var i = 1; i < nodes; i++)
            {
                if (!hasChild[i] && nodeFeatures[i] == -1)
                {
                    throw new ArgumentException("Every leaf must be an observed feature.", "parents");
                }
            }

            Parents = (int[])parents.Clone();
            States = (int[])states.Clone();
            Conditionals = new double[nodes][][];
            for (var i = 0; i < nodes; i++)
            {
                Conditionals[i] = new double[conditionals[i].Length][];
                for (var s = 0; s < conditionals[i].Length; s++)
                {
                    Conditionals[i][s] = (double[])conditionals[i][s].Clone();
                }
            }

            FeatureNodes = (int[])featureNodes.Clone();
            NodeFeatures = nodeFeatures;
            BinEdges = new double[binEdges.Length][];
            for (var f = 0; f < binEdges.Length; f++)
            {
                BinEdges[f] = (double[])binEdges[f].Clone();
            }

            MissingStates = (bool[])missingStates.Clone();

            shape = new LatentTreeShape(Parents, States);

            NumberOfInputs = featureNodes.Length;
            NumberOfOutputs = states[0];
            NumberOfClasses = states[0];
        }

        public int[] Parents { get; private set; }

        public int[] States { get; private set; }

        public double[][][] Conditionals { get; private set; }

        public int[] FeatureNodes { get; private set; }

        public int[] NodeFeatures { get; private set; }

        public double[][] BinEdges { get; private set; }

        public bool[] MissingStates { get; private set; }

        public double[] ClassPriors => Conditionals[0][0];

        public bool IsLatent(int node)
        {
            if (node < 0 || node >= Parents.Length)
            {
                throw new ArgumentOutOfRangeException("node");
            }

            return node > 0 && NodeFeatures[node] < 0;
        }

        public int[] GetLatentNodes()
        {
            var list = new List<int>();
            for (var i = 1; i < Parents.Length; i++)
            {
                if (NodeFeatures[i] < 0)
                {
                    list.Add(i);
                }
            }

            return list.ToArray();
        }

        public int[] GetDescendantFeatures(int node)
        {
            if (node < 0 || node >= Parents.Length)
            {
                throw new ArgumentOutOfRangeException("node");
            }

            var result = new List<int>();
            var stack = new Stack<int>();
            stack.Push(node);
            while (stack.Count > 0)
            {
                var v = stack.Pop();
                if (NodeFeatures[v] >= 0)
                {
                    result.Add(NodeFeatures[v]);
                }

                foreach (var c in shape.Children[v])
                {
                    stack.Push(c);
                }
            }

            result.Sort();
            return result.ToArray();
        }

        public int[][] GetFeatureGroups()
        {
            var latent = GetLatentNodes();
            var groups = new int[latent.Length][];
            for (var i = 0; i < latent.Length; i++)
            {
                groups[i] = GetDescendantFeatures(latent[i]);
            }

            return groups;
        }

        public int Discretise(int feature, double value)
        {
            return Bin(BinEdges[feature], MissingStates[feature], value);
        }

        internal static int Bin(double[] edges, bool missingState, double value)
        {
            if (double.IsNaN(value))
            {
                return missingState ? edges.Length + 1 : -1;
            }

            var lo = 0;
            var hi = edges.Length;
            while (lo < hi)
            {
                var mid = (lo + hi) >> 1;
                if (edges[mid] < value)
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

        public int[] Discretise(double[] input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (input.Length != NumberOfInputs)
            {
                throw new DimensionMismatchException("input", "The input must have " + NumberOfInputs + " values.");
            }

            var result = new int[input.Length];
            for (var f = 0; f < input.Length; f++)
            {
                result[f] = Discretise(f, input[f]);
            }

            return result;
        }

        public override double[] LogLikelihoods(double[] input, double[] result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            if (result.Length < NumberOfClasses)
            {
                throw new DimensionMismatchException("result", "The result array is too small.");
            }

            var ws = new LatentTreeWorkspace(shape);
            var logScale = Propagate(input, ws);
            var lam0 = ws.Lambda[0];
            var prior = Conditionals[0][0];
            for (var c = 0; c < NumberOfClasses; c++)
            {
                var p = prior[c] * lam0[c];
                result[c] = p > 0 && !double.IsNegativeInfinity(logScale)
                    ? System.Math.Log(p) + logScale
                    : double.NegativeInfinity;
            }

            return result;
        }

        public double LogMarginalLikelihood(double[] input)
        {
            var joint = LogLikelihoods(input, new double[NumberOfClasses]);
            var max = double.NegativeInfinity;
            for (var c = 0; c < joint.Length; c++)
            {
                if (joint[c] > max)
                {
                    max = joint[c];
                }
            }

            if (double.IsNegativeInfinity(max))
            {
                return max;
            }

            double sum = 0;
            for (var c = 0; c < joint.Length; c++)
            {
                sum += System.Math.Exp(joint[c] - max);
            }

            return max + System.Math.Log(sum);
        }

        public override double[] Probabilities(double[] input, double[] result)
        {
            LogLikelihoods(input, result);
            var max = double.NegativeInfinity;
            for (var c = 0; c < NumberOfClasses; c++)
            {
                if (result[c] > max)
                {
                    max = result[c];
                }
            }

            if (double.IsNegativeInfinity(max) || double.IsNaN(max))
            {
                var prior = Conditionals[0][0];
                for (var c = 0; c < NumberOfClasses; c++)
                {
                    result[c] = prior[c];
                }

                return result;
            }

            double sum = 0;
            for (var c = 0; c < NumberOfClasses; c++)
            {
                result[c] = System.Math.Exp(result[c] - max);
                sum += result[c];
            }

            for (var c = 0; c < NumberOfClasses; c++)
            {
                result[c] /= sum;
            }

            return result;
        }

        private double Propagate(double[] input, LatentTreeWorkspace ws)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            if (input.Length != NumberOfInputs)
            {
                throw new DimensionMismatchException("input", "The input must have " + NumberOfInputs + " values.");
            }

            for (var v = 0; v < ws.Observed.Length; v++)
            {
                ws.Observed[v] = -1;
            }

            for (var f = 0; f < input.Length; f++)
            {
                ws.Observed[FeatureNodes[f]] = Discretise(f, input[f]);
            }

            return LatentTreeInference.Upward(shape, Conditionals, ws);
        }

        private static void CheckDistribution(double[] row, string name)
        {
            double sum = 0;
            for (var i = 0; i < row.Length; i++)
            {
                if (double.IsNaN(row[i]) || row[i] < 0 || double.IsInfinity(row[i]))
                {
                    throw new ArgumentOutOfRangeException(name, "Probabilities must be finite and non-negative.");
                }

                sum += row[i];
            }

            if (System.Math.Abs(sum - 1.0) > 1e-6)
            {
                throw new ArgumentException("Every probability row must sum to one.", name);
            }
        }
    }
}