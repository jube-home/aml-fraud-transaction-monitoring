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
using Accord.Math;

namespace Accord.MachineLearning.Bayes
{
    [Serializable]
    public class KDependenceBayes : MulticlassLikelihoodClassifierBase<double[]>
    {
        private readonly double[][] logConditionals;
        private readonly double[] logPriors;
        private readonly int[] parentCombinations;

        internal KDependenceBayes(int k, int classes, double[][] binEdges, int[] numberOfStates, int[] order,
            int[][] parents, double[] priors, double[][] conditionals)
        {
            K = k;
            NumberOfInputs = binEdges.Length;
            NumberOfOutputs = classes;
            NumberOfClasses = classes;
            BinEdges = binEdges;
            NumberOfStates = numberOfStates;
            Order = order;
            Parents = parents;
            Priors = priors;
            Conditionals = conditionals;

            logPriors = DiscreteBayesTools.Log(priors);
            logConditionals = DiscreteBayesTools.Log(conditionals);
            parentCombinations = new int[parents.Length];
            for (var j = 0; j < parents.Length; j++)
            {
                var combinations = 1;
                for (var p = 0; p < parents[j].Length; p++)
                {
                    combinations *= numberOfStates[parents[j][p]];
                }

                parentCombinations[j] = combinations;
            }
        }

        public int K { get; private set; }

        public double[][] BinEdges { get; private set; }

        public int[] NumberOfStates { get; private set; }

        public int[] Order { get; private set; }

        public int[][] Parents { get; private set; }

        public double[] Priors { get; private set; }

        public double[][] Conditionals { get; private set; }

        public override double LogLikelihood(double[] input, int classIndex)
        {
            DiscreteBayesTools.CheckInput(input, NumberOfInputs);
            if (classIndex < 0 || classIndex >= NumberOfOutputs)
            {
                throw new ArgumentOutOfRangeException("classIndex");
            }

            return LogJoint(DiscreteBayesTools.States(input, BinEdges), classIndex);
        }

        public override double[] LogLikelihoods(double[] input, double[] result)
        {
            DiscreteBayesTools.CheckInput(input, NumberOfInputs);
            var states = DiscreteBayesTools.States(input, BinEdges);
            for (var c = 0; c < NumberOfOutputs; c++)
            {
                result[c] = LogJoint(states, c);
            }

            return result;
        }

        public override int Decide(double[] input)
        {
            return LogLikelihoods(input, new double[NumberOfOutputs]).ArgMax();
        }

        private double LogJoint(int[] states, int c)
        {
            var sum = logPriors[c];
            for (var j = 0; j < states.Length; j++)
            {
                var parentIndex = 0;
                var parents = Parents[j];
                for (var p = 0; p < parents.Length; p++)
                {
                    parentIndex = parentIndex * NumberOfStates[parents[p]] + states[parents[p]];
                }

                var index = (c * parentCombinations[j] + parentIndex) * NumberOfStates[j] + states[j];
                sum += logConditionals[j][index];
            }

            return sum;
        }
    }
}