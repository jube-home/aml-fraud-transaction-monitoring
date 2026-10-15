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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Service.Charts;
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
    public sealed class ExhaustiveNarrativeServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> modelIds = [];
        private readonly List<int> predictedActualIds = [];
        private readonly List<int> promotedIds = [];
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
                    Name = $"{DatabaseFixture.Prefix}Narrative{Guid.NewGuid():N}"[..40],
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

        private async Task<int> SeedPromotedAsync(DbContext dbContext, int searchId, double score,
            DateTime createdDate)
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
                    Json = "{}",
                    CreatedDate = createdDate
                });
            promotedIds.Add(promoted.Id);

            await dbContext.GetTable<ExhaustiveSearchInstancePromotedTrialInstance>()
                .Where(w => w.Id == promoted.Id).Set(s => s.CreatedDate, createdDate).UpdateAsync();

            return trial.Id;
        }

        private async Task SeedPredictedActualAsync(DbContext dbContext, int trialId,
            IReadOnlyList<(double Predicted, double Actual)> rows)
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

        private Task<ExhaustiveNarrativeService> NarrativeServiceAsync(DbContext dbContext, string user)
        {
            return ExhaustiveNarrativeService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                new NullServiceChangeBus());
        }

        [Fact]
        public async Task GetAsync_ProducesEveryGroupForARealisticPromotedTrialAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var searchId = await SeedSearchAsync(dbContext, user);
            var start = DateTime.UtcNow.AddDays(-3);
            await SeedPromotedAsync(dbContext, searchId, 0.5, start);
            await SeedPromotedAsync(dbContext, searchId, 0.65, start.AddHours(2));
            var latestTrialId = await SeedPromotedAsync(dbContext, searchId, 0.8, start.AddHours(4));

            var rows = PerformanceData.Scores
                .Select((s, i) => (Predicted: s, Actual: PerformanceData.Actual[i] ? 1.0 : 0.0)).ToList();

            await SeedPredictedActualAsync(dbContext, latestTrialId, rows);

            var service = await NarrativeServiceAsync(dbContext, user);

            var narrative = await service.GetAsync(searchId);

            narrative.Holistic.Should().NotBeEmpty();

            var expectedGroups = new[]
            {
                "Performance", "Calibration", "Confusion", "Discrimination", "Ks", "ThresholdSweep",
                "ScoreDistribution", "Gains", "PrecisionRecall", "Lift", "LearningCurve"
            };

            foreach (var group in expectedGroups)
            {
                narrative.Groups.Should().ContainKey(group, $"the {group} chart must have a narrative");
                narrative.Groups[group].Should().NotBeEmpty();
            }

            narrative.Groups["LearningCurve"].Should().Contain(p => p.Contains("3 promotions"));
        }

        [Fact]
        public async Task GetAsync_OmitsGroupsThatHaveNoDataInsteadOfGuessingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var searchId = await SeedSearchAsync(dbContext, user);

            var service = await NarrativeServiceAsync(dbContext, user);

            var narrative = await service.GetAsync(searchId);

            narrative.Groups.Should().NotContainKey("PrecisionRecall");
            narrative.Groups.Should().NotContainKey("Lift");
            narrative.Groups.Should().NotContainKey("LearningCurve");
        }
    }
}