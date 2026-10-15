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
using Jube.Engine.AdvancedAnalytics;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Reactivity.Models;
using Jube.Service.Repository.AdvancedAnalyticsOperation;
using Jube.Test.AdvancedAnalytics.Slice;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Newtonsoft.Json.Linq;
using Xunit;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;
using Jube.Engine.AdvancedAnalytics.Training.Models;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsTrainingServiceTests(DatabaseFixture fx)
        : AdvancedAnalyticsServiceTestBase(fx)
    {
        private async Task<int> InstanceAsync()
        {
            return (await NewInstanceAsync(TenantA)).Id;
        }

        private Task<int> RunCountAsync(int instanceId)
        {
            return Db.AdvancedAnalyticsTrainingRun.CountAsync(w => w.AdvancedAnalyticsInstanceId == instanceId);
        }

        [Fact]
        public async Task TrainingRecordsTheRunItsStepsMetricsVariablesAndCurvesAsync()
        {
            var instanceId = await InstanceAsync();
            var data = Dataset();

            var outcome = await TrainAsync(instanceId, UniqueName("Model"));

            outcome.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded);
            outcome.Error.Should().BeNull();
            outcome.Result.Should().NotBeNull();

            var run = await Db.AdvancedAnalyticsTrainingRun.FirstAsync(w => w.Id == outcome.RunId);
            run.Guid.Should().Be(outcome.RunGuid);
            run.StatusId.Should().Be(AdvancedAnalyticsStatus.Succeeded);
            run.KindId.Should().Be(AdvancedAnalyticsKind.Classifier);
            run.JobMode.Should().Be(AdvancedAnalyticsJobMode.Online);
            run.Seed.Should().Be(1);
            run.Progress.Should().Be(1);
            run.UserName.Should().Be(Permitted);
            run.TenantRegistryId.Should().Be(TenantA);
            run.StartedDate.Should().NotBeNull();
            run.CompletedDate.Should().NotBeNull();
            run.CompletedDate!.Value.Should().BeOnOrAfter(run.StartedDate!.Value);
            run.Error.Should().BeNull();

            var arguments = JObject.Parse(run.Arguments!);
            arguments["family"]!.Value<string>().Should().Be("LogisticRegression");
            arguments["rows"]!.Value<int>().Should().Be(data.Inputs.Length);
            arguments["seed"]!.Value<int>().Should().Be(1);
            arguments["variableNames"]!.Select(v => v.Value<string>()).Should().Equal(data.Names);
            run.Arguments.Should().NotContain(data.Inputs[0][0].ToString("R"));

            var steps = (await new AdvancedAnalyticsTrainingRunStepRepository(Db, TenantA, Permitted)
                .ListByRunIdAsync(run.Id)).ToList();

            steps.Select(s => s.Step).Should().Equal(
                AdvancedAnalyticsStep.Load, AdvancedAnalyticsStep.Clean, AdvancedAnalyticsStep.Split,
                AdvancedAnalyticsStep.Normalise, AdvancedAnalyticsStep.Fit, AdvancedAnalyticsStep.Validate,
                AdvancedAnalyticsStep.Serialise);

            steps.Should().OnlyContain(s => s.StatusId == AdvancedAnalyticsStatus.Succeeded);
            steps.Select(s => s.StepOrder).Should().BeInAscendingOrder();

            steps.First(s => s.Step == AdvancedAnalyticsStep.Load).DurationMilliseconds.Should()
                .BeGreaterThanOrEqualTo(0);
            steps.First(s => s.Step == AdvancedAnalyticsStep.Serialise).DurationMilliseconds.Should()
                .BeGreaterThanOrEqualTo(0);

            var metrics = (await new AdvancedAnalyticsTrainingRunMetricRepository(Db, TenantA, Permitted)
                .ListByRunIdAsync(run.Id)).ToList();

            var testAuc = metrics.Single(m => m.Name == "RocAuc" && m.Split == AdvancedAnalyticsSplit.Test);
            testAuc.Value.Should().BeApproximately(outcome.Result!.TestMetrics!.RocAuc!.Value, 1e-12);

            metrics.Should().Contain(m => m.Name == "Rows" && m.Split == AdvancedAnalyticsSplit.Train
                                                           && m.Value == outcome.Result.SplitSizes.Train);

            metrics.Should().Contain(m => m.Name == "Converged" && m.Value == 1);
            metrics.Should().Contain(m => m.Name == "PlattSlope" && m.Split == AdvancedAnalyticsSplit.Validation);
            metrics.Should().OnlyContain(m => m.Value == null || double.IsFinite(m.Value.Value));

            var variableRows = (await new AdvancedAnalyticsTrainingRunVariableRepository(Db, TenantA, Permitted)
                .ListByRunIdAsync(run.Id)).OrderBy(v => v.VariableSequence).ToList();

            variableRows.Select(v => v.Name).Should().Equal(data.Names);

            variableRows.Should()
                .OnlyContain(v => v.Minimum < v.Maximum && v.RowCount == outcome.Result.SplitSizes.Train);

            var curves = (await new AdvancedAnalyticsTrainingRunCurvePointRepository(Db, TenantA, Permitted)
                .ListByRunIdAsync(run.Id)).ToList();

            curves.Should()
                .Contain(c => c.Curve == AdvancedAnalyticsCurve.Roc && c.Split == AdvancedAnalyticsSplit.Test);
            curves.Should().Contain(c => c.Curve == AdvancedAnalyticsCurve.Calibration
                                         && c.Split == AdvancedAnalyticsSplit.Test);

            curves.Where(c => c.Curve == AdvancedAnalyticsCurve.Roc).Should().OnlyContain(c =>
                c.X >= 0 && c.X <= 1 && c.Y >= 0 && c.Y <= 1);
        }

        [Fact]
        public async Task TrainingStoresAnImmutableVersionUnderANewModelAndNeverActivatesItAsync()
        {
            var instanceId = await InstanceAsync();
            var name = UniqueName("Model");
            var service = await TrainingServiceAsync(Permitted);
            var request = TrainingRequest(instanceId, name);
            request.CorrelationKey = "corr-1";

            var outcome = await service.TrainLogisticRegressionAsync(request);

            outcome.Activated.Should().BeFalse();
            outcome.VersionNumber.Should().Be(1);

            var model = await Db.AdvancedAnalyticsTrainedModel.FirstAsync(w => w.Id == outcome.TrainedModelId);
            model.Guid.Should().Be(outcome.TrainedModelGuid!.Value);
            model.Name.Should().Be(name);
            model.KindId.Should().Be(AdvancedAnalyticsKind.Classifier);
            model.SourceId.Should().Be(AdvancedAnalyticsSource.Agent);
            model.CorrelationKey.Should().Be("corr-1");
            model.Active.Should().Be(0);
            model.ActiveVersionId.Should().BeNull();
            model.TenantRegistryId.Should().Be(TenantA);

            var version =
                await Db.AdvancedAnalyticsTrainedModelVersion.FirstAsync(w => w.Id == outcome.TrainedModelVersionId);
            version.VersionNumber.Should().Be(1);
            version.AdvancedAnalyticsTrainingRunId.Should().Be(outcome.RunId);
            version.ModelJson.Should().Be(outcome.Result!.SnapshotJson);
            var variablesJson = JArray.Parse(version.VariablesJson!);
            variablesJson.Select(v => v["name"]!.Value<string>()).Should().Equal(outcome.Result.Variables);

            variablesJson.Select(v => v["processingTypeId"]!.Value<int>())
                .Should().Equal(outcome.Result.Variables.Select((_, i) => i % 2 == 0 ? 1 : 5));

            var snapshot = JObject.Parse(version.ModelJson);

            snapshot["variablesInOrder"]!.Select(v => v.Value<string>())
                .Should().Equal(variablesJson.Select(v => v["name"]!.Value<string>()));

            JObject.Parse(version.CalibratorJson!)["method"]!.Value<string>().Should().Be("Platt");
            version.MetricsJson.Should().NotBeNullOrWhiteSpace();

            (await Db.AdvancedAnalyticsInstance.FirstAsync(w => w.Id == instanceId)).Active.Should().Be(0);
        }

        [Fact]
        public async Task TheStoredModelScoresExactlyAsItDidAtTrainingTimeAsync()
        {
            var instanceId = await InstanceAsync();
            var data = Dataset();

            var outcome = await TrainAsync(instanceId, UniqueName("Recall"));

            var version =
                await Db.AdvancedAnalyticsTrainedModelVersion.FirstAsync(w => w.Id == outcome.TrainedModelVersionId);

            var recall = LogisticRegressionRecall.FromSnapshot(version.ModelJson);
            var testRows = outcome.Result!.TestRows.Select(r => data.Inputs[r]).ToArray();
            recall.PredictProbabilities(testRows).Should().Equal(outcome.Result.TestPredictions.Probabilities);
        }

        [Fact]
        public async Task RetrainingUnderTheSameNameAddsAVersionToTheSameModelAsync()
        {
            var instanceId = await InstanceAsync();
            var name = UniqueName("Same");

            var first = await TrainAsync(instanceId, name);
            var second = await TrainAsync(instanceId, name.ToUpperInvariant(), 2);

            second.TrainedModelId.Should().Be(first.TrainedModelId);
            second.VersionNumber.Should().Be(2);
            second.TrainedModelVersionId.Should().NotBe(first.TrainedModelVersionId);

            (await Db.AdvancedAnalyticsTrainedModel.CountAsync(w => w.AdvancedAnalyticsInstanceId == instanceId))
                .Should().Be(1);

            var firstVersion = await Db.AdvancedAnalyticsTrainedModelVersion
                .FirstAsync(w => w.Id == first.TrainedModelVersionId);

            firstVersion.ModelJson.Should().Be(first.Result!.SnapshotJson);
        }

        [Fact]
        public async Task SameInputsAndSeedGiveIdenticalEvidenceInDistinctVersionsAsync()
        {
            var instanceId = await InstanceAsync();

            var first = await TrainAsync(instanceId, UniqueName("Det"), 5);
            var second = await TrainAsync(instanceId, UniqueName("Det"), 5);

            second.Result!.SnapshotJson.Should().Be(first.Result!.SnapshotJson);
            second.TrainedModelVersionId.Should().NotBe(first.TrainedModelVersionId);
        }

        [Fact]
        public async Task DifferentModelNamesInTheSameInstanceAreDifferentModelsAsync()
        {
            var instanceId = await InstanceAsync();

            var one = await TrainAsync(instanceId, UniqueName("A"));
            var two = await TrainAsync(instanceId, UniqueName("B"));

            two.TrainedModelId.Should().NotBe(one.TrainedModelId);
            one.VersionNumber.Should().Be(1);
            two.VersionNumber.Should().Be(1);
        }

        [Fact]
        public async Task TrainingRefusesUsersWithoutTheTrainingPermissionAndRecordsNothingAsync()
        {
            var instanceId = await InstanceAsync();

            var analyticsOnly = await UserWithSpecsAsync(AdvancedAnalyticsPermissionSpecifications.Analytics,
                AdvancedAnalyticsPermissionSpecifications.PromoteModel);

            foreach (var user in new[] { Unpermitted, analyticsOnly })
            {
                var service = await TrainingServiceAsync(user);

                var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
                    service.TrainLogisticRegressionAsync(TrainingRequest(instanceId, UniqueName("Denied"))));

                ex.RequiredSpecifications.Should().Equal(AdvancedAnalyticsPermissionSpecifications.Training);
            }

            (await RunCountAsync(instanceId)).Should().Be(0);
        }

        [Fact]
        public async Task AnotherTenantCannotTrainIntoAnInstanceItDoesNotOwnAsync()
        {
            var instanceId = await InstanceAsync();
            var service = await TrainingServiceAsync(OtherTenant);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.TrainLogisticRegressionAsync(TrainingRequest(instanceId, UniqueName("Foreign"))));

            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "InstanceNotFound");
            (await RunCountAsync(instanceId)).Should().Be(0);
        }

        [Fact]
        public async Task ADeletedOrMissingInstanceIsRefusedAsync()
        {
            var instance = await NewInstanceAsync(TenantA);
            await new AdvancedAnalyticsInstanceRepository(Db, TenantA, "ZzTestUser").DeleteAsync(instance.Id);
            var service = await TrainingServiceAsync(Permitted);

            foreach (var id in new[] { instance.Id, int.MaxValue - 1, 0 })
            {
                await Assert.ThrowsAsync<DtoValidationException>(() =>
                    service.TrainLogisticRegressionAsync(TrainingRequest(id, UniqueName("Gone"))));
            }
        }

        private static object[] Case(string code, Action<AdvancedAnalyticsLogisticRegressionTrainingDto> corrupt)
        {
            return [code, corrupt];
        }

        public static IEnumerable<object[]> InvalidRequests()
        {
            yield return Case("ModelNameNotEmpty", r => r.ModelName = " ");
            yield return Case("ModelNameMaximumLength", r => r.ModelName = new string('m', 257));
            yield return Case("CorrelationKeyMaximumLength", r => r.CorrelationKey = new string('k', 257));
            yield return Case("InputsNotEmpty", r => r.Inputs = []);
            yield return Case("VariablesNotEmpty", r => r.VariableNames = []);
            yield return Case("VariableNameBlank", r => r.VariableNames![0] = "");
            yield return Case("VariableNamesNotUnique", r => r.VariableNames![1] = r.VariableNames[0]);
            yield return Case("RowLengthMismatch", r => r.Inputs![3] = [1.0]);
            yield return Case("LabelsLengthMismatch", r => r.Labels = r.Labels![1..]);
            yield return Case("LabelsNotBinary", r => r.Labels![2] = 2);
            yield return Case("ValidationFractionRange", r => r.ValidationFraction = 1);
            yield return Case("TestFractionRange", r => r.TestFraction = -0.1);

            yield return Case("FractionsTooLarge", r =>
            {
                r.ValidationFraction = 0.5;
                r.TestFraction = 0.5;
            });

            yield return Case("ProcessingTypeIdsRequired", r => r.ProcessingTypeIds = null);
            yield return Case("ProcessingTypeIdsMismatch", r => r.ProcessingTypeIds = [1]);
            yield return Case("ProcessingTypeIdsInvalid", r => r.ProcessingTypeIds![0] = 8);
            yield return Case("RidgeInvalid", r => r.RidgeLambda = -1);
            yield return Case("RidgeInvalid", r => r.RidgeLambda = double.NaN);
            yield return Case("BaseRateInvalid", r => r.BaseRateOverride = 1);
            yield return Case("ThresholdInvalid", r => r.DecisionThreshold = 1.5);
            yield return Case("IterationsInvalid", r => r.MaximumIterations = 0);
            yield return Case("ToleranceInvalid", r => r.Tolerance = 0);
            yield return Case("CostsBothOrNeither", r => r.CostFalseNegative = 5);

            yield return Case("CostsInvalid", r =>
            {
                r.CostFalseNegative = 5;
                r.CostFalsePositive = -1;
            });
        }

        [Theory]
        [MemberData(nameof(InvalidRequests))]
        public async Task InvalidRequestsAreRejectedBeforeAnyRunIsCreatedAsync(string code,
            Action<AdvancedAnalyticsLogisticRegressionTrainingDto> corrupt)
        {
            var instanceId = await InstanceAsync();
            var request = TrainingRequest(instanceId, UniqueName("Bad"), SliceDataCopy());
            corrupt(request);
            var service = await TrainingServiceAsync(Permitted);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.TrainLogisticRegressionAsync(request));

            ex.Result.Errors.Should().Contain(e => e.ErrorCode == code);
            (await RunCountAsync(instanceId)).Should().Be(0);
        }

        private static SliceDataset SliceDataCopy()
        {
            var data = Dataset();

            return new SliceDataset([.. data.Inputs.Select(r => (double[])r.Clone())],
                (int[])data.Labels.Clone(), (string[])data.Names.Clone());
        }

        [Fact]
        public async Task ATrainerRefusalIsRecordedAsFailedWithItsReasonAndStoresNoModelAsync()
        {
            var instanceId = await InstanceAsync();
            var data = Dataset();

            var request = TrainingRequest(instanceId, UniqueName("OneClass"),
                new SliceDataset(data.Inputs, new int[data.Labels.Length], data.Names));

            var bus = new CapturingBus();
            var service = await TrainingServiceAsync(Permitted, bus: bus);

            var outcome = await service.TrainLogisticRegressionAsync(request);

            outcome.Status.Should().Be(AdvancedAnalyticsStatus.Failed);
            outcome.Error.Should().Contain("classes");
            outcome.TrainedModelId.Should().BeNull();
            outcome.TrainedModelVersionId.Should().BeNull();
            outcome.Result.Should().BeNull();
            outcome.Activated.Should().BeFalse();

            var run = await Db.AdvancedAnalyticsTrainingRun.FirstAsync(w => w.Id == outcome.RunId);
            run.StatusId.Should().Be(AdvancedAnalyticsStatus.Failed);
            run.Error.Should().Contain("classes");
            run.StartedDate.Should().NotBeNull();
            run.CompletedDate.Should().NotBeNull();

            await AssertNoModelStoredAsync(instanceId);
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task AnInvalidOperationFromTheTrainerIsAlsoAFailedRunAsync()
        {
            var instanceId = await InstanceAsync();
            var service = await TrainingServiceAsync(Permitted);
            service.Trainer = (_, _, _, _, _) => throw new InvalidOperationException("The fit diverged.");

            var outcome = await service.TrainLogisticRegressionAsync(TrainingRequest(instanceId, UniqueName("Div")));

            outcome.Status.Should().Be(AdvancedAnalyticsStatus.Failed);
            outcome.Error.Should().Be("The fit diverged.");
            await AssertNoModelStoredAsync(instanceId);
        }

        [Fact]
        public async Task AnUnexpectedTrainerCrashIsRecordedGenericallyAndStillPropagatesAsync()
        {
            var instanceId = await InstanceAsync();
            var log = new TestLog();
            var service = await TrainingServiceAsync(Permitted, log);
            service.Trainer = (_, _, _, _, _) => throw new NullReferenceException("secret internals");

            await Assert.ThrowsAsync<NullReferenceException>(() =>
                service.TrainLogisticRegressionAsync(TrainingRequest(instanceId, UniqueName("Crash"))));

            var run = await Db.AdvancedAnalyticsTrainingRun.FirstAsync(w =>
                w.AdvancedAnalyticsInstanceId == instanceId);

            run.StatusId.Should().Be(AdvancedAnalyticsStatus.Failed);
            run.Error.Should().NotContain("secret internals");
            log.Entries.Should().Contain(e => e.Level == "ERROR" && e.Exception is NullReferenceException);
            await AssertNoModelStoredAsync(instanceId);
        }

        [Fact]
        public async Task ACancelledRunIsRecordedAsCancelledStoresNoModelAndPropagatesAsync()
        {
            var instanceId = await InstanceAsync();
            var bus = new CapturingBus();
            var service = await TrainingServiceAsync(Permitted, bus: bus);
            using var cts = new CancellationTokenSource();

            service.Trainer = (_, _, _, _, token) =>
            {
#pragma warning disable VSTHRD103, AsyncFixer02
                cts.Cancel();
#pragma warning restore VSTHRD103, AsyncFixer02
                token.ThrowIfCancellationRequested();
                throw new InvalidOperationException("unreachable");
            };

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.TrainLogisticRegressionAsync(TrainingRequest(instanceId, UniqueName("Cancel")), cts.Token));

            var run = await Db.AdvancedAnalyticsTrainingRun.FirstAsync(w =>
                w.AdvancedAnalyticsInstanceId == instanceId);

            run.StatusId.Should().Be(AdvancedAnalyticsStatus.Cancelled);
            run.Error.Should().NotBeNullOrWhiteSpace();
            run.CompletedDate.Should().NotBeNull();
            await AssertNoModelStoredAsync(instanceId);
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task ATokenCancelledBeforeAnythingStartsCreatesNoRunAsync()
        {
            var instanceId = await InstanceAsync();
            var service = await TrainingServiceAsync(Permitted);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.TrainLogisticRegressionAsync(TrainingRequest(instanceId, UniqueName("Pre")), cts.Token));

            (await RunCountAsync(instanceId)).Should().Be(0);
        }

        [Fact]
        public async Task AFailedRunDoesNotDisturbAnEarlierSuccessfulVersionAsync()
        {
            var instanceId = await InstanceAsync();
            var name = UniqueName("Keep");
            var good = await TrainAsync(instanceId, name);
            var service = await TrainingServiceAsync(Permitted);
            service.Trainer = (_, _, _, _, _) => throw new ArgumentException("nope");

            var failed = await service.TrainLogisticRegressionAsync(TrainingRequest(instanceId, name));

            failed.Status.Should().Be(AdvancedAnalyticsStatus.Failed);

            (await Db.AdvancedAnalyticsTrainedModelVersion.CountAsync(w =>
                w.AdvancedAnalyticsTrainedModelId == good.TrainedModelId)).Should().Be(1);
        }

        [Fact]
        public async Task TrainingPublishesCreatedForANewModelAndUpdatedForANewVersionAsync()
        {
            var instanceId = await InstanceAsync();
            var bus = new CapturingBus();
            var service = await TrainingServiceAsync(Permitted, bus: bus);
            var name = UniqueName("Events");

            var first = await service.TrainLogisticRegressionAsync(TrainingRequest(instanceId, name));
            var second = await service.TrainLogisticRegressionAsync(TrainingRequest(instanceId, name));

            bus.Published.Select(e => e.Kind).Should().Equal(ServiceChangeKind.Created, ServiceChangeKind.Updated);

            bus.Published.Should().OnlyContain(e =>
                e.Area == AdvancedAnalyticsChangeAreas.TrainedModel && e.EntityId == first.TrainedModelId);

            bus.Published.Select(e => e.Version).Should().Equal(1, second.VersionNumber);
        }

        [Fact]
        public async Task TrainingWritesOneAuditLineAndNoWarningsAsync()
        {
            var instanceId = await InstanceAsync();
            var audit = new TestLog();
            var log = new TestLog();
            var service = await TrainingServiceAsync(Permitted, log, audit);

            await service.TrainLogisticRegressionAsync(TrainingRequest(instanceId, UniqueName("Audit")));

            audit.Entries.Should().ContainSingle().Which.Message.Should()
                .Contain("op=TrainLogisticRegression").And.Contain("outcome=ok");

            log.Entries.Should().NotContain(e => e.Level == "WARN" || e.Level == "ERROR");
        }

        [Fact]
        public async Task TheDatabaseTraceRecordsAStepItsMetricsAndItsDurationAsync()
        {
            var instanceId = await InstanceAsync();

            var run = await new AdvancedAnalyticsTrainingRunRepository(Db, TenantA, "ZzTestUser")
                .InsertAsync(new AdvancedAnalyticsTrainingRun
                {
                    AdvancedAnalyticsInstanceId = instanceId,
                    KindId = AdvancedAnalyticsKind.Classifier,
                    JobMode = AdvancedAnalyticsJobMode.Online
                });

            var trace = new AdvancedAnalyticsDatabaseTrainingTrace(Db, TenantA, "ZzTestUser");

            await trace.AppendAsync(new AdvancedAnalyticsTrainingTraceEntry(run.Guid, 5, "Fit",
                new Dictionary<string, double>
                {
                    [AdvancedAnalyticsDatabaseTrainingTrace.DurationKey] = 12.5,
                    ["Test.Auc"] = 0.75,
                    ["Validation.Loss"] = 0.4,
                    ["Bare"] = 3,
                    ["Test.Bad"] = double.NaN
                }));

            var step = (await new AdvancedAnalyticsTrainingRunStepRepository(Db, TenantA, "ZzTestUser")
                .ListByRunIdAsync(run.Id)).Single();

            step.Step.Should().Be(AdvancedAnalyticsStep.Fit);
            step.StepOrder.Should().Be(5);
            step.DurationMilliseconds.Should().Be(12.5);
            step.StatusId.Should().Be(AdvancedAnalyticsStatus.Succeeded);
            step.StartedDate.Should().NotBeNull();

            var metrics = (await new AdvancedAnalyticsTrainingRunMetricRepository(Db, TenantA, "ZzTestUser")
                .ListByRunIdAsync(run.Id)).ToList();
            metrics.Should().HaveCount(4);
            metrics.Should().NotContain(m => m.Name == AdvancedAnalyticsDatabaseTrainingTrace.DurationKey);
            metrics.Single(m => m.Name == "Auc").Split.Should().Be(AdvancedAnalyticsSplit.Test);
            metrics.Single(m => m.Name == "Loss").Split.Should().Be(AdvancedAnalyticsSplit.Validation);
            metrics.Single(m => m.Name == "Bare").Split.Should().Be(AdvancedAnalyticsSplit.Train);
            metrics.Single(m => m.Name == "Bad").Value.Should().BeNull();
        }

        [Fact]
        public async Task TheDatabaseTraceTurnsANonStepEntryIntoAnIterationAsync()
        {
            var instanceId = await InstanceAsync();

            var run = await new AdvancedAnalyticsTrainingRunRepository(Db, TenantA, "ZzTestUser")
                .InsertAsync(new AdvancedAnalyticsTrainingRun
                {
                    AdvancedAnalyticsInstanceId = instanceId,
                    KindId = AdvancedAnalyticsKind.Classifier,
                    JobMode = AdvancedAnalyticsJobMode.Online
                });

            var trace = new AdvancedAnalyticsDatabaseTrainingTrace(Db, TenantA, "ZzTestUser");

            await trace.AppendAsync(new AdvancedAnalyticsTrainingTraceEntry(run.Guid, 3, "epoch",
                new Dictionary<string, double> { ["trainLoss"] = 0.5, ["validationLoss"] = double.PositiveInfinity }));

            var iteration = (await new AdvancedAnalyticsTrainingRunIterationRepository(Db, TenantA, "ZzTestUser")
                .ListByRunIdAsync(run.Id)).Single();
            iteration.IterationNumber.Should().Be(3);
            iteration.TrainLoss.Should().Be(0.5);
            iteration.ValidationLoss.Should().BeNull();
            iteration.Score.Should().BeNull();
        }

        [Fact]
        public async Task TheDatabaseTraceRefusesARunItCannotSeeAsync()
        {
            var instanceId = await InstanceAsync();

            var run = await new AdvancedAnalyticsTrainingRunRepository(Db, TenantA, "ZzTestUser")
                .InsertAsync(new AdvancedAnalyticsTrainingRun
                {
                    AdvancedAnalyticsInstanceId = instanceId,
                    KindId = AdvancedAnalyticsKind.Classifier,
                    JobMode = AdvancedAnalyticsJobMode.Online
                });

            var foreign = new AdvancedAnalyticsDatabaseTrainingTrace(Db, TenantB, "ZzTestUser");
            var own = new AdvancedAnalyticsDatabaseTrainingTrace(Db, TenantA, "ZzTestUser");

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                foreign.AppendAsync(
                    new AdvancedAnalyticsTrainingTraceEntry(run.Guid, 1, "Load", new Dictionary<string, double>())));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                own.AppendAsync(new AdvancedAnalyticsTrainingTraceEntry(Guid.NewGuid(), 1, "Load",
                    new Dictionary<string, double>())));
        }

        private async Task AssertNoModelStoredAsync(int instanceId)
        {
            (await Db.AdvancedAnalyticsTrainedModel.CountAsync(w => w.AdvancedAnalyticsInstanceId == instanceId))
                .Should().Be(0);

            (await Db.AdvancedAnalyticsTrainedModelVersion.CountAsync(w =>
                    Db.AdvancedAnalyticsTrainingRun.Any(r => r.Id == w.AdvancedAnalyticsTrainingRunId
                                                             && r.AdvancedAnalyticsInstanceId == instanceId)))
                .Should().Be(0);
        }
    }
}