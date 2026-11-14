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
using Jube.Data.Repository;
using Jube.Dto.Repository.GraphRegistryEdge;
using Jube.Service.Exceptions.Repository.GraphRegistryEdge;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using GraphRegistryEdgeService = Jube.Service.Repository.GraphRegistryEdge.GraphRegistryEdgeService;

namespace Jube.Test.Service.Repository.GraphRegistryEdge
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class GraphRegistryEdgeServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const int AuthorPermission = 50;
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);
        private readonly List<int> createdEdgeIds = [];
        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdRegistryIds = [];
        private readonly List<int> createdRoleRegistryIds = [];
        private readonly List<string> createdUserNames = [];
        private readonly List<int> createdXpathIds = [];
        private string authorA = string.Empty;
        private int registryId;
        private int tenantAId;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            tenantAId = await dbContext.UserInTenant.Where(w => w.User == fx.Seed.UserWithPermission)
                .Select(w => w.TenantRegistryId).FirstAsync();
            authorA = await SeedActorAsync(dbContext, tenantAId, "EdgeAuthorA", [AuthorPermission]);
            var registry = await new GraphRegistryRepository(dbContext, authorA).InsertAsync(new Data.Poco.GraphRegistry
            {
                Name = $"{DatabaseFixture.Prefix}GraphE{Guid.NewGuid():N}"[..40], Active = 1, Locked = 0, Deleted = 0,
                ContextKindId = 1
            });
            registryId = registry.Id;
            createdRegistryIds.Add(registry.Id);
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.GraphRegistryEdgeVersion.Where(w => createdEdgeIds.Contains(w.GraphRegistryEdgeId)).DeleteAsync();
            await dbContext.GraphRegistryEdge.Where(w => w.GraphRegistryId != null && createdRegistryIds.Contains(w.GraphRegistryId.Value)).DeleteAsync();
            await dbContext.GraphRegistryVersion.Where(w => createdRegistryIds.Contains(w.GraphRegistryId)).DeleteAsync();
            await dbContext.GraphRegistry.Where(w => createdRegistryIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelRequestXpathVersion>()
                .Where(w => w.EntityAnalysisModelRequestXpathId != null && createdXpathIds.Contains(w.EntityAnalysisModelRequestXpathId.Value)).DeleteAsync();
            await dbContext.EntityAnalysisModelRequestXpath.Where(w => createdXpathIds.Contains(w.Id)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
            await CleanupActorsAsync(dbContext);
        }

        [Fact]
        public async Task CreatesUpdatesAndDeletesAnEdgeDefinitionWithAVersionRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, authorA);
            var created = await service.InsertAsync(ValidDto("Pays", 2, "AccountId", "ToAccountId", "AmountEUR"));
            createdEdgeIds.Add(created.Id);
            created.Guid.Should().NotBeEmpty();
            created.HalfLifeDays.Should().Be(90);

            created.KindPrior = 0.7;
            var updated = await service.UpdateAsync(created);
            updated.Version.Should().Be(2);
            (await dbContext.GraphRegistryEdgeVersion.CountAsync(v => v.GraphRegistryEdgeId == created.Id)).Should().Be(1);
            (await service.GetByGraphRegistryIdAsync(registryId)).Should().ContainSingle(e => e.Id == created.Id);

            await service.DeleteAsync(created.Id);
            (await service.GetByIdAsync(created.Id)).Should().BeNull();
        }

        [Fact]
        public async Task ValidatesKeysKindAndWeightsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, authorA);
            var same = ValidDto("Same", 1, "AccountId", "AccountId", null);
            var money = ValidDto("Money", 2, "AccountId", "ToAccountId", null);
            money.KindPrior = 1.5;
            money.HalfLifeDays = 0;
            money.HubThreshold = 1;

            (await service.ValidateAsync(same)).Errors.Select(e => e.ErrorCode).Should().Contain("KeysMustDiffer");
            (await service.ValidateAsync(money)).Errors.Select(e => e.ErrorCode).Should()
                .Contain(["AmountKeyNotEmpty", "KindPriorOutOfRange", "HalfLifeOutOfRange", "HubThresholdOutOfRange"]);

            var act = () => service.InsertAsync(same);
            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task ListsTheReportKeysOfTheTenantsModelsWithFlagsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var model = await new EntityAnalysisModelRepository(dbContext, authorA).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}GraphKeys{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(),
                    Active = 1, Locked = 0, Deleted = 0
                });
            createdModelIds.Add(model.Id);
            var flagged = $"{DatabaseFixture.Prefix}Flagged{Guid.NewGuid():N}"[..30];
            var unflagged = $"{DatabaseFixture.Prefix}Plain{Guid.NewGuid():N}"[..30];

            foreach (var (name, report) in new[] { (flagged, (byte)1), (unflagged, (byte)0) })
            {
                createdXpathIds.Add(await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModelRequestXpath
                {
                    EntityAnalysisModelId = model.Id, Name = name, XPath = "$." + name, DataTypeId = 1, Active = 1,
                    Deleted = 0, ReportTable = report, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = authorA,
                    Guid = Guid.NewGuid()
                }));
            }

            var service = await BuildServiceAsync(dbContext, authorA);
            var keys = await service.KeysAsync();
            var found = keys.Should().ContainSingle(k => k.Key == flagged).Subject;
            found.Models.Should().Contain(model.Name);
            found.UnflaggedModels.Should().BeEmpty();
            keys.Should().ContainSingle(k => k.Key == unflagged).Which.UnflaggedModels.Should().Contain(model.Name);
        }

        private static Task<GraphRegistryEdgeService> BuildServiceAsync(DbContext dbContext, string? userName)
        {
            return GraphRegistryEdgeService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), TestLog.NoOp);
        }

        private GraphRegistryEdgeDto ValidDto(string name, int kind, string from, string to, string? amount) => new()
        {
            GraphRegistryId = registryId,
            Name = $"{DatabaseFixture.Prefix}{name}{Guid.NewGuid():N}"[..40],
            Active = true,
            Locked = false,
            KindId = kind,
            FromKey = from,
            ToKey = to,
            AmountKey = amount,
            HalfLifeDays = 90,
            KindPrior = 1,
            MinimumAmount = 0
        };

        private async Task<string> SeedActorAsync(DbContext dbContext, int tenantRegistryId, string label,
            int[] specs)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var roleGuid = Guid.NewGuid();
            var roleId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = roleGuid,
                Name = $"{DatabaseFixture.Prefix}RoleGraph{label}{suffix}",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenantRegistryId,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdRoleRegistryIds.Add(roleId);

            foreach (var spec in specs)
            {
                await dbContext.InsertAsync(new Data.Poco.RoleRegistryPermission
                {
                    Guid = Guid.NewGuid(),
                    PermissionSpecificationId = spec,
                    RoleRegistryId = roleId,
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    Version = 1,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = DatabaseFixture.Prefix
                });
            }

            var userName = $"{DatabaseFixture.Prefix}UserGraph{label}{suffix}";
            await dbContext.InsertAsync(new Data.Poco.UserRegistry
            {
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleGuid,
                Name = userName,
                Email = $"{userName}@example.invalid",
                Password = "not-used-by-permission-checks",
                Active = 1,
                PasswordLocked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            await dbContext.InsertAsync(new Data.Poco.UserInTenant { User = userName, TenantRegistryId = tenantRegistryId });
            createdUserNames.Add(userName);

            return userName;
        }

        private async Task CleanupActorsAsync(DbContext dbContext)
        {
            await dbContext.UserInTenant.Where(w => createdUserNames.Contains(w.User)).DeleteAsync();
            await dbContext.UserRegistry.Where(w => createdUserNames.Contains(w.Name)).DeleteAsync();
            await dbContext.RoleRegistryPermission
                .Where(w => w.RoleRegistryId != null && createdRoleRegistryIds.Contains(w.RoleRegistryId.Value))
                .DeleteAsync();
            await dbContext.RoleRegistry.Where(w => createdRoleRegistryIds.Contains(w.Id)).DeleteAsync();
        }
    }
}
