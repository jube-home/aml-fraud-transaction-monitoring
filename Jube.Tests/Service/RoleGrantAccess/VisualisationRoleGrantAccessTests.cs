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
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.Service.RoleGrantAccess
{
    public sealed class VisualisationRoleGrantAccessTests(DatabaseFixture fx) : RoleGrantAccessBase(fx)
    {
        public static IEnumerable<object[]> Subjects =>
            new[]
            {
                "VisualisationRegistry", "VisualisationRegistryDirectory", "VisualisationRegistryDatasource",
                "VisualisationRegistryParameter"
            }.Select(subject => new object[] { subject });

        private async Task<(RoleGrantWorld World, ArrangedSubject Subject)> ArrangeAsync(string subject)
        {
            var world = await CreateWorldAsync();
            var (registryId, registryGuid) = await CreateRegistryAsync(world.TenantRegistryId);

            switch (subject)
            {
                case "VisualisationRegistry":
                    return (world, new ArrangedSubject("VisualisationRegistryRole", registryGuid, async userName =>
                    {
                        await using var dbContext = Fx.GetDbContext();
                        return await new Data.Repository.VisualisationRegistryRepository(dbContext, userName)
                            .GetByGuidActiveOnlyAsync(registryGuid) != null;
                    }));
                case "VisualisationRegistryDirectory":
                    return (world, new ArrangedSubject("VisualisationRegistryRole", registryGuid, async userName =>
                    {
                        await using var dbContext = Fx.GetDbContext();
                        return (await new Data.Repository.VisualisationRegistryRepository(dbContext, userName)
                            .GetByShowInDirectoryActiveOrderByIdDescAsync()).Any(w => w.Guid == registryGuid);
                    }));
            }

            await GrantAsync("VisualisationRegistryRole", registryGuid, world.RoleAGuid);
            await GrantAsync("VisualisationRegistryRole", registryGuid, world.RoleBGuid);

            await using var db = Fx.GetDbContext();
            var childGuid = Guid.NewGuid();
            var name = $"{DatabaseFixture.Prefix}RG{subject}{Guid.NewGuid():N}"[..40];

            if (subject == "VisualisationRegistryDatasource")
            {
                await db.InsertWithInt32IdentityAsync(new Data.Poco.VisualisationRegistryDatasource
                {
                    Name = name, Guid = childGuid, VisualisationRegistryId = registryId, Active = 1, Locked = 0,
                    Deleted = 0, Version = 1, CreatedUser = Fx.Seed.UserWithPermission, CreatedDate = DateTime.UtcNow
                });
                return (world, new ArrangedSubject("VisualisationRegistryDatasourceRole", childGuid, async userName =>
                {
                    await using var dbContext = Fx.GetDbContext();
                    return (await new Data.Repository.VisualisationRegistryDatasourceRepository(dbContext, userName)
                        .GetByVisualisationRegistryIdActiveOnlyAsync(registryId)).Any(w => w.Guid == childGuid);
                }));
            }

            await db.InsertWithInt32IdentityAsync(new Data.Poco.VisualisationRegistryParameter
            {
                Name = name, Guid = childGuid, VisualisationRegistryId = registryId, Active = 1, Locked = 0,
                Deleted = 0, Version = 1, CreatedUser = Fx.Seed.UserWithPermission, CreatedDate = DateTime.UtcNow
            });
            return (world, new ArrangedSubject("VisualisationRegistryParameterRole", childGuid, async userName =>
            {
                await using var dbContext = Fx.GetDbContext();
                return (await new Data.Repository.VisualisationRegistryParameterRepository(dbContext, userName)
                    .GetByVisualisationRegistryIdActiveOnlyAsync(registryId)).Any(w => w.Guid == childGuid);
            }));
        }

        [Theory]
        [MemberData(nameof(Subjects))]
        public async Task NoGrantMeansNoAccessAsync(string subject)
        {
            var (world, arranged) = await ArrangeAsync(subject);
            await ScenarioNoGrantMeansNoAccessAsync(arranged, world);
        }

        [Theory]
        [MemberData(nameof(Subjects))]
        public async Task GrantGivesAccessToThatRoleOnlyAsync(string subject)
        {
            var (world, arranged) = await ArrangeAsync(subject);
            await ScenarioGrantGivesAccessToThatRoleOnlyAsync(arranged, world);
        }

        [Theory]
        [MemberData(nameof(Subjects))]
        public async Task RevokeRemovesAccessAtOnceAsync(string subject)
        {
            var (world, arranged) = await ArrangeAsync(subject);
            await ScenarioRevokeRemovesAccessAtOnceAsync(arranged, world);
        }

        [Theory]
        [MemberData(nameof(Subjects))]
        public async Task RegrantAfterRevokeRestoresAccessAsync(string subject)
        {
            var (world, arranged) = await ArrangeAsync(subject);
            await ScenarioRegrantAfterRevokeRestoresAccessAsync(arranged, world);
        }

        [Theory]
        [MemberData(nameof(Subjects))]
        public async Task EachRoleKeepsItsOwnAccessAsync(string subject)
        {
            var (world, arranged) = await ArrangeAsync(subject);
            await ScenarioEachRoleKeepsItsOwnAccessAsync(arranged, world);
        }

        [Theory]
        [MemberData(nameof(Subjects))]
        public async Task DeletedRoleLosesAccessWithoutErrorsAsync(string subject)
        {
            var (world, arranged) = await ArrangeAsync(subject);
            await ScenarioDeletedRoleLosesAccessWithoutErrorsAsync(arranged, world);
        }

        [Theory]
        [MemberData(nameof(Subjects))]
        public async Task MovingAUserToAnotherRoleMovesTheirAccessAsync(string subject)
        {
            var (world, arranged) = await ArrangeAsync(subject);
            await ScenarioMovingAUserToAnotherRoleMovesTheirAccessAsync(arranged, world);
        }

        [Theory]
        [MemberData(nameof(Subjects))]
        public async Task AnotherTenantsRoleNeverGrantsAsync(string subject)
        {
            var (world, arranged) = await ArrangeAsync(subject);
            await ScenarioAnotherTenantsRoleNeverGrantsAsync(arranged, world);
        }
    }
}