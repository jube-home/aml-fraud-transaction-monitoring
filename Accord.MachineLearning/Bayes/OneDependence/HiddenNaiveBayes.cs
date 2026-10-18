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
    public class HiddenNaiveBayes : MulticlassLikelihoodClassifierBase<double[]>
    {
        private readonly double[][] logConditionals;
        private readonly double[] logPriors;

        internal HiddenNaiveBayes(int classes, double[][] binEdges, int[] numberOfStates, double[] priors,
            double[][] conditionals, double[][][] pairConditionals, double[][] parentWeights)
        {
            NumberOfInputs = binEdges.Length;
            NumberOfOutputs = classes;
            NumberOfClasses = classes;
            BinEdges = binEdges;
            NumberOfStates = numberOfStates;
            Priors = priors;
            Conditionals = conditionals;
            PairConditionals = pairConditionals;
            ParentWeights = parentWeights;

            logPriors = DiscreteBayesTools.Log(priors);
            logConditionals = DiscreteBayesTools.Log(conditionals);
        }

        public double[][] BinEdges { get; private set; }

        public int[] NumberOfStates { get; private set; }

        public double[] Priors { get; private set; }

        public double[][] Conditionals { get; private set; }

        public double[][][] PairConditionals { get; private set; }

        public double[][] ParentWeights { get; private set; }

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
            var d = states.Length;
            var sum = logPriors[c];

            if (d == 1)
            {
                return sum + logConditionals[0][c * NumberOfStates[0] + states[0]];
            }

            for (var i = 0; i < d; i++)
            {
                double mixture = 0;
                var weights = ParentWeights[i];
                for (var j = 0; j < d; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    var weight = weights[j];
                    if (weight == 0)
                    {
                        continue;
                    }

                    var index = (c * NumberOfStates[j] + states[j]) * NumberOfStates[i] + states[i];
                    mixture += weight * PairConditionals[i][j][index];
                }

                sum += System.Math.Log(mixture);
            }

            return sum;
        }
    }
}