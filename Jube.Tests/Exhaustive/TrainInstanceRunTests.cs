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
using FluentAssertions;
using Jube.Analytics.Anomaly;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.Exhaustive;
using Jube.Engine.Helpers;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using log4net;
using Xunit;
using Jube.Data.Query.GetNextExhaustiveSearchInstanceQuery;

namespace Jube.Test.Exhaustive
{
    [Collection("Database")]
    public class TrainInstanceRunTests(DatabaseFixture fx)
    {
        private static readonly SemaphoreSlim modelGate = new(1, 1);
        private static int? sharedModelId;
        private static int sharedTenantId;

        private static string ConnectionString =>
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString")
            ??
            "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;";

        private Training NewTraining(int seed = 1)
        {
            var environment = TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["ConnectionString"] = ConnectionString,
                ["ExhaustiveTrialsLimit"] = "3",
                ["ExhaustiveTopologySinceImprovementLimit"] = "2",
                ["ExhaustiveTopologyExplorationEpochs"] = "2",
                ["ExhaustiveTopologyFinalisationEpochs"] = "2",
                ["ExhaustiveActivationFunctionExplorationEpochs"] = "3",
                ["ExhaustiveSimulationsCount"] = "50"
            });

            return new Training(LogManager.GetLogger(typeof(TrainInstanceRunTests)), environment,
                new JsonSerializationHelper())
            {
                seeded = new Random(seed),
                pollDelay = TimeSpan.FromMilliseconds(20)
            };
        }

        private async Task<int> SharedModelAsync()
        {
            await modelGate.WaitAsync();

            try
            {
                await using var db = fx.GetDbContext();

                if (sharedModelId.HasValue
                    && await db.EntityAnalysisModel.AnyAsync(w => w.Id == sharedModelId.Value))
                {
                    return sharedModelId.Value;
                }

                var model = await new EntityAnalysisModelRepository(db, fx.Seed.UserWithPermission).InsertAsync(
                    new EntityAnalysisModel
                    {
                        Name = $"{DatabaseFixture.Prefix}Tr{Guid.NewGuid():N}"[..40],
                        Guid = Guid.NewGuid(),
                        Active = 1,
                        Locked = 0,
                        Deleted = 0
                    });

                sharedModelId = model.Id;
                sharedTenantId = model.TenantRegistryId!.Value;

                return model.Id;
            }
            finally
            {
                modelGate.Release();
            }
        }

        private async Task<GetNextExhaustiveSearchInstanceQueryDto> NewInstanceAsync(double anomalyProbability,
            byte statusId = 0, bool anomaly = true, byte dataSource = 1, int? algorithmId = null,
            string? parametersJson = null)
        {
            var modelId = await SharedModelAsync();
            await using var db = fx.GetDbContext();

            var instance = await new ExhaustiveSearchInstanceRepository(db, fx.Seed.UserWithPermission).InsertAsync(
                new ExhaustiveSearchInstance
                {
                    Name = $"{DatabaseFixture.Prefix}Tr{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    EntityAnalysisModelId = modelId,
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    StatusId = statusId,
                    Anomaly = (byte)(anomaly ? 1 : 0),
                    AnomalyProbability = anomalyProbability,
                    AnomalyAlgorithmId = algorithmId,
                    AnomalyParametersJson = parametersJson,
                    Filter = 0,
                    DataSourceId = dataSource
                });

            return new GetNextExhaustiveSearchInstanceQueryDto
            {
                Id = instance.Id,
                EntityAnalysisModelId = modelId,
                TenantRegistryId = sharedTenantId,
                Anomaly = anomaly,
                AnomalyProbability = anomalyProbability,
                DataSourceId = dataSource,
                AnomalyAlgorithmId = algorithmId,
                AnomalyParametersJson = parametersJson
            };
        }

        private async Task<ExhaustiveSearchInstance> ReloadAsync(int id)
        {
            await using var db = fx.GetDbContext();

            return await db.ExhaustiveSearchInstance.FirstAsync(w => w.Id == id);
        }

        [Fact]
        public async Task RunOneInstance_StopRequested_MovesStraightToStatusNineteenAndReturnsFalseAsync()
        {
            var instance = await NewInstanceAsync(0.5, 18, dataSource: 0);
            await using var db = fx.GetDbContext();

            var completed = await NewTraining().RunOneInstanceAsync(instance, db, CancellationToken.None);

            completed.Should().BeFalse();
            (await ReloadAsync(instance.Id)).StatusId.Should().Be(19);

            (await db.GetTable<ExhaustiveSearchInstanceVariable>()
                .CountAsync(w => w.ExhaustiveSearchInstanceId == instance.Id)).Should().Be(0);
        }

        [Fact]
        public async Task RunOneInstance_AlreadyStopped_StaysAtStatusNineteenAsync()
        {
            var instance = await NewInstanceAsync(0.5, 19, dataSource: 0);
            await using var db = fx.GetDbContext();

            var completed = await NewTraining().RunOneInstanceAsync(instance, db, CancellationToken.None);

            completed.Should().BeFalse();
            (await ReloadAsync(instance.Id)).StatusId.Should().Be(19);
        }

        [Fact]
        public async Task RunOneInstance_CancelledToken_ThrowsBeforeTouchingTheStatusAsync()
        {
            var instance = await NewInstanceAsync(0.5, dataSource: 0);
            await using var db = fx.GetDbContext();
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var token = cts.Token;

            // ReSharper disable once AccessToDisposedClosure
            var act = async () => await NewTraining().RunOneInstanceAsync(instance, db, token);

            await act.Should().ThrowAsync<OperationCanceledException>();
            (await ReloadAsync(instance.Id)).StatusId.Should().Be(0);
        }

        [Fact]
        public async Task RunOneInstance_AnomalyMockData_RunsEveryStageToCompletionAtStatusSixteenAsync()
        {
            var instance = await NewInstanceAsync(0.5);
            await using var db = fx.GetDbContext();

            var completed = await NewTraining().RunOneInstanceAsync(instance, db, CancellationToken.None);

            completed.Should().BeTrue();
            var row = await ReloadAsync(instance.Id);
            row.StatusId.Should().Be(16);
            row.CompletedDate.Should().NotBeNull();
            row.Models.Should().BeGreaterThan(0);

            var variables = await db.GetTable<ExhaustiveSearchInstanceVariable>()
                .Where(w => w.ExhaustiveSearchInstanceId == instance.Id).OrderBy(o => o.VariableSequence).ToListAsync();
            variables.Should().HaveCount(26);
            variables.Select(v => v.VariableSequence).Should().BeInAscendingOrder();
            variables.Should().OnlyContain(v => v.NormalisationTypeId >= 0 && v.NormalisationTypeId <= 2);

            var data = await db.GetTable<ExhaustiveSearchInstanceData>()
                .Where(w => w.ExhaustiveSearchInstanceId == instance.Id).ToListAsync();
            data.Should().NotBeEmpty();
            data.Count(d => d.Dependent == 1).Should().Be(data.Count(d => d.Dependent == 0));
            data.Should().OnlyContain(d => d.Independent.Length == 26);

            var multiCollinearity = await db.GetTable<ExhaustiveSearchInstanceVariableMultiCollinearity>()
                .CountAsync(w => variables.Select(v => v.Id).Contains(w.ExhaustiveSearchInstanceVariableId!.Value));
            multiCollinearity.Should().Be(26 * 25);

            var trials = await db.GetTable<ExhaustiveSearchInstanceTrialInstance>()
                .Where(w => w.ExhaustiveSearchInstanceId == instance.Id).ToListAsync();
            trials.Should().NotBeEmpty();
            var trialIds = trials.Select(t => t.Id).ToList();

            variables.Should().OnlyContain(v => v.Correlation != null);
            variables.Should().OnlyContain(v => v.Correlation >= -1 && v.Correlation <= 1);

            var promoted = await db.GetTable<ExhaustiveSearchInstancePromotedTrialInstance>()
                .Where(w => trialIds.Contains(w.ExhaustiveSearchInstanceTrialInstanceId!.Value)).ToListAsync();
            promoted.Should().NotBeEmpty();
            row.Score.Should().BeApproximately(promoted.Max(p => p.Score!.Value), 1e-9);
            row.TopologyComplexity.Should().BeGreaterThan(0);

            var promotedTrialIds = promoted.Select(p => p.ExhaustiveSearchInstanceTrialInstanceId!.Value).ToList();

            (await db.GetTable<ExhaustiveSearchInstancePromotedTrialInstanceRoc>()
                    .CountAsync(w => promotedTrialIds.Contains(w.ExhaustiveSearchInstanceTrialInstanceId!.Value)))
                .Should().Be(20 * promoted.Count);

            (await db.GetTable<ExhaustiveSearchInstancePromotedTrialInstancePredictedActual>()
                    .CountAsync(w => promotedTrialIds.Contains(w.ExhaustiveSearchInstanceTrialInstanceId!.Value)))
                .Should().BeGreaterThan(0);

            (await db.GetTable<ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial>()
                    .CountAsync(w => trialIds.Contains(w.ExhaustiveSearchInstanceTrialInstanceId!.Value)) % 10)
                .Should().Be(0);

            (await db.GetTable<ExhaustiveSearchInstanceTrialInstanceTopologyTrial>()
                .CountAsync(w => trialIds.Contains(w.ExhaustiveSearchInstanceTrialInstanceId!.Value)
                                 && w.Finalisation == 1)).Should().Be(trials.Count);

            (await db.GetTable<ExhaustiveSearchInstanceTrialInstanceVariable>()
                    .CountAsync(w => trialIds.Contains(w.ExhaustiveSearchInstanceTrialInstanceId!.Value)))
                .Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task RunOneInstance_AnomalyMockData_StoresClassHistogramsOnThePopulationBinEdgesAsync()
        {
            var instance = await NewInstanceAsync(0.5);
            await using var db = fx.GetDbContext();

            (await NewTraining().RunOneInstanceAsync(instance, db, CancellationToken.None)).Should().BeTrue();

            var variables = await db.GetTable<ExhaustiveSearchInstanceVariable>()
                .Where(w => w.ExhaustiveSearchInstanceId == instance.Id).OrderBy(o => o.VariableSequence).ToListAsync();

            var variableIds = variables.Select(v => v.Id).ToList();

            var population = await db.GetTable<ExhaustiveSearchInstanceVariableHistogram>()
                .Where(w => variableIds.Contains(w.ExhaustiveSearchInstanceVariableId!.Value)).ToListAsync();
            var classes = await db.GetTable<ExhaustiveSearchInstanceVariableHistogramClass>()
                .Where(w => variableIds.Contains(w.ExhaustiveSearchInstanceVariableId!.Value)).ToListAsync();

            classes.Should().NotBeEmpty();

            foreach (var variable in variables)
            {
                var all = population.Where(p => p.ExhaustiveSearchInstanceVariableId == variable.Id)
                    .OrderBy(p => p.BinSequence).ToList();

                var negative = classes.Where(c => c.ExhaustiveSearchInstanceVariableId == variable.Id
                                                  && c.ClassLabel == 0).OrderBy(c => c.BinSequence).ToList();
                var positive = classes.Where(c => c.ExhaustiveSearchInstanceVariableId == variable.Id
                                                  && c.ClassLabel == 1).OrderBy(c => c.BinSequence).ToList();

                negative.Should().HaveCount(all.Count);
                positive.Should().HaveCount(all.Count);
                negative.Select(n => n.BinRangeStart).Should().Equal(all.Select(a => a.BinRangeStart));
                positive.Select(n => n.BinRangeEnd).Should().Equal(all.Select(a => a.BinRangeEnd));

                for (var bin = 0; bin < all.Count; bin++)
                {
                    (negative[bin].Frequency + positive[bin].Frequency).Should().Be(all[bin].Frequency,
                        $"variable {variable.Name} bin {bin}");
                }

                positive.Sum(p => p.Frequency).Should().BeGreaterThan(0);
            }

            // ReSharper disable once AccessToDisposedClosure
            var anomalyClass = await db.GetTable<ExhaustiveSearchInstanceVariableHistogramAnomaly>()
                .Where(w => db.GetTable<ExhaustiveSearchInstanceVariableAnomaly>()
                    .Any(a => a.Id == w.ExhaustiveSearchInstanceVariableAnomalyId
                              && variableIds.Contains(a.ExhaustiveSearchInstanceVariableId!.Value)))
                .SumAsync(w => w.Frequency ?? 0);

            var firstVariable = variables.First().Id;

            // ReSharper disable once AccessToDisposedClosure
            classes.Where(c => c.ExhaustiveSearchInstanceVariableId == firstVariable && c.ClassLabel == 1)
                .Sum(c => c.Frequency).Should().Be(
                    await db.GetTable<ExhaustiveSearchInstanceVariableHistogramAnomaly>()
                        .Where(w => db.GetTable<ExhaustiveSearchInstanceVariableAnomaly>()
                            .Any(a => a.Id == w.ExhaustiveSearchInstanceVariableAnomalyId
                                      && a.ExhaustiveSearchInstanceVariableId == firstVariable))
                        .SumAsync(w => w.Frequency ?? 0));

            anomalyClass.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task RunOneInstance_TheDefaultAlgorithmRecordsAnSvmReportWithNoReductionAsync()
        {
            var instance = await NewInstanceAsync(0.5);
            await using var db = fx.GetDbContext();

            (await NewTraining().RunOneInstanceAsync(instance, db, CancellationToken.None)).Should().BeTrue();

            var report =
                AnomalyReport.FromJson((await ReloadAsync(instance.Id)).AnomalyReportJson);
            report.Should().NotBeNull();
            report!.Algorithm.Should().Be("OneClassSvm");
            report.Reduction.Should().Be("None");
            report.InputVariables.Should().Be(26);
            report.KeptVariables.Should().Be(26);
            report.Threshold!.Method.Should().Be("SvmProbability");
            report.Threshold.Flagged.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task RunOneInstance_ANativeAlgorithmLabelsTheContaminationFractionAndStoresTheReductionAsync()
        {
            var instance = await NewInstanceAsync(0.05, algorithmId: 1);
            await using var db = fx.GetDbContext();

            (await NewTraining().RunOneInstanceAsync(instance, db, CancellationToken.None)).Should().BeTrue();

            var row = await ReloadAsync(instance.Id);
            row.StatusId.Should().Be(16);
            var report = AnomalyReport.FromJson(row.AnomalyReportJson);
            report!.Algorithm.Should().Be("IsolationForest");
            report.Reduction.Should().Be("Standard");
            report.Variables.Should().HaveCount(26);
            report.KeptVariables.Should().BeLessThan(26);
            report.Variables.Where(v => !v.Kept).Should().OnlyContain(v => v.Reason.Length > 0 && v.Detail.Length > 0);
            report.Threshold!.Method.Should().Be("Contamination");
            report.Threshold.Flagged.Should().BeInRange((int)(0.04 * report.Rows), (int)(0.06 * report.Rows));

            var variables = await db.GetTable<ExhaustiveSearchInstanceVariable>()
                .Where(w => w.ExhaustiveSearchInstanceId == instance.Id).ToListAsync();
            variables.Should().HaveCount(26);
        }

        [Fact]
        public async Task RunOneInstance_AutomaticRecordsEveryCandidateEvaluationAsync()
        {
            var instance = await NewInstanceAsync(0.05, algorithmId: 100,
                parametersJson:
                "{\"auto\":{\"timeBudgetSeconds\":120,\"candidates\":[\"Hbos\",\"IsolationForest\",\"Pca\"]}}");

            await using var db = fx.GetDbContext();

            (await NewTraining().RunOneInstanceAsync(instance, db, CancellationToken.None)).Should().BeTrue();

            var report =
                AnomalyReport.FromJson((await ReloadAsync(instance.Id)).AnomalyReportJson);
            report!.Requested.Should().Be("Automatic");
            report.Algorithm.Should().BeOneOf("Hbos", "IsolationForest", "Pca");
            report.Auto!.Candidates.Should().HaveCount(3);
            report.Auto.Candidates.Count(c => c.Selected).Should().Be(1);
            report.Auto.UsedLabels.Should().BeFalse();
        }

        [Fact]
        public async Task RunOneInstance_AnomalyThatFlagsNothing_CompletesWithStatusSeventeenAndNoTrialsAsync()
        {
            var instance = await NewInstanceAsync(0);
            await using var db = fx.GetDbContext();

            var completed = await NewTraining().RunOneInstanceAsync(instance, db, CancellationToken.None);

            completed.Should().BeTrue();
            var row = await ReloadAsync(instance.Id);
            row.StatusId.Should().Be(17);
            row.CompletedDate.Should().NotBeNull();

            (await db.GetTable<ExhaustiveSearchInstanceTrialInstance>()
                .CountAsync(w => w.ExhaustiveSearchInstanceId == instance.Id)).Should().Be(0);

            (await db.GetTable<ExhaustiveSearchInstanceVariableAnomaly>()
                .CountAsync(w => w.ExhaustiveSearchInstanceVariableId != null
                                 && db.GetTable<ExhaustiveSearchInstanceVariable>()
                                     .Any(v => v.Id == w.ExhaustiveSearchInstanceVariableId
                                               && v.ExhaustiveSearchInstanceId == instance.Id))).Should().Be(0);
        }

        [Fact]
        public async Task StartAsync_PollsRunsTheNextInstanceThenKeepsPollingUntilCancelledAsync()
        {
            var instance = await NewInstanceAsync(0.5);
            var training = NewTraining();
            var polls = 0;

            training.fetchNext = async (context, token) =>
            {
                polls++;

                if (polls > 1)
                {
                    return null!;
                }

                await new ExhaustiveSearchInstanceRepository(context).UpdateStatusAsync(instance.Id, 18, token);

                return instance;
            };

            using var cts = new CancellationTokenSource();
            var run = training.StartAsync(cts.Token);

            var deadline = DateTime.UtcNow.AddSeconds(120);

            while (polls < 3 && DateTime.UtcNow < deadline)
            {
                await Task.Delay(50);
            }

            await cts.CancelAsync();
            await run;

            polls.Should().BeGreaterThanOrEqualTo(3);
            (await ReloadAsync(instance.Id)).StatusId.Should().Be(19);
        }

        [Fact]
        public async Task
            StartAsync_AnAnomalyInstanceThatFlagsNothing_CompletesAtStatusSeventeenAndPollingContinuesAsync()
        {
            var instance = await NewInstanceAsync(0);
            var training = NewTraining();
            var polls = 0;

            training.fetchNext = async (context, token) =>
            {
                polls++;

                if (polls > 1)
                {
                    return null!;
                }

                await new ExhaustiveSearchInstanceRepository(context).UpdateStatusAsync(instance.Id, 1, token);

                return instance;
            };

            using var cts = new CancellationTokenSource();
            var run = training.StartAsync(cts.Token);

            var deadline = DateTime.UtcNow.AddSeconds(120);

            while (polls < 3 && DateTime.UtcNow < deadline)
            {
                await Task.Delay(50);
            }

            await cts.CancelAsync();
            await run;

            polls.Should().BeGreaterThanOrEqualTo(3);
            var row = await ReloadAsync(instance.Id);
            row.StatusId.Should().Be(17);
            row.CompletedDate.Should().NotBeNull();
        }

        [Fact]
        public async Task StartAsync_ARunThatThrows_IsSurvivedAndPollingContinuesAsync()
        {
            var training = NewTraining();
            var polls = 0;

            training.fetchNext = (_, _) =>
            {
                polls++;

                if (polls == 1)
                {
                    throw new InvalidOperationException("boom");
                }

                return Task.FromResult<GetNextExhaustiveSearchInstanceQueryDto>(null!);
            };

            using var cts = new CancellationTokenSource();
            var run = training.StartAsync(cts.Token);

            var deadline = DateTime.UtcNow.AddSeconds(60);

            while (polls < 3 && DateTime.UtcNow < deadline)
            {
                await Task.Delay(20);
            }

            await cts.CancelAsync();
            await run;

            polls.Should().BeGreaterThanOrEqualTo(3);
        }

        [Fact]
        public async Task StartAsync_CancelledBeforeStart_ReturnsImmediatelyWithoutPollingAsync()
        {
            var training = NewTraining();
            var polls = 0;

            training.fetchNext = (_, _) =>
            {
                polls++;

                return Task.FromResult<GetNextExhaustiveSearchInstanceQueryDto>(null!);
            };

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await training.StartAsync(cts.Token);

            polls.Should().Be(0);
        }
    }
}