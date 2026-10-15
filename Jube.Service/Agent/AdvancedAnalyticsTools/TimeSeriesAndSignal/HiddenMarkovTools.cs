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

using System.ComponentModel;
using Accord.Statistics.Distributions.Fitting;
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Models.Markov;
using Accord.Statistics.Models.Markov.Learning;
using Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal
{
    [AdvancedAnalyticsToolGroup("TimeSeriesAndSignal")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class HiddenMarkovTools
    {
        private const int MaximumStates = 12;
        private const int MaximumObservations = 200000;

        private static void Rows(double[][]? matrix, int columns, string name, string what)
        {
            if (matrix == null || matrix.Length == 0)
            {
                throw new ArgumentException($"{name} is required: {what}", name);
            }

            for (var i = 0; i < matrix.Length; i++)
            {
                if (matrix[i] == null || matrix[i].Length != columns)
                {
                    throw new ArgumentException(
                        $"{name} row {i} has {(matrix[i] == null ? 0 : matrix[i].Length)} entries but {columns} are expected.",
                        name);
                }

                if (matrix[i].Any(x => double.IsNaN(x) || x < 0 || x > 1 + 1e-9) ||
                    Math.Abs(matrix[i].Sum() - 1) > 1e-6)
                {
                    throw new ArgumentException($"{name} row {i} must be probabilities between 0 and 1 summing to 1.",
                        name);
                }
            }
        }

        private static void ModelShape(double[][] transitions, double[] initial, out int states)
        {
            if (transitions == null || transitions.Length == 0)
            {
                throw new ArgumentException(
                    "transitions is required: a square array whose row i gives the probability of moving from hidden state i to each hidden state.",
                    nameof(transitions));
            }

            states = transitions.Length;

            if (states > MaximumStates)
            {
                throw new ArgumentException($"The model has {states} hidden states; the limit is {MaximumStates}.",
                    nameof(transitions));
            }

            Rows(transitions, states, nameof(transitions), "a square array of transition probabilities.");

            if (initial == null || initial.Length != states || initial.Any(x => double.IsNaN(x) || x < 0) ||
                Math.Abs(initial.Sum() - 1) > 1e-6)
            {
                throw new ArgumentException(
                    $"initial must contain {states} non-negative probabilities (one per hidden state) summing to 1.",
                    nameof(initial));
            }
        }

        private static HiddenMarkovModel BuildDiscrete(double[][] transitions, double[][] emissions, double[] initial,
            out int symbols)
        {
            ModelShape(transitions, initial, out var states);

            if (emissions == null || emissions.Length != states || emissions[0] == null || emissions[0].Length < 1)
            {
                throw new ArgumentException(
                    $"emissions must have one row per hidden state ({states}) and one column per possible symbol.",
                    nameof(emissions));
            }

            symbols = emissions[0].Length;

            Rows(emissions, symbols, nameof(emissions),
                "one row per hidden state giving the probability of each symbol.");

            return new HiddenMarkovModel(transitions, emissions, initial);
        }

        private static HiddenMarkovModel<NormalDistribution, double> BuildGaussian(double[][] transitions,
            double[] means,
            double[] standardDeviations, double[] initial)
        {
            ModelShape(transitions, initial, out var states);

            if (means == null || means.Length != states || means.Any(x => !double.IsFinite(x)))
            {
                throw new ArgumentException($"means must contain {states} finite numbers, one per hidden state.",
                    nameof(means));
            }

            if (standardDeviations == null || standardDeviations.Length != states ||
                standardDeviations.Any(x => !(x > 0) || double.IsInfinity(x)))
            {
                throw new ArgumentException(
                    $"standardDeviations must contain {states} numbers above 0, one per hidden state.",
                    nameof(standardDeviations));
            }

            var emissions = means.Select((m, i) => new NormalDistribution(m, standardDeviations[i])).ToArray();

            return new HiddenMarkovModel<NormalDistribution, double>(transitions, emissions, initial);
        }

        private static int[] Symbols(int[]? observations, int symbols, string name)
        {
            if (observations == null || observations.Length < 1)
            {
                throw new ArgumentException($"{name} must contain at least one observed symbol code.", name);
            }

            if (observations.Length > MaximumObservations)
            {
                throw new ArgumentException(
                    $"{name} has {observations.Length} values; the limit is {MaximumObservations}.",
                    name);
            }

            for (var i = 0; i < observations.Length; i++)
            {
                if (observations[i] < 0 || observations[i] >= symbols)
                {
                    throw new ArgumentException(
                        $"{name}[{i}] is {observations[i]} but symbol codes must be from 0 to {symbols - 1} (the number of columns in emissions).",
                        name);
                }
            }

            return observations;
        }

        private static double[] Values(double[]? observations, string name)
        {
            var v = TimeSeriesGuard.Finite(observations, name, 1);

            if (v.Length > MaximumObservations)
            {
                throw new ArgumentException($"{name} has {v.Length} values; the limit is {MaximumObservations}.", name);
            }

            return v;
        }

        private static HmmLikelihoodResult Likelihood(double[,] forwardResult, double[] scaling, double logLikelihood,
            int n)
        {
            _ = forwardResult;
            var steps = scaling.Take(n).Select(s => s > 0 ? Math.Log(s) : double.NegativeInfinity).ToArray();
            var worst = 0;

            for (var t = 1; t < steps.Length; t++)
            {
                if (steps[t] < steps[worst])
                {
                    worst = t;
                }
            }

            return new HmmLikelihoodResult(logLikelihood, logLikelihood / n, steps, worst, n);
        }

        private static HmmPosteriorResult PosteriorFrom(double[,] forward, double[,] backward, int n, int states,
            double logLikelihood)
        {
            var posterior = new double[n][];
            var best = new int[n];

            for (var t = 0; t < n; t++)
            {
                posterior[t] = new double[states];
                var sum = 0.0;

                for (var i = 0; i < states; i++)
                {
                    posterior[t][i] = forward[t, i] * backward[t, i];
                    sum += posterior[t][i];
                }

                for (var i = 0; i < states; i++)
                {
                    posterior[t][i] = sum > 0 ? posterior[t][i] / sum : double.NaN;
                }

                best[t] = Array.IndexOf(posterior[t], posterior[t].Max());
            }

            return new HmmPosteriorResult(posterior, best, logLikelihood);
        }

        [AdvancedAnalyticsTool("HiddenMarkovDiscreteDecode")]
        [Description(
            "Viterbi decoding for a hidden Markov model whose observations are discrete symbols: finds the single most likely sequence of hidden states that produced the observed symbols. Use it to label each step of a behaviour sequence with a latent regime (for example normal versus compromised, or browsing versus purchasing) under a model you supply or trained with HiddenMarkovDiscreteTrain. " +
            "The model is: transitions[i][j] = P(next hidden state j | current i); emissions[i][k] = P(symbol k | hidden state i); initial[i] = P(first hidden state i). Every row must sum to 1. observations are symbol codes from 0 to the number of emission columns minus 1. " +
            "StatePath[t] is the decoded hidden state (zero-based) at step t and LogLikelihoodOfPath the natural-log probability of that path together with the observations. Limited to 12 hidden states and 200,000 observations.")]
        public HmmDecodeResult HiddenMarkovDiscreteDecode(
            [Description("The observed symbol codes in time order, each from 0 to the number of symbols minus 1.")]
            int[] observations,
            [Description("Hidden-state transition matrix, states x states, each row summing to 1.")]
            double[][] transitions,
            [Description(
                "Emission matrix, states x symbols: row i gives the probability of each symbol in hidden state i, summing to 1.")]
            double[][] emissions,
            [Description("Probability of each hidden state at the first step, summing to 1.")]
            double[] initial)
        {
            var model = BuildDiscrete(transitions, emissions, initial, out var symbols);
            var obs = Symbols(observations, symbols, nameof(observations));
            model.Algorithm = HiddenMarkovModelAlgorithm.Viterbi;
            int[]? path = null;
            var logLikelihood = model.LogLikelihood(obs, ref path);

            return new HmmDecodeResult(path!, logLikelihood, obs.Length);
        }

        [AdvancedAnalyticsTool("HiddenMarkovDiscreteLogLikelihood")]
        [Description(
            "Log-likelihood of an observed symbol sequence under a discrete hidden Markov model, computed with the forward algorithm summing over all possible hidden paths. Use it to score how well a sequence fits a behavioural model, to compare two candidate models, or to flag sequences that a model of normal behaviour finds very improbable. " +
            "The model is given as transitions, emissions and initial probabilities (rows sum to 1). LogLikelihood is the natural-log probability of the whole sequence; AverageLogLikelihoodPerObservation divides by the length so sequences of different lengths can be compared; StepLogProbabilities[t] is the log of the probability of symbol t given all earlier symbols (a low value means a surprising step) and MostSurprisingIndex the step with the lowest value. " +
            "Symbols the model gives zero probability produce minus infinity. Limited to 12 hidden states and 200,000 observations.")]
        public HmmLikelihoodResult HiddenMarkovDiscreteLogLikelihood(
            [Description("The observed symbol codes in time order, each from 0 to the number of symbols minus 1.")]
            int[] observations,
            [Description("Hidden-state transition matrix, states x states, each row summing to 1.")]
            double[][] transitions,
            [Description("Emission matrix, states x symbols, each row summing to 1.")]
            double[][] emissions,
            [Description("Probability of each hidden state at the first step, summing to 1.")]
            double[] initial)
        {
            var model = BuildDiscrete(transitions, emissions, initial, out var symbols);
            var obs = Symbols(observations, symbols, nameof(observations));
            var forward = ForwardBackwardAlgorithm.Forward(model, obs, out var scaling, out var logLikelihood);

            return Likelihood(forward, scaling, logLikelihood, obs.Length);
        }

        [AdvancedAnalyticsTool("HiddenMarkovDiscretePosterior")]
        [Description(
            "Smoothed posterior state probabilities for a discrete hidden Markov model, from the forward-backward algorithm: for every step, the probability of each hidden state given the whole observed sequence (past and future). Use it instead of Viterbi decoding when you want to know how confident the model is about each step, for example the probability an account is in a compromised state at each transaction. " +
            "StateProbabilities[t][i] is the probability of hidden state i at step t (each row sums to 1); MostLikelyStates[t] is the state with the highest posterior at each step (which can differ from the Viterbi path); LogLikelihood is the sequence log-likelihood. " +
            "The model is given as transitions, emissions and initial probabilities (rows sum to 1). Limited to 12 hidden states and 200,000 observations.")]
        public HmmPosteriorResult HiddenMarkovDiscretePosterior(
            [Description("The observed symbol codes in time order, each from 0 to the number of symbols minus 1.")]
            int[] observations,
            [Description("Hidden-state transition matrix, states x states, each row summing to 1.")]
            double[][] transitions,
            [Description("Emission matrix, states x symbols, each row summing to 1.")]
            double[][] emissions,
            [Description("Probability of each hidden state at the first step, summing to 1.")]
            double[] initial)
        {
            var model = BuildDiscrete(transitions, emissions, initial, out var symbols);
            var obs = Symbols(observations, symbols, nameof(observations));
            var forward = ForwardBackwardAlgorithm.Forward(model, obs, out var scaling, out var logLikelihood);
            var backward = ForwardBackwardAlgorithm.Backward(model, obs, scaling);

            return PosteriorFrom(forward, backward, obs.Length, transitions.Length, logLikelihood);
        }

        [AdvancedAnalyticsTool("HiddenMarkovDiscreteNextSymbolDistribution")]
        [Description(
            "Predicts what comes next in a symbol sequence under a discrete hidden Markov model: the probability of each possible next symbol given everything observed so far. Use it for next-action prediction or to check whether an actual next event was expected. " +
            "FilteredStateProbabilities is the belief about the hidden state at the last observation; NextStateProbabilities is that belief pushed one step through the transition matrix; NextSymbolProbabilities[k] is the probability that the next symbol is k (sums to 1) and MostLikelyNextSymbol the most probable one. " +
            "The model is given as transitions, emissions and initial probabilities (rows sum to 1). Limited to 12 hidden states and 200,000 observations.")]
        public HmmNextDistributionResult HiddenMarkovDiscreteNextSymbolDistribution(
            [Description("The observed symbol codes so far in time order, at least 1.")]
            int[] observations,
            [Description("Hidden-state transition matrix, states x states, each row summing to 1.")]
            double[][] transitions,
            [Description("Emission matrix, states x symbols, each row summing to 1.")]
            double[][] emissions,
            [Description("Probability of each hidden state at the first step, summing to 1.")]
            double[] initial)
        {
            var model = BuildDiscrete(transitions, emissions, initial, out var symbols);
            var obs = Symbols(observations, symbols, nameof(observations));
            var forward = ForwardBackwardAlgorithm.Forward(model, obs, out var _, out var _);
            var states = transitions.Length;
            var last = obs.Length - 1;
            var filtered = Enumerable.Range(0, states).Select(i => forward[last, i]).ToArray();
            var total = filtered.Sum();
            filtered = filtered.Select(x => total > 0 ? x / total : double.NaN).ToArray();
            var next = new double[states];
            var nextSymbol = new double[symbols];

            for (var i = 0; i < states; i++)
            {
                for (var j = 0; j < states; j++)
                {
                    next[j] += filtered[i] * transitions[i][j];
                }
            }

            for (var j = 0; j < states; j++)
            {
                for (var k = 0; k < symbols; k++)
                {
                    nextSymbol[k] += next[j] * emissions[j][k];
                }
            }

            return new HmmNextDistributionResult(filtered, next, nextSymbol,
                Array.IndexOf(nextSymbol, nextSymbol.Max()));
        }

        [AdvancedAnalyticsTool("HiddenMarkovGaussianDecode")]
        [Description(
            "Viterbi decoding for a hidden Markov model whose observations are numbers drawn from a normal distribution that depends on the hidden state (a regime-switching model): finds the most likely sequence of hidden regimes for a numeric series. Use it to label each point of a series (for example daily volume) as belonging to a quiet, normal or busy regime under a model you supply or trained with HiddenMarkovGaussianTrain. " +
            "transitions[i][j] = P(next regime j | current i); means[i] and standardDeviations[i] describe the normal distribution of values in regime i; initial[i] = P(first regime i). Every transition row must sum to 1 and every standard deviation must be above 0. " +
            "StatePath[t] is the decoded regime (zero-based) and LogLikelihoodOfPath the natural-log density of that path with the observations. Limited to 12 regimes and 200,000 observations.")]
        public HmmDecodeResult HiddenMarkovGaussianDecode(
            [Description("The numeric series in time order, finite.")]
            double[] observations,
            [Description("Regime transition matrix, states x states, each row summing to 1.")]
            double[][] transitions,
            [Description("Mean of the observations in each regime, one per state.")]
            double[] means,
            [Description("Standard deviation of the observations in each regime, one per state, each above 0.")]
            double[] standardDeviations,
            [Description("Probability of each regime at the first step, summing to 1.")]
            double[] initial)
        {
            var model = BuildGaussian(transitions, means, standardDeviations, initial);
            var obs = Values(observations, nameof(observations));
            model.Algorithm = HiddenMarkovModelAlgorithm.Viterbi;
            int[]? path = null;
            var logLikelihood = model.LogLikelihood(obs, ref path);

            return new HmmDecodeResult(path!, logLikelihood, obs.Length);
        }

        [AdvancedAnalyticsTool("HiddenMarkovGaussianLogLikelihood")]
        [Description(
            "Log-likelihood of a numeric series under a Gaussian-emission hidden Markov model (regime-switching normal model), computed with the forward algorithm. Use it to judge how well a regime model explains a series, to compare models with different numbers of regimes or to flag series that a model of normal behaviour finds unlikely. " +
            "LogLikelihood is the natural-log density of the whole series (a density, so it can be positive); AverageLogLikelihoodPerObservation divides by the length; StepLogProbabilities[t] is the log predictive density of value t given the earlier values (low means surprising) and MostSurprisingIndex the lowest step. " +
            "Every transition row must sum to 1 and every standard deviation be above 0. Limited to 12 regimes and 200,000 observations.")]
        public HmmLikelihoodResult HiddenMarkovGaussianLogLikelihood(
            [Description("The numeric series in time order, finite.")]
            double[] observations,
            [Description("Regime transition matrix, states x states, each row summing to 1.")]
            double[][] transitions,
            [Description("Mean of the observations in each regime, one per state.")]
            double[] means,
            [Description("Standard deviation of the observations in each regime, one per state, each above 0.")]
            double[] standardDeviations,
            [Description("Probability of each regime at the first step, summing to 1.")]
            double[] initial)
        {
            var model = BuildGaussian(transitions, means, standardDeviations, initial);
            var obs = Values(observations, nameof(observations));

            var forward =
                ForwardBackwardAlgorithm.Forward(model, obs, out var scaling,
                    out var logLikelihood);

            return Likelihood(forward, scaling, logLikelihood, obs.Length);
        }

        [AdvancedAnalyticsTool("HiddenMarkovGaussianPosterior")]
        [Description(
            "Smoothed posterior regime probabilities for a Gaussian-emission hidden Markov model, from the forward-backward algorithm: for every point of a numeric series, the probability of each hidden regime given the whole series. Use it to see how certain the regime assignment is, for example how likely each day is to belong to a high-activity regime. " +
            "StateProbabilities[t][i] is the probability of regime i at step t (each row sums to 1); MostLikelyStates[t] is the highest-probability regime per step; LogLikelihood is the series log-density. " +
            "Every transition row must sum to 1 and every standard deviation be above 0. Limited to 12 regimes and 200,000 observations.")]
        public HmmPosteriorResult HiddenMarkovGaussianPosterior(
            [Description("The numeric series in time order, finite.")]
            double[] observations,
            [Description("Regime transition matrix, states x states, each row summing to 1.")]
            double[][] transitions,
            [Description("Mean of the observations in each regime, one per state.")]
            double[] means,
            [Description("Standard deviation of the observations in each regime, one per state, each above 0.")]
            double[] standardDeviations,
            [Description("Probability of each regime at the first step, summing to 1.")]
            double[] initial)
        {
            var model = BuildGaussian(transitions, means, standardDeviations, initial);
            var obs = Values(observations, nameof(observations));

            var forward =
                ForwardBackwardAlgorithm.Forward(model, obs, out var scaling,
                    out var logLikelihood);

            var backward = ForwardBackwardAlgorithm.Backward(model, obs, scaling);

            return PosteriorFrom(forward, backward, obs.Length, transitions.Length, logLikelihood);
        }

        [AdvancedAnalyticsTool("HiddenMarkovGaussianNextValueDistribution")]
        [Description(
            "One-step-ahead forecast of a numeric series under a Gaussian-emission hidden Markov model: the expected next value and its standard deviation, given the regime beliefs after everything observed so far. Use it for regime-aware forecasting or to standardise a new value (subtract ExpectedNextValue, divide by NextValueStandardDeviation) before comparing it with a threshold. " +
            "FilteredStateProbabilities is the belief about the current regime, NextStateProbabilities the belief one step later, ExpectedNextValue the mixture mean and NextValueStandardDeviation the mixture standard deviation (it includes both within-regime spread and uncertainty about the regime). " +
            "Every transition row must sum to 1 and every standard deviation be above 0. Limited to 12 regimes and 200,000 observations.")]
        public HmmNextValueResult HiddenMarkovGaussianNextValueDistribution(
            [Description("The numeric series so far in time order, finite, at least 1 value.")]
            double[] observations,
            [Description("Regime transition matrix, states x states, each row summing to 1.")]
            double[][] transitions,
            [Description("Mean of the observations in each regime, one per state.")]
            double[] means,
            [Description("Standard deviation of the observations in each regime, one per state, each above 0.")]
            double[] standardDeviations,
            [Description("Probability of each regime at the first step, summing to 1.")]
            double[] initial)
        {
            var model = BuildGaussian(transitions, means, standardDeviations, initial);
            var obs = Values(observations, nameof(observations));

            var forward =
                ForwardBackwardAlgorithm.Forward(model, obs, out var _, out var _);

            var states = transitions.Length;
            var last = obs.Length - 1;
            var filtered = Enumerable.Range(0, states).Select(i => forward[last, i]).ToArray();
            var total = filtered.Sum();
            filtered = filtered.Select(x => total > 0 ? x / total : double.NaN).ToArray();
            var next = new double[states];

            for (var i = 0; i < states; i++)
            {
                for (var j = 0; j < states; j++)
                {
                    next[j] += filtered[i] * transitions[i][j];
                }
            }

            var mean = Enumerable.Range(0, states).Sum(j => next[j] * means[j]);

            var second = Enumerable.Range(0, states)
                .Sum(j => next[j] * (standardDeviations[j] * standardDeviations[j] + means[j] * means[j]));

            return new HmmNextValueResult(filtered, next, mean, Math.Sqrt(Math.Max(0, second - mean * mean)));
        }

        private static double[] RandomDistribution(Random random, int length)
        {
            var weights = Enumerable.Range(0, length).Select(_ => -Math.Log(1 - random.NextDouble())).ToArray();
            var total = weights.Sum();

            return weights.Select(w => w / total).ToArray();
        }

        private static double[][] Floor(double[][] rows, double floor)
        {
            return rows.Select(r =>
            {
                var lifted = r.Select(x => Math.Max(x, floor)).ToArray();
                var total = lifted.Sum();

                return lifted.Select(x => x / total).ToArray();
            }).ToArray();
        }

        private static void RequireTraining(int states, int restarts, int maxIterations, double tolerance)
        {
            if (states < 1 || states > MaximumStates)
            {
                throw new ArgumentException($"states must be between 1 and {MaximumStates} but was {states}.",
                    nameof(states));
            }

            if (restarts < 1 || restarts > 20)
            {
                throw new ArgumentException($"restarts must be between 1 and 20 but was {restarts}.", nameof(restarts));
            }

            if (maxIterations < 1 || maxIterations > 1000)
            {
                throw new ArgumentException($"maxIterations must be between 1 and 1000 but was {maxIterations}.",
                    nameof(maxIterations));
            }

            TimeSeriesGuard.Positive(tolerance, nameof(tolerance));
        }

        [AdvancedAnalyticsTool("HiddenMarkovDiscreteTrain")]
        [Description(
            "Trains a discrete hidden Markov model from example symbol sequences with the Baum-Welch (expectation-maximisation) algorithm, learning hidden-state transition probabilities, per-state symbol probabilities and initial probabilities. Use it to discover latent regimes behind categorical event streams (for example channel or merchant-category sequences) when you have no labels. " +
            "Training starts from random probabilities generated from the seed (so a given seed always gives the same result) and is repeated 'restarts' times with different starts; the run with the highest log-likelihood is returned because Baum-Welch only finds a local optimum. Transitions, Emissions and Initial are the fitted probabilities (rows sum to 1, with a tiny floor so nothing is exactly zero); the hidden-state numbering is arbitrary. LogLikelihood, Aic and Bic measure fit (higher log-likelihood, lower Aic or Bic is better) and Iterations and Converged report the best run. " +
            "sequences is an array of symbol-code arrays (each code from 0 to symbols - 1). Needs at least 2 observations per hidden state; limited to 12 states, 20 restarts, 1000 iterations and 200,000 observations in total. The result can be passed to the HiddenMarkovDiscrete tools.")]
        public HmmDiscreteTrainResult HiddenMarkovDiscreteTrain(
            [Description(
                "Training sequences: an array of symbol-code arrays, each in time order. A single long sequence is fine.")]
            int[][] sequences,
            [Description("Number of hidden states to learn, from 1 to 12.")]
            int states,
            [Description("Number of distinct symbol codes; 0 (default) uses the largest code seen plus 1.")]
            int symbols = 0,
            [Description("Number of random restarts, from 1 to 20. Default 3.")]
            int restarts = 3,
            [Description("Most Baum-Welch iterations per restart, 1 to 1000. Default 100.")]
            int maxIterations = 100,
            [Description("Stop when the log-likelihood improves by less than this, above 0. Default 0.000001.")]
            double tolerance = 1e-6,
            [Description("Seed for the random starting probabilities so the result is reproducible. Default 1.")]
            int seed = 1)
        {
            RequireTraining(states, restarts, maxIterations, tolerance);

            if (sequences == null || sequences.Length == 0 || sequences.Any(s => s == null || s.Length == 0))
            {
                throw new ArgumentException("sequences must contain at least one non-empty array of symbol codes.",
                    nameof(sequences));
            }

            var total = sequences.Sum(s => s.Length);

            if (total > MaximumObservations)
            {
                throw new ArgumentException(
                    $"sequences hold {total} observations in total; the limit is {MaximumObservations}.",
                    nameof(sequences));
            }

            if (sequences.Any(s => s.Any(c => c < 0)))
            {
                throw new ArgumentException("Symbol codes must not be negative.", nameof(sequences));
            }

            var alphabet = symbols > 0 ? symbols : sequences.Max(s => s.Max()) + 1;

            if (alphabet < 2)
            {
                throw new ArgumentException(
                    "There must be at least 2 distinct symbol codes to learn a model (set symbols or supply varied data).",
                    nameof(symbols));
            }

            if (sequences.Any(s => s.Any(c => c >= alphabet)))
            {
                throw new ArgumentException(
                    $"A symbol code is not below symbols ({alphabet}). Codes must be from 0 to symbols - 1.",
                    nameof(symbols));
            }

            if (total < 2 * states)
            {
                throw new ArgumentException(
                    $"Only {total} observations for {states} hidden states; supply at least {2 * states} or fewer states.",
                    nameof(states));
            }

            HmmDiscreteTrainResult? best = null;

            for (var r = 0; r < restarts; r++)
            {
                var random = new Random(unchecked(seed + r * 7919));
                var transitions = Enumerable.Range(0, states).Select(_ => RandomDistribution(random, states)).ToArray();
                var emissions = Enumerable.Range(0, states).Select(_ => RandomDistribution(random, alphabet)).ToArray();
                var initial = RandomDistribution(random, states);
                var model = new HiddenMarkovModel(transitions, emissions, initial);
                var learner = new BaumWelchLearning(model) { MaxIterations = maxIterations, Tolerance = tolerance };
                learner.ParallelOptions.MaxDegreeOfParallelism = 1;
                var fitted = learner.Learn(sequences);

                var fittedTransitions = Floor(
                    fitted.LogTransitions.Select(row => row.Select(Math.Exp).ToArray()).ToArray(),
                    1e-9);

                var fittedEmissions = Floor(Enumerable.Range(0, states)
                    .Select(i =>
                        Enumerable.Range(0, alphabet).Select(k => Math.Exp(fitted.LogEmissions[i][k])).ToArray())
                    .ToArray(), 1e-9);

                var fittedInitial = Floor(new[] { fitted.LogInitial.Select(Math.Exp).ToArray() }, 1e-9)[0];
                var final = new HiddenMarkovModel(fittedTransitions, fittedEmissions, fittedInitial);
                var logLikelihood = 0.0;

                foreach (var sequence in sequences)
                {
                    ForwardBackwardAlgorithm.Forward(final, sequence, out var _, out var ll);
                    logLikelihood += ll;
                }

                var parameters = states * (states - 1) + (states - 1) + states * (alphabet - 1);

                var candidate = new HmmDiscreteTrainResult(states, alphabet, fittedTransitions, fittedEmissions,
                    fittedInitial,
                    logLikelihood, 2 * parameters - 2 * logLikelihood, parameters * Math.Log(total) - 2 * logLikelihood,
                    learner.CurrentIteration, learner.HasConverged, r, restarts, total);

                if (best == null || candidate.LogLikelihood > best.LogLikelihood)
                {
                    best = candidate;
                }
            }

            return best!;
        }

        private static double PooledStandardDeviation(double[][] sequences)
        {
            var all = sequences.SelectMany(s => s).ToArray();

            return TimeSeriesGuard.SampleStandardDeviation(all);
        }

        private static HmmGaussianTrainResult TrainGaussian(double[][] sequences, int states, int restarts,
            int maxIterations, double tolerance, int seed, double floorRatio)
        {
            var total = sequences.Sum(s => s.Length);
            var all = sequences.SelectMany(s => s).OrderBy(x => x).ToArray();
            var pooledSd = PooledStandardDeviation(sequences);

            if (!(pooledSd > 0))
            {
                throw new ArgumentException(
                    "The training values are all identical, so regimes cannot be separated. Supply a series with variation.",
                    nameof(sequences));
            }

            var floor = floorRatio * pooledSd;
            HmmGaussianTrainResult? best = null;

            for (var r = 0; r < restarts; r++)
            {
                var random = new Random(unchecked(seed + r * 7919));

                var means = Enumerable.Range(0, states).Select(i =>
                {
                    var position = (int)Math.Min(all.Length - 1, Math.Floor((i + 0.5) / states * all.Length));
                    var jitter = r == 0 ? 0 : (random.NextDouble() - 0.5) * pooledSd / states;

                    return all[position] + jitter;
                }).ToArray();

                var deviations = Enumerable.Repeat(pooledSd / Math.Max(2, states), states).ToArray();

                var transitions = Enumerable.Range(0, states).Select(i =>
                {
                    var row = new double[states];

                    for (var j = 0; j < states; j++)
                    {
                        row[j] = states == 1 ? 1 : i == j ? 0.8 : 0.2 / (states - 1) * (0.5 + random.NextDouble());
                    }

                    var sum = row.Sum();

                    return row.Select(x => x / sum).ToArray();
                }).ToArray();

                var initial = Enumerable.Repeat(1.0 / states, states).ToArray();

                var model = new HiddenMarkovModel<NormalDistribution, double>(transitions,
                    means.Select((m, i) => new NormalDistribution(m, deviations[i])).ToArray(), initial);

                var learner = new BaumWelchLearning<NormalDistribution, double>(model)
                {
                    MaxIterations = maxIterations,
                    Tolerance = tolerance,
                    FittingOptions = new NormalOptions { Regularization = floor * floor }
                };

                learner.ParallelOptions.MaxDegreeOfParallelism = 1;
                var fitted = learner.Learn(sequences);
                var fittedMeans = fitted.Emissions.Select(e => e.Mean).ToArray();
                var fittedDeviations = fitted.Emissions.Select(e => Math.Max(e.StandardDeviation, floor)).ToArray();

                var fittedTransitions = Floor(
                    fitted.LogTransitions.Select(row => row.Select(Math.Exp).ToArray()).ToArray(),
                    1e-9);

                var fittedInitial = Floor(new[] { fitted.LogInitial.Select(Math.Exp).ToArray() }, 1e-9)[0];
                var order = Enumerable.Range(0, states).OrderBy(i => fittedMeans[i]).ToArray();
                var sortedMeans = order.Select(i => fittedMeans[i]).ToArray();
                var sortedDeviations = order.Select(i => fittedDeviations[i]).ToArray();

                var sortedTransitions =
                    order.Select(i => order.Select(j => fittedTransitions[i][j]).ToArray()).ToArray();

                var sortedInitial = order.Select(i => fittedInitial[i]).ToArray();

                var final = new HiddenMarkovModel<NormalDistribution, double>(sortedTransitions,
                    sortedMeans.Select((m, i) => new NormalDistribution(m, sortedDeviations[i])).ToArray(),
                    sortedInitial);

                var logLikelihood = 0.0;

                foreach (var sequence in sequences)
                {
                    ForwardBackwardAlgorithm.Forward(final, sequence, out var _,
                        out var ll);

                    logLikelihood += ll;
                }

                var parameters = states * (states - 1) + (states - 1) + 2 * states;

                var candidate = new HmmGaussianTrainResult(states, sortedTransitions, sortedMeans, sortedDeviations,
                    sortedInitial,
                    logLikelihood, 2 * parameters - 2 * logLikelihood, parameters * Math.Log(total) - 2 * logLikelihood,
                    learner.CurrentIteration, learner.HasConverged, r, restarts, total);

                if (best == null || candidate.LogLikelihood > best.LogLikelihood)
                {
                    best = candidate;
                }
            }

            return best!;
        }

        private static double[][] RequireSequences(double[][]? sequences, int minimumTotal)
        {
            if (sequences == null || sequences.Length == 0 || sequences.Any(s => s == null || s.Length == 0))
            {
                throw new ArgumentException("sequences must contain at least one non-empty array of numbers.",
                    nameof(sequences));
            }

            foreach (var s in sequences)
            {
                TimeSeriesGuard.Finite(s, nameof(sequences), 1);
            }

            var total = sequences.Sum(s => s.Length);

            if (total > MaximumObservations)
            {
                throw new ArgumentException(
                    $"sequences hold {total} observations in total; the limit is {MaximumObservations}.",
                    nameof(sequences));
            }

            if (total < minimumTotal)
            {
                throw new ArgumentException(
                    $"sequences hold {total} observations but at least {minimumTotal} are needed.",
                    nameof(sequences));
            }

            return sequences;
        }

        [AdvancedAnalyticsTool("HiddenMarkovGaussianTrain")]
        [Description(
            "Trains a Gaussian-emission hidden Markov model (a regime-switching normal model) on numeric series with the Baum-Welch algorithm: it learns a mean and standard deviation for each hidden regime, how likely regimes are to persist or switch, and the starting regime probabilities. Use it to segment a metric such as daily volume or amount into regimes (quiet, normal, burst) without labels. " +
            "Training starts from means spread across the data quantiles with reproducible seeded variation, is repeated 'restarts' times and the run with the highest log-likelihood is kept. Regimes are returned ordered by increasing mean; Transitions rows sum to 1 (a large diagonal means persistent regimes), Means and StandardDeviations describe each regime, and StandardDeviations never fall below minimumStandardDeviation. LogLikelihood, Aic and Bic measure fit (lower Aic or Bic is better when comparing state counts). " +
            "sequences is an array of series (a single long series is fine). Needs at least 2 observations per regime and some variation; limited to 12 states, 20 restarts, 1000 iterations and 200,000 observations. The result can be passed to the HiddenMarkovGaussian tools.")]
        public HmmGaussianTrainResult HiddenMarkovGaussianTrain(
            [Description(
                "Training series: an array of numeric arrays, each in time order and finite. A single long series is fine.")]
            double[][] sequences,
            [Description("Number of hidden regimes to learn, from 1 to 12.")]
            int states,
            [Description("Number of random restarts, from 1 to 20. Default 3.")]
            int restarts = 3,
            [Description("Most Baum-Welch iterations per restart, 1 to 1000. Default 100.")]
            int maxIterations = 100,
            [Description("Stop when the log-likelihood improves by less than this, above 0. Default 0.000001.")]
            double tolerance = 1e-6,
            [Description("Seed for the reproducible starting values. Default 1.")]
            int seed = 1,
            [Description(
                "Smallest allowed standard deviation of a regime as a fraction of the overall standard deviation, above 0 and below 1; stops a regime collapsing onto a single point. Default 0.001.")]
            double minimumStandardDeviationFraction = 0.001)
        {
            RequireTraining(states, restarts, maxIterations, tolerance);
            var data = RequireSequences(sequences, 2 * states);

            TimeSeriesGuard.Unit(minimumStandardDeviationFraction, nameof(minimumStandardDeviationFraction), false,
                false);

            return TrainGaussian(data, states, restarts, maxIterations, tolerance, seed,
                minimumStandardDeviationFraction);
        }

        [AdvancedAnalyticsTool("HiddenMarkovGaussianSelectStateCount")]
        [Description(
            "Chooses how many hidden regimes a numeric series needs by fitting Gaussian hidden Markov models with 1 up to maxStates regimes and comparing them with the Bayesian (BIC) and Akaike (AIC) information criteria, which reward fit and penalise complexity (lower is better). Use it before HiddenMarkovGaussianTrain when you do not know how many regimes exist. " +
            "Candidates lists, for each tried number of regimes, the log-likelihood, AIC, BIC and parameter count. BestStatesByBic and BestStatesByAic are the winners (BIC favours simpler models); BestModel is the fitted model with the best BIC. A single regime is the plain normal distribution. " +
            "Training is seeded and reproducible. Each candidate needs at least 2 observations per regime; limited to 12 states, 20 restarts and 200,000 observations.")]
        public HmmStateSelectionResult HiddenMarkovGaussianSelectStateCount(
            [Description(
                "Training series: an array of numeric arrays, each in time order and finite. A single long series is fine.")]
            double[][] sequences,
            [Description("Largest number of regimes to try, from 1 to 12. Default 4.")]
            int maxStates = 4,
            [Description("Number of random restarts per candidate, from 1 to 20. Default 3.")]
            int restarts = 3,
            [Description("Most Baum-Welch iterations per fit, 1 to 1000. Default 100.")]
            int maxIterations = 100,
            [Description("Seed for the reproducible starting values. Default 1.")]
            int seed = 1)
        {
            RequireTraining(maxStates, restarts, maxIterations, 1e-6);
            var data = RequireSequences(sequences, 2 * maxStates);
            var total = data.Sum(s => s.Length);
            var all = data.SelectMany(s => s).ToArray();
            var mean = TimeSeriesGuard.Mean(all);
            var sd = Math.Sqrt(all.Sum(x => (x - mean) * (x - mean)) / all.Length);

            if (!(sd > 0))
            {
                throw new ArgumentException(
                    "The training values are all identical, so regimes cannot be separated. Supply a series with variation.",
                    nameof(sequences));
            }

            var single = all.Sum(x => Math.Log(new NormalDistribution(mean, sd).ProbabilityDensityFunction(x)));

            var candidates = new List<HmmStateCountCandidate>
                { new(1, single, 2 * 2 - 2 * single, 2 * Math.Log(total) - 2 * single, 2) };

            var models = new Dictionary<int, HmmGaussianTrainResult>
            {
                [1] = new(1, new[] { new[] { 1.0 } }, new[] { mean }, new[] { sd }, new[] { 1.0 },
                    single,
                    4 - 2 * single, 2 * Math.Log(total) - 2 * single, 0, true, 0, 1, total)
            };

            for (var k = 2; k <= maxStates; k++)
            {
                var fit = TrainGaussian(data, k, restarts, maxIterations, 1e-6, seed, 0.001);
                var parameters = k * (k - 1) + (k - 1) + 2 * k;
                candidates.Add(new HmmStateCountCandidate(k, fit.LogLikelihood, fit.Aic, fit.Bic, parameters));
                models[k] = fit;
            }

            var bestBic = candidates.OrderBy(c => c.Bic).ThenBy(c => c.States).First();
            var bestAic = candidates.OrderBy(c => c.Aic).ThenBy(c => c.States).First();

            return new HmmStateSelectionResult(candidates.ToArray(), bestBic.States, bestAic.States,
                models[bestBic.States]);
        }
    }
}