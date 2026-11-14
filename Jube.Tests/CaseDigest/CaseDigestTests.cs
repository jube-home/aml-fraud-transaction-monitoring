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
using Jube.Dto.Options;
using Jube.Engine.CaseDigest;
using Jube.Test.CaseRisk;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Newtonsoft.Json.Linq;
using Xunit;
using Digest = Jube.Engine.CaseDigest.CaseDigest;

namespace Jube.Test.CaseDigest
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseDigestTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly DateTime now = new(2026, 11, 10, 7, 0, 0, DateTimeKind.Utc);
        private readonly CaseRiskSeeder seeder = new(fx);

        private string User => fx.Seed.UserWithPermission;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.UserNotification.Where(w => w.DedupeKey.StartsWith("case-digest:")
                                                        && w.DedupeKey.EndsWith(":" + now.ToString("yyyyMMdd")))
                .DeleteAsync();
            await dbContext.CaseServiceLevelBreach.Where(w => seeder.CaseIds.Contains(w.CaseId)).DeleteAsync();
            await seeder.CleanAsync(dbContext);
        }

        private async Task<CaseRiskSeeder.Seeded> WorkflowWithFiguresAsync(DbContext dbContext)
        {
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, User);

            var held = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            await dbContext.Case.Where(w => w.Id == held).Set(s => s.Locked, (byte?)1).Set(s => s.LockedUser, User)
                .UpdateAsync();

            var overdue = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            await dbContext.InsertAsync(new CaseServiceLevelBreach { CaseId = overdue, DimensionId = 3, BreachedDate = now });
            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed, 3, closedDate: now.AddHours(-3));
            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed, 3, closedDate: now.AddDays(-5));

            return workflow;
        }

        private static (List<(string To, string Subject, string Body)> Sent, Func<string, string, string, bool> Send)
            Recorder()
        {
            var sent = new List<(string, string, string)>();

            return (sent, (to, subject, body) =>
            {
                sent.Add((to, subject, body));

                return true;
            });
        }

        [Fact]
        public async Task ASupervisorGetsOneNotificationPerWorkflowWithTheCountsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await WorkflowWithFiguresAsync(dbContext);
            var (_, send) = Recorder();

            var result = await Digest.RunAsync(dbContext, now, false, send);

            result.Notified.Should().BeGreaterThanOrEqualTo(1);
            var mine = await dbContext.UserNotification
                .Where(w => w.RecipientUser == User && w.KindId == (short)NotificationKind.CaseDigest
                            && w.DedupeKey.EndsWith(":" + now.ToString("yyyyMMdd")))
                .ToListAsync();
            var ours = mine.Select(m => JObject.Parse(m.ArgumentsJson))
                .Single(a => a.Value<int>("open") == 3);
            ours.Value<int>("unassigned").Should().Be(2);
            ours.Value<int>("overdue").Should().Be(1);
            ours.Value<int>("closed").Should().Be(1, "only the case closed in the last day counts");
        }

        [Fact]
        public async Task RunningTheDigestAgainTheSameDaySendsNothingMoreAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await WorkflowWithFiguresAsync(dbContext);
            var (sent, send) = Recorder();
            await dbContext.UserRegistry.Where(w => w.Name == User).Set(s => s.Email, "supervisor@example.invalid")
                .UpdateAsync();

            await Digest.RunAsync(dbContext, now, true, send);
            var emailsFirst = sent.Count;
            var second = await Digest.RunAsync(dbContext, now, true, send);

            second.Notified.Should().Be(0);
            sent.Count.Should().Be(emailsFirst);
        }

        [Fact]
        public async Task EmailIsOnlySentWhenSwitchedOnAndCarriesCountsButNoCaseKeysAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await WorkflowWithFiguresAsync(dbContext);
            await dbContext.UserRegistry.Where(w => w.Name == User).Set(s => s.Email, "supervisor@example.invalid")
                .UpdateAsync();

            var (off, sendOff) = Recorder();
            await Digest.RunAsync(dbContext, now, false, sendOff);
            off.Should().BeEmpty();

            await dbContext.UserNotification.Where(w => w.DedupeKey.StartsWith("case-digest:")
                                                        && w.DedupeKey.EndsWith(":" + now.ToString("yyyyMMdd")))
                .DeleteAsync();

            var (on, sendOn) = Recorder();
            await Digest.RunAsync(dbContext, now, true, sendOn);

            var mail = on.Should().Contain(m => m.To == "supervisor@example.invalid").Subject;
            mail.Subject.Should().StartWith("Jube case digest:");
            mail.Body.Should().Contain("Open cases: 3").And.Contain("Counts only")
                .And.NotContain("AccountId").And.NotContain("K1");
        }

        [Fact]
        public async Task AWorkflowWithNothingOpenAndNothingClosedRecentlyGetsNoDigestAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, User);
            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed, 3, closedDate: now.AddDays(-9));
            var (_, send) = Recorder();

            await Digest.RunAsync(dbContext, now, false, send);

            (await dbContext.UserNotification.CountAsync(w => w.RecipientUser == User
                                                              && w.KindId == (short)NotificationKind.CaseDigest
                                                              && w.ArgumentsJson != null
                                                              && w.DedupeKey.EndsWith(":" + now.ToString("yyyyMMdd"))
                                                              && w.DedupeKey.Contains(workflow.WorkflowGuid.ToString("N"))))
                .Should().Be(0);
        }

        [Theory]
        [InlineData(7, "1,2,3,4,5", 1, true)]
        [InlineData(8, "1,2,3,4,5", 1, false)]
        [InlineData(7, "1,2,3,4,5", 0, false)]
        [InlineData(7, "0,6", 6, true)]
        [InlineData(7, "", 1, false)]
        [InlineData(7, "x,1", 1, true)]
        public void TheScheduleIsDueOnlyInTheHourOnTheChosenDays(int hour, string days, int dayOfWeek, bool due)
        {
            var monday = new DateTime(2026, 11, 9, 7, 30, 0, DateTimeKind.Utc);
            var at = monday.AddDays(((dayOfWeek - 1) + 7) % 7);

            Digest.IsDue(at, hour, days, null).Should().Be(due);
        }

        [Fact]
        public void TheScheduleDoesNotRunTwiceInOneDay()
        {
            var at = new DateTime(2026, 11, 10, 7, 30, 0, DateTimeKind.Utc);

            Digest.IsDue(at, 7, "2", at.AddMinutes(-20)).Should().BeFalse();
            Digest.IsDue(at, 7, "2", at.AddDays(-1)).Should().BeTrue();
        }
    }
}
