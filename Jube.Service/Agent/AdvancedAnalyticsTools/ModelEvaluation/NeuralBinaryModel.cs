/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using Accord.Neuro;
using Accord.Neuro.Learning;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed class NeuralBinaryModel : IBinaryModel
    {
        private readonly int epochsRun;
        private readonly object gate = new();
        private readonly int[] layers;
        private readonly ActivationNetwork network;
        private readonly EvaluationNumber[] trace;

        public NeuralBinaryModel(ActivationNetwork network, EvaluationNumber[] trace, int epochsRun, int[] layers)
        {
            this.network = network;
            this.trace = trace;
            this.epochsRun = epochsRun;
            this.layers = layers;
        }

        public bool OutputsProbability => true;

        public double[] Score(double[][] rows)
        {
            var result = new double[rows.Length];

            lock (gate)
            {
                for (var i = 0; i < rows.Length; i++)
                {
                    result[i] = network.Compute(rows[i])[0];
                }
            }

            return result;
        }

        public ModelDescription Describe(string[] names)
        {
            var facts = new List<EvaluationNumber>
            {
                ModelText.Fact("EpochsRun", epochsRun),
                ModelText.Fact("Parameters", ParameterCount(names.Length))
            };
            facts.AddRange(trace);

            return new ModelDescription(
                $"A multilayer perceptron with hidden layers {string.Join("-", layers)} and sigmoid units trained by resilient backpropagation on standardised predictors from a seeded start; the output is a probability, calibrated afterwards. The trace lists the training squared error per epoch.",
                facts.ToArray(),
                [],
                null,
                []);
        }

        public static NeuralBinaryModel Fit(LearnerConfig c, double[][] x, int[] y, FitContext ctx)
        {
            var p = x[0].Length;
            var layers = c.Hidden.Length == 0 ? [8] : c.Hidden;
            var network = NeuralNetworkBuilder.Build(p, layers, 1, ctx.Seed);
            var targets = y.Select(v => new[] { (double)v }).ToArray();
            var learner = new ResilientBackpropagationLearning(network);
            var trace = new List<EvaluationNumber>();
            var best = double.MaxValue;
            var bestWeights = default(double[][][]);
            var bestThresholds = default(double[][]);
            var sinceBest = 0;
            var epochs = 0;
            var stopping = c.Patience > 0 && ctx.ValidationX is { Length: > 0 } && ctx.ValidationY != null;

            for (var epoch = 1; epoch <= c.Epochs; epoch++)
            {
                ctx.Token.ThrowIfCancellationRequested();
                var error = learner.RunEpoch(x, targets);
                epochs = epoch;

                if (epoch is 1 or 5 or 10 or 25 or 50 or 100 or 200 or 500 or 1000 || epoch == c.Epochs)
                {
                    trace.Add(new EvaluationNumber($"TrainSquaredErrorEpoch{epoch}",
                        double.IsFinite(error) ? error : null));
                }

                if (!stopping)
                {
                    continue;
                }

                var validation = 0d;

                for (var i = 0; i < ctx.ValidationX!.Length; i++)
                {
                    var d = network.Compute(ctx.ValidationX[i])[0] - ctx.ValidationY![i];
                    validation += d * d;
                }

                if (validation < best)
                {
                    best = validation;
                    sinceBest = 0;
                    (bestWeights, bestThresholds) = NeuralNetworkBuilder.Snapshot(network);
                }
                else if (++sinceBest >= c.Patience)
                {
                    break;
                }
            }

            if (stopping && bestWeights != null && bestThresholds != null)
            {
                NeuralNetworkBuilder.Restore(network, bestWeights, bestThresholds);
            }

            return new NeuralBinaryModel(network, trace.ToArray(), epochs, layers);
        }

        private double ParameterCount(int inputs)
        {
            var count = 0d;
            var previous = inputs;

            foreach (var units in layers.Append(1))
            {
                count += units * (previous + 1);
                previous = units;
            }

            return count;
        }
    }
}