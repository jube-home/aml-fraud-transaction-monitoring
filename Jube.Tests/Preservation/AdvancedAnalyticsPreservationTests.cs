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
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Cryptography;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions;
using Jube.Engine.Helpers;
using Jube.Preservation.Exceptions;
using Jube.Preservation.Models;
using Jube.Service.Exceptions.Repository.Preservation;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.Repository.Preservation.Models;
using LinqToDB;
using MessagePack;
using Xunit;
using EntityAnalysisModelDomain = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;
using SyncContext = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Context;

namespace Jube.Test.Preservation
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsPreservationTests(DatabaseFixture fx)
    {
        private readonly PreservationHarness harness = new(fx);

        private static async Task<Snapshot> LoadAsync(DbContext db, int tenantId, bool liveOnly = true)
        {
            var instances = await db.AdvancedAnalyticsInstance
                .Where(w => w.TenantRegistryId == tenantId && (!liveOnly || w.Deleted == 0)).OrderBy(o => o.Id)
                .ToListAsync();
            var models = await db.AdvancedAnalyticsTrainedModel
                .Where(w => w.TenantRegistryId == tenantId && (!liveOnly || w.Deleted == 0)).OrderBy(o => o.Id)
                .ToListAsync();

            var liveModelIds = models.Select(s => s.Id).ToList();

            var versions = await db.AdvancedAnalyticsTrainedModelVersion
                .Where(w => w.TenantRegistryId == tenantId && liveModelIds.Contains(w.AdvancedAnalyticsTrainedModelId))
                .OrderBy(o => o.Id).ToListAsync();

            var runs = await db.AdvancedAnalyticsTrainingRun
                .Where(w => w.TenantRegistryId == tenantId && (!liveOnly || w.Deleted == 0)).OrderBy(o => o.Id)
                .ToListAsync();

            var runIds = runs.Select(s => s.Id).ToList();

            return new Snapshot(instances, models, versions, runs,
                await db.AdvancedAnalyticsTrainingRunStep
                    .Where(w => w.TenantRegistryId == tenantId && runIds.Contains(w.AdvancedAnalyticsTrainingRunId))
                    .OrderBy(o => o.Id).ToListAsync(),
                await db.AdvancedAnalyticsTrainingRunTrial
                    .Where(w => w.TenantRegistryId == tenantId && runIds.Contains(w.AdvancedAnalyticsTrainingRunId))
                    .OrderBy(o => o.Id).ToListAsync(),
                await db.AdvancedAnalyticsTrainingRunIteration
                    .Where(w => w.TenantRegistryId == tenantId && runIds.Contains(w.AdvancedAnalyticsTrainingRunId))
                    .OrderBy(o => o.Id).ToListAsync(),
                await db.AdvancedAnalyticsTrainingRunVariable
                    .Where(w => w.TenantRegistryId == tenantId && runIds.Contains(w.AdvancedAnalyticsTrainingRunId))
                    .OrderBy(o => o.Id).ToListAsync(),
                await db.AdvancedAnalyticsTrainingRunMetric
                    .Where(w => w.TenantRegistryId == tenantId && runIds.Contains(w.AdvancedAnalyticsTrainingRunId))
                    .OrderBy(o => o.Id).ToListAsync(),
                await db.AdvancedAnalyticsTrainingRunCurvePoint
                    .Where(w => w.TenantRegistryId == tenantId && runIds.Contains(w.AdvancedAnalyticsTrainingRunId))
                    .OrderBy(o => o.Id).ToListAsync());
        }

        private static int Total(Snapshot s)
        {
            return s.Instances.Count + s.Models.Count + s.Versions.Count + s.Runs.Count + s.Steps.Count
                   + s.Trials.Count + s.Iterations.Count + s.Variables.Count + s.Metrics.Count + s.CurvePoints.Count;
        }

        private async Task<(PreservationTenant Source, PreservationTenant Target, SeededAnalytics Seeded)>
            SourceAndTargetAsync(string label)
        {
            var source = await harness.CreateTenantAsync(label + "S");
            var target = await harness.CreateTenantAsync(label + "T");
            var modelId = await harness.CreateModelAsync(source, label);
            await using var db = fx.GetDbContext();
            var seeded = await AdvancedAnalyticsSeed.SeedAsync(db, source.Id, modelId, label);

            return (source, target, seeded);
        }

        private static void Corrupt(string kind, AdvancedAnalyticsPayload p)
        {
            var instances = p.Instances!.ToList();
            var models = p.TrainedModels!.ToList();
            var versions = p.TrainedModelVersions!.ToList();
            var runs = p.TrainingRuns!.ToList();
            var steps = p.TrainingRunSteps!.ToList();
            var trials = p.TrainingRunTrials!.ToList();
            var metrics = p.TrainingRunMetrics!.ToList();
            var curves = p.TrainingRunCurvePoints!.ToList();
            var versionOne = versions.First(f => f.VersionNumber == 1);

            switch (kind)
            {
                case "modeljson-malformed": versionOne.ModelJson = "{not json"; break;
                case "modeljson-empty": versionOne.ModelJson = ""; break;
                case "modeljson-null": versionOne.ModelJson = null!; break;
                case "modeljson-too-deep":
                    versionOne.ModelJson = new string('[', 33) + new string(']', 33); break;
                case "modeljson-too-large":
                    versionOne.ModelJson = "[" + new string(' ', 16 * 1024 * 1024) + "]"; break;
                case "calibrator-malformed": versionOne.CalibratorJson = "{\"a\":"; break;
                case "variables-malformed": versionOne.VariablesJson = "nope"; break;
                case "metrics-nul-escape": versionOne.MetricsJson = "{\"a\":\"\\u0000\"}"; break;
                case "arguments-malformed": runs[0].Arguments = "{{"; break;
                case "hyperparameters-malformed": trials[0].Hyperparameters = "]["; break;
                case "duplicate-instance-id": instances[1].Id = instances[0].Id; break;
                case "duplicate-instance-name": instances[1].Name = instances[0].Name.ToUpperInvariant(); break;
                case "trained-model-unknown-instance": models[0].AdvancedAnalyticsInstanceId = 987654321; break;
                case "version-unknown-model": versionOne.AdvancedAnalyticsTrainedModelId = 987654321; break;
                case "duplicate-version-number":
                    versions.First(f => f.VersionNumber == 2).VersionNumber = 1; break;
                case "version-number-zero": versionOne.VersionNumber = 0; break;
                case "active-version-of-other-model":
                    models.First(f => f.ActiveVersionId != null).ActiveVersionId =
                        versions.First(f =>
                            f.AdvancedAnalyticsTrainedModelId != models.First(m => m.ActiveVersionId != null).Id).Id;

                    break;
                case "run-progress-out-of-range": runs[0].Progress = 2; break;
                case "run-status-undefined": runs[0].StatusId = (AdvancedAnalyticsStatus)99; break;
                case "kind-undefined": models[0].KindId = 0; break;
                case "source-undefined": models[0].SourceId = (AdvancedAnalyticsSource)7; break;
                case "split-undefined": metrics[0].Split = 0; break;
                case "curve-undefined": curves[0].Curve = (AdvancedAnalyticsCurve)9; break;
                case "step-undefined": steps[0].Step = 0; break;
                case "step-unknown-run": steps[0].AdvancedAnalyticsTrainingRunId = 987654321; break;
                case "metric-trial-of-other-run":
                    metrics.First(f => f.AdvancedAnalyticsTrainingRunTrialId != null).AdvancedAnalyticsTrainingRunId =
                        runs.First(f => f.Id != metrics.First(m => m.AdvancedAnalyticsTrainingRunTrialId != null)
                            .AdvancedAnalyticsTrainingRunId).Id;

                    break;
                case "flag-out-of-range": instances[0].Active = 2; break;
                case "name-nul": instances[0].Name = "a\0b"; break;
                case "name-too-long": instances[0].Name = new string('n', 1025); break;
                case "null-row": instances.Add(null!); break;
                case "duplicate-trial-id": trials[1].Id = trials[0].Id; break;
                default: throw new InvalidOperationException(kind);
            }

            p.Instances = instances;
            p.TrainedModels = models;
            p.TrainedModelVersions = versions;
            p.TrainingRuns = runs;
            p.TrainingRunSteps = steps;
            p.TrainingRunTrials = trials;
            p.TrainingRunMetrics = metrics;
            p.TrainingRunCurvePoints = curves;
        }

        private sealed record Snapshot(
            List<AdvancedAnalyticsInstance> Instances,
            List<AdvancedAnalyticsTrainedModel> Models,
            List<AdvancedAnalyticsTrainedModelVersion> Versions,
            List<AdvancedAnalyticsTrainingRun> Runs,
            List<AdvancedAnalyticsTrainingRunStep> Steps,
            List<AdvancedAnalyticsTrainingRunTrial> Trials,
            List<AdvancedAnalyticsTrainingRunIteration> Iterations,
            List<AdvancedAnalyticsTrainingRunVariable> Variables,
            List<AdvancedAnalyticsTrainingRunMetric> Metrics,
            List<AdvancedAnalyticsTrainingRunCurvePoint> CurvePoints);

        [Fact]
        public async Task FullRoundTripIntoFreshTenantReproducesEveryRowWithRemappedKeysAsync()
        {
            var (source, target, seeded) = await SourceAndTargetAsync("Rt");
            await using var db = fx.GetDbContext();
            var before = await LoadAsync(db, source.Id);

            var bytes = await harness.ExportAsync(source, PreservationHarness.ExportOptionsFor());
            await harness.ImportAsync(target, bytes);

            var sourceAfter = await LoadAsync(db, source.Id);
            Total(sourceAfter).Should().Be(Total(before));
            sourceAfter.Instances.Select(s => s.Id).Should().Equal(before.Instances.Select(s => s.Id));
            sourceAfter.Versions.Select(s => s.ModelJson).Should().Equal(before.Versions.Select(s => s.ModelJson));

            var t = await LoadAsync(db, target.Id);
            Total(t).Should().Be(Total(before));

            before.Instances.ShouldMatch(t.Instances, RowCompare.Ignoring("Guid", "TenantRegistryId",
                "EntityAnalysisModelId", "CreatedUser", "CreatedDate", "UpdatedDate", "Version"), "instances");

            before.Models.ShouldMatch(t.Models, RowCompare.Ignoring("Guid", "TenantRegistryId",
                "AdvancedAnalyticsInstanceId", "ActiveVersionId", "CreatedUser", "CreatedDate", "UpdatedDate",
                "Version"), "models");

            before.Versions.ShouldMatch(t.Versions, RowCompare.Ignoring("Guid", "TenantRegistryId",
                "AdvancedAnalyticsTrainedModelId", "AdvancedAnalyticsTrainingRunId"), "versions");

            before.Runs.ShouldMatch(t.Runs, RowCompare.Ignoring("Guid", "TenantRegistryId",
                "AdvancedAnalyticsInstanceId", "InputPointer", "StatusId", "CompletedDate", "Error"), "runs");

            before.Steps.ShouldMatch(t.Steps, RowCompare.Ignoring("TenantRegistryId", "AdvancedAnalyticsInstanceId",
                "AdvancedAnalyticsTrainingRunId", "StatusId"), "steps");

            before.Trials.ShouldMatch(t.Trials, RowCompare.Ignoring("TenantRegistryId", "AdvancedAnalyticsInstanceId",
                "AdvancedAnalyticsTrainingRunId"), "trials");

            before.Iterations.ShouldMatch(t.Iterations, RowCompare.Ignoring("TenantRegistryId",
                    "AdvancedAnalyticsInstanceId", "AdvancedAnalyticsTrainingRunId",
                    "AdvancedAnalyticsTrainingRunTrialId"),
                "iterations");

            before.Variables.ShouldMatch(t.Variables, RowCompare.Ignoring("TenantRegistryId",
                "AdvancedAnalyticsInstanceId", "AdvancedAnalyticsTrainingRunId"), "variables");

            before.Metrics.ShouldMatch(t.Metrics, RowCompare.Ignoring("TenantRegistryId",
                    "AdvancedAnalyticsInstanceId", "AdvancedAnalyticsTrainingRunId",
                    "AdvancedAnalyticsTrainingRunTrialId"),
                "metrics");

            before.CurvePoints.ShouldMatch(t.CurvePoints, RowCompare.Ignoring("TenantRegistryId",
                "AdvancedAnalyticsInstanceId", "AdvancedAnalyticsTrainingRunId"), "curve points");

            var importedModelId = (await db.EntityAnalysisModel
                .SingleAsync(w => w.TenantRegistryId == target.Id && (w.Deleted == 0 || w.Deleted == null))).Id;

            importedModelId.Should().NotBe(seeded.ModelId);

            var linked = t.Instances.Single(s => s.Name == seeded.Linked.Name);
            var free = t.Instances.Single(s => s.Name == seeded.Free.Name);
            linked.EntityAnalysisModelId.Should().Be(importedModelId);
            free.EntityAnalysisModelId.Should().BeNull();

            t.Instances.Should().OnlyContain(o => o.TenantRegistryId == target.Id && o.Deleted == 0
                && o.CreatedUser == target.UserName && o.Version == 1);

            t.Instances.Select(s => s.Guid).Should().NotIntersectWith(before.Instances.Select(s => s.Guid));

            var modelA = t.Models.Single(s => s.Name == seeded.ModelA.Name);
            var modelB = t.Models.Single(s => s.Name == seeded.ModelB.Name);
            modelA.AdvancedAnalyticsInstanceId.Should().Be(linked.Id);
            modelB.AdvancedAnalyticsInstanceId.Should().Be(free.Id);
            t.Models.Should().OnlyContain(o => o.TenantRegistryId == target.Id && o.Deleted == 0);

            var a1 = t.Versions.Single(s => s.AdvancedAnalyticsTrainedModelId == modelA.Id && s.VersionNumber == 1);
            var a2 = t.Versions.Single(s => s.AdvancedAnalyticsTrainedModelId == modelA.Id && s.VersionNumber == 2);
            modelA.ActiveVersionId.Should().Be(a2.Id);
            modelA.Active.Should().Be(1);
            modelB.ActiveVersionId.Should().BeNull();
            modelB.Active.Should().Be(0);

            a1.ModelJson.Should().Be(AdvancedAnalyticsSeed.OddModelJson);
            a1.CalibratorJson.Should().Be(AdvancedAnalyticsSeed.OddCalibratorJson);
            a1.VariablesJson.Should().Be(seeded.VersionA1.VariablesJson);
            a1.MetricsJson.Should().Be(seeded.VersionA1.MetricsJson);
            a2.ModelJson.Should().Be(AdvancedAnalyticsSeed.Trained.SnapshotJson);
            t.Versions.Should().OnlyContain(o => o.TenantRegistryId == target.Id);

            var runSucceeded = t.Runs.Single(s => s.Seed == 4242424242L);
            var runRunning = t.Runs.Single(s => s.Seed == null);
            runSucceeded.AdvancedAnalyticsInstanceId.Should().Be(linked.Id);
            runSucceeded.StatusId.Should().Be(AdvancedAnalyticsStatus.Succeeded);
            runSucceeded.InputPointer.Should().BeNull();
            runRunning.AdvancedAnalyticsInstanceId.Should().Be(free.Id);
            runRunning.StatusId.Should().Be(AdvancedAnalyticsStatus.Cancelled);
            runRunning.Error.Should().NotBeNullOrEmpty();
            runRunning.CompletedDate.Should().NotBeNull();
            t.Runs.Should().OnlyContain(o => o.TenantRegistryId == target.Id && o.Deleted == 0);
            a1.AdvancedAnalyticsTrainingRunId.Should().Be(runSucceeded.Id);
            a2.AdvancedAnalyticsTrainingRunId.Should().BeNull();

            t.Steps.Single(s => s.AdvancedAnalyticsTrainingRunId == runRunning.Id).StatusId
                .Should().Be(AdvancedAnalyticsStatus.Cancelled);

            t.Steps.Where(s => s.AdvancedAnalyticsTrainingRunId == runSucceeded.Id).Select(s => s.StatusId)
                .Should().Equal(AdvancedAnalyticsStatus.Succeeded, AdvancedAnalyticsStatus.Failed);

            var runById = t.Runs.ToDictionary(s => s.Id);

            foreach (var child in t.Steps.Select(s => (s.TenantRegistryId, s.AdvancedAnalyticsInstanceId,
                             s.AdvancedAnalyticsTrainingRunId))
                         .Concat(t.Trials.Select(s => (s.TenantRegistryId, s.AdvancedAnalyticsInstanceId,
                             s.AdvancedAnalyticsTrainingRunId)))
                         .Concat(t.Iterations.Select(s => (s.TenantRegistryId, s.AdvancedAnalyticsInstanceId,
                             s.AdvancedAnalyticsTrainingRunId)))
                         .Concat(t.Variables.Select(s => (s.TenantRegistryId, s.AdvancedAnalyticsInstanceId,
                             s.AdvancedAnalyticsTrainingRunId)))
                         .Concat(t.Metrics.Select(s => (s.TenantRegistryId, s.AdvancedAnalyticsInstanceId,
                             s.AdvancedAnalyticsTrainingRunId)))
                         .Concat(t.CurvePoints.Select(s => (s.TenantRegistryId, s.AdvancedAnalyticsInstanceId,
                             s.AdvancedAnalyticsTrainingRunId))))
            {
                child.TenantRegistryId.Should().Be(target.Id);
                runById.Should().ContainKey(child.AdvancedAnalyticsTrainingRunId);

                child.AdvancedAnalyticsInstanceId.Should()
                    .Be(runById[child.AdvancedAnalyticsTrainingRunId].AdvancedAnalyticsInstanceId);
            }

            var trialOne = t.Trials.Single(s => s.TrialNumber == 1);
            var trialTwo = t.Trials.Single(s => s.TrialNumber == 2);
            trialOne.AdvancedAnalyticsTrainingRunId.Should().Be(runSucceeded.Id);

            t.Iterations.Single(s => s.IterationNumber == 1).AdvancedAnalyticsTrainingRunTrialId.Should()
                .Be(trialOne.Id);
            t.Iterations.Single(s => s.IterationNumber == 2).AdvancedAnalyticsTrainingRunTrialId.Should()
                .Be(trialTwo.Id);

            t.Iterations.Single(s => s.IterationNumber == 3).AdvancedAnalyticsTrainingRunTrialId.Should().BeNull();

            t.Metrics.Single(s => s.Split == AdvancedAnalyticsSplit.Validation).AdvancedAnalyticsTrainingRunTrialId
                .Should().Be(trialTwo.Id);

            t.Metrics.Single(s => s.Split == AdvancedAnalyticsSplit.Test).AdvancedAnalyticsTrainingRunTrialId
                .Should().BeNull();

            (await db.AdvancedAnalyticsCache.CountAsync(w => w.TenantRegistryId == target.Id)).Should().Be(0);
            (await db.AdvancedAnalyticsCache.CountAsync(w => w.Guid == seeded.CacheGuid)).Should().Be(1);
        }

        [Fact]
        public async Task ExportContainsNoTenantIdNoCacheAndOnlyTheCallersRowsAsync()
        {
            var (source, other, seeded) = await SourceAndTargetAsync("Ex");
            var otherModel = await harness.CreateModelAsync(other, "ExO");
            await using var db = fx.GetDbContext();
            var otherSeeded = await AdvancedAnalyticsSeed.SeedAsync(db, other.Id, otherModel, "ExO");

            var bytes = await harness.ExportAsync(source, PreservationHarness.ExportOptionsFor());
            var payload = PreservationHarness.Decrypt(bytes).Payload!.AdvancedAnalytics!;

            payload.Instances!.Select(s => s.Name).Should()
                .BeEquivalentTo(seeded.Linked.Name, seeded.Free.Name);

            payload.Instances!.Select(s => s.Name).Should().NotContain(otherSeeded.Linked.Name);
            payload.TrainedModels!.Should().HaveCount(2);
            payload.TrainedModelVersions!.Should().HaveCount(3);
            payload.TrainingRuns!.Should().HaveCount(2);
            payload.Instances!.Select(s => s.TenantRegistryId).Should().OnlyContain(o => o == 0);
            payload.TrainedModels!.Select(s => s.TenantRegistryId).Should().OnlyContain(o => o == 0);
            payload.TrainedModelVersions!.Select(s => s.TenantRegistryId).Should().OnlyContain(o => o == 0);
            payload.TrainingRuns!.Select(s => s.TenantRegistryId).Should().OnlyContain(o => o == 0);
            payload.TrainingRunMetrics!.Select(s => s.TenantRegistryId).Should().OnlyContain(o => o == 0);

            typeof(AdvancedAnalyticsPayload).GetProperties().Select(s => s.Name).Should()
                .NotContain(n => n.Contains("Cache"));

            var without = PreservationHarness.Decrypt(
                await harness.ExportAsync(source, PreservationHarness.ExportOptionsFor(advancedAnalytics: false)));

            without.Payload!.AdvancedAnalytics.Should().BeNull();
        }

        [Fact]
        public async Task ImportedActiveVersionIsTheOneTheEngineSyncLoadsAsync()
        {
            var (source, target, seeded) = await SourceAndTargetAsync("Sy");

            await harness.ImportAsync(target,
                await harness.ExportAsync(source, PreservationHarness.ExportOptionsFor()));

            await using var db = fx.GetDbContext();

            var importedModelId = (await db.EntityAnalysisModel
                .SingleAsync(w => w.TenantRegistryId == target.Id && (w.Deleted == 0 || w.Deleted == null))).Id;

            var importedModelA = await db.AdvancedAnalyticsTrainedModel
                .SingleAsync(w => w.TenantRegistryId == target.Id && w.Name == seeded.ModelA.Name);

            var importedActive = await db.AdvancedAnalyticsTrainedModelVersion
                .SingleAsync(w => w.Id == importedModelA.ActiveVersionId);

            var log = new TestLog(false);

            var domain = new EntityAnalysisModelDomain
            {
                Instance = { Id = importedModelId, Guid = Guid.NewGuid(), TenantRegistryId = target.Id }
            };

            var context = new SyncContext
            {
                JsonSerializationHelper = new JsonSerializationHelper(),
                Services = { DbContext = db, Log = log, Parser = new Parser.Parser(log, []) },
                EntityAnalysisModels =
                {
                    ActiveEntityAnalysisModels = new ConcurrentDictionary<int, EntityAnalysisModelDomain>
                        { [importedModelId] = domain }
                }
            };

            await context.SyncAdvancedAnalyticsInstancesAsync();
            context.Snapshots.Publish();

            var loaded = domain.Snapshot.AdvancedAnalyticsModels.Should().ContainSingle().Subject;
            loaded.Id.Should().Be(importedModelA.Id);
            loaded.VersionId.Should().Be(importedActive.Id);
            importedActive.VersionNumber.Should().Be(2);
            loaded.ResponsePayload.Should().BeTrue();
            loaded.Recall([0.2, 0.3]).Should().Equal(AdvancedAnalyticsSeed.Trained.Recall.Predict([0.2, 0.3]));
        }

        [Fact]
        public async Task ImportOverAnExistingTenantReplacesItsRowsAndSoftDeletesTheOldOnesAsync()
        {
            var tenant = await harness.CreateTenantAsync("Ov");
            var modelId = await harness.CreateModelAsync(tenant, "Ov");
            await using var db = fx.GetDbContext();
            var first = await AdvancedAnalyticsSeed.SeedAsync(db, tenant.Id, modelId, "OvOne");
            var bytes = await harness.ExportAsync(tenant, PreservationHarness.ExportOptionsFor());
            var second = await AdvancedAnalyticsSeed.SeedAsync(db, tenant.Id, modelId, "OvTwo");

            await harness.ImportAsync(tenant, bytes);

            var live = await LoadAsync(db, tenant.Id);
            live.Instances.Select(s => s.Name).Should().BeEquivalentTo(first.Linked.Name, first.Free.Name);

            live.Instances.Select(s => s.Guid).Should()
                .NotIntersectWith([first.Linked.Guid, first.Free.Guid, second.Linked.Guid, second.Free.Guid]);

            live.Models.Should().HaveCount(2);
            live.Versions.Should().HaveCount(3);
            live.Runs.Should().HaveCount(2);

            var all = await LoadAsync(db, tenant.Id, false);
            var old = all.Instances.Where(w => w.Deleted == 1).ToList();

            old.Select(s => s.Id).Should().BeEquivalentTo([
                first.Linked.Id, first.Free.Id, second.Linked.Id, second.Free.Id
            ]);

            old.Should().OnlyContain(o => o.Active == 0 && o.DeletedUser == tenant.UserName && o.DeletedDate != null);

            all.Models.Where(w => w.Deleted == 1).Should()
                .OnlyContain(o => o.Active == 0 && o.DeletedUser == tenant.UserName);

            all.Models.Count(w => w.Deleted == 1).Should().Be(4);
            all.Runs.Count(w => w.Deleted == 1).Should().Be(4);

            var replacement = live.Models.Single(s => s.Name == first.ModelA.Name);
            replacement.Id.Should().NotBe(first.ModelA.Id);
            replacement.ActiveVersionId.Should().NotBeNull();
            live.Versions.Single(s => s.Id == replacement.ActiveVersionId).VersionNumber.Should().Be(2);
        }

        [Fact]
        public async Task ExportMadeWithoutTheCollectionsStillImportsAndReplacesExistingRowsLikeTheNeighboursAsync()
        {
            var source = await harness.CreateTenantAsync("LgS");
            var target = await harness.CreateTenantAsync("LgT");
            var sourceModel = await harness.CreateModelAsync(source, "Lg");
            var targetModel = await harness.CreateModelAsync(target, "LgT");
            await using var db = fx.GetDbContext();
            await AdvancedAnalyticsSeed.SeedAsync(db, source.Id, sourceModel, "Lg");
            var targetSeed = await AdvancedAnalyticsSeed.SeedAsync(db, target.Id, targetModel, "LgT");

            var withoutCollections =
                await harness.ExportAsync(source, PreservationHarness.ExportOptionsFor(advancedAnalytics: false));

            await harness.ImportAsync(target, withoutCollections);

            var live = await LoadAsync(db, target.Id);
            Total(live).Should().Be(0);
            (await db.AdvancedAnalyticsInstance.SingleAsync(w => w.Id == targetSeed.Linked.Id)).Deleted.Should().Be(1);
            (await db.AdvancedAnalyticsTrainedModel.SingleAsync(w => w.Id == targetSeed.ModelA.Id)).Deleted.Should()
                .Be(1);

            (await db.AdvancedAnalyticsCache.CountAsync(w => w.Guid == targetSeed.CacheGuid)).Should().Be(1);
        }

        [Fact]
        public async Task GenuineLegacyFileWithFourPayloadKeysImportsAndAnOlderReaderReadsTheNewFileAsync()
        {
            var source = await harness.CreateTenantAsync("LkS");
            var target = await harness.CreateTenantAsync("LkT");
            var sourceModel = await harness.CreateModelAsync(source, "Lk");
            await using var db = fx.GetDbContext();
            await AdvancedAnalyticsSeed.SeedAsync(db, source.Id, sourceModel, "Lk");

            var newFile = await harness.ExportAsync(source, PreservationHarness.ExportOptionsFor());
            var newBytes = PreservationHarness.DecryptToBytes(newFile);

            var older = MessagePackSerializer.Deserialize<LegacyWrapper>(newBytes, PreservationHarness.Lz4);
            older.Payload!.EntityAnalysisModel!.Should().ContainSingle();

            var legacy = new LegacyWrapper
            {
                Version = 2,
                Guid = Guid.NewGuid(),
                Payload = new LegacyPayload
                {
                    EntityAnalysisModel = older.Payload.EntityAnalysisModel,
                    EntityPermission = new EntityPermission()
                }
            };

            var legacyBytes = MessagePackSerializer.Serialize(legacy, PreservationHarness.Lz4);
            var newer = MessagePackSerializer.Deserialize<Wrapper>(legacyBytes, PreservationHarness.Lz4);
            newer.Payload!.AdvancedAnalytics.Should().BeNull();

            await harness.ImportAsync(target, new JempAesEncryption(PreservationHarness.Password,
                PreservationHarness.Salt).Encrypt(legacyBytes));

            (await db.EntityAnalysisModel.CountAsync(w => w.TenantRegistryId == target.Id
                                                          && (w.Deleted == 0 || w.Deleted == null))).Should().Be(1);

            Total(await LoadAsync(db, target.Id)).Should().Be(0);
        }

        [Fact]
        public async Task TenantIdsAndModelIdsInsideTheFileAreNeverTrustedAsync()
        {
            var (source, target, seeded) = await SourceAndTargetAsync("Fo");
            var victim = await harness.CreateTenantAsync("FoV");
            var victimModel = await harness.CreateModelAsync(victim, "FoV");
            await using var db = fx.GetDbContext();
            var victimSeed = await AdvancedAnalyticsSeed.SeedAsync(db, victim.Id, victimModel, "FoV");
            var victimBefore = await LoadAsync(db, victim.Id);

            var wrapper =
                PreservationHarness.Decrypt(await harness.ExportAsync(source, PreservationHarness.ExportOptionsFor()));

            var payload = wrapper.Payload!.AdvancedAnalytics!;
            var instances = payload.Instances!.ToList();
            var trainedModels = payload.TrainedModels!.ToList();
            var versions = payload.TrainedModelVersions!.ToList();
            var runs = payload.TrainingRuns!.ToList();

            foreach (var row in instances)
            {
                row.TenantRegistryId = victim.Id;
                row.Deleted = 1;
                row.DeletedUser = "attacker";
            }

            instances.Single(s => s.EntityAnalysisModelId != null).EntityAnalysisModelId = victimModel;

            foreach (var row in trainedModels)
            {
                row.TenantRegistryId = victim.Id;
            }

            foreach (var row in versions)
            {
                row.TenantRegistryId = victim.Id;
            }

            foreach (var row in runs)
            {
                row.TenantRegistryId = victim.Id;
            }

            foreach (var row in payload.TrainingRunSteps!)
            {
                row.TenantRegistryId = victim.Id;
            }

            foreach (var row in payload.TrainingRunMetrics!)
            {
                row.TenantRegistryId = victim.Id;
            }

            payload.Instances = instances;
            wrapper.Payload!.EntityAnalysisModel!.ToList().ForEach(f => f.TenantRegistryId = victim.Id);

            await harness.ImportAsync(target, PreservationHarness.Encrypt(wrapper));

            var t = await LoadAsync(db, target.Id);
            t.Instances.Should().HaveCount(2);

            t.Instances.Should().OnlyContain(o => o.TenantRegistryId == target.Id && o.Deleted == 0
                && o.DeletedUser == null);

            t.Instances.Single(s => s.Name == seeded.Linked.Name).EntityAnalysisModelId.Should().NotBe(victimModel);

            t.Instances.Single(s => s.Name == seeded.Linked.Name).EntityAnalysisModelId.Should().BeNull(
                "a model id that is not a model of the file is never trusted");

            Total(t).Should().Be(Total(await LoadAsync(db, source.Id)));

            var victimAfter = await LoadAsync(db, victim.Id);
            Total(victimAfter).Should().Be(Total(victimBefore));
            victimAfter.Instances.Select(s => s.Id).Should().Equal(victimBefore.Instances.Select(s => s.Id));
            victimAfter.Versions.Select(s => s.ModelJson).Should()
                .Equal(victimBefore.Versions.Select(s => s.ModelJson));

            (await db.AdvancedAnalyticsInstance.CountAsync(w => w.Id == victimSeed.Linked.Id && w.Deleted == 0))
                .Should().Be(1);
        }

        public static IEnumerable<object[]> HostileFiles()
        {
            return new[]
            {
                "modeljson-malformed", "modeljson-empty", "modeljson-null", "modeljson-too-deep", "modeljson-too-large",
                "calibrator-malformed", "variables-malformed", "metrics-nul-escape", "arguments-malformed",
                "hyperparameters-malformed", "duplicate-instance-id", "duplicate-instance-name",
                "trained-model-unknown-instance", "version-unknown-model", "duplicate-version-number",
                "version-number-zero", "active-version-of-other-model", "run-progress-out-of-range",
                "run-status-undefined",
                "kind-undefined", "source-undefined", "split-undefined", "curve-undefined", "step-undefined",
                "step-unknown-run", "metric-trial-of-other-run", "flag-out-of-range", "name-nul", "name-too-long",
                "null-row", "duplicate-trial-id"
            }.Select(s => new object[] { s });
        }

        [Theory]
        [MemberData(nameof(HostileFiles))]
        public async Task ACorruptOrHostileFileIsRejectedWholeAndLeavesTheTargetUntouchedAsync(string kind)
        {
            var (source, target, _) = await SourceAndTargetAsync("Ho");
            var targetModel = await harness.CreateModelAsync(target, "HoT");
            await using var db = fx.GetDbContext();
            var targetSeed = await AdvancedAnalyticsSeed.SeedAsync(db, target.Id, targetModel, "HoT");
            var targetBefore = await LoadAsync(db, target.Id);

            var wrapper =
                PreservationHarness.Decrypt(await harness.ExportAsync(source, PreservationHarness.ExportOptionsFor()));

            Corrupt(kind, wrapper.Payload!.AdvancedAnalytics!);
            var bus = new CapturingBus();

            var exception = await Record.ExceptionAsync(() =>
                harness.ImportAsync(target, PreservationHarness.Encrypt(wrapper), bus: bus));

            exception.Should().BeOfType<InvalidPreservationFileException>();
            exception!.InnerException.Should().BeOfType<InvalidAdvancedAnalyticsPreservationException>();

            var targetAfter = await LoadAsync(db, target.Id);
            Total(targetAfter).Should().Be(Total(targetBefore));
            targetAfter.Instances.Select(s => s.Id).Should().Equal(targetBefore.Instances.Select(s => s.Id));
            targetAfter.Instances.Should().OnlyContain(o => o.Deleted == 0);

            (await db.AdvancedAnalyticsTrainedModel.SingleAsync(w => w.Id == targetSeed.ModelA.Id)).Active.Should()
                .Be(1);

            (await db.EntityAnalysisModel.SingleAsync(w => w.Id == targetModel)).Deleted.Should().NotBe(1);
            var importRow = await db.Import.SingleAsync(w => w.TenantRegistryId == target.Id);
            importRow.InError.Should().Be(1);
            importRow.ErrorStack.Should().Contain(nameof(InvalidAdvancedAnalyticsPreservationException));
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task AFailureAfterTheAnalyticsRowsAreInsertedRollsEverythingBackAsync()
        {
            var (source, target, _) = await SourceAndTargetAsync("Rb");
            var targetModel = await harness.CreateModelAsync(target, "RbT");
            await using var db = fx.GetDbContext();
            var targetSeed = await AdvancedAnalyticsSeed.SeedAsync(db, target.Id, targetModel, "RbT");
            var targetBefore = await LoadAsync(db, target.Id);
            var roles = await harness.ExportAsync(source, PreservationHarness.ExportOptionsFor(roles: true));

            await Assert.ThrowsAsync<UserRegistryRecordsExistOnRoleRegistryRekeyImportException>(() =>
                harness.ImportAsync(target, roles));

            var after = await LoadAsync(db, target.Id, false);
            Total(after).Should().Be(Total(targetBefore));
            after.Instances.Select(s => s.Id).Should().Equal(targetBefore.Instances.Select(s => s.Id));
            after.Instances.Should().OnlyContain(o => o.Deleted == 0);
            after.Models.Should().OnlyContain(o => o.Deleted == 0);
            after.Runs.Should().OnlyContain(o => o.Deleted == 0);

            (await db.AdvancedAnalyticsTrainedModel.SingleAsync(w => w.Id == targetSeed.ModelA.Id))
                .ActiveVersionId.Should().Be(targetSeed.VersionA2.Id);
        }

        [Fact]
        public async Task ACancelledImportChangesNothingAsync()
        {
            var (source, target, _) = await SourceAndTargetAsync("Cx");
            await using var db = fx.GetDbContext();
            var bytes = await harness.ExportAsync(source, PreservationHarness.ExportOptionsFor());
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                harness.ImportAsync(target, bytes, cts.Token));

            Total(await LoadAsync(db, target.Id, false)).Should().Be(0);
        }

        [MessagePackObject]
        public sealed class LegacyWrapper
        {
            [Key(0)] public int Version { get; set; }
            [Key(2)] public Guid Guid { get; set; }
            [Key(3)] public LegacyPayload? Payload { get; set; }
        }

        [MessagePackObject]
        public sealed class LegacyPayload
        {
            [Key(0)] public IEnumerable<EntityAnalysisModel>? EntityAnalysisModel { get; set; }
            [Key(1)] public IEnumerable<VisualisationRegistry>? VisualisationRegistry { get; set; }
            [Key(2)] public EntityPermission? EntityPermission { get; set; }
            [Key(3)] public IEnumerable<RoleRegistry>? RoleRegistry { get; set; }
        }
    }
}