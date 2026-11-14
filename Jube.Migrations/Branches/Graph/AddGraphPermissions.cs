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

namespace Jube.Migrations.Branches.Graph
{
    using System;
    using FluentMigrator;

    [Migration(20263108330000)]
    public class AddGraphPermissions : Migration
    {
        public override void Up()
        {
            Insert.IntoTable("PermissionSpecification").Row(new
            {
                Id = 49,
                Name = "View Graph"
            });

            Insert.IntoTable("PermissionSpecification").Row(new
            {
                Id = 50,
                Name = "Author Graph"
            });

            Insert.IntoTable("PermissionSpecification").Row(new
            {
                Id = 51,
                Name = "Resolve Graph Entities and Patterns"
            });

            foreach (var permission in new[] { 49, 50, 51 })
            {
                Insert.IntoTable("RoleRegistryPermission").Row(new
                {
                    RoleRegistryId = 1,
                    PermissionSpecificationId = permission,
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
