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
using Jube.Test.Charting.Performance;
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
    public sealed class ExhaustivePerformanceChartServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly ExhaustivePerformanceChart[] allCharts = Enum.GetValues<ExhaustivePerformanceChart>();
        private readonly List<int> modelIds = [];
        private readonly List<int> predictedActualIds = [];
        private readonly List<int> promotedIds = [];
        private readonly List<int> rocIds = [];
        private readonly List<int> searchIds = [];
        private readonly List<int> trialIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

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

        private Task<ExhaustivePerformanceChartService> PerformanceAsync(DbContext dbContext, string? user)
        {
            return ExhaustivePerformanceChartService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                new NullServiceChangeBus());
        }

        private async Task<int> SeedFortyRowsAsync(DbContext dbContext, string user)
        {
            var searchId = await SeedSearchAsync(dbContext, user);
            var trialId = await SeedPromotedAsync(dbContext, searchId, 0.8, DateTime.UtcNow);

            await SeedPredictedActualAsync(dbContext, trialId,
                [.. PerformanceData.Scores.Select((s, i) => (s, PerformanceData.Actual[i] ? 1.0 : 0.0))]);

            return searchId;
        }

        private static string WithoutId(string svg, int id)
        {
            return svg.Replace("-" + id, "-ID");
        }

        [Fact]
        public async Task PerformanceAndHistoryServicesRefuseAnUnauthenticatedCallerAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var userName in new[] { null, "", "   ", fx.Seed.UnknownUser, fx.Seed.UserNoTenant })
            {
                var ex1 = await Assert.ThrowsAnyAsync<ServiceException>(() =>
                    ExhaustivePerformanceChartService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers,
                        new NullServiceChangeBus()));
                ex1.Code.Should().Be("NotAuthenticated");

                var ex2 = await Assert.ThrowsAnyAsync<ServiceException>(() =>
                    ExhaustivePromotionHistoryChartService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers,
                        new NullServiceChangeBus()));
                ex2.Code.Should().Be("NotAuthenticated");
            }
        }

        [Fact]
        public async Task EveryPerformanceChartAndTheHistoryDenyACallerWithoutThePermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await PerformanceAsync(dbContext, fx.Seed.UserWithoutPermission);

            foreach (var chart in allCharts)
            {
                await AssertForbiddenAsync(async () => await service.GetSvgAsync(1, chart));
            }

            await AssertForbiddenAsync(async () =>
                await (await ExhaustivePromotionHistoryChartService.CreateAsync(dbContext,
                        fx.Seed.UserWithoutPermission, TestLog.NoOp, localizers, new NullServiceChangeBus()))
                    .GetSvgAsync(1));
        }

        [Fact]
        public async Task UnknownIdsGiveTheEmptyStateForEveryPerformanceChartAndTheHistoryAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await PerformanceAsync(dbContext, fx.Seed.UserWithPermission);

            foreach (var chart in allCharts)
            {
                AssertEmptyState(await service.GetSvgAsync(int.MaxValue, chart));
            }

            AssertEmptyState(await (await ExhaustivePromotionHistoryChartService.CreateAsync(dbContext,
                fx.Seed.UserWithPermission, TestLog.NoOp, localizers, new NullServiceChangeBus())).GetSvgAsync(
                int.MaxValue));
        }

        [Fact]
        public async Task ForeignTenantAndUnknownIdsAreIndistinguishableForEveryChartAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await SeedFortyRowsAsync(dbContext, fx.Seed.UserWithPermission);
            var owner = await PerformanceAsync(dbContext, fx.Seed.UserWithPermission);
            var other = await PerformanceAsync(dbContext, fx.Seed.UserTenantB);

            foreach (var chart in allCharts)
            {
                var own = await owner.GetSvgAsync(searchId, chart);
                var foreign = await other.GetSvgAsync(searchId, chart);
                var unknown = await other.GetSvgAsync(int.MaxValue, chart);

                AssertEmptyState(foreign);

                if (chart != ExhaustivePerformanceChart.ValueCaptured)
                {
                    own.Should().NotContain(ExhaustiveChartRenderer.EmptyMessage, chart.ToString());
                }

                WithoutId(foreign, searchId).Should().Be(WithoutId(unknown, int.MaxValue), chart.ToString());
            }
        }

        [Fact]
        public async Task PromotionHistoryChartMapsEveryPromotionAndIsIsolatedByTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            var start = DateTime.UtcNow.AddHours(-3);
            await SeedPromotedAsync(dbContext, searchId, 0.5, start);
            await SeedPromotedAsync(dbContext, searchId, 0.7, start.AddHours(1));
            await SeedPromotedAsync(dbContext, searchId, 0.9, start.AddHours(2));
            var bus = new NullServiceChangeBus();

            var owner = await ExhaustivePromotionHistoryChartService.CreateAsync(dbContext,
                fx.Seed.UserWithPermission, TestLog.NoOp, localizers, bus);
            var other = await ExhaustivePromotionHistoryChartService.CreateAsync(dbContext, fx.Seed.UserTenantB,
                TestLog.NoOp, localizers, bus);

            var text = TextOf(await owner.GetSvgAsync(searchId)).ToList();

            text.Should().Contain("Promotion Over Time");
            text.Should().Contain("Score at promotion");
            text.Should().Contain("Best score so far");
            text.Should().Contain(t => t.StartsWith("3 promotions, best score 0.9"));
            text.Should().Contain("Promotion 1 after 1 trial, active");
            text.Should().Contain("Promotion 3 after 1 trial, active");
            text.Should().Contain("Score: 0.7");
            AssertEmptyState(await other.GetSvgAsync(searchId));
        }

        [Fact]
        public async Task MeasuresPanelMatchesThePythonReferenceThroughTheDatabaseAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await SeedFortyRowsAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await PerformanceAsync(dbContext, fx.Seed.UserWithPermission);

            var text = TextOf(await service.GetSvgAsync(searchId, ExhaustivePerformanceChart.Measures)).ToList();

            text.Should().Contain(t => t.StartsWith("40 test rows, 12 positive class (30.0%)"));
            text.Should().Contain(t => t.Contains("Not calibrated (promoted before calibration was introduced)"));
            text.Should().Contain("0.876");
            text.Should().Contain("95% interval 0.77 to 0.983");
            text.Should().Contain("0.753");
            text.Should().Contain("0.643");
            text.Should().Contain("0.737");
            text.Should().Contain("0.153");
            text.Should().Contain("0.469");
            text.Should().Contain("0.775");
            text.Should().Contain("0.588");
            text.Should().Contain("0.833");
            text.Should().Contain("0.690");
            text.Should().Contain("0.541");
            text.Should().Contain("At threshold 0.5");
        }

        [Fact]
        public async Task ThresholdAndCostsAreHonouredAndBadValuesFallBackToTheDefaultAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await SeedFortyRowsAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await PerformanceAsync(dbContext, fx.Seed.UserWithPermission);

            var moved = TextOf(await service.GetSvgAsync(searchId, ExhaustivePerformanceChart.Measures, 0.75)).ToList();
            var bad = TextOf(await service.GetSvgAsync(searchId, ExhaustivePerformanceChart.Measures, 7)).ToList();

            var sweep = TextOf(await service.GetSvgAsync(searchId, ExhaustivePerformanceChart.ThresholdSweep, 0.5, 9,
                1)).ToList();

            moved.Should().Contain("At threshold 0.75");
            moved.Should().Contain("0.600");
            bad.Should().Contain("At threshold 0.5");
            sweep.Should().Contain(t => t.StartsWith("Minimum-risk threshold 0.1"));
        }

        [Fact]
        public async Task EveryChartForARealSeededModelIsSafeSvgWithUniqueDistinctIdsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await SeedFortyRowsAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await PerformanceAsync(dbContext, fx.Seed.UserWithPermission);
            var ids = new List<string>();

            foreach (var chart in allCharts)
            {
                var svg = await service.GetSvgAsync(searchId, chart);
                var document = AssertSafeSvg(svg);
                ids.Add(document.DocumentElement!.GetAttribute("aria-labelledby"));
            }

            ids.Should().OnlyHaveUniqueItems();

            ids.Should().OnlyContain(i => i.StartsWith("ex-p", StringComparison.Ordinal) &&
                                          i.Contains("-" + searchId, StringComparison.Ordinal));
        }

        [Fact]
        public async Task OnlyTheMostRecentPromotedTrialRowsAreChartedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            var oldTrial = await SeedPromotedAsync(dbContext, searchId, 0.5, DateTime.UtcNow.AddHours(-2));
            await SeedPredictedActualAsync(dbContext, oldTrial, (0.1, 0), (0.9, 0), (0.2, 0), (0.3, 1));
            var newTrial = await SeedPromotedAsync(dbContext, searchId, 0.9, DateTime.UtcNow);
            await SeedPredictedActualAsync(dbContext, newTrial, (0.1, 0), (0.2, 0), (0.8, 1), (0.9, 1));
            var service = await PerformanceAsync(dbContext, fx.Seed.UserWithPermission);

            var text = TextOf(await service.GetSvgAsync(searchId, ExhaustivePerformanceChart.Measures)).ToList();

            text.Should().Contain(t => t.StartsWith("4 test rows, 2 positive class"));
            text.Should().Contain("1.000");
        }
    }
}