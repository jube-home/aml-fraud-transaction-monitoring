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
using FluentMigrator;

namespace Jube.Migrations.Branches.AskJooby
{
    [Migration(20260920180100)]
    public class AddAdvancedAnalyticsPermissions : Migration
    {
        private static readonly (int Id, string Name)[] permissions =
        [
            (66, "Advanced Analytics Training"),
            (46, "Advanced Analytics Recall"),
            (47, "Advanced Analytics Promote Model"),
            (48, "Advanced Analytics")
        ];

        public override void Up()
        {
            foreach (var (id, name) in permissions)
            {
                Insert.IntoTable("PermissionSpecification").Row(new
                {
                    Id = id,
                    Name = name
                });

                Insert.IntoTable("RoleRegistryPermission").Row(new
                {
                    RoleRegistryId = 1,
                    PermissionSpecificationId = id,
                    Active = 1,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = "Administrator",
                    Version = 1,
                    Guid = Guid.NewGuid()
                });
            }
        }

        public override void Down()
        {
        }
    }
}