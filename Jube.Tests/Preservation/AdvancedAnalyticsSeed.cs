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
using System.Linq;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Test.AdvancedAnalytics.Engine;
using LinqToDB;

namespace Jube.Test.Preservation
{
    internal sealed record SeededAnalytics(
        int ModelId,
        AskJoobyAnalyticsInstance Linked,
        AskJoobyAnalyticsInstance Free,
        AskJoobyAnalyticsTrainedModel ModelA,
        AskJoobyAnalyticsTrainedModel ModelB,
        AskJoobyAnalyticsTrainedModelVersion VersionA1,
        AskJoobyAnalyticsTrainedModelVersion VersionA2,
        AskJoobyAnalyticsTrainedModelVersion VersionB1,
        AdvancedAnalyticsTrainingRun RunSucceeded,
        AdvancedAnalyticsTrainingRun RunRunning,
        AdvancedAnalyticsTrainingRunTrial TrialFirst,
        AdvancedAnalyticsTrainingRunTrial TrialSecond,
        Guid CacheGuid);

    internal static class AdvancedAnalyticsSeed
    {
        public const string OddModelJson =
            "{ \"kind\": \"logistic\",  \"weights\" : [-0, 1E+3, 0.10, 1.0e-7, 12345678901234567890],\"z\":1,\"a\":{\"b\":1,\"a\":2} }";

        public const string OddCalibratorJson = "{\"slope\":  1.50 ,\"intercept\":-0,\"knots\":[0.1,  0.20]}";
        public static readonly string[] Names = ["Payload.A", "Abstraction.B"];
        public static TrainedFixture Trained => EngineModelFactory.Trained(51, Names, [1, 5]);

        public static DateTime At(int second, int microseconds = 0)
        {
            return new DateTime(2026, 3, 4, 5, 6, second, DateTimeKind.Unspecified).AddTicks(microseconds * 10L);
        }

        public static async Task<SeededAnalytics> SeedAsync(DbContext db, int tenantId, int modelId, string tag,
            string user = "ZzTestSeedUser")
        {
            var suffix = Guid.NewGuid().ToString("N")[..10];
            var real = Trained;

            var linkedId = await db.InsertWithInt32IdentityAsync(new AskJoobyAnalyticsInstance
            {
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantId,
                EntityAnalysisModelId = modelId,
                Name = $"ZzTest{tag}Linked{suffix}",
                Active = 1,
                ResponsePayload = 1,
                ReportTable = 1,
                CreatedDate = At(1, 111111),
                CreatedUser = user,
                Version = 4
            });

            var freeId = await db.InsertWithInt32IdentityAsync(new AskJoobyAnalyticsInstance
            {
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantId,
                EntityAnalysisModelId = null,
                Name = $"ZzTest{tag}Free{suffix}",
                Active = 0,
                CreatedDate = At(2),
                CreatedUser = user
            });

            var runSucceededId = await db.InsertWithInt32IdentityAsync(new AdvancedAnalyticsTrainingRun
            {
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantId,
                AskJoobyAnalyticsInstanceId = linkedId,
                UserName = user,
                KindId = AdvancedAnalyticsKind.Classifier,
                JobMode = AdvancedAnalyticsJobMode.Batch,
                StatusId = AdvancedAnalyticsStatus.Succeeded,
                Arguments = "{\"learningRate\": 0.1,  \"iterations\":  50}",
                InputPointer = Guid.NewGuid(),
                Seed = 4242424242L,
                Progress = 1,
                CreatedDate = At(3, 123456),
                StartedDate = At(4, 234567),
                CompletedDate = At(5, 345678),
                UpdatedDate = At(6),
                Error = null,
                CreatedUser = user
            });

            var runRunningId = await db.InsertWithInt32IdentityAsync(new AdvancedAnalyticsTrainingRun
            {
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantId,
                AskJoobyAnalyticsInstanceId = freeId,
                KindId = AdvancedAnalyticsKind.AnomalyDetector,
                JobMode = AdvancedAnalyticsJobMode.Online,
                StatusId = AdvancedAnalyticsStatus.Running,
                Progress = 0.5,
                CreatedDate = At(7),
                StartedDate = At(8),
                CreatedUser = user
            });

            var trialFirstId = await db.InsertWithInt32IdentityAsync(new AdvancedAnalyticsTrainingRunTrial
            {
                TenantRegistryId = tenantId,
                AskJoobyAnalyticsInstanceId = linkedId,
                AdvancedAnalyticsTrainingRunId = runSucceededId,
                TrialNumber = 1,
                StatusId = AdvancedAnalyticsStatus.Succeeded,
                Hyperparameters = "{\"depth\": 3}",
                Score = 0.81,
                Selected = 0,
                CreatedDate = At(9),
                CompletedDate = At(10),
                DurationMilliseconds = 1234.5
            });

            var trialSecondId = await db.InsertWithInt32IdentityAsync(new AdvancedAnalyticsTrainingRunTrial
            {
                TenantRegistryId = tenantId,
                AskJoobyAnalyticsInstanceId = linkedId,
                AdvancedAnalyticsTrainingRunId = runSucceededId,
                TrialNumber = 2,
                StatusId = AdvancedAnalyticsStatus.Succeeded,
                Hyperparameters = "{\"depth\": 5}",
                Score = 0.93,
                Selected = 1,
                CreatedDate = At(11)
            });

            await db.InsertAsync(new AdvancedAnalyticsTrainingRunStep
            {
                TenantRegistryId = tenantId,
                AskJoobyAnalyticsInstanceId = linkedId,
                AdvancedAnalyticsTrainingRunId = runSucceededId,
                StepOrder = 1,
                Step = AdvancedAnalyticsStep.Load,
                StatusId = AdvancedAnalyticsStatus.Succeeded,
                StartedDate = At(12),
                CompletedDate = At(13),
                DurationMilliseconds = 5.25
            });

            await db.InsertAsync(new AdvancedAnalyticsTrainingRunStep
            {
                TenantRegistryId = tenantId,
                AskJoobyAnalyticsInstanceId = linkedId,
                AdvancedAnalyticsTrainingRunId = runSucceededId,
                StepOrder = 2,
                Step = AdvancedAnalyticsStep.Fit,
                StatusId = AdvancedAnalyticsStatus.Failed,
                Error = "boom"
            });

            await db.InsertAsync(new AdvancedAnalyticsTrainingRunStep
            {
                TenantRegistryId = tenantId,
                AskJoobyAnalyticsInstanceId = freeId,
                AdvancedAnalyticsTrainingRunId = runRunningId,
                StepOrder = 1,
                Step = AdvancedAnalyticsStep.Load,
                StatusId = AdvancedAnalyticsStatus.Running,
                StartedDate = At(14)
            });

            await db.InsertAsync(new AdvancedAnalyticsTrainingRunIteration
            {
                TenantRegistryId = tenantId,
                AskJoobyAnalyticsInstanceId = linkedId,
                AdvancedAnalyticsTrainingRunId = runSucceededId,
                AdvancedAnalyticsTrainingRunTrialId = trialFirstId,
                IterationNumber = 1,
                TrainLoss = 0.6931471805599453,
                ValidationLoss = 0.7,
                Score = null,
                RecordedDate = At(15, 999999)
            });

            await db.InsertAsync(new AdvancedAnalyticsTrainingRunIteration
            {
                TenantRegistryId = tenantId,
                AskJoobyAnalyticsInstanceId = linkedId,
                AdvancedAnalyticsTrainingRunId = runSucceededId,
                AdvancedAnalyticsTrainingRunTrialId = trialSecondId,
                IterationNumber = 2,
                TrainLoss = 0.5,
                RecordedDate = At(16)
            });

            await db.InsertAsync(new AdvancedAnalyticsTrainingRunIteration
            {
                TenantRegistryId = tenantId,
                AskJoobyAnalyticsInstanceId = linkedId,
                AdvancedAnalyticsTrainingRunId = runSucceededId,
                AdvancedAnalyticsTrainingRunTrialId = null,
                IterationNumber = 3,
                RecordedDate = At(17)
            });

            await db.InsertAsync(new AdvancedAnalyticsTrainingRunVariable
            {
                TenantRegistryId = tenantId,
                AskJoobyAnalyticsInstanceId = linkedId,
                AdvancedAnalyticsTrainingRunId = runSucceededId,
                VariableSequence = 0,
                Name = "Payload.A",
                Mean = 1.5,
                StandardDeviation = 0.25,
                Minimum = -1,
                Maximum = 9,
                ImputationValue = 1.5,
                MissingCount = 12,
                RowCount = 3000000000L,
                Importance = 0.75
            });

            await db.InsertAsync(new AdvancedAnalyticsTrainingRunVariable
            {
                TenantRegistryId = tenantId,
                AskJoobyAnalyticsInstanceId = linkedId,
                AdvancedAnalyticsTrainingRunId = runSucceededId,
                VariableSequence = 1,
                Name = "Abstraction.B"
            });

            await db.InsertAsync(new AdvancedAnalyticsTrainingRunMetric
            {
                TenantRegistryId = tenantId,
                AskJoobyAnalyticsInstanceId = linkedId,
                AdvancedAnalyticsTrainingRunId = runSucceededId,
                AdvancedAnalyticsTrainingRunTrialId = trialSecondId,
                Name = "auc",
                Value = 0.9123456789,
                Split = AdvancedAnalyticsSplit.Validation
            });

            await db.InsertAsync(new AdvancedAnalyticsTrainingRunMetric
            {
                TenantRegistryId = tenantId,
                AskJoobyAnalyticsInstanceId = linkedId,
                AdvancedAnalyticsTrainingRunId = runSucceededId,
                Name = "auc",
                Value = 0.9,
                Split = AdvancedAnalyticsSplit.Test
            });

            await db.InsertAsync(new AdvancedAnalyticsTrainingRunCurvePoint
            {
                TenantRegistryId = tenantId,
                AskJoobyAnalyticsInstanceId = linkedId,
                AdvancedAnalyticsTrainingRunId = runSucceededId,
                Curve = AdvancedAnalyticsCurve.Roc,
                Split = AdvancedAnalyticsSplit.Test,
                PointIndex = 0,
                X = 0,
                Y = 0,
                Threshold = 1
            });

            await db.InsertAsync(new AdvancedAnalyticsTrainingRunCurvePoint
            {
                TenantRegistryId = tenantId,
                AskJoobyAnalyticsInstanceId = linkedId,
                AdvancedAnalyticsTrainingRunId = runSucceededId,
                Curve = AdvancedAnalyticsCurve.Calibration,
                Split = AdvancedAnalyticsSplit.Validation,
                PointIndex = 1,
                X = 0.5,
                Y = 0.4
            });

            var modelAId = await db.InsertWithInt32IdentityAsync(new AskJoobyAnalyticsTrainedModel
            {
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantId,
                AskJoobyAnalyticsInstanceId = linkedId,
                Name = $"ZzTest{tag}ModelA{suffix}",
                KindId = AdvancedAnalyticsKind.Classifier,
                SourceId = AdvancedAnalyticsSource.Agent,
                CorrelationKey = "corr-" + suffix,
                CreatedDate = At(18),
                CreatedUser = user,
                Version = 3
            });

            var modelBId = await db.InsertWithInt32IdentityAsync(new AskJoobyAnalyticsTrainedModel
            {
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantId,
                AskJoobyAnalyticsInstanceId = freeId,
                Name = $"ZzTest{tag}ModelB{suffix}",
                KindId = AdvancedAnalyticsKind.Regressor,
                SourceId = AdvancedAnalyticsSource.Upload,
                CreatedDate = At(19)
            });

            var versionA1Id = await db.InsertWithInt32IdentityAsync(new AskJoobyAnalyticsTrainedModelVersion
            {
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantId,
                AskJoobyAnalyticsTrainedModelId = modelAId,
                VersionNumber = 1,
                ModelJson = OddModelJson,
                VariablesJson = "{\"b\": 1,   \"a\": [1,2 ]}",
                MetricsJson = "{\"auc\":0.91}",
                CalibratorJson = OddCalibratorJson,
                AdvancedAnalyticsTrainingRunId = runSucceededId,
                CreatedDate = At(20, 654321),
                CreatedUser = user
            });

            var versionA2Id = await db.InsertWithInt32IdentityAsync(new AskJoobyAnalyticsTrainedModelVersion
            {
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantId,
                AskJoobyAnalyticsTrainedModelId = modelAId,
                VersionNumber = 2,
                ModelJson = real.SnapshotJson,
                VariablesJson = real.VariablesJson,
                CreatedDate = At(21)
            });

            var versionB1Id = await db.InsertWithInt32IdentityAsync(new AskJoobyAnalyticsTrainedModelVersion
            {
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantId,
                AskJoobyAnalyticsTrainedModelId = modelBId,
                VersionNumber = 1,
                ModelJson = "{\"kind\":\"other\"}",
                CreatedDate = At(22)
            });

            await db.AskJoobyAnalyticsTrainedModel.Where(w => w.Id == modelAId)
                .Set(s => s.ActiveVersionId, versionA2Id)
                .Set(s => s.Active, (byte)1)
                .UpdateAsync();

            var cacheGuid = Guid.NewGuid();

            await db.InsertAsync(new AdvancedAnalyticsCache
            {
                Guid = cacheGuid,
                TenantRegistryId = tenantId,
                Data = new byte[16],
                Rows = 2,
                Columns = 1,
                PackingVersion = 1,
                SizeBytes = 16,
                CreatedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddHours(1)
            });

            return new SeededAnalytics(modelId,
                await db.AskJoobyAnalyticsInstance.SingleAsync(w => w.Id == linkedId),
                await db.AskJoobyAnalyticsInstance.SingleAsync(w => w.Id == freeId),
                await db.AskJoobyAnalyticsTrainedModel.SingleAsync(w => w.Id == modelAId),
                await db.AskJoobyAnalyticsTrainedModel.SingleAsync(w => w.Id == modelBId),
                await db.AskJoobyAnalyticsTrainedModelVersion.SingleAsync(w => w.Id == versionA1Id),
                await db.AskJoobyAnalyticsTrainedModelVersion.SingleAsync(w => w.Id == versionA2Id),
                await db.AskJoobyAnalyticsTrainedModelVersion.SingleAsync(w => w.Id == versionB1Id),
                await db.AdvancedAnalyticsTrainingRun.SingleAsync(w => w.Id == runSucceededId),
                await db.AdvancedAnalyticsTrainingRun.SingleAsync(w => w.Id == runRunningId),
                await db.AdvancedAnalyticsTrainingRunTrial.SingleAsync(w => w.Id == trialFirstId),
                await db.AdvancedAnalyticsTrainingRunTrial.SingleAsync(w => w.Id == trialSecondId),
                cacheGuid);
        }
    }
}