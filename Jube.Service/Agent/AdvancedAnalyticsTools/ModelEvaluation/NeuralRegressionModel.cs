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

using Accord.Math;
using Accord.Neuro;
using Accord.Neuro.Learning;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed class NeuralRegressionModel : IRegressionModel
    {
        private readonly int epochs;
        private readonly object gate = new();
        private readonly int[] layers;
        private readonly double low;
        private readonly ActivationNetwork network;
        private readonly double span;
        private readonly EvaluationNumber[] trace;

        private NeuralRegressionModel(ActivationNetwork network, double low, double span, int[] layers,
            EvaluationNumber[] trace, int epochs)
        {
            this.network = network;
            this.low = low;
            this.span = span;
            this.layers = layers;
            this.trace = trace;
            this.epochs = epochs;
        }

        public double[] Predict(double[][] rows)
        {
            var result = new double[rows.Length];

            lock (gate)
            {
                for (var i = 0; i < rows.Length; i++)
                {
                    result[i] = (network.Compute(rows[i])[0] - 0.05) / 0.9 * span + low;
                }
            }

            return result;
        }

        public EvaluationNumber[] Coefficients(string[] names)
        {
            return [];
        }

        public ModelDescription Describe(string[] names)
        {
            var facts = new List<EvaluationNumber> { ModelText.Fact("EpochsRun", epochs) };
            facts.AddRange(trace);

            return new ModelDescription(
                $"A multilayer perceptron with hidden layers {string.Join("-", layers)} trained by resilient backpropagation on standardised predictors and a target rescaled into the sigmoid range; predictions are mapped back to the original target scale.",
                facts.ToArray(),
                [],
                null,
                []);
        }

        public static NeuralRegressionModel Fit(RegressorConfig c, double[][] x, double[] y, RegressionFitContext ctx)
        {
            var low = y.Min();
            var span = y.Max() - low;

            if (span <= 0)
            {
                span = 1d;
            }

            var layers = c.Hidden.Length == 0 ? [8] : c.Hidden;
            var network = NeuralNetworkBuilder.Build(x[0].Length, layers, 1, ctx.Seed);
            var targets = y.Select(v => new[] { 0.05 + 0.9 * (v - low) / span }).ToArray();
            var learner = new ResilientBackpropagationLearning(network);
            var trace = new List<EvaluationNumber>();

            for (var epoch = 1; epoch <= c.Epochs; epoch++)
            {
                ctx.Token.ThrowIfCancellationRequested();
                var error = learner.RunEpoch(x, targets);

                if (epoch is 1 or 10 or 50 or 100 or 200 or 500 or 1000 || epoch == c.Epochs)
                {
                    trace.Add(new EvaluationNumber($"TrainSquaredErrorEpoch{epoch}",
                        double.IsFinite(error) ? error : null));
                }
            }

            return new NeuralRegressionModel(network, low, span, layers, trace.ToArray(), c.Epochs);
        }
    }
}