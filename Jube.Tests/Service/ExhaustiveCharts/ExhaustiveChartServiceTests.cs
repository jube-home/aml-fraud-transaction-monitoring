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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Service.Charts;
using Jube.Service.Exceptions;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.ExhaustiveCharts
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ExhaustiveChartServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> histogramIds = [];
        private readonly List<int> modelIds = [];
        private readonly List<int> predictedActualIds = [];
        private readonly List<int> promotedIds = [];
        private readonly List<int> rocIds = [];
        private readonly List<int> searchIds = [];
        private readonly List<int> trialIds = [];
        private readonly List<int> variableIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<ExhaustiveSearchInstanceVariableHistogram>()
                .Where(w => histogramIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<ExhaustiveSearchInstanceVariable>()
                .Where(w => variableIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<ExhaustiveSearchInstancePromotedTrialInstancePredictedActual>()
                .Where(w => predictedActualIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<ExhaustiveSearchInstancePromotedTrialInstanceRoc>()
                .Where(w => rocIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<ExhaustiveSearchInstancePromotedTrialInstance>()
                .Where(w => promotedIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<ExhaustiveSearchInstanceTrialInstance>()
                .Where(w => trialIds.Contains(w.Id)).DeleteAsync();

            foreach (var id in searchIds)
            {
                await dbContext.GetTable<ExhaustiveSearchInstanceVersion>()
                    .Where(w => w.ExhaustiveSearchInstanceId == id).DeleteAsync();

                await dbContext.GetTable<Data.Poco.ExhaustiveSearchInstance>().Where(w => w.Id == id)
                    .DeleteAsync();
            }

            foreach (var modelId in modelIds)
            {
                await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                    .DeleteAsync();

                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private async Task<int> SeedSearchAsync(DbContext dbContext, string createdUser)
        {
            var model = await new EntityAnalysisModelRepository(dbContext, createdUser).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0
                });
            modelIds.Add(model.Id);

            var search = await new ExhaustiveSearchInstanceRepository(dbContext, createdUser).InsertAsync(
                new Data.Poco.ExhaustiveSearchInstance
                {
                    Name = $"{DatabaseFixture.Prefix}Search{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    EntityAnalysisModelId = model.Id,
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    StatusId = 0
                });
            searchIds.Add(search.Id);

            return search.Id;
        }

        private async Task<int> SeedPromotedAsync(DbContext dbContext, int searchId, double score, DateTime createdDate,
            int tp = 0, int tn = 0, int fp = 0, int fn = 0)
        {
            var trial = await new ExhaustiveSearchInstanceTrialInstanceRepository(dbContext).InsertAsync(
                new ExhaustiveSearchInstanceTrialInstance
                {
                    ExhaustiveSearchInstanceId = searchId,
                    CreatedDate = createdDate
                });
            trialIds.Add(trial.Id);

            var promoted = await new ExhaustiveSearchInstancePromotedTrialInstanceRepository(dbContext).InsertAsync(
                new ExhaustiveSearchInstancePromotedTrialInstance
                {
                    ExhaustiveSearchInstanceTrialInstanceId = trial.Id,
                    Active = 1,
                    Score = score,
                    TruePositive = tp,
                    TrueNegative = tn,
                    FalsePositive = fp,
                    FalseNegative = fn,
                    Json = "{}",
                    CreatedDate = createdDate
                });
            promotedIds.Add(promoted.Id);

            await dbContext.GetTable<ExhaustiveSearchInstancePromotedTrialInstance>()
                .Where(w => w.Id == promoted.Id).Set(s => s.CreatedDate, createdDate).UpdateAsync();

            return trial.Id;
        }

        private async Task SeedRocAsync(DbContext dbContext, int trialId,
            params (double score, int tp, int tn, int fp, int fn)[] points)
        {
            foreach (var (score, tp, tn, fp, fn) in points)
            {
                rocIds.Add(await dbContext.InsertWithInt32IdentityAsync(
                    new ExhaustiveSearchInstancePromotedTrialInstanceRoc
                    {
                        ExhaustiveSearchInstanceTrialInstanceId = trialId,
                        Score = score,
                        TruePositive = tp,
                        TrueNegative = tn,
                        FalsePositive = fp,
                        FalseNegative = fn,
                        Threshold = score,
                        Deleted = 0
                    }));
            }
        }

        private async Task SeedPredictedActualAsync(DbContext dbContext, int trialId,
            params (double predicted, double actual)[] rows)
        {
            foreach (var (predicted, actual) in rows)
            {
                predictedActualIds.Add(await dbContext.InsertWithInt32IdentityAsync(
                    new ExhaustiveSearchInstancePromotedTrialInstancePredictedActual
                    {
                        ExhaustiveSearchInstanceTrialInstanceId = trialId,
                        Predicted = predicted,
                        Actual = actual
                    }));
            }
        }

        private async Task<int> SeedVariableAsync(DbContext dbContext, int searchId, string name,
            params int[] frequencies)
        {
            var variableId = await dbContext.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstanceVariable
            {
                ExhaustiveSearchInstanceId = searchId,
                Name = name,
                Mean = 1.5,
                StandardDeviation = 2.5
            });
            variableIds.Add(variableId);
            var sequence = 0;

            foreach (var frequency in frequencies)
            {
                histogramIds.Add(await dbContext.InsertWithInt32IdentityAsync(
                    new ExhaustiveSearchInstanceVariableHistogram
                    {
                        ExhaustiveSearchInstanceVariableId = variableId,
                        BinSequence = sequence,
                        BinRangeStart = sequence,
                        BinRangeEnd = sequence + 1,
                        Frequency = frequency
                    }));

                sequence++;
            }

            return variableId;
        }

        private static async Task AssertForbiddenAsync(Func<Task> action)
        {
            var ex = await Assert.ThrowsAnyAsync<ServiceException>(action);
            ex.Code.Should().Be("PermissionDenied");
        }

        private static XmlDocument AssertSafeSvg(string svg)
        {
            var document = new XmlDocument { XmlResolver = null };
            document.LoadXml(svg);
            document.DocumentElement!.LocalName.Should().Be("svg");
            document.GetElementsByTagName("script").Count.Should().Be(0);
            svg.Should().NotContainEquivalentOf("<script");
            svg.Should().NotContainEquivalentOf("javascript:");
            svg.Should().NotContainEquivalentOf("foreignObject");
            Regex.IsMatch(svg, @"\son[a-z]+\s*=", RegexOptions.IgnoreCase).Should().BeFalse();
            svg.Should().NotContain("<style");
            svg.Should().NotContain("style=");
            document.DocumentElement!.GetAttribute("class").Split(' ').Should().Contain("jc");
            document.DocumentElement.GetAttribute("aria-labelledby").Should().StartWith("ex-");

            return document;
        }

        private static IEnumerable<string> TextOf(string svg)
        {
            var document = AssertSafeSvg(svg);

            return
            [
                .. document.GetElementsByTagName("text").Cast<XmlNode>().Select(n => n.InnerText),
                .. document.GetElementsByTagName("title").Cast<XmlNode>().Select(n => n.InnerText)
            ];
        }

        private static void AssertEmptyState(string svg)
        {
            TextOf(svg).Should().Contain(t => t.Contains(ExhaustiveChartRenderer.EmptyMessage));
        }

        [Fact]
        public async Task EveryChartServiceRefusesAnUnauthenticatedCallerAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var userName in new[] { null, "", "   ", fx.Seed.UnknownUser, fx.Seed.UserNoTenant })
            {
                var ex1 = await Assert.ThrowsAnyAsync<ServiceException>(() => ExhaustiveRocChartService.CreateAsync(
                    dbContext, userName, TestLog.NoOp, localizers, new NullServiceChangeBus()));
                ex1.Code.Should().Be("NotAuthenticated");

                var ex2 = await Assert.ThrowsAnyAsync<ServiceException>(() =>
                    ExhaustiveVariableHistogramChartService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers,
                        new NullServiceChangeBus()));
                ex2.Code.Should().Be("NotAuthenticated");
            }
        }

        [Fact]
        public async Task EveryChartServiceDeniesACallerWithoutThePermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithoutPermission;
            var bus = new NullServiceChangeBus();

            await AssertForbiddenAsync(async () =>
                await (await ExhaustiveLearningCurveChartService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                    bus)).GetSvgAsync(1));
            await AssertForbiddenAsync(async () =>
                await (await ExhaustiveRocChartService.CreateAsync(dbContext, user, TestLog.NoOp, localizers, bus))
                    .GetSvgAsync(1));
            await AssertForbiddenAsync(async () =>
                await (await ExhaustivePredictedActualChartService.CreateAsync(dbContext, user, TestLog.NoOp,
                    localizers, bus)).GetSvgAsync(1));
            await AssertForbiddenAsync(async () =>
                await (await ExhaustiveErrorHistogramChartService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                    bus)).GetSvgAsync(1));
            await AssertForbiddenAsync(async () =>
                await (await ExhaustiveConfusionChartService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                    bus)).GetSvgAsync(1));
            await AssertForbiddenAsync(async () =>
                await (await ExhaustiveSensitivityChartService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                    bus)).GetSvgAsync(1));

            await AssertForbiddenAsync(async () =>
                await (await ExhaustiveVariableHistogramChartService.CreateAsync(dbContext, user, TestLog.NoOp,
                    localizers, bus)).GetSvgAsync(1, 1));
        }

        [Fact]
        public async Task UnknownIdsReturnAnEmptyStateChartForEveryChartAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var bus = new NullServiceChangeBus();

            AssertEmptyState(
                await (await ExhaustiveLearningCurveChartService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                    bus)).GetSvgAsync(int.MaxValue));
            AssertEmptyState(
                await (await ExhaustiveRocChartService.CreateAsync(dbContext, user, TestLog.NoOp, localizers, bus))
                    .GetSvgAsync(int.MaxValue));
            AssertEmptyState(
                await (await ExhaustivePredictedActualChartService.CreateAsync(dbContext, user, TestLog.NoOp,
                    localizers, bus)).GetSvgAsync(int.MaxValue));
            AssertEmptyState(
                await (await ExhaustiveErrorHistogramChartService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                    bus)).GetSvgAsync(int.MaxValue));
            AssertEmptyState(
                await (await ExhaustiveConfusionChartService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                    bus)).GetSvgAsync(int.MaxValue));
            AssertEmptyState(
                await (await ExhaustiveSensitivityChartService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                    bus)).GetSvgAsync(int.MaxValue));

            AssertEmptyState(
                await (await ExhaustiveVariableHistogramChartService.CreateAsync(dbContext, user, TestLog.NoOp,
                    localizers, bus)).GetSvgAsync(int.MaxValue, int.MaxValue));
        }

        [Fact]
        public async Task RocChartMapsThePointsAndIsIndistinguishableForAnotherTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            var trialId = await SeedPromotedAsync(dbContext, searchId, 0.9, DateTime.UtcNow);
            await SeedRocAsync(dbContext, trialId, (0.9, 10, 60, 20, 10), (0.4, 40, 40, 10, 10), (0.1, 5, 5, 5, 5));
            var bus = new NullServiceChangeBus();

            var owner = await ExhaustiveRocChartService.CreateAsync(dbContext, fx.Seed.UserWithPermission, TestLog.NoOp,
                localizers, bus);
            var other = await ExhaustiveRocChartService.CreateAsync(dbContext, fx.Seed.UserTenantB, TestLog.NoOp,
                localizers, bus);

            var svg = await owner.GetSvgAsync(searchId);
            var text = TextOf(svg).ToList();

            text.Should().Contain("ROC");
            text.Should().Contain("False Positive Rate");
            text.Should().Contain("True Positive Rate");
            text.Should().Contain(t => t.Contains("Model"));
            text.Should().NotContain(t => t.Contains(ExhaustiveChartRenderer.EmptyMessage));
            AssertEmptyState(await other.GetSvgAsync(searchId));
        }

        [Fact]
        public async Task LearningCurveMapsOnePointPerPromotionAndIsIsolatedByTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            var start = DateTime.UtcNow.AddHours(-3);
            await SeedPromotedAsync(dbContext, searchId, 0.5, start);
            await SeedPromotedAsync(dbContext, searchId, 0.7, start.AddHours(1));
            await SeedPromotedAsync(dbContext, searchId, 0.9, start.AddHours(2));
            var bus = new NullServiceChangeBus();

            var owner = await ExhaustiveLearningCurveChartService.CreateAsync(dbContext, fx.Seed.UserWithPermission,
                TestLog.NoOp, localizers, bus);
            var other = await ExhaustiveLearningCurveChartService.CreateAsync(dbContext, fx.Seed.UserTenantB,
                TestLog.NoOp, localizers, bus);

            var svg = await owner.GetSvgAsync(searchId);
            var text = TextOf(svg).ToList();

            text.Should().Contain("Learning Curve");
            text.Should().Contain("Promotion");
            text.Should().Contain("Score");
            svg.Should().Contain("Score");
            AssertEmptyState(await other.GetSvgAsync(searchId));
        }

        [Fact]
        public async Task PredictedActualHasPositiveAndNegativeSeriesAndIsIsolatedByTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            var trialId = await SeedPromotedAsync(dbContext, searchId, 0.9, DateTime.UtcNow);
            await SeedPredictedActualAsync(dbContext, trialId, (0.9, 1), (0.8, 1), (0.1, 0), (0.2, 0), (0.3, 0));
            var bus = new NullServiceChangeBus();

            var owner = await ExhaustivePredictedActualChartService.CreateAsync(dbContext, fx.Seed.UserWithPermission,
                TestLog.NoOp, localizers, bus);
            var other = await ExhaustivePredictedActualChartService.CreateAsync(dbContext, fx.Seed.UserTenantB,
                TestLog.NoOp, localizers, bus);

            var svg = await owner.GetSvgAsync(searchId);
            var text = TextOf(svg).ToList();

            text.Should().Contain("Predicted vs. Actual");
            text.Should().Contain("Predicted");
            text.Should().Contain("Actual");
            text.Should().Contain(t => t.Contains(ExhaustiveChartRenderer.PositiveSeriesName));
            text.Should().Contain(t => t.Contains(ExhaustiveChartRenderer.NegativeSeriesName));
            AssertEmptyState(await other.GetSvgAsync(searchId));
        }

        [Fact]
        public async Task ErrorHistogramRendersBinsAndIsIsolatedByTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            var trialId = await SeedPromotedAsync(dbContext, searchId, 0.9, DateTime.UtcNow);
            await SeedPredictedActualAsync(dbContext, trialId, (1, 1), (1, 2), (1, 2), (1, 3), (1, 6), (1, 11));
            var bus = new NullServiceChangeBus();

            var owner = await ExhaustiveErrorHistogramChartService.CreateAsync(dbContext, fx.Seed.UserWithPermission,
                TestLog.NoOp, localizers, bus);
            var other = await ExhaustiveErrorHistogramChartService.CreateAsync(dbContext, fx.Seed.UserTenantB,
                TestLog.NoOp, localizers, bus);

            var text = TextOf(await owner.GetSvgAsync(searchId)).ToList();

            text.Should().Contain("Errors");
            text.Should().Contain("Frequency");
            text.Should().Contain(t => t.StartsWith("Error"));
            text.Should().NotContain(t => t.Contains(ExhaustiveChartRenderer.EmptyMessage));
            AssertEmptyState(await other.GetSvgAsync(searchId));
        }

        [Fact]
        public async Task VariableHistogramRendersTheRequestedVariableOnlyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            var first = await SeedVariableAsync(dbContext, searchId, "AmountVariable", 5, 9, 2);
            var second = await SeedVariableAsync(dbContext, searchId, "CountryVariable", 1, 1);
            var bus = new NullServiceChangeBus();

            var owner = await ExhaustiveVariableHistogramChartService.CreateAsync(dbContext, fx.Seed.UserWithPermission,
                TestLog.NoOp, localizers, bus);
            var other = await ExhaustiveVariableHistogramChartService.CreateAsync(dbContext, fx.Seed.UserTenantB,
                TestLog.NoOp, localizers, bus);

            var firstText = TextOf(await owner.GetSvgAsync(searchId, first)).ToList();
            var secondText = TextOf(await owner.GetSvgAsync(searchId, second)).ToList();

            firstText.Should().Contain("AmountVariable");
            firstText.Should().NotContain("CountryVariable");
            secondText.Should().Contain("CountryVariable");
            AssertEmptyState(await owner.GetSvgAsync(searchId, int.MaxValue));
            AssertEmptyState(await other.GetSvgAsync(searchId, first));
        }

        [Fact]
        public async Task ConfusionChartUsesTheStoredCountsAndIsIsolatedByTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            await SeedPromotedAsync(dbContext, searchId, 0.9, DateTime.UtcNow, 155, 9120, 180, 45);
            var bus = new NullServiceChangeBus();

            var owner = await ExhaustiveConfusionChartService.CreateAsync(dbContext, fx.Seed.UserWithPermission,
                TestLog.NoOp, localizers, bus);
            var other = await ExhaustiveConfusionChartService.CreateAsync(dbContext, fx.Seed.UserTenantB, TestLog.NoOp,
                localizers, bus);

            var text = TextOf(await owner.GetSvgAsync(searchId)).ToList();

            text.Should().Contain("Confusion Matrix");
            text.Should().Contain(ExhaustiveChartRenderer.PositiveSeriesName);
            text.Should().Contain(ExhaustiveChartRenderer.NegativeSeriesName);
            text.Should().Contain(t => t.Contains("9120"));
            AssertEmptyState(await other.GetSvgAsync(searchId));
        }
    }
}