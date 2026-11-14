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

namespace Jube.Migrations.Branches.TelemetryPermissionsRetired
{
    using FluentMigrator;

    [Migration(2026110812000000)]
    public class RetireOpenTelemetryPermissions : Migration
    {
        public override void Up()
        {
            Update.Table("PermissionSpecification")
                .Set(new { Name = "Retired - no effect (OpenTelemetry Log Counter is landlord only)" })
                .Where(new { Id = 42 });

            Update.Table("PermissionSpecification")
                .Set(new { Name = "Retired - no effect (OpenTelemetry Exclude is landlord only)" })
                .Where(new { Id = 43 });
        }

        public override void Down()
        {
        }
    }
}
