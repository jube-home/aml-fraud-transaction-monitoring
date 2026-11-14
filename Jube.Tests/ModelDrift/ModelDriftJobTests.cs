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
using Jube.Data.Context;
using Jube.Service.Query.ModelDrift;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using Xunit;

namespace Jube.Test.ModelDrift
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ModelDriftJobTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly DateTime now = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
        private readonly string score = "Drift" + Guid.NewGuid().ToString("N")[..8];
        private readonly Guid marker = Guid.NewGuid();
        private static readonly ModelDriftOptions options = new(7, 28, 50, 5000);

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.ExecuteAsync("DELETE FROM \"Archive\" WHERE \"EntryKeyValue\" = @k",
                new DataParameter("k", marker.ToString()));
            await dbContext.ModelDriftObservation.Where(w => w.ScoreName == score).DeleteAsync();
            await dbContext.UserNotification.Where(w => w.DedupeKey.Contains(score)).DeleteAsync();
        }

        private async Task SeedAsync(DbContext dbContext, DateTime from, int count, double shift, string facet)
        {
            for (var i = 0; i < count; i++)
            {
                var value = (shift + i / (double)count).ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                var json = $"{{\"{facet}\":{{\"{score}\":{value}}}}}";

                await dbContext.ExecuteAsync(
                    "INSERT INTO \"Archive\" (\"Json\",\"EntityAnalysisModelInstanceEntryGuid\",\"EntryKeyValue\",\"EntityAnalysisModelId\",\"CreatedDate\",\"ReferenceDate\") VALUES (@j::jsonb,@g,@k,1,@d,@d)",
                    new DataParameter("j", json), new DataParameter("g", Guid.NewGuid()),
                    new DataParameter("k", marker.ToString()), new DataParameter("d", from.AddMinutes(i)));
            }
        }

        [Fact]
        public async Task StableScoreIsRecordedWithoutNotification()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedAsync(dbContext, now.AddDays(-20), 100, 0, "exhaustiveAdaptation");
            await SeedAsync(dbContext, now.AddDays(-3), 100, 0, "exhaustiveAdaptation");

            await ModelDriftJob.RunOnceAsync(dbContext, now, options);

            var rows = await dbContext.ModelDriftObservation.Where(w => w.ScoreName == score).ToListAsync();
            rows.Should().ContainSingle();
            rows[0].StatusId.Should().Be(1);
            rows[0].SourceId.Should().Be(1);
            rows[0].BaselineCount.Should().Be(100);
            rows[0].CurrentCount.Should().Be(100);
            (await dbContext.UserNotification.CountAsync(w => w.DedupeKey.Contains(score))).Should().Be(0);
        }

        [Fact]
        public async Task SignificantDriftIsRecordedAndNotifiedOncePerDay()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedAsync(dbContext, now.AddDays(-20), 100, 0, "advancedAnalyticsAdaptation");
            await SeedAsync(dbContext, now.AddDays(-3), 100, 0.7, "advancedAnalyticsAdaptation");

            await ModelDriftJob.RunOnceAsync(dbContext, now, options);
            await ModelDriftJob.RunOnceAsync(dbContext, now, options);

            var rows = await dbContext.ModelDriftObservation.Where(w => w.ScoreName == score).ToListAsync();
            rows.Should().HaveCount(2);
            rows.Should().OnlyContain(o => o.StatusId == 3 && o.SourceId == 2);
            var notifications = await dbContext.UserNotification.Where(w => w.DedupeKey.Contains(score)).ToListAsync();
            notifications.Select(n => n.RecipientUser).Should().OnlyHaveUniqueItems();
            notifications.Should().OnlyContain(n => n.KindId == 4);
        }

        [Fact]
        public async Task ThinWindowsAreSkipped()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedAsync(dbContext, now.AddDays(-20), 100, 0, "exhaustiveAdaptation");
            await SeedAsync(dbContext, now.AddDays(-3), 10, 0.9, "exhaustiveAdaptation");

            await ModelDriftJob.RunOnceAsync(dbContext, now, options);

            (await dbContext.ModelDriftObservation.CountAsync(w => w.ScoreName == score)).Should().Be(0);
        }
    }
}
