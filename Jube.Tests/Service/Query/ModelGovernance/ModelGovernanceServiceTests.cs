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
using Jube.Dto.Options;
using Jube.Dto.Query.ModelGovernance;
using Jube.Service.Query.ModelGovernance;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Test.Service.Query.ModelGovernance
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ModelGovernanceServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<int> models = [];
        private readonly List<Guid> guids = [];

        private string User => fx.Seed.UserWithPermission;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.ModelDriftObservation.Where(w => models.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.ModelGovernanceRecord.Where(w => guids.Contains(w.ModelGuid)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => models.Contains(w.Id)).DeleteAsync();
        }

        private static Task<ModelGovernanceService> BuildAsync(DbContext dbContext, string user) =>
            ModelGovernanceService.CreateAsync(dbContext, user, TestLog.NoOp, new NullServiceChangeBus());

        private async Task<(int Id, Guid Guid)> ModelAsync(DbContext dbContext, string name = "<b>Governed</b> model")
        {
            var tenant = (await UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, User))!.Value;
            var guid = Guid.NewGuid();
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = name, Guid = guid, Active = 1, Locked = 0, Deleted = 0, TenantRegistryId = tenant
            });
            models.Add(id);
            guids.Add(guid);

            return (id, guid);
        }

        private static ModelGovernanceRecordDto Statement(Guid guid, string owner = "Head of Fraud",
            DateTime? review = null, int tier = 2) =>
            new()
            {
                KindId = 1, ModelGuid = guid, OwnerUser = owner, IntendedPurpose = "Score card payments for fraud.",
                RiskTierId = tier, HumanOversight = "Analysts review every alert.", NextReviewDate = review
            };

        [Fact]
        public async Task EachSaveAppendsAVersionAndTheHistoryIsKeptAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, guid) = await ModelAsync(dbContext);
            var service = await BuildAsync(dbContext, User);

            var first = await service.SetAsync(Statement(guid));
            var second = await service.SetAsync(Statement(guid, "  New owner  ", tier: 3));

            (first.Version, second.Version, second.OwnerUser, second.CreatedUser).Should().Be((1, 2, "New owner", User));

            var history = await service.GetAsync(1, guid);
            history.Current!.RiskTierId.Should().Be(3);
            history.History.Select(h => h.Version).Should().Equal(2, 1);
        }

        [Fact]
        public async Task BadStatementsAndUnknownModelsAreRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, guid) = await ModelAsync(dbContext);
            var service = await BuildAsync(dbContext, User);

            foreach (var bad in new[]
                     {
                         Statement(guid, ""), Statement(guid, new string('o', 201)), Statement(guid, tier: 0),
                         Statement(guid, tier: 4),
                         new ModelGovernanceRecordDto { KindId = 1, ModelGuid = guid, OwnerUser = "o", IntendedPurpose = "", RiskTierId = 1, HumanOversight = "h" },
                         new ModelGovernanceRecordDto { KindId = 1, ModelGuid = guid, OwnerUser = "o", IntendedPurpose = "p", RiskTierId = 1, HumanOversight = new string('h', 2001) },
                         new ModelGovernanceRecordDto { KindId = 9, ModelGuid = guid, OwnerUser = "o", IntendedPurpose = "p", RiskTierId = 1, HumanOversight = "h" }
                     })
            {
                await FluentActions.Awaiting(() => service.SetAsync(bad)).Should().ThrowAsync<ArgumentException>();
            }

            await FluentActions.Awaiting(() => service.SetAsync(Statement(Guid.NewGuid())))
                .Should().ThrowAsync<CaseExceptions.NotFoundException>();
            await FluentActions.Awaiting(() => service.GetAsync(1, Guid.NewGuid()))
                .Should().ThrowAsync<CaseExceptions.NotFoundException>();
        }

        [Fact]
        public async Task EveryVerbNeedsThePermissionAndAnotherTenantSeesNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, guid) = await ModelAsync(dbContext);
            var without = await BuildAsync(dbContext, fx.Seed.UserWithoutPermission);

            await FluentActions.Awaiting(() => without.InventoryAsync()).Should().ThrowAsync<CaseExceptions.ForbiddenException>();
            await FluentActions.Awaiting(() => without.GetAsync(1, guid)).Should().ThrowAsync<CaseExceptions.ForbiddenException>();
            await FluentActions.Awaiting(() => without.CardAsync(1, guid)).Should().ThrowAsync<CaseExceptions.ForbiddenException>();
            await FluentActions.Awaiting(() => without.SetAsync(Statement(guid))).Should().ThrowAsync<CaseExceptions.ForbiddenException>();

            var other = await BuildAsync(dbContext, fx.Seed.UserTenantB);
            await FluentActions.Awaiting(() => other.GetAsync(1, guid)).Should().ThrowAsync<CaseExceptions.NotFoundException>();
            (await other.InventoryAsync()).Should().NotContain(r => r.ModelGuid == guid);
        }

        [Fact]
        public async Task TheInventoryShowsEveryModelWithItsReviewStatusAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, none) = await ModelAsync(dbContext);
            var (_, current) = await ModelAsync(dbContext);
            var (_, soon) = await ModelAsync(dbContext);
            var (_, overdue) = await ModelAsync(dbContext);
            var service = await BuildAsync(dbContext, User);

            await service.SetAsync(Statement(current, review: DateTime.UtcNow.AddDays(200)));
            await service.SetAsync(Statement(soon, review: DateTime.UtcNow.AddDays(5)));
            await service.SetAsync(Statement(overdue, review: DateTime.UtcNow.AddDays(-1)));

            var rows = (await service.InventoryAsync()).ToDictionary(r => r.ModelGuid);

            rows[none].ReviewStatusId.Should().Be((int)ModelReviewStatus.NotRecorded);
            rows[none].Recorded.Should().BeFalse();
            rows[current].ReviewStatusId.Should().Be((int)ModelReviewStatus.Current);
            rows[soon].ReviewStatusId.Should().Be((int)ModelReviewStatus.DueSoon);
            rows[overdue].ReviewStatusId.Should().Be((int)ModelReviewStatus.Overdue);
            rows[current].OwnerUser.Should().Be("Head of Fraud");
        }

        [Fact]
        public async Task TheCardIsOneEscapedHtmlDocumentWithTheStatementAndDriftHistoryAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (id, guid) = await ModelAsync(dbContext);
            var tenant = (await UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, User))!.Value;
            var service = await BuildAsync(dbContext, User);
            await service.SetAsync(Statement(guid, "Owner <script>x</script>"));

            foreach (var (days, psi, status) in new[] { (3, 0.05, 1), (2, 0.15, 2), (1, 0.31, 3) })
            {
                await dbContext.InsertAsync(new ModelDriftObservation
                {
                    TenantRegistryId = tenant, EntityAnalysisModelId = id, SourceId = 1, ScoreName = "Score<1>",
                    ObservedDate = DateTime.UtcNow.AddDays(-days), BaselineCount = 500, CurrentCount = 500, Psi = psi,
                    StatusId = (short)status
                });
            }

            var (html, name) = await service.CardAsync(1, guid);

            name.Should().Be($"model-card-{guid:N}.html");
            html.Should().StartWith("<!doctype html>").And.Contain("&lt;b&gt;Governed&lt;/b&gt; model");
            html.Should().Contain("Owner &lt;script&gt;x&lt;/script&gt;").And.NotContain("<script>");
            html.Should().Contain("Score&lt;1&gt;").And.Contain("<svg").And.Contain("Significant drift");
            html.Should().Contain("0.310").And.Contain("Risk tier");
        }

        [Fact]
        public void ReviewStatusIsWorkedOutFromTheNextReviewDate()
        {
            var now = new DateTime(2026, 11, 11, 0, 0, 0, DateTimeKind.Utc);

            ModelGovernanceService.ReviewStatus(false, null, now).Should().Be(ModelReviewStatus.NotRecorded);
            ModelGovernanceService.ReviewStatus(true, null, now).Should().Be(ModelReviewStatus.Current);
            ModelGovernanceService.ReviewStatus(true, now.AddDays(-1), now).Should().Be(ModelReviewStatus.Overdue);
            ModelGovernanceService.ReviewStatus(true, now.AddDays(29), now).Should().Be(ModelReviewStatus.DueSoon);
            ModelGovernanceService.ReviewStatus(true, now.AddDays(31), now).Should().Be(ModelReviewStatus.Current);
        }

        [Fact]
        public void ACardWithNoStatementOrDriftSaysSoAndStillRenders()
        {
            var html = ModelCardBuilder.Build(new ModelCardInput("M", 1, Guid.Empty, DateTime.UtcNow, "u", null, 0, [],
                null));

            html.Should().Contain("No governance statement has been recorded").And.Contain("No drift checks");
        }
    }
}
