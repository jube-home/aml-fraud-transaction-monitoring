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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Neuro;
using FluentAssertions;
using Jube.Engine.Exhaustive.Algorithms;
using Jube.Engine.Exhaustive.Algorithms.Models;
using Jube.Engine.Helpers;
using Newtonsoft.Json;
using Xunit;

namespace Jube.Test.Exhaustive
{
    public class TrainLoopTests
    {
        private static Task<TrainPrepared> PreparedAsync()
        {
            return TrainSupport.PrepareMockAsync(120, 7, 10);
        }

        private static async Task<(bool Completed, TrainRecordingRecorder Recorder)> RunAsync(
            ExhaustiveTrainingSettings settings, TrainRecordingRecorder? recorder = null, int seed = 3,
            CancellationToken token = default)
        {
            recorder ??= new TrainRecordingRecorder();
            var supervised = TrainSupport.NewSupervised(settings, recorder, await PreparedAsync(), seed);
            var completed = await supervised.TrainAsync(token);

            return (completed, recorder);
        }

        [Fact]
        public async Task TrainAsync_RunsTrialsLimitMinusOneTrials_AndReturnsTrueAsync()
        {
            var (completed, recorder) = await RunAsync(TrainSupport.Settings(3));

            completed.Should().BeTrue();
            recorder.TrialInstances.Should().HaveCount(2);
            recorder.ModelsUpdates.Should().Equal(1, 2);
        }

        [Fact]
        public async Task TrainAsync_TrialsLimitOfOne_RunsNoTrialsAtAllAsync()
        {
            var (completed, recorder) = await RunAsync(TrainSupport.Settings(1));

            completed.Should().BeTrue();
            recorder.Events.Should().BeEmpty();
        }

        [Fact]
        public async Task TrainAsync_RecordsEveryTrialInStageOrderAsync()
        {
            var (_, recorder) = await RunAsync(TrainSupport.Settings(3));

            var perTrial = new List<List<string>>();

            foreach (var e in recorder.Events)
            {
                if (e == "stop-check")
                {
                    perTrial.Add([]);
                    continue;
                }

                perTrial[^1].Add(e);
            }

            perTrial.Should().HaveCount(2);

            foreach (var events in perTrial)
            {
                events[0].Should().Be("trial");
                events[1].Should().Be("delete-variables");

                var firstActivation = events.IndexOf("activation");
                var firstTopology = events.IndexOf("topology");
                var finalisation = events.IndexOf("finalisation");
                var models = events.IndexOf("models");
                events.IndexOf("variable").Should().BeGreaterThan(1).And.BeLessThan(firstActivation);
                firstActivation.Should().BeLessThan(firstTopology);
                firstTopology.Should().BeLessThan(finalisation);
                finalisation.Should().BeLessThan(models);
                events.Count(e => e == "finalisation").Should().Be(1);

                events.Last().Should().BeOneOf("models-since-best", "prescription", "prescription-histogram",
                    "promoted-sensitivity", "models");
            }
        }

        [Fact]
        public async Task TrainAsync_ActivationSearch_RecordsTenCandidatesPerVariableEliminationIterationAsync()
        {
            var (_, recorder) = await RunAsync(TrainSupport.Settings(2));

            recorder.ActivationFunctionTrials.Should().NotBeEmpty();
            (recorder.ActivationFunctionTrials.Count % 10).Should().Be(0);

            foreach (var group in recorder.ActivationFunctionTrials.Chunk(10))
            {
                group.Select(g => (int)g.ActivationFunctionId!).Should().Equal(Enumerable.Range(1, 10));
                group.Select(g => g.ExhaustiveSearchInstanceTrialInstanceId).Distinct().Should().HaveCount(1);
            }
        }

        [Fact]
        public async Task TrainAsync_TopologyTrials_GrowOneNeuronAtATimeAndEndWithExactlyOneFinalisationRowAsync()
        {
            var (_, recorder) = await RunAsync(TrainSupport.Settings(3));

            foreach (var trial in recorder.TrialInstances)
            {
                var rows = recorder.TopologyTrials.Where(t => t.ExhaustiveSearchInstanceTrialInstanceId == trial.Id)
                    .ToList();
                rows.Should().NotBeEmpty();
                rows[^1].Finalisation.Should().Be(1);
                var exploration = rows.Where(r => r.Finalisation == 0).ToList();
                exploration.Select(r => r.Neurons!.Value).Should().Equal(Enumerable.Range(1, exploration.Count));
                exploration.Should().OnlyContain(r => r.Layer == 1 && r.TopologyTrials == 1);
                exploration.Select(r => r.TrialsSinceImprovement).Should().OnlyContain(s => s <= 2);
                rows.Should().OnlyContain(r => r.Score >= 0);
                rows.Skip(1).Zip(rows, (b, a) => b.Score >= a.Score).Should().OnlyContain(x => x);
            }
        }

        [Fact]
        public async Task TrainAsync_TopologySinceImprovementLimit_BoundsTheTopologySearchAsync()
        {
            var settings = TrainSupport.Settings(2);
            var (_, recorder) = await RunAsync(settings);

            foreach (var trial in recorder.TrialInstances)
            {
                var rows = recorder.TopologyTrials.Where(t => t.ExhaustiveSearchInstanceTrialInstanceId == trial.Id)
                    .ToList();

                rows[^2].TrialsSinceImprovement.Should().Be(settings.TopologySinceImprovementLimit - 1);
                rows[^1].TrialsSinceImprovement.Should().Be(settings.TopologySinceImprovementLimit);
            }
        }

        [Fact]
        public async Task TrainAsync_Promotion_IsStrictlyIncreasingAndStartsFromZeroAsync()
        {
            var (_, recorder) = await RunAsync(TrainSupport.Settings(6));

            recorder.Promoted.Should().NotBeEmpty("the first scoring trial is always promoted");
            var scores = recorder.Promoted.Select(p => p.Score!.Value).ToList();
            scores.Should().OnlyContain(s => s > 0);
            scores.Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
            recorder.BestScoreUpdates.Select(b => b.Score).Should().Equal(scores);
            recorder.Promoted.Select(p => p.ExhaustiveSearchInstanceTrialInstanceId).Should().OnlyHaveUniqueItems();
            recorder.Promoted.Should().OnlyContain(p => p.Active == 1);
            recorder.BestScoreUpdates.Should().OnlyContain(b => b.Complexity >= 2);
        }

        [Fact]
        public async Task TrainAsync_NonPromotedTrials_IncrementModelsSinceBest_AndPromotionDoesNotReportItAsync()
        {
            var (_, recorder) = await RunAsync(TrainSupport.Settings(6));

            var trials = recorder.TrialInstances.Count;
            recorder.ModelsUpdates.Should().Equal(Enumerable.Range(1, trials));
            (recorder.Promoted.Count + recorder.ModelsSinceBestUpdates.Count).Should().Be(trials);
            recorder.ModelsSinceBestUpdates.Should().OnlyContain(m => m >= 1);

            recorder.ModelsSinceBestUpdates.Zip(recorder.ModelsSinceBestUpdates.Skip(1),
                (a, b) => b == a + 1 || b == 1).Should().OnlyContain(x => x);
        }

        [Fact]
        public async Task TrainAsync_PerPromotion_WritesTwentyRocRowsWithTheFixedThresholdGridAsync()
        {
            var (_, recorder) = await RunAsync(TrainSupport.Settings(3));

            recorder.Roc.Count.Should().Be(20 * recorder.Promoted.Count);

            foreach (var group in recorder.Roc.Chunk(20))
            {
                group.Select(g => g.Threshold!.Value).Should()
                    .BeEquivalentTo(Enumerable.Range(1, 20).Select(i => i * 0.05), o => o.WithStrictOrdering()
                        .Using<double>(c => c.Subject.Should().BeApproximately(c.Expectation, 1e-9))
                        .WhenTypeIs<double>());

                group.Select(g => g.ExhaustiveSearchInstanceTrialInstanceId).Distinct().Should().HaveCount(1);

                var total = group[0].TruePositive + group[0].FalsePositive + group[0].TrueNegative +
                            group[0].FalseNegative;

                group.Should().OnlyContain(g => g.TruePositive + g.FalsePositive + g.TrueNegative +
                    g.FalseNegative == total);

                group.Select(g => g.TruePositive).Should().BeInDescendingOrder();
                group.Select(g => g.FalsePositive).Should().BeInDescendingOrder();
            }
        }

        [Fact]
        public async Task TrainAsync_PerPromotion_WritesPredictedVersusActualForEveryTestRowAsync()
        {
            var prepared = await PreparedAsync();
            var (_, recorder) = await RunAsync(TrainSupport.Settings(3));

            var expectedTestRows = (int)(prepared.Outputs.Length * 0.2);
            recorder.PredictedActual.Count.Should().Be(recorder.Promoted.Count * expectedTestRows);
            recorder.PredictedActual.Should().OnlyContain(p => p.Actual == 0 || p.Actual == 1);
        }

        [Fact]
        public async Task TrainAsync_PerPromotion_WritesOnePromotedSensitivityPerVariableWithDenseRanksAsync()
        {
            var (_, recorder) = await RunAsync(TrainSupport.Settings(3));

            var variablesPerTrial = recorder.TrialVariables.GroupBy(v => v.ExhaustiveSearchInstanceTrialInstanceId ?? 0)
                .ToDictionary(g => g.Key, g => g.Count());

            recorder.PromotedSensitivities.Should().NotBeEmpty();
            recorder.PromotedSensitivities.Count.Should().BeGreaterThan(0);

            foreach (var group in recorder.PromotedSensitivities.GroupBy(s => s.SensitivityRank == 1)
                         .SelectMany(g => g))
            {
                group.SensitivityRank.Should().BeGreaterThan(0);
            }

            var ranks = recorder.PromotedSensitivities.Select(s => s.SensitivityRank).ToList();
            ranks.Should().OnlyContain(r => r >= 1);
            variablesPerTrial.Should().NotBeEmpty();
        }

        [Fact]
        public async Task TrainAsync_PerPromotion_PersistsPrescriptionHistogramsLinkedToTheInsertedPrescriptionAsync()
        {
            var (_, recorder) = await RunAsync(TrainSupport.Settings(3));

            recorder.Prescriptions.Should().NotBeEmpty();
            recorder.PrescriptionHistograms.Should().NotBeEmpty();
            var ids = recorder.Prescriptions.Select(p => (int?)p.Id).ToHashSet();

            recorder.PrescriptionHistograms.Should()
                .OnlyContain(h => ids.Contains(h.ExhaustiveSearchInstancePromotedTrialInstanceVariableId));

            foreach (var group in recorder.PrescriptionHistograms.GroupBy(h =>
                         h.ExhaustiveSearchInstancePromotedTrialInstanceVariableId))
            {
                group.Select(g => g.BinIndex!.Value).Should().Equal(Enumerable.Range(0, group.Count()));
                group.Should().OnlyContain(g => g.BinRangeStart <= g.BinRangeEnd);
                var prescription = recorder.Prescriptions.Single(p => p.Id == group.Key);
                group.Min(g => g.BinRangeStart).Should().BeApproximately(prescription.Minimum!.Value, 1e-9);
                group.Max(g => g.BinRangeEnd).Should().BeApproximately(prescription.Maximum!.Value, 1e-9);
            }
        }

        [Fact]
        public async Task TrainAsync_PerPromotion_WritesTheModelJsonThatReproducesTheRecordedScoresAsync()
        {
            var (_, recorder) = await RunAsync(TrainSupport.Settings(3));

            var promoted = recorder.Promoted[0];

            var network = JsonConvert.DeserializeObject<ActivationNetwork>(promoted.Json!,
                new JsonSerializationHelper().DeserializeTopologyNetworkJsonSerializerSettings)!;
            network.InputsCount.Should().BeGreaterThan(0);

            var trialId = promoted.ExhaustiveSearchInstanceTrialInstanceId;

            var scores = recorder.PredictedActual.Take(recorder.PredictedActual.Count / recorder.Promoted.Count)
                .Select(p => p.Predicted).ToArray();
            scores.Should().NotBeEmpty();
            scores.Should().OnlyContain(s => !double.IsNaN(s!.Value));

            recorder.TrialVariables.Count(v => v.ExhaustiveSearchInstanceTrialInstanceId == trialId)
                .Should().BeGreaterThanOrEqualTo(network.InputsCount);

            (promoted.TruePositive + promoted.TrueNegative + promoted.FalsePositive + promoted.FalseNegative)
                .Should().Be(scores.Length);
        }

        [Fact]
        public async Task TrainAsync_PromotedConfusion_IsRecordedAtTheHalfThresholdAsync()
        {
            var (_, recorder) = await RunAsync(TrainSupport.Settings(3));

            var promoted = recorder.Promoted[0];
            var half = recorder.Roc.First(r => Math.Abs(r.Threshold!.Value - 0.5) < 1e-6);
            half.ExhaustiveSearchInstanceTrialInstanceId.Should().Be(promoted.ExhaustiveSearchInstanceTrialInstanceId);
            half.TruePositive.Should().Be(promoted.TruePositive);
            half.TrueNegative.Should().Be(promoted.TrueNegative);
            half.FalsePositive.Should().Be(promoted.FalsePositive);
            half.FalseNegative.Should().Be(promoted.FalseNegative);
            half.Score.Should().BeApproximately(promoted.Score!.Value, 1e-9);
        }

        [Fact]
        public async Task TrainAsync_RemovedVariables_AreOnlyVariablesThatWereSelectedInThatTrialAsync()
        {
            var (_, recorder) = await RunAsync(TrainSupport.Settings());

            foreach (var (variableId, trialId) in recorder.RemovedVariables)
            {
                recorder.TrialVariables.Should().Contain(v =>
                    v.ExhaustiveSearchInstanceTrialInstanceId == trialId &&
                    v.ExhaustiveSearchInstanceVariableId == variableId);
            }

            recorder.RemovedVariables.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public async Task TrainAsync_VariableSelection_ClearsThePreviousSelectionBeforeEveryDrawAsync()
        {
            var (_, recorder) = await RunAsync(TrainSupport.Settings(3));

            var deletes = recorder.Events.Count(e => e == "delete-variables");
            deletes.Should().BeGreaterThanOrEqualTo(recorder.TrialInstances.Count);

            recorder.DeletedTrialVariableSets.Should().OnlyContain(id =>
                recorder.TrialInstances.Any(t => t.Id == id));

            recorder.TrialVariables.Should().OnlyContain(v => v.VariableSequence >= 0);
        }

        [Fact]
        public async Task TrainAsync_SelectedVariableCount_RespectsMinimumAndMaximumAsync()
        {
            var settings = TrainSupport.Settings(3, 7, 9);
            var (_, recorder) = await RunAsync(settings);

            foreach (var perDraw in SplitDraws(recorder))
            {
                perDraw.Should().BeInRange(7, 9);
            }
        }

        [Fact]
        public async Task TrainAsync_MinimumAboveAvailableVariables_KeepsEveryVariableAndCompletesAsync()
        {
            var (completed, recorder) = await RunAsync(TrainSupport.Settings(2, 40, 60));

            completed.Should().BeTrue();
            recorder.TrialInstances.Should().HaveCount(1);
            SplitDraws(recorder).Should().Equal(10);
            recorder.RemovedVariables.Should().BeEmpty();
        }

        [Fact]
        public async Task TrainAsync_DrawEqualToTheMinimum_RemovesNothingAndCompletesAsync()
        {
            var (completed, recorder) = await RunAsync(TrainSupport.Settings(2, 7, 7));

            completed.Should().BeTrue();
            SplitDraws(recorder).Should().Equal(7);
            recorder.RemovedVariables.Should().BeEmpty();
        }

        [Fact]
        public async Task TrainAsync_Elimination_NeverLeavesFewerThanTheMinimumVariablesAsync()
        {
            var (completed, recorder) = await RunAsync(TrainSupport.Settings(4, 5, 10));

            completed.Should().BeTrue();

            foreach (var trial in recorder.TrialInstances)
            {
                var drawn = recorder.TrialVariables.Count(v => v.ExhaustiveSearchInstanceTrialInstanceId == trial.Id);
                var removed = recorder.RemovedVariables.Count(r => r.Item2 == trial.Id);
                (drawn - removed).Should().BeGreaterThanOrEqualTo(5);
            }
        }

        [Fact]
        public async Task TrainAsync_StopRequestedBeforeTheFirstTrial_ReturnsFalseAndRecordsNothingAsync()
        {
            var (completed, recorder) = await RunAsync(TrainSupport.Settings(),
                new TrainRecordingRecorder { StopFromStart = true });

            completed.Should().BeFalse();
            recorder.Events.Should().Equal("stop-check");
            recorder.TrialInstances.Should().BeEmpty();
        }

        [Fact]
        public async Task TrainAsync_StopRequestedBetweenTrials_FinishesTheRunningTrialThenStopsAsync()
        {
            var (completed, recorder) = await RunAsync(TrainSupport.Settings(6),
                new TrainRecordingRecorder { StopAfterTrialInstances = 1 });

            completed.Should().BeFalse();
            recorder.TrialInstances.Should().HaveCount(1);
            recorder.Events.Should().Contain("finalisation").And.Contain("models");
            recorder.Events.Last().Should().Be("stop-check");
        }

        [Fact]
        public async Task TrainAsync_CancellationToken_PropagatesAsOperationCanceledNotSwallowedPerTrialAsync()
        {
            using var cts = new CancellationTokenSource();
            var recorder = new TrainRecordingRecorder();
            await cts.CancelAsync();
            var token = cts.Token;

            var act = async () => await RunAsync(TrainSupport.Settings(), recorder, token: token);

            await act.Should().ThrowAsync<OperationCanceledException>();
            recorder.TrialInstances.Should().BeEmpty();
        }

        [Fact]
        public async Task TrainAsync_CancellationMidTrial_PropagatesFromTheRecorderAsync()
        {
            var recorder = new TrainRecordingRecorder
            {
                FailOnTrialInstance = n => n == 2 ? new OperationCanceledException() : null
            };

            var act = async () => await RunAsync(TrainSupport.Settings(6), recorder);

            await act.Should().ThrowAsync<OperationCanceledException>();
            recorder.ModelsUpdates.Should().Equal(1);
        }

        [Fact]
        public async Task TrainAsync_AnErrorInsideOneTrial_IsSwallowedAndTheNextTrialStillRunsAsync()
        {
            var recorder = new TrainRecordingRecorder
            {
                FailOnTrialInstance = n => n == 1 ? new InvalidOperationException("boom") : null
            };

            var (completed, _) = await RunAsync(TrainSupport.Settings(), recorder);

            completed.Should().BeTrue();
            recorder.TrialInstances.Should().HaveCount(3);
            recorder.ModelsUpdates.Should().Equal(1, 2);
        }

        [Fact]
        public async Task
            TrainAsync_AnErrorDuringPromotionStorage_SkipsTheRestOfThatPromotionButKeepsTheBestScoreAsync()
        {
            var recorder = new TrainRecordingRecorder
            {
                FailOnRoc = _ => new InvalidOperationException("roc")
            };

            var (completed, _) = await RunAsync(TrainSupport.Settings(), recorder);

            completed.Should().BeTrue();
            recorder.Promoted.Should().NotBeEmpty();
            recorder.Roc.Should().BeEmpty();
            recorder.PredictedActual.Should().BeEmpty();
            recorder.Prescriptions.Should().BeEmpty();
            recorder.BestScoreUpdates.Should().HaveCount(recorder.Promoted.Count);
        }

        [Fact]
        public async Task TrainAsync_SeparableData_ReachesAPerfectScoreAndStopsBeforeTheTrialLimitAsync()
        {
            var random = new Random(5);
            var outputs = Enumerable.Range(0, 120).Select(i => (double)(i % 2)).ToArray();

            var data = outputs.Select(o => new[]
            {
                (o * 2 - 1) * 3 + random.NextDouble() * 0.1,
                (o * 2 - 1) * 2 + random.NextDouble() * 0.1,
                random.NextDouble() - 0.5,
                o * 2 - 1 + random.NextDouble() * 0.1,
                random.NextDouble() - 0.5,
                random.NextDouble() - 0.5
            }).ToArray();

            var variables = Enumerable.Range(0, 6).ToDictionary(i => i,
                i => new Variable { Name = $"v{i}", ProcessingTypeId = 1, NormalisationType = 0 });

            var prepared = new TrainPrepared(variables, data, outputs);
            var recorder = new TrainRecordingRecorder();

            var completed = await TrainSupport.NewSupervised(TrainSupport.Settings(50, 3, 6), recorder, prepared, 9)
                .TrainAsync();

            completed.Should().BeTrue();
            recorder.Promoted.Last().Score.Should().BeApproximately(1, 0.0001);
            recorder.TrialInstances.Should().HaveCount(recorder.Promoted.Count);
            recorder.TrialInstances.Count.Should().BeLessThan(49);
        }

        private static List<int> SplitDraws(TrainRecordingRecorder recorder)
        {
            var draws = new List<int>();
            var count = 0;

            foreach (var e in recorder.Events)
            {
                if (e == "delete-variables")
                {
                    if (count > 0)
                    {
                        draws.Add(count);
                    }

                    count = 0;
                }
                else if (e == "variable")
                {
                    count++;
                }
            }

            if (count > 0)
            {
                draws.Add(count);
            }

            return draws;
        }
    }
}