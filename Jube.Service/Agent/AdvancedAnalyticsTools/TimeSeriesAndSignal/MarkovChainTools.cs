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
using Accord.Math;
using Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal
{
    [AdvancedAnalyticsToolGroup("TimeSeriesAndSignal")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class MarkovChainTools
    {
        private static string[] RequireStates(string[]? sequence, string name, int minimum)
        {
            if (sequence == null || sequence.Length < minimum)
            {
                throw new ArgumentException(
                    $"{name} needs at least {minimum} state labels in time order but has {(sequence == null ? 0 : sequence.Length)}.",
                    name);
            }

            if (sequence.Any(s => s == null))
            {
                throw new ArgumentException(
                    $"{name} contains a missing (null) state label. Replace it with a label such as 'Unknown'.", name);
            }

            return sequence;
        }

        private static double[][] Counts(string[] sequence, string[] states, double smoothing, out int[][] rawCounts)
        {
            var index = states.Select((s, i) => (s, i)).ToDictionary(t => t.s, t => t.i, StringComparer.Ordinal);
            var k = states.Length;
            rawCounts = new int[k][];

            for (var i = 0; i < k; i++)
            {
                rawCounts[i] = new int[k];
            }

            for (var t = 0; t + 1 < sequence.Length; t++)
            {
                rawCounts[index[sequence[t]]][index[sequence[t + 1]]]++;
            }

            var result = new double[k][];

            for (var i = 0; i < k; i++)
            {
                var total = rawCounts[i].Sum() + smoothing * k;
                result[i] = new double[k];

                for (var j = 0; j < k; j++)
                {
                    result[i][j] = total > 0 ? (rawCounts[i][j] + smoothing) / total : 0;
                }
            }

            return result;
        }

        [AdvancedAnalyticsTool("MarkovChainTransitionMatrix")]
        [Description(
            "Estimates a first-order Markov chain from a sequence of states in time order (for example account status changes, transaction channels or risk bands) and summarises its behaviour. Use it to see how likely each state is to be followed by each other state and where the process settles in the long run. " +
            "States lists the distinct labels in ordinal alphabetical order and indexes every array. TransitionCounts[i][j] is the number of observed moves from state i to j; TransitionProbabilities are the counts divided by the row total after adding smoothing to every cell (0 gives the plain maximum-likelihood estimate). A state that only appears at the end has no observed exits; with no smoothing it is treated as absorbing and listed in StatesWithoutOutgoingTransitions. " +
            "StationaryDistribution is the long-run share of time in each state (StationaryIsUnique is false when the chain has several separate recurrent classes), ExpectedReturnSteps is 1 divided by that share, MostLikelyNextState the most probable successor of each state, EntropyRateBits the average uncertainty about the next state in bits, and AbsorbingStates those that can never be left. Needs at least 2 states in the sequence.")]
        public MarkovChainEstimateResult MarkovChainTransitionMatrix(
            [Description(
                "The observed state labels in time order, at least 2, for example ['Active','Active','Dormant','Active'].")]
            string[] sequence,
            [Description(
                "Pseudo-count added to every transition cell before normalising, 0 or more. Default 0 (no smoothing); use 0.5 or 1 for sparse data.")]
            double smoothing = 0)
        {
            var seq = RequireStates(sequence, nameof(sequence), 2);

            if (double.IsNaN(smoothing) || smoothing < 0)
            {
                throw new ArgumentException($"smoothing must be 0 or more but was {smoothing}.", nameof(smoothing));
            }

            var states = seq.Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToArray();

            if (states.Length > MarkovMath.MaximumStates)
            {
                throw new ArgumentException(
                    $"sequence has {states.Length} distinct states; the limit is {MarkovMath.MaximumStates}. Group rare states first.",
                    nameof(sequence));
            }

            var probabilities = Counts(seq, states, smoothing, out var counts);
            var noExit = new List<string>();

            for (var i = 0; i < states.Length; i++)
            {
                if (counts[i].Sum() == 0 && smoothing == 0)
                {
                    noExit.Add(states[i]);
                    probabilities[i] = new double[states.Length];
                    probabilities[i][i] = 1;
                }
            }

            var stationary =
                MarkovMath.Stationary(probabilities, out var unique, out _, out _, out var absorbing, out _);

            var stateCounts = states.Select(s => seq.Count(x => x == s)).ToArray();
            var entropy = 0.0;

            for (var i = 0; i < states.Length; i++)
            {
                for (var j = 0; j < states.Length; j++)
                {
                    if (probabilities[i][j] > 0)
                    {
                        entropy -= stationary[i] * probabilities[i][j] * MarkovMath.Log2(probabilities[i][j]);
                    }
                }
            }

            return new MarkovChainEstimateResult(
                states,
                stateCounts,
                counts,
                probabilities,
                stationary,
                unique,
                stationary.Select(x => x > 0 ? 1 / x : double.PositiveInfinity).ToArray(),
                probabilities.Select(row => states[Array.IndexOf(row, row.Max())]).ToArray(),
                entropy,
                absorbing.Select(i => states[i]).ToArray(),
                noExit.ToArray(),
                seq.Length - 1);
        }

        [AdvancedAnalyticsTool("MarkovChainStationaryDistribution")]
        [Description(
            "Long-run (stationary) distribution of a Markov chain whose transition matrix you supply: the share of time the chain spends in each state after many steps. Use it when the transition probabilities are known or were estimated elsewhere. " +
            "The matrix is square with row i giving the probability of moving from state i to each state, each row summing to 1. StationaryDistribution sums to 1 and satisfies pi = pi x P; IsUnique is true when the chain has exactly one recurrent class (then it does not depend on the start); RecurrentClasses counts closed classes; TransientStates are those the chain eventually leaves for good; AbsorbingStates cannot be left; ExpectedReturnSteps is 1 / pi. " +
            "If there are several recurrent classes the answer depends on the start and the result is the average from a uniform start, with an explanation in Note. Limited to 500 states.")]
        public MarkovStationaryResult MarkovChainStationaryDistribution(
            [Description(
                "Square transition matrix as an array of rows; row i lists P(next = j | current = i) and sums to 1.")]
            double[][] transitionMatrix)
        {
            var p = MarkovMath.Matrix(transitionMatrix, nameof(transitionMatrix));

            var stationary = MarkovMath.Stationary(p, out var unique, out var classes, out var transient,
                out var absorbing,
                out var note);

            return new MarkovStationaryResult(stationary, unique, classes, transient, absorbing,
                stationary.Select(x => x > 0 ? 1 / x : double.PositiveInfinity).ToArray(), note);
        }

        [AdvancedAnalyticsTool("MarkovChainMultiStepProbabilities")]
        [Description(
            "Probability of being in each state after a given number of steps, for a Markov chain whose transition matrix you supply. Use it to project where a customer, case or account is likely to be in 3 or 12 periods given where it is now. " +
            "Give either startState (the zero-based index of the current state) or startDistribution (probabilities over states); Distribution is the state distribution after 'steps' moves and StepMatrix is the full matrix P raised to the power steps, whose row i is the distribution from starting in state i. Steps 0 returns the start itself. " +
            "The matrix rows must each sum to 1; limited to 500 states and 1,000,000 steps.")]
        public MarkovMultiStepResult MarkovChainMultiStepProbabilities(
            [Description(
                "Square transition matrix as an array of rows; row i lists P(next = j | current = i) and sums to 1.")]
            double[][] transitionMatrix,
            [Description("Number of steps ahead, 0 to 1000000.")]
            int steps,
            [Description(
                "Zero-based index of the current state. Use -1 (default) when giving startDistribution instead.")]
            int startState = -1,
            [Description("Probabilities over states at the start, summing to 1. Leave empty when giving startState.")]
            double[]? startDistribution = null)
        {
            var p = MarkovMath.Matrix(transitionMatrix, nameof(transitionMatrix));
            var n = p.Length;

            if (steps < 0 || steps > 1_000_000)
            {
                throw new ArgumentException($"steps must be between 0 and 1000000 but was {steps}.", nameof(steps));
            }

            double[] start;

            if (startDistribution != null && startDistribution.Length > 0)
            {
                if (startState >= 0)
                {
                    throw new ArgumentException("Give startState or startDistribution, not both.", nameof(startState));
                }

                if (startDistribution.Length != n || startDistribution.Any(x => double.IsNaN(x) || x < 0) ||
                    Math.Abs(startDistribution.Sum() - 1) > 1e-6)
                {
                    throw new ArgumentException(
                        $"startDistribution must have {n} non-negative probabilities summing to 1.",
                        nameof(startDistribution));
                }

                start = startDistribution;
            }
            else
            {
                if (startState < 0 || startState >= n)
                {
                    throw new ArgumentException(
                        $"startState must be between 0 and {n - 1} (or give startDistribution) but was {startState}.",
                        nameof(startState));
                }

                start = new double[n];
                start[startState] = 1;
            }

            var result = Matrix.Identity(n).ToJagged();
            var basePower = p.Select(r => (double[])r.Clone()).ToArray();
            var remaining = steps;

            while (remaining > 0)
            {
                if ((remaining & 1) == 1)
                {
                    result = result.Dot(basePower);
                }

                remaining >>= 1;

                if (remaining > 0)
                {
                    basePower = basePower.Dot(basePower);
                }
            }

            var distribution = new double[n];

            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    distribution[j] += start[i] * result[i][j];
                }
            }

            return new MarkovMultiStepResult(result, distribution, steps);
        }

        [AdvancedAnalyticsTool("MarkovChainSequenceSurprise")]
        [Description(
            "Scores how unusual a new sequence of states is under a Markov chain learned from a reference (training) sequence, to flag behaviour that departs from normal patterns of movement between states. " +
            "The transition probabilities are estimated from trainingSequence with additive smoothing over the union of all states seen in either sequence, so unseen moves get a small non-zero probability. SurprisalBits[t] is minus log base 2 of the probability of the move from testSequence[t] to testSequence[t+1] (0 bits is certain, 1 bit is a coin flip, high values are rare moves); AverageBitsPerTransition and Perplexity (2 to that power) summarise the whole sequence; TotalLogLikelihood is the natural-log likelihood; MostSurprisingTransition is the index t of the largest surprisal and UnseenTransitions counts moves never seen in training. " +
            "Needs at least 2 states in each sequence; use smoothing above 0 or unseen moves would be impossible.")]
        public MarkovSurpriseResult MarkovChainSequenceSurprise(
            [Description("The reference sequence of states in time order that defines normal behaviour, at least 2.")]
            string[] trainingSequence,
            [Description("The sequence of states to score, in time order, at least 2.")]
            string[] testSequence,
            [Description("Pseudo-count added to every transition cell, above 0. Default 1.")]
            double smoothing = 1)
        {
            var train = RequireStates(trainingSequence, nameof(trainingSequence), 2);
            var test = RequireStates(testSequence, nameof(testSequence), 2);
            TimeSeriesGuard.Positive(smoothing, nameof(smoothing));

            var states = train.Concat(test).Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal)
                .ToArray();

            var probabilities = Counts(train, states, smoothing, out var counts);
            var index = states.Select((s, i) => (s, i)).ToDictionary(t => t.s, t => t.i, StringComparer.Ordinal);
            var surprisal = new double[test.Length - 1];
            var unseen = 0;
            var log = 0.0;

            for (var t = 0; t + 1 < test.Length; t++)
            {
                var i = index[test[t]];
                var j = index[test[t + 1]];
                surprisal[t] = -MarkovMath.Log2(probabilities[i][j]);
                log += Math.Log(probabilities[i][j]);

                if (counts[i][j] == 0)
                {
                    unseen++;
                }
            }

            var average = surprisal.Average();

            return new MarkovSurpriseResult(surprisal, average, log, Math.Pow(2, average),
                Array.IndexOf(surprisal, surprisal.Max()), unseen, surprisal.Length);
        }

        [AdvancedAnalyticsTool("MarkovChainMeanFirstPassageTimes")]
        [Description(
            "Expected number of steps for a Markov chain to first reach each target state from each starting state, for a transition matrix you supply. Use it for questions like 'on average how many periods until an active account becomes dormant?' " +
            "MeanFirstPassageSteps[i][j] is the expected steps to first arrive at j starting from i; the diagonal entry [j][j] is the mean recurrence time (steps to return to j after leaving it), which equals 1 divided by the stationary probability for an irreducible chain. An entry is infinity when the target may never be reached (some path leads to states from which it cannot be reached). " +
            "Rows of the matrix must sum to 1; limited to 500 states.")]
        public MarkovFirstPassageResult MarkovChainMeanFirstPassageTimes(
            [Description(
                "Square transition matrix as an array of rows; row i lists P(next = j | current = i) and sums to 1.")]
            double[][] transitionMatrix)
        {
            var p = MarkovMath.Matrix(transitionMatrix, nameof(transitionMatrix));
            var n = p.Length;
            var result = new double[n][];

            for (var i = 0; i < n; i++)
            {
                result[i] = Enumerable.Repeat(double.PositiveInfinity, n).ToArray();
            }

            for (var target = 0; target < n; target++)
            {
                var reach = new bool[n, n];

                for (var i = 0; i < n; i++)
                {
                    var stack = new Stack<int>();
                    stack.Push(i);
                    reach[i, i] = true;

                    while (stack.Count > 0)
                    {
                        var current = stack.Pop();

                        if (current == target && current != i)
                        {
                            continue;
                        }

                        for (var j = 0; j < n; j++)
                        {
                            if (p[current][j] > 0 && !reach[i, j])
                            {
                                reach[i, j] = true;
                                stack.Push(j);
                            }
                        }
                    }
                }

                var canReach = Enumerable.Range(0, n).Where(i => i != target)
                    .ToDictionary(i => i, i => Reaches(p, i, target));

                var finite = Enumerable.Range(0, n).Where(i => i != target &&
                                                               Enumerable.Range(0, n)
                                                                   .Where(k => k != target && reach[i, k])
                                                                   .All(k => canReach[k])
                                                               && canReach[i]).ToArray();

                if (finite.Length > 0)
                {
                    var a = new double[finite.Length, finite.Length];
                    var b = new double[finite.Length];

                    for (var r = 0; r < finite.Length; r++)
                    {
                        b[r] = 1;

                        for (var c = 0; c < finite.Length; c++)
                        {
                            a[r, c] = (r == c ? 1 : 0) - p[finite[r]][finite[c]];
                        }
                    }

                    var m = a.Solve(b);

                    for (var r = 0; r < finite.Length; r++)
                    {
                        result[finite[r]][target] = m[r];
                    }
                }

                var returns = true;
                var recurrence = 1.0;

                for (var k = 0; k < n; k++)
                {
                    if (k == target || p[target][k] <= 0)
                    {
                        continue;
                    }

                    if (double.IsInfinity(result[k][target]))
                    {
                        returns = false;
                        break;
                    }

                    recurrence += p[target][k] * result[k][target];
                }

                result[target][target] = returns ? recurrence : double.PositiveInfinity;
            }

            return new MarkovFirstPassageResult(result,
                "Entry [i][j] is the expected number of steps to first reach state j from state i; the diagonal is the mean recurrence time.");
        }

        private static bool Reaches(double[][] p, int from, int target)
        {
            var n = p.Length;
            var seen = new bool[n];
            var stack = new Stack<int>();
            stack.Push(from);
            seen[from] = true;

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (current == target)
                {
                    return true;
                }

                for (var j = 0; j < n; j++)
                {
                    if (p[current][j] > 0 && !seen[j])
                    {
                        seen[j] = true;
                        stack.Push(j);
                    }
                }
            }

            return false;
        }

        [AdvancedAnalyticsTool("MarkovChainAbsorptionAnalysis")]
        [Description(
            "Absorbing Markov chain analysis for a process that ends in one of several final states (for example a case that is closed as confirmed, dismissed or escalated). Supply the full transition matrix in which each final state has probability 1 of staying put. " +
            "TransientStates and AbsorbingStates list state indices. ExpectedStepsToAbsorption[i] is the expected number of steps from the i-th transient state until absorption; AbsorptionProbabilities[i][k] is the probability that starting from the i-th transient state the chain ends in the k-th absorbing state (each row sums to 1); ExpectedVisits[i][j] is the expected number of visits to the j-th transient state starting from the i-th (the fundamental matrix). " +
            "Every recurrent class must be a single absorbing state and every transient state must be able to reach one; otherwise the call fails with an explanation. Limited to 500 states.")]
        public MarkovAbsorptionResult MarkovChainAbsorptionAnalysis(
            [Description(
                "Square transition matrix as an array of rows; final states have a 1 on their diagonal. Each row sums to 1.")]
            double[][] transitionMatrix)
        {
            var p = MarkovMath.Matrix(transitionMatrix, nameof(transitionMatrix));
            var reach = MarkovMath.Reachability(p);
            var (closed, transient, absorbing) = MarkovMath.Classify(p, reach);

            if (absorbing.Length == 0)
            {
                throw new ArgumentException(
                    "The chain has no absorbing state (no state with probability 1 of staying put). Use MarkovChainStationaryDistribution for a chain that never ends.",
                    nameof(transitionMatrix));
            }

            if (closed != absorbing.Length)
            {
                throw new ArgumentException(
                    "The chain has recurrent classes that are not single absorbing states, so absorption analysis does not apply. Make each final state absorbing.",
                    nameof(transitionMatrix));
            }

            if (transient.Length == 0)
            {
                return new MarkovAbsorptionResult(transient, absorbing, Array.Empty<double>(), Array.Empty<double[]>(),
                    Array.Empty<double[]>());
            }

            var t = transient.Length;
            var identityMinusQ = new double[t, t];
            var r = new double[t, absorbing.Length];

            for (var i = 0; i < t; i++)
            {
                for (var j = 0; j < t; j++)
                {
                    identityMinusQ[i, j] = (i == j ? 1 : 0) - p[transient[i]][transient[j]];
                }

                for (var k = 0; k < absorbing.Length; k++)
                {
                    r[i, k] = p[transient[i]][absorbing[k]];
                }
            }

            var fundamental = identityMinusQ.Solve(Matrix.Identity(t));
            var absorptionProbabilities = fundamental.Dot(r);

            var steps = Enumerable.Range(0, t).Select(i => Enumerable.Range(0, t).Sum(j => fundamental[i, j]))
                .ToArray();

            return new MarkovAbsorptionResult(transient, absorbing, steps, absorptionProbabilities.ToJagged(),
                fundamental.ToJagged());
        }

        [AdvancedAnalyticsTool("MarkovDependenceTest")]
        [Description(
            "Chi-square test of whether the next state in a sequence depends on the current state, that is, whether a Markov model has anything to add over treating states as independent draws. Use it before building a transition model or Markov-based anomaly detector on a sequence of statuses or categories. " +
            "The counts of consecutive pairs form a contingency table (current state by next state) that is tested for independence. A small PValue (below alpha, Dependent true) means the sequence has real state-to-state structure. CramersV (0 to 1) measures the strength; DegreesOfFreedom is (rows - 1) x (columns - 1) over states that occur as a current and next state respectively. " +
            "Note warns when many expected counts are below 5 (the chi-square approximation is then unreliable). Needs at least 2 states with variation and at least 10 transitions.")]
        public MarkovDependenceResult MarkovDependenceTest(
            [Description("The observed state labels in time order, at least 11.")]
            string[] sequence,
            [Description("Significance level for the Dependent flag. Default 0.05.")]
            double alpha = 0.05)
        {
            var seq = RequireStates(sequence, nameof(sequence), 11);
            TimeSeriesGuard.Unit(alpha, nameof(alpha), false, false);
            var states = seq.Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToArray();
            Counts(seq, states, 0, out var counts);
            var k = states.Length;
            var rowTotals = Enumerable.Range(0, k).Select(i => counts[i].Sum()).ToArray();
            var colTotals = Enumerable.Range(0, k).Select(j => Enumerable.Range(0, k).Sum(i => counts[i][j])).ToArray();
            var rows = Enumerable.Range(0, k).Where(i => rowTotals[i] > 0).ToArray();
            var cols = Enumerable.Range(0, k).Where(j => colTotals[j] > 0).ToArray();
            var total = (double)(seq.Length - 1);

            if (rows.Length < 2 || cols.Length < 2)
            {
                throw new ArgumentException(
                    "The sequence has fewer than 2 distinct current or next states, so dependence cannot be tested.",
                    nameof(sequence));
            }

            var chi = 0.0;
            var smallExpected = 0;

            foreach (var i in rows)
            {
                foreach (var j in cols)
                {
                    var expected = rowTotals[i] * colTotals[j] / total;
                    chi += (counts[i][j] - expected) * (counts[i][j] - expected) / expected;

                    if (expected < 5)
                    {
                        smallExpected++;
                    }
                }
            }

            var df = (rows.Length - 1) * (cols.Length - 1);
            var p = TimeSeriesGuard.ChiSquareUpperTail(chi, df);
            var v = Math.Sqrt(chi / (total * Math.Min(rows.Length - 1, cols.Length - 1)));

            var note = smallExpected > 0.2 * rows.Length * cols.Length
                ? $"{smallExpected} of {rows.Length * cols.Length} cells have an expected count below 5, so the chi-square p-value is approximate."
                : "Expected counts are adequate for the chi-square approximation.";

            return new MarkovDependenceResult(chi, df, p, v, seq.Length - 1, p < alpha, note);
        }
    }
}