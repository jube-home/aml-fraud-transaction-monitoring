/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

namespace Jube.Migrations.Branches.MakerChecker
{
    using FluentMigrator;

    [Migration(20260929120600)]
    public class BackfillEntityApprovalForAdvancedAnalyticsInstances : Migration
    {
        private const string ApprovedBy = "Administrator";

        public override void Up()
        {
            Execute.Sql($"""
                         insert into "EntityApproval" ("TenantRegistryId","EntityApprovalKindId","EntityId",
                             "EntityVersion","StateId","CreatedDate","CreatedUser","Guid")
                         select r."TenantRegistryId", 18, r."Id", r."Version", 1,
                             now() at time zone 'utc', '{ApprovedBy}', gen_random_uuid()
                         from "AdvancedAnalyticsInstance" r
                         where r."Deleted" = 0;
                         """);
        }

        public override void Down()
        {
        }
    }
}