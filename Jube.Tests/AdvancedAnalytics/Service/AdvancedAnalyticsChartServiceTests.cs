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
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using FluentAssertions;
using Jube.Service.Charts;
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Reactivity;
using Jube.Service.Repository.AdvancedAnalyticsOperation;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;

namespace Jube.Test.AdvancedAnalytics.Service
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsChartServiceTests(DatabaseFixture fx)
        : AdvancedAnalyticsServiceTestBase(fx)
    {
        private Task<AdvancedAnalyticsChartService> ChartsAsync(string? user)
        {
            return AdvancedAnalyticsChartService.CreateAsync(Db, user, TestLog.NoOp, Localizers,
                new NullServiceChangeBus(), TestLog.NoOp);
        }

        private static void AssertSafeExternalSvg(string svg)
        {
            var document = new XmlDocument();
            document.LoadXml(svg);
            document.DocumentElement!.LocalName.Should().Be("svg");
            svg.Should().NotContainEquivalentOf("<script");
            svg.Should().NotContainEquivalentOf("javascript:");
            svg.Should().NotContain("<style");
            svg.Should().Contain("class=\"jc");
        }

        private async Task<(AdvancedAnalyticsTrainingOutcomeDto Trained, int InstanceId)> TrainedAsync(
            int tenant, string user)
        {
            var instance = await NewInstanceAsync(tenant);
            var trained = await TrainAsync(instance.Id, UniqueName("Model"), 1, user);
            trained.Status.Should().Be(AdvancedAnalyticsStatus.Succeeded);

            return (trained, instance.Id);
        }

        [Fact]
        public async Task ChartsRefuseUsersWithoutTheAnalyticsPermissionAsync()
        {
            var (trained, _) = await TrainedAsync(TenantA, Permitted);
            var promoteOnly = await UserWithSpecsAsync(AdvancedAnalyticsPermissionSpecifications.PromoteModel);

            foreach (var user in new[] { Unpermitted, promoteOnly })
            {
                var charts = await ChartsAsync(user);
                var versionId = trained.TrainedModelVersionId!.Value;
                await Assert.ThrowsAsync<ForbiddenException>(() => charts.GetModelDiagramSvgAsync(versionId));
                await Assert.ThrowsAsync<ForbiddenException>(() => charts.GetCalibrationSvgAsync(versionId));
                await Assert.ThrowsAsync<ForbiddenException>(() => charts.GetRocSvgAsync(versionId));
            }

            await Assert.ThrowsAsync<NotAuthenticatedException>(() => ChartsAsync(null));
            await Assert.ThrowsAsync<NotAuthenticatedException>(() => ChartsAsync("ZzTestNoSuchUser"));
        }

        [Fact]
        public async Task ChartsRenderExternalStyleSvgForAnOwnVersionAsync()
        {
            var (trained, _) = await TrainedAsync(TenantA, Permitted);
            var charts = await ChartsAsync(Permitted);
            var versionId = trained.TrainedModelVersionId!.Value;

            var diagram = await charts.GetModelDiagramSvgAsync(versionId);
            var calibration = await charts.GetCalibrationSvgAsync(versionId);
            var roc = await charts.GetRocSvgAsync(versionId);

            foreach (var svg in new[] { diagram, calibration, roc })
            {
                AssertSafeExternalSvg(svg);
                svg.Should().NotContainEquivalentOf(AdvancedAnalyticsChartRenderer.EmptyMessage);
                svg.Should().NotContainEquivalentOf("accord");
            }

            diagram.Should().Contain("x0");
            (await charts.GetModelDiagramSvgAsync(versionId)).Should().Be(diagram);
        }

        [Fact]
        public async Task ChartsForForeignAndMissingVersionsAreIdenticalEmptyStatesAsync()
        {
            var (foreign, _) = await TrainedAsync(TenantB, OtherTenant);
            var charts = await ChartsAsync(Permitted);
            var foreignVersion = foreign.TrainedModelVersionId!.Value;

            (await charts.GetModelDiagramSvgAsync(foreignVersion)).Should()
                .Be(await charts.GetModelDiagramSvgAsync(int.MaxValue));
            (await charts.GetCalibrationSvgAsync(foreignVersion)).Should()
                .Be(await charts.GetCalibrationSvgAsync(int.MaxValue));
            (await charts.GetRocSvgAsync(foreignVersion)).Should().Be(await charts.GetRocSvgAsync(int.MaxValue));

            var empty = await charts.GetRocSvgAsync(int.MaxValue);
            AssertSafeExternalSvg(empty);
            empty.Should().Contain(AdvancedAnalyticsChartRenderer.EmptyMessage);

            var name = (await (await ModelServiceAsync(OtherTenant)).GetModelAsync(foreign.TrainedModelId!.Value))!
                .Name!;

            (await charts.GetModelDiagramSvgAsync(foreignVersion)).Should().NotContain(name);
        }

        [Fact]
        public async Task AnUnrecognisedStoredModelShowsTheMessageInsteadOfFailingAsync()
        {
            var (trained, _) = await TrainedAsync(TenantA, Permitted);

            await Db.AdvancedAnalyticsTrainedModelVersion.Where(w => w.Id == trained.TrainedModelVersionId)
                .Set(s => s.ModelJson, "{\"unknown\":true}").UpdateAsync();

            var charts = await ChartsAsync(Permitted);

            var svg = await charts.GetModelDiagramSvgAsync(trained.TrainedModelVersionId!.Value);

            AssertSafeExternalSvg(svg);
            svg.Should().NotContain(AdvancedAnalyticsChartRenderer.EmptyMessage);
        }

        [Fact]
        public async Task ACalibrationChartWithoutEvidenceIsAnEmptyStateAsync()
        {
            var (trained, _) = await TrainedAsync(TenantA, Permitted);

            await Db.AdvancedAnalyticsTrainedModelVersion.Where(w => w.Id == trained.TrainedModelVersionId)
                .Set(s => s.MetricsJson, (string?)null).UpdateAsync();

            var charts = await ChartsAsync(Permitted);

            var svg = await charts.GetCalibrationSvgAsync(trained.TrainedModelVersionId!.Value);

            svg.Should().Contain(AdvancedAnalyticsChartRenderer.EmptyMessage);
            AssertSafeExternalSvg(svg);
        }

        [Fact]
        public async Task ARocChartWithoutCurvePointsIsAnEmptyStateAsync()
        {
            var (trained, _) = await TrainedAsync(TenantA, Permitted);

            await Db.AdvancedAnalyticsTrainingRunCurvePoint
                .Where(w => w.AdvancedAnalyticsTrainingRunId == trained.RunId)
                .DeleteAsync();

            var charts = await ChartsAsync(Permitted);

            var svg = await charts.GetRocSvgAsync(trained.TrainedModelVersionId!.Value);

            svg.Should().Contain(AdvancedAnalyticsChartRenderer.EmptyMessage);
        }
    }
}