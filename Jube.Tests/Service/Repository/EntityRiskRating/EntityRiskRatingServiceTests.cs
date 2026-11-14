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
using Jube.Dto.Options;
using Jube.Dto.Repository.EntityRiskRating;
using Jube.Service.Query.EntityRiskRating;
using Jube.Service.Reactivity;
using Jube.Service.Repository.EntityRiskRating;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Test.Service.Repository.EntityRiskRating
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityRiskRatingServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly string name = "RiskKey" + Guid.NewGuid().ToString("N")[..8];

        private string User => fx.Seed.UserWithPermission;
        private string Proposer => fx.Seed.UserWithPermissionNoApproval;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.EntityRiskRatingRequest.Where(w => w.KeyName == name).DeleteAsync();
            await dbContext.EntityRiskRating.Where(w => w.KeyName == name).DeleteAsync();
            await dbContext.UserNotification.Where(w => w.DedupeKey.StartsWith("entity-risk-review:")
                                                        && w.DedupeKey.EndsWith(":20991231")).DeleteAsync();
        }

        private static Task<EntityRiskRatingService> BuildAsync(DbContext dbContext, string user) =>
            EntityRiskRatingService.CreateAsync(dbContext, user, TestLog.NoOp, new NullServiceChangeBus());

        private async Task<EntityRiskRatingDto> SetApprovedAsync(DbContext dbContext, EntityRiskRatingDto rating)
        {
            var request = await (await BuildAsync(dbContext, Proposer)).SetAsync(rating);
            await (await BuildAsync(dbContext, User)).ApproveAsync(new EntityRiskRatingDecisionDto { Guid = request.Guid });

            return (await (await BuildAsync(dbContext, User)).GetAsync(rating.KeyName, rating.KeyValue)).Current!;
        }

        private EntityRiskRatingDto Rating(string value, int tier = 2, string? reason = "Because.",
            DateTime? review = null) =>
            new() { KeyName = name, KeyValue = value, TierId = tier, Reason = reason, ReviewByDate = review };

        [Fact]
        public async Task SettingARatingByHandAppendsAVersionAndKeepsTheHistoryAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildAsync(dbContext, User);

            var first = await SetApprovedAsync(dbContext, Rating("A1", 1));
            var second = await SetApprovedAsync(dbContext, Rating("A1", 3, " Adverse media. "));

            (first.Version, second.Version, second.SourceId, second.CreatedUser).Should().Be((1, 2, 2, Proposer));

            var history = await service.GetAsync(name, "A1");
            history.Current!.TierId.Should().Be(3);
            history.Current.Reason.Should().Be("Adverse media.");
            history.History.Select(h => h.Version).Should().Equal(2, 1);
        }

        [Fact]
        public async Task ABadRatingIsRefusedAndMassAssignmentIsIgnoredAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildAsync(dbContext, Proposer);

            foreach (var bad in new[]
                     {
                         Rating("A1", 0), Rating("A1", 4), Rating("A1", 2, null), Rating("A1", 2, "  "),
                         Rating("", 2), Rating("A1", 2, new string('x', 501)),
                         new EntityRiskRatingDto { KeyName = new string('k', 129), KeyValue = "A1", TierId = 1, Reason = "r" },
                         new EntityRiskRatingDto { KeyName = name, KeyValue = new string('v', 257), TierId = 1, Reason = "r" },
                         new EntityRiskRatingDto { KeyName = name, KeyValue = "A1", TierId = 1, Reason = "r", Score = double.NaN }
                     })
            {
                await ((Func<Task>)(() => service.SetAsync(bad))).Should().ThrowAsync<ArgumentException>();
            }

            var forged = Rating("A2", 1);
            forged.Version = 99;
            forged.SourceId = 1;
            forged.CreatedUser = "someone.else";
            var proposed = await service.SetAsync(forged);
            (proposed.StateId, proposed.RequestedUser, proposed.BasedOnVersion).Should()
                .Be(((int)EntityRiskRequestState.Pending, Proposer, 0));
            (await (await BuildAsync(dbContext, User)).GetAsync(name, "A2")).Current.Should().BeNull();

            await (await BuildAsync(dbContext, User)).ApproveAsync(new EntityRiskRatingDecisionDto { Guid = proposed.Guid });
            var stored = (await service.GetAsync(name, "A2")).Current!;
            (stored.Version, stored.SourceId, stored.CreatedUser).Should().Be((1, 2, Proposer));
        }

        [Fact]
        public async Task AnImportWritesChangesSkipsUnchangedAndReportsBadRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildAsync(dbContext, User);

            var first = await service.ImportAsync(new EntityRiskRatingImportDto
                { Ratings = [Rating("B1", 1, null), Rating("B2", 2, null), Rating("B3", 9, null)] });
            first.Written.Should().Be(2);
            first.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("Ratings[2]");

            var second = await service.ImportAsync(new EntityRiskRatingImportDto
                { Ratings = [Rating("B1", 1, null), Rating("B2", 3, null)] });
            (second.Written, second.Unchanged).Should().Be((1, 1));

            var history = await service.GetAsync(name, "B2");
            history.History.Select(h => (h.Version, h.TierId, h.SourceId)).Should().Equal((2, 3, 1), (1, 2, 1));
        }

        [Fact]
        public async Task AnImportOfMoreThanTheLimitIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildAsync(dbContext, User);

            await ((Func<Task>)(() => service.ImportAsync(new EntityRiskRatingImportDto
                {
                    Ratings = Enumerable.Range(0, 501).Select(i => Rating("X" + i, 1, null)).ToList()
                }))).Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task OnlyCurrentRatingsPastTheirReviewDateAreDueAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildAsync(dbContext, User);
            var past = DateTime.UtcNow.AddDays(-1);

            await SetApprovedAsync(dbContext, Rating("D1", 2, "r", past));
            await SetApprovedAsync(dbContext, Rating("D2", 2, "r", DateTime.UtcNow.AddDays(30)));
            await SetApprovedAsync(dbContext, Rating("D3", 2, "r", past));
            await SetApprovedAsync(dbContext, Rating("D3", 2, "r", DateTime.UtcNow.AddDays(30)));
            await SetApprovedAsync(dbContext, Rating("D4", 2, "r"));

            (await service.ListDueAsync()).Where(d => d.KeyName == name).Select(d => d.KeyValue).Should().Equal("D1");
        }

        [Fact]
        public async Task ReadNeedsCasePermissionWriteNeedsWorkflowPermissionAndTenantsAreSeparateAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildAsync(dbContext, User);
            await SetApprovedAsync(dbContext, Rating("T1", 3));

            var without = await BuildAsync(dbContext, fx.Seed.UserWithoutPermission);
            await ((Func<Task>)(() => without.GetAsync(name, "T1"))).Should().ThrowAsync<CaseExceptions.ForbiddenException>();
            await ((Func<Task>)(() => without.ListDueAsync())).Should().ThrowAsync<CaseExceptions.ForbiddenException>();
            await ((Func<Task>)(() => without.SetAsync(Rating("T1", 1)))).Should().ThrowAsync<CaseExceptions.ForbiddenException>();
            await ((Func<Task>)(() => without.ImportAsync(new EntityRiskRatingImportDto()))).Should().ThrowAsync<CaseExceptions.ForbiddenException>();

            var other = await BuildAsync(dbContext, fx.Seed.UserTenantB);
            (await other.GetAsync(name, "T1")).Current.Should().BeNull();
            await other.SetAsync(Rating("T1", 1));
            (await service.GetAsync(name, "T1")).Current!.TierId.Should().Be(3);
        }

        [Fact]
        public async Task TheReviewJobNotifiesWorkflowHoldersOncePerDayWhenRatingsAreDueAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SetApprovedAsync(dbContext, Rating("J1", 2, "r", new DateTime(2099, 12, 30, 0, 0, 0, DateTimeKind.Utc)));
            var now = new DateTime(2099, 12, 31, 8, 0, 0, DateTimeKind.Utc);

            await EntityRiskReviewJob.RunOnceAsync(dbContext, now);
            await EntityRiskReviewJob.RunOnceAsync(dbContext, now);

            var rows = await dbContext.UserNotification.Where(w => w.DedupeKey.EndsWith(":20991231")
                                                                   && w.DedupeKey.StartsWith("entity-risk-review:"))
                .ToListAsync();
            rows.Should().NotBeEmpty();
            rows.Select(r => (r.RecipientUser, r.TenantRegistryId)).Should().OnlyHaveUniqueItems();
            rows.Should().OnlyContain(r => r.KindId == (short)NotificationKind.EntityRiskReviewDue);
            rows.Should().Contain(r => r.RecipientUser == User && r.ArgumentsJson!.Contains("\"count\":"));
        }

        [Fact]
        public async Task AProposedChangeChangesNothingUntilASecondPersonApprovesItAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var proposer = await BuildAsync(dbContext, Proposer);
            var approver = await BuildAsync(dbContext, User);

            var request = await proposer.SetAsync(Rating("F1", 3));

            (await approver.GetAsync(name, "F1")).Current.Should().BeNull();
            (await approver.ListPendingAsync()).Should().Contain(r => r.Guid == request.Guid);

            var approved = await approver.ApproveAsync(new EntityRiskRatingDecisionDto { Guid = request.Guid, Note = "ok" });
            (approved.StateId, approved.DecidedUser, approved.ResultingVersion).Should()
                .Be(((int)EntityRiskRequestState.Approved, User, 1));
            (await approver.GetAsync(name, "F1")).Current!.TierId.Should().Be(3);
            (await approver.ListPendingAsync()).Should().NotContain(r => r.Guid == request.Guid);
        }

        [Fact]
        public async Task NobodyCanApproveOrRejectTheirOwnProposalAndOnlyApproversCanDecideAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var approver = await BuildAsync(dbContext, User);
            var own = await approver.SetAsync(Rating("F2", 1));

            await ((Func<Task>)(() => approver.ApproveAsync(new EntityRiskRatingDecisionDto { Guid = own.Guid })))
                .Should().ThrowAsync<CaseExceptions.ConflictException>();
            await ((Func<Task>)(() => approver.RejectAsync(new EntityRiskRatingDecisionDto { Guid = own.Guid, Note = "n" })))
                .Should().ThrowAsync<CaseExceptions.ConflictException>();

            var proposer = await BuildAsync(dbContext, Proposer);
            var other = await approver.RetractAsync(own.Guid);
            other.StateId.Should().Be((int)EntityRiskRequestState.Retracted);

            var second = await proposer.SetAsync(Rating("F2", 2));
            await ((Func<Task>)(() => proposer.ApproveAsync(new EntityRiskRatingDecisionDto { Guid = second.Guid })))
                .Should().ThrowAsync<CaseExceptions.ForbiddenException>();
        }

        [Fact]
        public async Task RejectingNeedsAReasonAndOnlyTheProposerCanRetractAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var proposer = await BuildAsync(dbContext, Proposer);
            var approver = await BuildAsync(dbContext, User);
            var request = await proposer.SetAsync(Rating("F3", 2));

            await ((Func<Task>)(() => approver.RejectAsync(new EntityRiskRatingDecisionDto { Guid = request.Guid })))
                .Should().ThrowAsync<ArgumentException>();
            await ((Func<Task>)(() => approver.RetractAsync(request.Guid)))
                .Should().ThrowAsync<CaseExceptions.ConflictException>();

            var rejected = await approver.RejectAsync(new EntityRiskRatingDecisionDto { Guid = request.Guid, Note = "no" });
            rejected.StateId.Should().Be((int)EntityRiskRequestState.Rejected);
            (await approver.GetAsync(name, "F3")).Current.Should().BeNull();

            await ((Func<Task>)(() => approver.ApproveAsync(new EntityRiskRatingDecisionDto { Guid = request.Guid })))
                .Should().ThrowAsync<CaseExceptions.ConflictException>();
        }

        [Fact]
        public async Task ASecondPendingChangeForTheSameKeyAndAStaleApprovalAreRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var proposer = await BuildAsync(dbContext, Proposer);
            var approver = await BuildAsync(dbContext, User);
            var first = await proposer.SetAsync(Rating("F4", 1));

            await ((Func<Task>)(() => proposer.SetAsync(Rating("F4", 2))))
                .Should().ThrowAsync<CaseExceptions.ConflictException>();

            await approver.ImportAsync(new EntityRiskRatingImportDto { Ratings = [Rating("F4", 3, null)] });

            await ((Func<Task>)(() => approver.ApproveAsync(new EntityRiskRatingDecisionDto { Guid = first.Guid })))
                .Should().ThrowAsync<CaseExceptions.ConflictException>();
            (await approver.GetAsync(name, "F4")).Current!.TierId.Should().Be(3);
        }
    }
}
