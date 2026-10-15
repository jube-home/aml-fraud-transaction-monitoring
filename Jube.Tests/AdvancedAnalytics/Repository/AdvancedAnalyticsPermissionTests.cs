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

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Repository
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class AdvancedAnalyticsPermissionTests(DatabaseFixture fx)
    {
        [Fact]
        public async Task TheFourPermissionsExistWithTheAgreedIdsAndNamesAsync()
        {
            await using var db = fx.GetDbContext();

            var rows = await db.PermissionSpecification.Where(w => w.Id == 46 || w.Id == 47 || w.Id == 48
                                                                   || w.Id == 66).OrderBy(o => o.Id).ToListAsync();

            rows.Select(s => (s.Id, s.Name)).Should().Equal(
                (46, "Advanced Analytics Recall"),
                (47, "Advanced Analytics Promote Model"),
                (48, "Advanced Analytics"),
                (66, "Advanced Analytics Training"));

            db.PermissionSpecification.Max(m => m.Id).Should().BeGreaterThanOrEqualTo(66);
        }

        [Fact]
        public async Task TheAdministratorRoleIsGrantedAllFourAsync()
        {
            await using var db = fx.GetDbContext();

            var granted = await db.RoleRegistryPermission
                .Where(w => w.RoleRegistryId == 1 && w.PermissionSpecificationId >= 44
                                                  && w.PermissionSpecificationId <= 47)
                .Select(s => s.PermissionSpecificationId).ToListAsync();

            granted.OrderBy(o => o).Should().Equal(44, 45, 46, 47);
        }
    }
}