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
    public class AveragedOneDependenceEstimators : MulticlassLikelihoodClassifierBase<double[]>
    {
        private readonly double[][] logConditionals;
        private readonly double[] logEstimatorWeights;
        private readonly double[][] logPairs;
        private readonly double[] logPriors;

        internal AveragedOneDependenceEstimators(int classes, double minimumSupport, bool weighted, double[][] binEdges,
            int[] numberOfStates, double[] priors, double[][] conditionals, double[][][] pairConditionals,
            double[][] superParentSupport, double[] estimatorWeights)
        {
            NumberOfInputs = binEdges.Length;
            NumberOfOutputs = classes;
            NumberOfClasses = classes;
            MinimumSupport = minimumSupport;
            Weighted = weighted;
            BinEdges = binEdges;
            NumberOfStates = numberOfStates;
            Priors = priors;
            Conditionals = conditionals;
            PairConditionals = pairConditionals;
            SuperParentSupport = superParentSupport;
            EstimatorWeights = estimatorWeights;

            logPriors = DiscreteBayesTools.Log(priors);
            logConditionals = DiscreteBayesTools.Log(conditionals);
            logEstimatorWeights = DiscreteBayesTools.Log(estimatorWeights);
            logPairs = new double[pairConditionals.Length * pairConditionals.Length][];
            for (var i = 0; i < pairConditionals.Length; i++)
            {
                for (var j = 0; j < pairConditionals.Length; j++)
                {
                    if (pairConditionals[i][j] != null)
                    {
                        logPairs[i * pairConditionals.Length + j] = DiscreteBayesTools.Log(pairConditionals[i][j]);
                    }
                }
            }
        }

        public double MinimumSupport { get; private set; }

        public bool Weighted { get; private set; }

        public double[][] BinEdges { get; private set; }

        public int[] NumberOfStates { get; private set; }

        public double[] Priors { get; private set; }

        public double[][] Conditionals { get; private set; }

        public double[][][] PairConditionals { get; private set; }

        public double[][] SuperParentSupport { get; private set; }

        public double[] EstimatorWeights { get; private set; }

        public override double LogLikelihood(double[] input, int classIndex)
        {
            DiscreteBayesTools.CheckInput(input, NumberOfInputs);
            if (classIndex < 0 || classIndex >= NumberOfOutputs)
            {
                throw new ArgumentOutOfRangeException("classIndex");
            }

            var states = DiscreteBayesTools.States(input, BinEdges);
            return LogJoint(states, classIndex, Eligible(states), new double[states.Length]);
        }

        public override double[] LogLikelihoods(double[] input, double[] result)
        {
            DiscreteBayesTools.CheckInput(input, NumberOfInputs);
            var states = DiscreteBayesTools.States(input, BinEdges);
            var eligible = Eligible(states);
            var buffer = new double[states.Length];
            for (var c = 0; c < NumberOfOutputs; c++)
            {
                result[c] = LogJoint(states, c, eligible, buffer);
            }

            return result;
        }

        public override int Decide(double[] input)
        {
            return LogLikelihoods(input, new double[NumberOfOutputs]).ArgMax();
        }

        private bool[] Eligible(int[] states)
        {
            var eligible = new bool[states.Length];
            for (var i = 0; i < states.Length; i++)
            {
                eligible[i] = SuperParentSupport[i][states[i]] >= MinimumSupport;
            }

            return eligible;
        }

        private double LogJoint(int[] states, int c, bool[] eligible, double[] buffer)
        {
            var d = states.Length;
            var count = 0;
            double weightSum = 0;
            for (var i = 0; i < d; i++)
            {
                if (eligible[i])
                {
                    count++;
                    weightSum += EstimatorWeights[i];
                }
            }

            if (count == 0)
            {
                var naive = logPriors[c];
                for (var j = 0; j < d; j++)
                {
                    naive += logConditionals[j][c * NumberOfStates[j] + states[j]];
                }

                return naive;
            }

            var uniform = !(weightSum > 0);
            var logNormalizer = System.Math.Log(uniform ? count : weightSum);
            var used = 0;
            for (var i = 0; i < d; i++)
            {
                if (!eligible[i])
                {
                    continue;
                }

                var sum = logPriors[c] + logConditionals[i][c * NumberOfStates[i] + states[i]];
                for (var j = 0; j < d; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    sum += logPairs[i * d + j][(c * NumberOfStates[i] + states[i]) * NumberOfStates[j] + states[j]];
                }

                buffer[used++] = sum + (uniform ? 0 : logEstimatorWeights[i]);
            }

            return DiscreteBayesTools.LogSumExp(buffer, used) - logNormalizer;
        }
    }
}