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
using FluentAssertions.Execution;
using Jube.Data.Repository;
using Jube.Service.Query.TreeChildren;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using Poco = Jube.Data.Poco;

namespace Jube.Test.Service.Query.TreeChildren
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class TreeChildrenPendingColourTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> modelIds = [];
        private readonly List<int> xPathIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.GetTable<Poco.EntityAnalysisModelRequestXpathVersion>()
                .Where(w => xPathIds.Contains(w.EntityAnalysisModelRequestXpathId!.Value)).DeleteAsync();
            await dbContext.EntityAnalysisModelRequestXpath.Where(w => xPathIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Poco.EntityAnalysisModelVersion>()
                .Where(w => modelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => modelIds.Contains(w.Id)).DeleteAsync();
        }

        private async Task<(int ModelId, int XPathId)> SeedAsync(string maker, int version)
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await dbContext.UserInTenant
                .Where(w => w.User == fx.Seed.UserWithPermission)
                .Select(s => s.TenantRegistryId)
                .FirstAsync();

            var model = new Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}TreeP{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            };
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model);
            modelIds.Add(model.Id);

            var name = $"ZzTreeP{Guid.NewGuid():N}"[..20];
            var xPathId = await dbContext.InsertWithInt32IdentityAsync(new Poco.EntityAnalysisModelRequestXpath
            {
                EntityAnalysisModelId = model.Id,
                Name = name,
                XPath = $"$.{name}",
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = version,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = maker
            });
            xPathIds.Add(xPathId);

            return (model.Id, xPathId);
        }

        private async Task<string?> ColourOfAsync(string user, int modelId, int xPathId)
        {
            await using var dbContext = fx.GetDbContext();

            var service = await TreeChildrenService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                new NullServiceChangeBus());

            var children = await service.GetRequestXPathAsync(modelId);
            return children.SingleOrDefault(c => c.Key == xPathId)?.Color;
        }

        [Fact]
        public async Task APendingRequestXPathIsOrangeForACallerWhoMayViewPendingApprovalsAsync()
        {
            var (modelId, xPathId) = await SeedAsync("ZzTreeMaker", version: 1);

            var colour = await ColourOfAsync(fx.Seed.UserWithPermission, modelId, xPathId);

            colour.Should().Be("orange");
        }

        [Fact]
        public async Task ANonPendingRequestXPathKeepsItsActiveColourAsync()
        {
            var (modelId, xPathId) = await SeedAsync("ZzTreeMaker", version: 1);

            await using (var seed = fx.GetDbContext())
            {
                await new EntityApprovalRepository(seed, fx.Seed.UserWithPermission).InsertAsync(
                    new Poco.EntityApproval
                    {
                        EntityApprovalKindId = (int)EntityApprovalKind.EntityAnalysisModelRequestXPath,
                        EntityId = xPathId,
                        EntityVersion = 1,
                        StateId = (int)EntityApprovalState.Approved,
                        CreatedUser = EntityApprovalSeed.Checker,
                        CreatedDate = DateTime.UtcNow,
                        Guid = Guid.NewGuid()
                    });
            }

            var colour = await ColourOfAsync(fx.Seed.UserWithPermission, modelId, xPathId);

            using var scope = new AssertionScope();
            colour.Should().Be("green", "the approved version is active and no change is waiting");
        }

        [Fact]
        public async Task APendingDeletionOfARequestXPathIsOrangeAndStaysInTheTreeAsync()
        {
            var (modelId, xPathId) = await SeedAsync("ZzTreeMaker", version: 1);

            await using (var deleting = fx.GetDbContext())
            {
                await new EntityAnalysisModelRequestXpathRepository(deleting, fx.Seed.UserWithPermission)
                    .DeleteAsync(xPathId);
            }

            var colour = await ColourOfAsync(fx.Seed.UserWithPermission, modelId, xPathId);

            colour.Should().Be("orange", "a deletion waiting for a checker is shown as a pending change");
        }

        [Fact]
        public async Task AnApprovedDeletionOfARequestXPathLeavesTheTreeAsync()
        {
            var (modelId, xPathId) = await SeedAsync("ZzTreeMaker", version: 1);

            await using (var seed = fx.GetDbContext())
            {
                await new EntityApprovalRepository(seed, fx.Seed.UserWithPermission).InsertAsync(
                    new Poco.EntityApproval
                    {
                        EntityApprovalKindId = (int)EntityApprovalKind.EntityAnalysisModelRequestXPath,
                        EntityId = xPathId,
                        EntityVersion = 1,
                        StateId = (int)EntityApprovalState.Approved,
                        CreatedUser = EntityApprovalSeed.Checker,
                        CreatedDate = DateTime.UtcNow,
                        Guid = Guid.NewGuid()
                    });
            }

            await using (var deleting = fx.GetDbContext())
            {
                await new EntityAnalysisModelRequestXpathRepository(deleting, fx.Seed.UserWithPermission)
                    .DeleteAsync(xPathId);
            }

            await using (var seed = fx.GetDbContext())
            {
                await new EntityApprovalRepository(seed, fx.Seed.UserWithPermission).InsertAsync(
                    new Poco.EntityApproval
                    {
                        EntityApprovalKindId = (int)EntityApprovalKind.EntityAnalysisModelRequestXPath,
                        EntityId = xPathId,
                        EntityVersion = 2,
                        StateId = (int)EntityApprovalState.Approved,
                        CreatedUser = EntityApprovalSeed.Checker,
                        CreatedDate = DateTime.UtcNow,
                        Guid = Guid.NewGuid()
                    });
            }

            var colour = await ColourOfAsync(fx.Seed.UserWithPermission, modelId, xPathId);

            colour.Should().BeNull("an approved deletion has finished and leaves the tree");
        }

        [Fact]
        public async Task ARejectedRequestXPathIsPurpleForACallerWhoMayViewPendingApprovalsAsync()
        {
            var (modelId, xPathId) = await SeedAsync("ZzTreeMaker", version: 1);

            await using (var seed = fx.GetDbContext())
            {
                await new EntityApprovalRepository(seed, fx.Seed.UserWithPermission).InsertAsync(
                    new Poco.EntityApproval
                    {
                        EntityApprovalKindId = (int)EntityApprovalKind.EntityAnalysisModelRequestXPath,
                        EntityId = xPathId,
                        EntityVersion = 1,
                        StateId = (int)EntityApprovalState.Rejected,
                        CreatedUser = EntityApprovalSeed.Checker,
                        CreatedDate = DateTime.UtcNow,
                        Guid = Guid.NewGuid()
                    });
            }

            var colour = await ColourOfAsync(fx.Seed.UserWithPermission, modelId, xPathId);

            colour.Should().Be("purple");
        }
    }
}