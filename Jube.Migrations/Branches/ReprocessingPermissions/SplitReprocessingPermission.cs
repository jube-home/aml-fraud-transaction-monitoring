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

namespace Jube.Migrations.Branches.ReprocessingPermissions
{
    using FluentMigrator;

    [Migration(20263108630001)]
    public class SplitReprocessingPermission : Migration
    {
        public const int EditRulePermissionId = 26;
        public const int TriggerPermissionId = 73;
        public const int StopPermissionId = 74;

        public const string EditRuleName = "Read Write Reprocessing Rule";
        public const string TriggerName = "Trigger Reprocessing Run";
        public const string StopName = "Stop or Delete Reprocessing Run";

        public static string BuildBackfillSql(int permissionId)
        {
            return $"""
                    INSERT INTO public."RoleRegistryPermission" ("RoleRegistryId", "PermissionSpecificationId", "Active",
                        "Locked", "CreatedDate", "CreatedUser", "Version", "Guid")
                    SELECT DISTINCT ON (p."RoleRegistryId") p."RoleRegistryId", {permissionId}, p."Active", p."Locked",
                        now() at time zone 'utc', 'Administrator', 1, gen_random_uuid()
                    FROM public."RoleRegistryPermission" p
                    WHERE p."PermissionSpecificationId" = {EditRulePermissionId}
                      AND (p."Deleted" = 0 OR p."Deleted" IS NULL)
                      AND NOT EXISTS (SELECT 1 FROM public."RoleRegistryPermission" x
                                      WHERE x."RoleRegistryId" = p."RoleRegistryId"
                                        AND x."PermissionSpecificationId" = {permissionId}
                                        AND (x."Deleted" = 0 OR x."Deleted" IS NULL))
                    ORDER BY p."RoleRegistryId", p."Active" DESC NULLS LAST, p."Id";

                    INSERT INTO public."RoleRegistryPermission" ("RoleRegistryId", "PermissionSpecificationId", "Active",
                        "CreatedDate", "CreatedUser", "Version", "Guid")
                    SELECT 1, {permissionId}, 1, now() at time zone 'utc', 'Administrator', 1, gen_random_uuid()
                    WHERE NOT EXISTS (SELECT 1 FROM public."RoleRegistryPermission" x
                                      WHERE x."RoleRegistryId" = 1
                                        AND x."PermissionSpecificationId" = {permissionId}
                                        AND (x."Deleted" = 0 OR x."Deleted" IS NULL));
                    """;
        }

        public override void Up()
        {
            Update.Table("PermissionSpecification")
                .Set(new { Name = EditRuleName })
                .Where(new { Id = EditRulePermissionId });

            Insert.IntoTable("PermissionSpecification").Row(new
            {
                Id = TriggerPermissionId,
                Name = TriggerName
            });

            Insert.IntoTable("PermissionSpecification").Row(new
            {
                Id = StopPermissionId,
                Name = StopName
            });

            Execute.Sql(BuildBackfillSql(TriggerPermissionId));
            Execute.Sql(BuildBackfillSql(StopPermissionId));
        }

        public override void Down()
        {
        }
    }
}
