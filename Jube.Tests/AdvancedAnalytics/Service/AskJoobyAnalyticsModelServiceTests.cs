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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Reactivity.Models;
using Jube.Service.Repository.AdvancedAnalyticsOperation;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Newtonsoft.Json.Linq;
using Xunit;
using Jube.Service.Repository.AskJoobyAnalyticsModel.Models;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AskJoobyAnalyticsModelServiceTests(DatabaseFixture fx) : AdvancedAnalyticsServiceTestBase(fx)
    {
        private const string LogisticWarningCodesNoTest = "NoTestSet";

        private async Task<Setup> AllocatedAsync()
        {
            var modelId = await NewEntityAnalysisModelAsync(TenantA);
            var instance = await NewInstanceAsync(TenantA, modelId);

            return new Setup(instance.Id, modelId, UniqueName("Model"));
        }

        private async Task<Setup> UnallocatedAsync()
        {
            var instance = await NewInstanceAsync(TenantA);

            return new Setup(instance.Id, 0, UniqueName("Model"));
        }

        private async Task<AskJoobyAnalyticsModelVersionDto[]> ModelRowAsync(int trainedModelId)
        {
            var service = await ModelServiceAsync(Permitted);

            return [.. await service.ListVersionsAsync(trainedModelId)];
        }

        [Fact]
        public async Task ReadOperationsRefuseAnUnpermittedUserAsync()
        {
            var setup = await AllocatedAsync();
            var trained = await TrainAsync(setup.InstanceId, setup.ModelName);
            var service = await ModelServiceAsync(Unpermitted);

            var promoteOnly = await ModelServiceAsync(await UserWithSpecsAsync(
                AdvancedAnalyticsPermissionSpecifications.PromoteModel));

            foreach (var candidate in new[] { service, promoteOnly })
            {
                await Assert.ThrowsAsync<ForbiddenException>(() => candidate.ListModelsAsync());

                await Assert.ThrowsAsync<ForbiddenException>(() =>
                    candidate.GetModelAsync(trained.TrainedModelId!.Value));
                await Assert.ThrowsAsync<ForbiddenException>(() =>
                    candidate.ListVersionsAsync(trained.TrainedModelId!.Value));
                await Assert.ThrowsAsync<ForbiddenException>(() =>
                    candidate.GetVersionAsync(trained.TrainedModelVersionId!.Value));

                await Assert.ThrowsAsync<ForbiddenException>(() =>
                    candidate.CompareVersionsAsync(1, 2));

                await Assert.ThrowsAsync<ForbiddenException>(() =>
                    candidate.GetPromotionEvidenceAsync(trained.TrainedModelVersionId!.Value));
            }
        }

        [Fact]
        public async Task PromotionAndRollbackRefuseAUserWithoutThePromotePermissionAsync()
        {
            var setup = await AllocatedAsync();
            var trained = await TrainAsync(setup.InstanceId, setup.ModelName);
            var noPromote = await UserWithSpecsAsync(AdvancedAnalyticsPermissionSpecifications.Analytics,
                AdvancedAnalyticsPermissionSpecifications.Training);

            foreach (var user in new[] { Unpermitted, noPromote })
            {
                var service = await ModelServiceAsync(user);

                var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
                    service.PromoteVersionAsync(trained.TrainedModelId!.Value, trained.TrainedModelVersionId!.Value));

                ex.RequiredSpecifications.Should().Equal(AdvancedAnalyticsPermissionSpecifications.PromoteModel);

                await Assert.ThrowsAsync<ForbiddenException>(() =>
                    service.RollbackAsync(trained.TrainedModelId!.Value));
            }

            (await Db.AskJoobyAnalyticsTrainedModel.FirstAsync(w => w.Id == trained.TrainedModelId)).Active
                .Should().Be(0);
        }

        [Fact]
        public async Task ModelsAndVersionsAreListedWithHeadlineMetricsAndWarningsButNoPayloadAsync()
        {
            var setup = await AllocatedAsync();
            var first = await TrainAsync(setup.InstanceId, setup.ModelName);
            var second = await TrainAsync(setup.InstanceId, setup.ModelName, 2);
            var service = await ModelServiceAsync(Permitted);

            var listed = await service.ListModelsAsync(200, null, setup.InstanceId);
            var model = listed.Items.Should().ContainSingle().Subject;
            model.Id.Should().Be(first.TrainedModelId);
            model.Active.Should().BeFalse();
            model.ActiveVersionId.Should().BeNull();
            model.Versions.Should().BeNull();

            var detail = (await service.GetModelAsync(model.Id))!;
            detail.Versions!.Select(v => v.VersionNumber).Should().Equal(2, 1);
            detail.Versions.Should().OnlyContain(v => !v.IsActive && v.Evidence == null);
            var newest = detail.Versions![0];
            newest.Id.Should().Be(second.TrainedModelVersionId);
            newest.TestRocAuc.Should().Be(second.Result!.TestMetrics!.RocAuc);
            newest.TestBrierScore.Should().Be(second.Result.TestMetrics.BrierScore);
            newest.TestRows.Should().Be(second.Result.TestMetrics.Rows);
            newest.WarningCodes.Should().Equal(second.Result.Warnings.Select(w => w.Code));

            var version = (await service.GetVersionAsync(first.TrainedModelVersionId!.Value))!;
            version.Evidence.Should().NotBeNull();
            var versionEvidence =
                (Jube.Service.Repository.AskJoobyAnalyticsModel.Models.AdvancedAnalyticsLogisticEvidence)version
                    .Evidence!;
            versionEvidence.Seed.Should().Be(1);

            versionEvidence.Coefficients.Select(c => c.Name).Should()
                .Equal(first.Result!.Coefficients.Select(c => c.Name));

            typeof(AskJoobyAnalyticsModelVersionDto).GetProperties().Select(p => p.Name).Should()
                .NotContain(["ModelJson", "SnapshotJson"]);
        }

        [Fact]
        public async Task ModelPagingClampsTakeAndContinuesAfterAnIdAsync()
        {
            var setup = await UnallocatedAsync();
            var a = await TrainAsync(setup.InstanceId, UniqueName("P1"));
            var b = await TrainAsync(setup.InstanceId, UniqueName("P2"));
            var service = await ModelServiceAsync(Permitted);

            var firstPage = await service.ListModelsAsync(1, null, setup.InstanceId);
            firstPage.Items.Should().ContainSingle().Which.Id.Should().Be(a.TrainedModelId);
            var next = await service.ListModelsAsync(1, a.TrainedModelId, setup.InstanceId);
            next.Items.Should().ContainSingle().Which.Id.Should().Be(b.TrainedModelId);
            (await service.ListModelsAsync(10_000)).Items.Count.Should().BeLessThanOrEqualTo(200);
        }

        [Fact]
        public async Task TenantsCannotSeeOrPromoteEachOthersModelsAsync()
        {
            var setup = await AllocatedAsync();
            var trained = await TrainAsync(setup.InstanceId, setup.ModelName);
            var other = await ModelServiceAsync(OtherTenant);
            var modelId = trained.TrainedModelId!.Value;
            var versionId = trained.TrainedModelVersionId!.Value;

            (await other.ListModelsAsync(200)).Items.Should().NotContain(m => m.Id == modelId);
            (await other.GetModelAsync(modelId)).Should().BeNull();
            (await other.GetVersionAsync(versionId)).Should().BeNull();
            await Assert.ThrowsAsync<NotFoundException>(() => other.ListVersionsAsync(modelId));
            await Assert.ThrowsAsync<NotFoundException>(() => other.GetPromotionEvidenceAsync(versionId));
            await Assert.ThrowsAsync<NotFoundException>(() => other.PromoteVersionAsync(modelId, versionId));
            await Assert.ThrowsAsync<NotFoundException>(() => other.RollbackAsync(modelId));
            await Assert.ThrowsAsync<NotFoundException>(() => other.CompareVersionsAsync(versionId, versionId + 1));
        }

        [Fact]
        public async Task PromotionEvidenceCarriesExactlyWhatTheDecisionNeedsAsync()
        {
            var setup = await UnallocatedAsync();
            var trained = await TrainAsync(setup.InstanceId, setup.ModelName, 3);
            var service = await ModelServiceAsync(Permitted);

            var evidence = (AdvancedAnalyticsPromotionEvidenceDto)await service.GetPromotionEvidenceAsync(
                trained.TrainedModelVersionId!.Value);

            evidence.VersionId.Should().Be(trained.TrainedModelVersionId);
            evidence.VersionNumber.Should().Be(1);
            evidence.TrainedModelId.Should().Be(trained.TrainedModelId);
            evidence.ModelName.Should().Be(setup.ModelName);
            evidence.Seed.Should().Be(3);
            evidence.HasHeldOutTestSet.Should().BeTrue();
            evidence.SplitSizes.Should().Be(trained.Result!.SplitSizes);
            evidence.TestMetrics!.RocAuc.Should().Be(trained.Result.TestMetrics!.RocAuc);

            evidence.TestMetrics.ExpectedCalibrationError.Should()
                .Be(trained.Result.TestMetrics.ExpectedCalibrationError);

            evidence.TrainMetrics!.Rows.Should().Be(trained.Result.SplitSizes.Train);
            evidence.Calibration.Applied.Should().Be(trained.Result.Calibration.Applied);
            evidence.Calibration.Slope.Should().Be(trained.Result.Calibration.Slope);
            evidence.Fit.Converged.Should().BeTrue();
            evidence.Priors.BaseRate.Should().Be(trained.Result.Priors.BaseRate);
            evidence.Coefficients.Should().HaveCount(trained.Result.Coefficients.Length);
            evidence.Coefficients.Where(c => c.Kind != "Intercept").Should().Contain(c => c.PValue < 0.001);
            evidence.Warnings.Select(w => w.Code).Should().Equal(trained.Result.Warnings.Select(w => w.Code));
            evidence.InstanceAllocated.Should().BeFalse();
            evidence.IsActiveVersion.Should().BeFalse();
            evidence.ProducingRunId.Should().Be(trained.RunId);
        }

        [Fact]
        public async Task PromotionEvidenceExposesAMissingTestSetAndItsWarningAsync()
        {
            var setup = await UnallocatedAsync();
            var service = await TrainingServiceAsync(Permitted);
            var request = TrainingRequest(setup.InstanceId, setup.ModelName);
            request.TestFraction = 0;
            var trained = await service.TrainLogisticRegressionAsync(request);

            var evidence = (AdvancedAnalyticsPromotionEvidenceDto)await (await ModelServiceAsync(Permitted))
                .GetPromotionEvidenceAsync(trained.TrainedModelVersionId!.Value);

            evidence.HasHeldOutTestSet.Should().BeFalse();
            evidence.TestMetrics.Should().BeNull();
            evidence.Warnings.Should().Contain(w => w.Code == LogisticWarningCodesNoTest);
        }

        [Fact]
        public async Task CompareVersionsShowsFairnessAndTheMetricDifferencesAsync()
        {
            var setup = await UnallocatedAsync();
            var one = await TrainAsync(setup.InstanceId, setup.ModelName);
            var two = await TrainAsync(setup.InstanceId, setup.ModelName, 2);
            var three = await TrainAsync(setup.InstanceId, setup.ModelName);
            var service = await ModelServiceAsync(Permitted);

            var same = await service.CompareVersionsAsync(one.TrainedModelVersionId!.Value,
                three.TrainedModelVersionId!.Value);
            same.SameSeed.Should().BeTrue();
            same.SameSplitSizes.Should().BeTrue();
            same.SameVariables.Should().BeTrue();
            same.Metrics.Should().OnlyContain(m => m.Difference == 0 || m.Difference == null);
            same.VersionA.Evidence.Should().BeNull();

            var differ = await service.CompareVersionsAsync(one.TrainedModelVersionId.Value,
                two.TrainedModelVersionId!.Value);
            differ.SameSeed.Should().BeFalse();
            var auc = differ.Metrics.Single(m => m.Name == "TestRocAuc");
            auc.ValueA.Should().Be(one.Result!.TestMetrics!.RocAuc);
            auc.ValueB.Should().Be(two.Result!.TestMetrics!.RocAuc);
            auc.Difference.Should().BeApproximately(auc.ValueB!.Value - auc.ValueA!.Value, 1e-15);
            differ.VersionA.VersionNumber.Should().Be(1);
            differ.VersionB.VersionNumber.Should().Be(2);
        }

        [Fact]
        public async Task CompareNeedsTwoDistinctVersionsThatExistAsync()
        {
            var setup = await UnallocatedAsync();
            var one = await TrainAsync(setup.InstanceId, setup.ModelName);
            var service = await ModelServiceAsync(Permitted);
            var id = one.TrainedModelVersionId!.Value;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.CompareVersionsAsync(id, id));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "VersionIdsInvalid");
            await Assert.ThrowsAsync<NotFoundException>(() => service.CompareVersionsAsync(id, int.MaxValue - 1));
        }

        [Fact]
        public async Task PromotionRequiresTheInstanceToBeAllocatedAndChangesNothingWithoutItAsync()
        {
            var setup = await UnallocatedAsync();
            var trained = await TrainAsync(setup.InstanceId, setup.ModelName);
            var bus = new CapturingBus();
            var service = await ModelServiceAsync(Permitted, bus: bus);

            var ex = await Assert.ThrowsAsync<ConflictException>(() =>
                service.PromoteVersionAsync(trained.TrainedModelId!.Value, trained.TrainedModelVersionId!.Value));

            ex.Reason.Should().Be("InstanceNotAllocated");
            var model = await Db.AskJoobyAnalyticsTrainedModel.FirstAsync(w => w.Id == trained.TrainedModelId);
            model.Active.Should().Be(0);
            model.ActiveVersionId.Should().BeNull();
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task PromotionRefusesWhenTheAllocatedJubeModelHasSinceBeenDeletedAsync()
        {
            var setup = await AllocatedAsync();
            var trained = await TrainAsync(setup.InstanceId, setup.ModelName);

            await Db.EntityAnalysisModel.Where(w => w.Id == setup.EntityAnalysisModelId)
                .Set(s => s.Deleted, (byte)1).UpdateAsync();

            var service = await ModelServiceAsync(Permitted);

            var ex = await Assert.ThrowsAsync<ConflictException>(() =>
                service.PromoteVersionAsync(trained.TrainedModelId!.Value, trained.TrainedModelVersionId!.Value));

            ex.Reason.Should().Be("AllocatedModelUnavailable");
        }

        [Fact]
        public async Task PromotionMakesTheVersionLivePublishesOneEventAndRecordsTheEvidenceInTheAuditAsync()
        {
            var setup = await AllocatedAsync();
            var trained = await TrainAsync(setup.InstanceId, setup.ModelName, 7);
            var bus = new CapturingBus();
            var audit = new TestLog();
            var service = await ModelServiceAsync(Permitted, auditLog: audit, bus: bus);

            var result = await service.PromoteVersionAsync(trained.TrainedModelId!.Value,
                trained.TrainedModelVersionId!.Value);

            result.Changed.Should().BeTrue();
            result.ActiveVersionId.Should().Be(trained.TrainedModelVersionId);
            result.ActiveVersionNumber.Should().Be(1);
            result.PreviousActiveVersionId.Should().BeNull();
            result.EntityAnalysisModelId.Should().Be(setup.EntityAnalysisModelId);
            result.AskJoobyAnalyticsInstanceId.Should().Be(setup.InstanceId);

            var model = await Db.AskJoobyAnalyticsTrainedModel.FirstAsync(w => w.Id == trained.TrainedModelId);
            model.Active.Should().Be(1);
            model.ActiveVersionId.Should().Be(trained.TrainedModelVersionId);

            bus.Published.Should().ContainSingle();
            bus.Published[0].Kind.Should().Be(ServiceChangeKind.Updated);
            bus.Published[0].Area.Should().Be(AdvancedAnalyticsChangeAreas.TrainedModel);
            bus.Published[0].EntityId.Should().Be(trained.TrainedModelId);
            bus.Published[0].Version.Should().Be(model.Version);

            var line = audit.Entries.Select(e => e.Message)
                .Single(m => m.Contains("op=PromoteVersionEvidence"));

            line.Should().Contain($"actor={Permitted}").And.Contain($"model={trained.TrainedModelId}")
                .And.Contain($"version={trained.TrainedModelVersionId}")
                .And.Contain($"entityAnalysisModel={setup.EntityAnalysisModelId}");

            var json = JObject.Parse(
                line[(line.IndexOf("evidence=", StringComparison.Ordinal) + "evidence=".Length)..]);
            json["seed"]!.Value<int>().Should().Be(7);
            json["hasHeldOutTestSet"]!.Value<bool>().Should().BeTrue();
            json["testMetrics"]!["rocAuc"]!.Value<double>().Should().Be(trained.Result!.TestMetrics!.RocAuc!.Value);
            json["warnings"]!.Should().HaveCount(trained.Result.Warnings.Length);
            json["coefficients"]!.Should().NotBeEmpty();

            audit.Entries.Should()
                .Contain(e => e.Message.Contains("op=PromoteVersion ") && e.Message.Contains("outcome=ok"));
        }

        [Fact]
        public async Task TheServiceAppliesNoQualityGateItRecordsTheWarningsInsteadAsync()
        {
            var setup = await AllocatedAsync();
            var trainer = await TrainingServiceAsync(Permitted);
            var request = TrainingRequest(setup.InstanceId, setup.ModelName);
            request.TestFraction = 0;
            var trained = await trainer.TrainLogisticRegressionAsync(request);
            var audit = new TestLog();
            var service = await ModelServiceAsync(Permitted, auditLog: audit);

            var result = await service.PromoteVersionAsync(trained.TrainedModelId!.Value,
                trained.TrainedModelVersionId!.Value);

            result.Changed.Should().BeTrue();

            ((AdvancedAnalyticsPromotionEvidenceDto)result.Evidence).Warnings.Should()
                .Contain(w => w.Code == LogisticWarningCodesNoTest);

            audit.Entries.Single(e => e.Message.Contains("PromoteVersionEvidence")).Message
                .Should().Contain(LogisticWarningCodesNoTest);
        }

        [Fact]
        public async Task PromotingTheLiveVersionAgainChangesNothingAndPublishesNothingAsync()
        {
            var setup = await AllocatedAsync();
            var trained = await TrainAsync(setup.InstanceId, setup.ModelName);
            var bus = new CapturingBus();
            var audit = new TestLog();
            var service = await ModelServiceAsync(Permitted, auditLog: audit, bus: bus);
            await service.PromoteVersionAsync(trained.TrainedModelId!.Value, trained.TrainedModelVersionId!.Value);

            var again = await service.PromoteVersionAsync(trained.TrainedModelId.Value,
                trained.TrainedModelVersionId.Value);

            again.Changed.Should().BeFalse();
            bus.Published.Should().ContainSingle();
            audit.Entries.Count(e => e.Message.Contains("PromoteVersionEvidence")).Should().Be(1);
        }

        [Fact]
        public async Task ADeallocatedInstanceCanNoLongerPromoteAsync()
        {
            var setup = await AllocatedAsync();
            var trained = await TrainAsync(setup.InstanceId, setup.ModelName);
            await (await InstanceServiceAsync(Permitted)).DeallocateAsync(setup.InstanceId);
            var service = await ModelServiceAsync(Permitted);

            var ex = await Assert.ThrowsAsync<ConflictException>(() =>
                service.PromoteVersionAsync(trained.TrainedModelId!.Value, trained.TrainedModelVersionId!.Value));

            ex.Reason.Should().Be("InstanceNotAllocated");
        }

        [Fact]
        public async Task AVersionOfAnotherModelCannotBePromotedThroughThisOneAsync()
        {
            var setup = await AllocatedAsync();
            var one = await TrainAsync(setup.InstanceId, UniqueName("One"));
            var two = await TrainAsync(setup.InstanceId, UniqueName("Two"));
            var service = await ModelServiceAsync(Permitted);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.PromoteVersionAsync(one.TrainedModelId!.Value, two.TrainedModelVersionId!.Value));

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.PromoteVersionAsync(one.TrainedModelId!.Value, int.MaxValue - 1));

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.PromoteVersionAsync(int.MaxValue - 1, one.TrainedModelVersionId!.Value));
        }

        [Fact]
        public async Task PromotingASecondVersionReplacesTheFirstAsTheOnlyLiveOneAsync()
        {
            var setup = await AllocatedAsync();
            var one = await TrainAsync(setup.InstanceId, setup.ModelName);
            var two = await TrainAsync(setup.InstanceId, setup.ModelName, 2);
            var service = await ModelServiceAsync(Permitted);
            await service.PromoteVersionAsync(one.TrainedModelId!.Value, one.TrainedModelVersionId!.Value);

            var result = await service.PromoteVersionAsync(one.TrainedModelId.Value, two.TrainedModelVersionId!.Value);

            result.PreviousActiveVersionId.Should().Be(one.TrainedModelVersionId);
            var versions = await ModelRowAsync(one.TrainedModelId.Value);
            versions.Count(v => v.IsActive).Should().Be(1);
            versions.Single(v => v.IsActive).Id.Should().Be(two.TrainedModelVersionId);
            (await service.GetModelAsync(one.TrainedModelId.Value))!.ActiveVersionNumber.Should().Be(2);
        }

        [Fact]
        public async Task RollbackReturnsToTheNextOlderVersionOrToANamedOneAsync()
        {
            var setup = await AllocatedAsync();
            var v1 = await TrainAsync(setup.InstanceId, setup.ModelName);
            var v2 = await TrainAsync(setup.InstanceId, setup.ModelName, 2);
            var v3 = await TrainAsync(setup.InstanceId, setup.ModelName, 3);
            var bus = new CapturingBus();
            var audit = new TestLog();
            var service = await ModelServiceAsync(Permitted, auditLog: audit, bus: bus);
            var modelId = v1.TrainedModelId!.Value;
            await service.PromoteVersionAsync(modelId, v3.TrainedModelVersionId!.Value);
            bus.Published.Should().HaveCount(1);

            var back = await service.RollbackAsync(modelId);
            back.ActiveVersionId.Should().Be(v2.TrainedModelVersionId);
            back.PreviousActiveVersionId.Should().Be(v3.TrainedModelVersionId);
            bus.Published.Should().HaveCount(2);

            audit.Entries.Should().Contain(e => e.Message.Contains("op=RollbackEvidence")
                                                && e.Message.Contains($"version={v2.TrainedModelVersionId}"));

            var named = await service.RollbackAsync(modelId, v1.TrainedModelVersionId);
            named.ActiveVersionId.Should().Be(v1.TrainedModelVersionId);
            (await ModelRowAsync(modelId)).Count(v => v.IsActive).Should().Be(1);
        }

        [Fact]
        public async Task RollbackNeedsALiveVersionAnEarlierOneAndAnAllocationAsync()
        {
            var setup = await AllocatedAsync();
            var v1 = await TrainAsync(setup.InstanceId, setup.ModelName);
            var modelId = v1.TrainedModelId!.Value;
            var service = await ModelServiceAsync(Permitted);

            (await Assert.ThrowsAsync<ConflictException>(() => service.RollbackAsync(modelId))).Reason
                .Should().Be("ModelHasNoActiveVersion");

            await service.PromoteVersionAsync(modelId, v1.TrainedModelVersionId!.Value);

            (await Assert.ThrowsAsync<ConflictException>(() => service.RollbackAsync(modelId))).Reason
                .Should().Be("NoEarlierVersion");

            var v2 = await TrainAsync(setup.InstanceId, setup.ModelName, 2);
            await service.PromoteVersionAsync(modelId, v2.TrainedModelVersionId!.Value);
            await (await InstanceServiceAsync(Permitted)).DeallocateAsync(setup.InstanceId);

            (await Assert.ThrowsAsync<ConflictException>(() => service.RollbackAsync(modelId))).Reason
                .Should().Be("InstanceNotAllocated");

            var foreign = await NewInstanceAsync(TenantA);
            var other = await TrainAsync(foreign.Id, UniqueName("Other"));

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.RollbackAsync(modelId, other.TrainedModelVersionId));
        }

        [Fact]
        public async Task TwoRacingPromotionsLeaveExactlyOneLiveVersionAsync()
        {
            var setup = await AllocatedAsync();
            var v1 = await TrainAsync(setup.InstanceId, setup.ModelName);
            var v2 = await TrainAsync(setup.InstanceId, setup.ModelName, 2);
            var modelId = v1.TrainedModelId!.Value;

            for (var round = 0; round < 6; round++)
            {
                await using var dbA = Fixture.GetDbContext();
                await using var dbB = Fixture.GetDbContext();
                var bus = new CapturingBus();
                var serviceA = await ModelServiceAsync(Permitted, db: dbA, bus: bus);
                var serviceB = await ModelServiceAsync(Permitted, db: dbB, bus: bus);
                var first = round % 2 == 0 ? v1 : v2;
                var second = round % 2 == 0 ? v2 : v1;

                var results = await Task.WhenAll(
                    Task.Run(() => serviceA.PromoteVersionAsync(modelId, first.TrainedModelVersionId!.Value)),
                    Task.Run(() => serviceB.PromoteVersionAsync(modelId, second.TrainedModelVersionId!.Value)));

                results.Should().HaveCount(2);
                var versions = await ModelRowAsync(modelId);
                versions.Count(v => v.IsActive).Should().Be(1);
                var model = await Db.AskJoobyAnalyticsTrainedModel.FirstAsync(w => w.Id == modelId);
                model.Active.Should().Be(1);
                new[] { v1.TrainedModelVersionId, v2.TrainedModelVersionId }.Should().Contain(model.ActiveVersionId);
                versions.Single(v => v.IsActive).Id.Should().Be(model.ActiveVersionId);
            }
        }

        [Fact]
        public async Task RacingPromotionAndRollbackNeverLeaveAModelWithoutAConsistentLiveVersionAsync()
        {
            var setup = await AllocatedAsync();
            var v1 = await TrainAsync(setup.InstanceId, setup.ModelName);
            var v2 = await TrainAsync(setup.InstanceId, setup.ModelName, 2);
            var modelId = v1.TrainedModelId!.Value;
            await (await ModelServiceAsync(Permitted)).PromoteVersionAsync(modelId, v2.TrainedModelVersionId!.Value);

            await using var dbA = Fixture.GetDbContext();
            await using var dbB = Fixture.GetDbContext();
            var serviceA = await ModelServiceAsync(Permitted, db: dbA);
            var serviceB = await ModelServiceAsync(Permitted, db: dbB);

            await Task.WhenAll(
                Task.Run(() => serviceA.RollbackAsync(modelId)),
                Task.Run(() => serviceB.PromoteVersionAsync(modelId, v1.TrainedModelVersionId!.Value)));

            var versions = await ModelRowAsync(modelId);
            versions.Count(v => v.IsActive).Should().Be(1);
            (await Db.AskJoobyAnalyticsTrainedModel.FirstAsync(w => w.Id == modelId)).Active.Should().Be(1);
        }

        private sealed record Setup(int InstanceId, int EntityAnalysisModelId, string ModelName);
    }
}