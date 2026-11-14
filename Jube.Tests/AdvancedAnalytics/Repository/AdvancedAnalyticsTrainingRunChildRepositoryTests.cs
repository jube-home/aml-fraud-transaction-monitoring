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

using Jube.Data.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Data;
using Npgsql;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Repository
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsTrainingRunChildRepositoryTests(DatabaseFixture fx)
        : AdvancedAnalyticsRepositoryTestBase(fx)
    {
        private const string User = "ZzTestUser";

        private async Task<(AskJoobyAnalyticsInstance Instance, AdvancedAnalyticsTrainingRun Run)> NewPairAsync(
            int tenant)
        {
            var instance = await NewInstanceAsync(tenant);
            var run = await NewRunAsync(tenant, instance.Id);

            return (instance, run);
        }

        [Fact]
        public async Task StepsAreOrderedByStepOrderTakeInstanceFromTheRunAndRoundTripEnumsAsync()
        {
            var (instance, run) = await NewPairAsync(TenantA);
            var repository = new AdvancedAnalyticsTrainingRunStepRepository(Db, TenantA, User);

            foreach (var step in Enum.GetValues<AdvancedAnalyticsStep>().Reverse())
            {
                await repository.InsertAsync(new AdvancedAnalyticsTrainingRunStep
                {
                    AdvancedAnalyticsTrainingRunId = run.Id,
                    AskJoobyAnalyticsInstanceId = int.MaxValue,
                    TenantRegistryId = TenantB,
                    StepOrder = (int)step,
                    Step = step,
                    StatusId = AdvancedAnalyticsStatus.Pending
                });
            }

            var listed = (await repository.ListByRunIdAsync(run.Id)).ToList();

            listed.Select(s => s.Step).Should().Equal(Enum.GetValues<AdvancedAnalyticsStep>());

            listed.Should().OnlyContain(o =>
                o.TenantRegistryId == TenantA && o.AskJoobyAnalyticsInstanceId == instance.Id);
        }

        [Fact]
        public async Task StepUpdateStampsTimingAndDurationOnTerminalStatusAsync()
        {
            var (_, run) = await NewPairAsync(TenantA);
            var repository = new AdvancedAnalyticsTrainingRunStepRepository(Db, TenantA, User);

            var step = await repository.InsertAsync(new AdvancedAnalyticsTrainingRunStep
            {
                AdvancedAnalyticsTrainingRunId = run.Id,
                StepOrder = 1,
                Step = AdvancedAnalyticsStep.Fit,
                StatusId = AdvancedAnalyticsStatus.Pending
            });

            await repository.UpdateAsync(step.Id, AdvancedAnalyticsStatus.Running);
            await repository.UpdateAsync(step.Id, AdvancedAnalyticsStatus.Failed, "nope");

            var read = (await repository.ListByRunIdAsync(run.Id)).Single();
            read.StatusId.Should().Be(AdvancedAnalyticsStatus.Failed);
            read.StartedDate.Should().NotBeNull();
            read.CompletedDate.Should().NotBeNull();
            read.DurationMilliseconds.Should().BeGreaterThanOrEqualTo(0);
            read.Error.Should().Be("nope");
        }

        [Fact]
        public async Task TrialsRecordHyperparametersScoreAndExactlyOneCanBeSelectedAsync()
        {
            var (_, run) = await NewPairAsync(TenantA);
            var repository = new AdvancedAnalyticsTrainingRunTrialRepository(Db, TenantA, User);
            var trials = new List<AdvancedAnalyticsTrainingRunTrial>();

            for (var i = 1; i <= 3; i++)
            {
                trials.Add(await repository.InsertAsync(new AdvancedAnalyticsTrainingRunTrial
                {
                    AdvancedAnalyticsTrainingRunId = run.Id,
                    TrialNumber = i,
                    StatusId = AdvancedAnalyticsStatus.Running,
                    Hyperparameters = $"{{\"c\":{i}.5}}",
                    Selected = 1
                }));
            }

            await repository.UpdateAsync(trials[0].Id, AdvancedAnalyticsStatus.Succeeded, 0.71);
            await repository.UpdateAsync(trials[1].Id, AdvancedAnalyticsStatus.Succeeded, 0.93);
            await repository.UpdateAsync(trials[2].Id, AdvancedAnalyticsStatus.Failed, null, "diverged");
            await repository.MarkSelectedAsync(run.Id, trials[0].Id);
            await repository.MarkSelectedAsync(run.Id, trials[1].Id);

            var listed = (await repository.ListByRunIdAsync(run.Id)).ToList();

            listed.Select(s => s.TrialNumber).Should().Equal(1, 2, 3);
            listed.Select(s => s.Selected).Should().Equal(0, 1, 0);
            listed[1].Score.Should().Be(0.93);
            listed[1].Hyperparameters.Should().Contain("\"c\"");
            listed[2].StatusId.Should().Be(AdvancedAnalyticsStatus.Failed);
            listed[2].Score.Should().BeNull();
            listed[2].Error.Should().Be("diverged");
            listed[0].DurationMilliseconds.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task MarkSelectedRejectsATrialOfAnotherRunAsync()
        {
            var (instance, run) = await NewPairAsync(TenantA);
            var otherRun = await NewRunAsync(TenantA, instance.Id);
            var repository = new AdvancedAnalyticsTrainingRunTrialRepository(Db, TenantA, User);

            var trial = await repository.InsertAsync(new AdvancedAnalyticsTrainingRunTrial
            {
                AdvancedAnalyticsTrainingRunId = otherRun.Id,
                TrialNumber = 1,
                StatusId = AdvancedAnalyticsStatus.Succeeded
            });

            await ((Func<Task>)(() => repository.MarkSelectedAsync(run.Id, trial.Id)))
                .Should().ThrowAsync<KeyNotFoundException>();

            (await repository.GetByIdAsync(trial.Id))!.Selected.Should().Be(0);
        }

        [Fact]
        public async Task IterationsInsertSingleAndInBulkAndListInIterationOrderAsync()
        {
            var (_, run) = await NewPairAsync(TenantA);
            var trials = new AdvancedAnalyticsTrainingRunTrialRepository(Db, TenantA, User);

            var trial = await trials.InsertAsync(new AdvancedAnalyticsTrainingRunTrial
            {
                AdvancedAnalyticsTrainingRunId = run.Id,
                TrialNumber = 1,
                StatusId = AdvancedAnalyticsStatus.Running
            });

            var repository = new AdvancedAnalyticsTrainingRunIterationRepository(Db, TenantA, User);

            await repository.InsertAsync(new AdvancedAnalyticsTrainingRunIteration
            {
                AdvancedAnalyticsTrainingRunId = run.Id,
                IterationNumber = 0,
                TrainLoss = 1.5
            });

            await repository.InsertRangeAsync([
                .. Enumerable.Range(1, 50).Reverse().Select(i =>
                    new AdvancedAnalyticsTrainingRunIteration
                    {
                        AdvancedAnalyticsTrainingRunId = run.Id,
                        AdvancedAnalyticsTrainingRunTrialId = trial.Id,
                        IterationNumber = i,
                        TrainLoss = 1.0 / i,
                        ValidationLoss = double.NaN,
                        Score = -0.0
                    })
            ]);

            var all = (await repository.ListByRunIdAsync(run.Id)).ToList();
            var forTrial = (await repository.ListByRunIdAsync(run.Id, trial.Id)).ToList();

            all.Should().HaveCount(51);
            all.Select(s => s.IterationNumber).Should().BeInAscendingOrder();
            forTrial.Should().HaveCount(50);
            forTrial[9].TrainLoss.Should().Be(0.1);
            double.IsNaN(forTrial[0].ValidationLoss!.Value).Should().BeTrue();
            all.Should().OnlyContain(o => o.TenantRegistryId == TenantA);
        }

        [Fact]
        public async Task BulkInsertIsAtomicWhenOneRowIsRejectedAsync()
        {
            var (instance, run) = await NewPairAsync(TenantA);
            var foreignInstance = await NewInstanceAsync(TenantB);
            var foreignRun = await NewRunAsync(TenantB, foreignInstance.Id);
            var repository = new AdvancedAnalyticsTrainingRunMetricRepository(Db, TenantA, User);

            var rows = new List<AdvancedAnalyticsTrainingRunMetric>
            {
                new()
                {
                    AdvancedAnalyticsTrainingRunId = run.Id, Name = "auc", Value = 0.9,
                    Split = AdvancedAnalyticsSplit.Test
                },
                new()
                {
                    AdvancedAnalyticsTrainingRunId = foreignRun.Id, Name = "auc", Value = 0.9,
                    Split = AdvancedAnalyticsSplit.Test
                }
            };

            await ((Func<Task>)(() => repository.InsertRangeAsync(rows)))
                .Should().ThrowAsync<KeyNotFoundException>();

            (await repository.ListByRunIdAsync(run.Id)).Should().BeEmpty();

            Db.AdvancedAnalyticsTrainingRunMetric.Count(w => w.AskJoobyAnalyticsInstanceId == instance.Id)
                .Should().Be(0);
        }

        [Fact]
        public async Task BulkInsertRollsBackWhenTheDatabaseRejectsALaterRowAsync()
        {
            var (_, run) = await NewPairAsync(TenantA);
            var repository = new AdvancedAnalyticsTrainingRunMetricRepository(Db, TenantA, User);

            var rows = new List<AdvancedAnalyticsTrainingRunMetric>
            {
                new()
                {
                    AdvancedAnalyticsTrainingRunId = run.Id, Name = "auc", Value = 0.9,
                    Split = AdvancedAnalyticsSplit.Test
                },
                new()
                {
                    AdvancedAnalyticsTrainingRunId = run.Id, Name = null!, Value = 0.9,
                    Split = AdvancedAnalyticsSplit.Test
                }
            };

            await ((Func<Task>)(() => repository.InsertRangeAsync(rows)))
                .Should().ThrowAsync<PostgresException>();

            (await repository.ListByRunIdAsync(run.Id)).Should().BeEmpty();
        }

        [Fact]
        public async Task MetricsRoundTripEverySplitAndFilterBySplitAsync()
        {
            var (_, run) = await NewPairAsync(TenantA);
            var repository = new AdvancedAnalyticsTrainingRunMetricRepository(Db, TenantA, User);

            await repository.InsertRangeAsync([
                .. Enum.GetValues<AdvancedAnalyticsSplit>().Select(split =>
                    new AdvancedAnalyticsTrainingRunMetric
                    {
                        AdvancedAnalyticsTrainingRunId = run.Id,
                        Name = "auc",
                        Value = 0.5 + (int)split / 10.0,
                        Split = split
                    })
            ]);

            (await repository.ListByRunIdAsync(run.Id)).Select(s => s.Split)
                .Should().Equal(Enum.GetValues<AdvancedAnalyticsSplit>());

            var test = (await repository.ListByRunIdAsync(run.Id, AdvancedAnalyticsSplit.Test)).Single();
            test.Value.Should().Be(0.8);

            Db.Execute<int>("select \"Split\" from \"AdvancedAnalyticsTrainingRunMetric\" where \"Id\" = " + test.Id)
                .Should().Be((int)AdvancedAnalyticsSplit.Test);
        }

        [Fact]
        public async Task MetricOrIterationTrialFromAnotherRunIsNotFoundAsync()
        {
            var (instance, run) = await NewPairAsync(TenantA);
            var otherRun = await NewRunAsync(TenantA, instance.Id);

            var trial = await new AdvancedAnalyticsTrainingRunTrialRepository(Db, TenantA, User).InsertAsync(
                new AdvancedAnalyticsTrainingRunTrial
                {
                    AdvancedAnalyticsTrainingRunId = otherRun.Id,
                    TrialNumber = 1,
                    StatusId = AdvancedAnalyticsStatus.Running
                });

            await ((Func<Task>)(() => new AdvancedAnalyticsTrainingRunMetricRepository(Db, TenantA, User)
                    .InsertAsync(new AdvancedAnalyticsTrainingRunMetric
                    {
                        AdvancedAnalyticsTrainingRunId = run.Id,
                        AdvancedAnalyticsTrainingRunTrialId = trial.Id,
                        Name = "auc",
                        Split = AdvancedAnalyticsSplit.Train
                    })))
                .Should().ThrowAsync<KeyNotFoundException>();

            await ((Func<Task>)(() => new AdvancedAnalyticsTrainingRunIterationRepository(Db, TenantA, User)
                    .InsertAsync(new AdvancedAnalyticsTrainingRunIteration
                    {
                        AdvancedAnalyticsTrainingRunId = run.Id,
                        AdvancedAnalyticsTrainingRunTrialId = trial.Id,
                        IterationNumber = 1
                    })))
                .Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task VariablesKeepSequenceOrderAndStatisticsIncludingNonFiniteValuesAsync()
        {
            var (_, run) = await NewPairAsync(TenantA);
            var repository = new AdvancedAnalyticsTrainingRunVariableRepository(Db, TenantA, User);

            await repository.InsertRangeAsync(
            [
                new AdvancedAnalyticsTrainingRunVariable
                {
                    AdvancedAnalyticsTrainingRunId = run.Id, VariableSequence = 2, Name = "b", Mean = 2.5,
                    StandardDeviation = 0.5, Minimum = 1, Maximum = 4, ImputationValue = 2.5, MissingCount = 3,
                    RowCount = 100, Importance = 0.2
                },
                new AdvancedAnalyticsTrainingRunVariable
                {
                    AdvancedAnalyticsTrainingRunId = run.Id, VariableSequence = 1, Name = "a",
                    Mean = double.NegativeInfinity
                }
            ]);

            var listed = (await repository.ListByRunIdAsync(run.Id)).ToList();

            listed.Select(s => s.Name).Should().Equal("a", "b");
            listed[0].Mean.Should().Be(double.NegativeInfinity);
            listed[1].MissingCount.Should().Be(3);
            listed[1].RowCount.Should().Be(100);
            listed[1].Importance.Should().Be(0.2);
        }

        [Fact]
        public async Task CurvePointsRoundTripEveryCurveAndFilterAndOrderAsync()
        {
            var (_, run) = await NewPairAsync(TenantA);
            var repository = new AdvancedAnalyticsTrainingRunCurvePointRepository(Db, TenantA, User);
            var points = new List<AdvancedAnalyticsTrainingRunCurvePoint>();

            foreach (var curve in Enum.GetValues<AdvancedAnalyticsCurve>())
            {
                for (var i = 4; i >= 0; i--)
                {
                    points.Add(new AdvancedAnalyticsTrainingRunCurvePoint
                    {
                        AdvancedAnalyticsTrainingRunId = run.Id,
                        Curve = curve,
                        Split = AdvancedAnalyticsSplit.Validation,
                        PointIndex = i,
                        X = i / 4.0,
                        Y = i / 8.0,
                        Threshold = 1 - i / 4.0
                    });
                }
            }

            await repository.InsertRangeAsync(points);

            var all = (await repository.ListByRunIdAsync(run.Id)).ToList();
            var roc = (await repository.ListByRunIdAsync(run.Id, AdvancedAnalyticsCurve.Roc)).ToList();
            var none = await repository.ListByRunIdAsync(run.Id, split: AdvancedAnalyticsSplit.Test);

            all.Should().HaveCount(5 * Enum.GetValues<AdvancedAnalyticsCurve>().Length);
            roc.Select(s => s.PointIndex).Should().Equal(0, 1, 2, 3, 4);
            roc[4].X.Should().Be(1.0);
            roc[4].Threshold.Should().Be(0.0);
            none.Should().BeEmpty();

            Db.Execute<int>("select \"Curve\" from \"AdvancedAnalyticsTrainingRunCurvePoint\" where \"Id\" = " +
                            roc[0].Id).Should().Be((int)AdvancedAnalyticsCurve.Roc);
        }

        [Fact]
        public async Task ForeignTenantCannotWriteOrReadAnyChildTableAsync()
        {
            var (_, run) = await NewPairAsync(TenantA);
            var stepA = new AdvancedAnalyticsTrainingRunStepRepository(Db, TenantA, User);

            var step = await stepA.InsertAsync(new AdvancedAnalyticsTrainingRunStep
            {
                AdvancedAnalyticsTrainingRunId = run.Id, StepOrder = 1, Step = AdvancedAnalyticsStep.Load,
                StatusId = AdvancedAnalyticsStatus.Pending
            });

            var trial = await new AdvancedAnalyticsTrainingRunTrialRepository(Db, TenantA, User).InsertAsync(
                new AdvancedAnalyticsTrainingRunTrial
                {
                    AdvancedAnalyticsTrainingRunId = run.Id, TrialNumber = 1,
                    StatusId = AdvancedAnalyticsStatus.Running
                });

            var b = TenantB;

            (await new AdvancedAnalyticsTrainingRunStepRepository(Db, b, User).ListByRunIdAsync(run.Id))
                .Should().BeEmpty();
            (await new AdvancedAnalyticsTrainingRunTrialRepository(Db, b, User).ListByRunIdAsync(run.Id))
                .Should().BeEmpty();
            (await new AdvancedAnalyticsTrainingRunTrialRepository(Db, b, User).GetByIdAsync(trial.Id))
                .Should().BeNull();
            (await new AdvancedAnalyticsTrainingRunIterationRepository(Db, b, User).ListByRunIdAsync(run.Id))
                .Should().BeEmpty();
            (await new AdvancedAnalyticsTrainingRunVariableRepository(Db, b, User).ListByRunIdAsync(run.Id))
                .Should().BeEmpty();
            (await new AdvancedAnalyticsTrainingRunMetricRepository(Db, b, User).ListByRunIdAsync(run.Id))
                .Should().BeEmpty();
            (await new AdvancedAnalyticsTrainingRunCurvePointRepository(Db, b, User).ListByRunIdAsync(run.Id))
                .Should().BeEmpty();

            await ((Func<Task>)(() => new AdvancedAnalyticsTrainingRunStepRepository(Db, b, User).InsertAsync(
                    new AdvancedAnalyticsTrainingRunStep
                    {
                        AdvancedAnalyticsTrainingRunId = run.Id, StepOrder = 2, Step = AdvancedAnalyticsStep.Fit,
                        StatusId = AdvancedAnalyticsStatus.Pending
                    })))
                .Should().ThrowAsync<KeyNotFoundException>();

            await ((Func<Task>)(() => new AdvancedAnalyticsTrainingRunStepRepository(Db, b, User).UpdateAsync(step.Id,
                    AdvancedAnalyticsStatus.Failed)))
                .Should().ThrowAsync<KeyNotFoundException>();

            await ((Func<Task>)(() => new AdvancedAnalyticsTrainingRunTrialRepository(Db, b, User)
                    .UpdateAsync(trial.Id, AdvancedAnalyticsStatus.Failed, null)))
                .Should().ThrowAsync<KeyNotFoundException>();

            await ((Func<Task>)(() => new AdvancedAnalyticsTrainingRunTrialRepository(Db, b, User)
                    .MarkSelectedAsync(run.Id, trial.Id)))
                .Should().ThrowAsync<KeyNotFoundException>();

            await ((Func<Task>)(() => new AdvancedAnalyticsTrainingRunVariableRepository(Db, b, User).InsertAsync(
                    new AdvancedAnalyticsTrainingRunVariable
                        { AdvancedAnalyticsTrainingRunId = run.Id, Name = "x" })))
                .Should().ThrowAsync<KeyNotFoundException>();

            await ((Func<Task>)(() => new AdvancedAnalyticsTrainingRunCurvePointRepository(Db, b, User)
                    .InsertRangeAsync([
                        new AdvancedAnalyticsTrainingRunCurvePoint
                            { AdvancedAnalyticsTrainingRunId = run.Id, Curve = AdvancedAnalyticsCurve.Roc }
                    ])))
                .Should().ThrowAsync<KeyNotFoundException>();

            (await stepA.ListByRunIdAsync(run.Id)).Single().StatusId.Should().Be(AdvancedAnalyticsStatus.Pending);
        }

        [Fact]
        public async Task ChildrenOfASoftDeletedRunCannotBeAddedAsync()
        {
            var (_, run) = await NewPairAsync(TenantA);
            await new AdvancedAnalyticsTrainingRunRepository(Db, TenantA, User).DeleteAsync(run.Id);

            await ((Func<Task>)(() => new AdvancedAnalyticsTrainingRunMetricRepository(Db, TenantA, User).InsertAsync(
                    new AdvancedAnalyticsTrainingRunMetric
                    {
                        AdvancedAnalyticsTrainingRunId = run.Id, Name = "auc", Split = AdvancedAnalyticsSplit.Test
                    })))
                .Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task EveryChildTableHasARealForeignKeyToTheInstanceAndTheRunAsync()
        {
            var (instance, run) = await NewPairAsync(TenantA);

            var step = new AdvancedAnalyticsTrainingRunStep
            {
                TenantRegistryId = TenantA,
                AskJoobyAnalyticsInstanceId = int.MaxValue,
                AdvancedAnalyticsTrainingRunId = run.Id,
                StepOrder = 1,
                Step = AdvancedAnalyticsStep.Load,
                StatusId = AdvancedAnalyticsStatus.Pending
            };

            var wrongInstance = () => Db.InsertAsync(step);
            var violation = await wrongInstance.Should().ThrowAsync<PostgresException>();
            violation.Which.SqlState.Should().Be("23503");

            step.AskJoobyAnalyticsInstanceId = instance.Id;
            step.AdvancedAnalyticsTrainingRunId = int.MaxValue;
            var wrongRun = () => Db.InsertAsync(step);
            (await wrongRun.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("23503");

            var foreignKeys = Db.Query<string>("""
                                               select distinct tc.table_name || ':' || kcu.column_name
                                               from information_schema.table_constraints tc
                                               join information_schema.key_column_usage kcu
                                                 on kcu.constraint_name = tc.constraint_name
                                               where tc.constraint_type = 'FOREIGN KEY'
                                                 and kcu.column_name in
                                                     ('AskJoobyAnalyticsInstanceId','AdvancedAnalyticsTrainingRunId')
                                               """).ToList();

            foreach (var table in new[]
                     {
                         "Step", "Trial", "Iteration", "Variable", "Metric", "CurvePoint"
                     })
            {
                foreignKeys.Should().Contain($"AdvancedAnalyticsTrainingRun{table}:AskJoobyAnalyticsInstanceId");
                foreignKeys.Should().Contain($"AdvancedAnalyticsTrainingRun{table}:AdvancedAnalyticsTrainingRunId");
            }

            foreignKeys.Should().Contain("AdvancedAnalyticsTrainingRun:AskJoobyAnalyticsInstanceId");
            foreignKeys.Should().Contain("AskJoobyAnalyticsTrainedModel:AskJoobyAnalyticsInstanceId");
        }

        [Fact]
        public async Task CancelledTokenStopsChildOperationsAsync()
        {
            var (_, run) = await NewPairAsync(TenantA);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var token = cts.Token;

            await ((Func<Task>)(() => new AdvancedAnalyticsTrainingRunStepRepository(Db, TenantA, User)
                    .ListByRunIdAsync(run.Id, token)))
                .Should().ThrowAsync<OperationCanceledException>();
            await ((Func<Task>)(() => new AdvancedAnalyticsTrainingRunTrialRepository(Db, TenantA, User)
                    .ListByRunIdAsync(run.Id, token)))
                .Should().ThrowAsync<OperationCanceledException>();

            await ((Func<Task>)(() => new AdvancedAnalyticsTrainingRunIterationRepository(Db, TenantA, User)
                    .InsertRangeAsync([
                        new AdvancedAnalyticsTrainingRunIteration
                            { AdvancedAnalyticsTrainingRunId = run.Id, IterationNumber = 1 }
                    ], token)))
                .Should().ThrowAsync<OperationCanceledException>();

            await ((Func<Task>)(() => new AdvancedAnalyticsTrainingRunMetricRepository(Db, TenantA, User)
                    .ListByRunIdAsync(run.Id, null, token)))
                .Should().ThrowAsync<OperationCanceledException>();

            await ((Func<Task>)(() => new AdvancedAnalyticsTrainingRunVariableRepository(Db, TenantA, User)
                    .ListByRunIdAsync(run.Id, token)))
                .Should().ThrowAsync<OperationCanceledException>();

            await ((Func<Task>)(() => new AdvancedAnalyticsTrainingRunCurvePointRepository(Db, TenantA, User)
                    .ListByRunIdAsync(run.Id, null, null, token)))
                .Should().ThrowAsync<OperationCanceledException>();
        }
    }
}