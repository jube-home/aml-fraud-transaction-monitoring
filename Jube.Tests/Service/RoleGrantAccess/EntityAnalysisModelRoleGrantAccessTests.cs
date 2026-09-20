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

// ReSharper disable once RedundantUsingDirective

using System;
using System.Linq;
using System.Threading.Tasks;
using Jube.Test.Infrastructure.DatabaseFixture;
using Xunit;

namespace Jube.Test.Service.RoleGrantAccess
{
    public sealed class EntityAnalysisModelRoleGrantAccessTests(DatabaseFixture fx) : RoleGrantAccessBase(fx)
    {
        private async Task<(RoleGrantWorld World, ArrangedSubject Subject)> ArrangeAsync()
        {
            var world = await CreateWorldAsync();
            var (modelId, modelGuid) = await CreateModelAsync();

            await AddApiKeyAsync(world.UserA);
            await AddApiKeyAsync(world.UserB);

            return (world, new ArrangedSubject("EntityAnalysisModelRole", modelGuid, async userName =>
            {
                await using var dbContext = Fx.GetDbContext();
                return (await new Data.Query.GetEntityAnalysisModelApiUsersQuery(dbContext).ExecuteAsync(modelId))
                    .Any(w => w.Username == userName);
            }));
        }

        [Fact]
        public async Task NoGrantMeansNoAccessAsync()
        {
            var (world, arranged) = await ArrangeAsync();
            await ScenarioNoGrantMeansNoAccessAsync(arranged, world);
        }

        [Fact]
        public async Task GrantGivesAccessToThatRoleOnlyAsync()
        {
            var (world, arranged) = await ArrangeAsync();
            await ScenarioGrantGivesAccessToThatRoleOnlyAsync(arranged, world);
        }

        [Fact]
        public async Task RevokeRemovesAccessAtOnceAsync()
        {
            var (world, arranged) = await ArrangeAsync();
            await ScenarioRevokeRemovesAccessAtOnceAsync(arranged, world);
        }

        [Fact]
        public async Task RegrantAfterRevokeRestoresAccessAsync()
        {
            var (world, arranged) = await ArrangeAsync();
            await ScenarioRegrantAfterRevokeRestoresAccessAsync(arranged, world);
        }

        [Fact]
        public async Task EachRoleKeepsItsOwnAccessAsync()
        {
            var (world, arranged) = await ArrangeAsync();
            await ScenarioEachRoleKeepsItsOwnAccessAsync(arranged, world);
        }

        [Fact]
        public async Task DeletedRoleLosesAccessWithoutErrorsAsync()
        {
            var (world, arranged) = await ArrangeAsync();
            await ScenarioDeletedRoleLosesAccessWithoutErrorsAsync(arranged, world);
        }

        [Fact]
        public async Task MovingAUserToAnotherRoleMovesTheirAccessAsync()
        {
            var (world, arranged) = await ArrangeAsync();
            await ScenarioMovingAUserToAnotherRoleMovesTheirAccessAsync(arranged, world);
        }

        [Fact]
        public async Task AnotherTenantsRoleNeverGrantsAsync()
        {
            var (world, arranged) = await ArrangeAsync();
            await ScenarioAnotherTenantsRoleNeverGrantsAsync(arranged, world);
        }
    }
}