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

namespace Jube.Migrations.Branches.CaseChecklists
{
    using System;
    using FluentMigrator;

    [Migration(20263108440000)]
    public class CreateCaseChecklistOverride : Migration
    {
        public override void Up()
        {
            Create.Table("CaseChecklistOverride")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseId").AsInt32().NotNullable()
                .WithColumn("CaseWorkflowStatusGuid").AsGuid().NotNullable()
                .WithColumn("Reason").AsString(int.MaxValue).NotNullable()
                .WithColumn("OpenItemsJson").AsString(int.MaxValue).Nullable()
                .WithColumn("GrantedUser").AsString().NotNullable()
                .WithColumn("GrantedDate").AsDateTime2().NotNullable()
                .WithColumn("ConsumedDate").AsDateTime2().Nullable()
                .WithColumn("ConsumedUser").AsString().Nullable();

            Create.Index("IX_CaseChecklistOverride_CaseId").OnTable("CaseChecklistOverride")
                .OnColumn("CaseId").Ascending();

            Execute.Sql("""
                        CREATE UNIQUE INDEX "UX_CaseChecklistOverride_OpenGrant"
                            ON public."CaseChecklistOverride" ("CaseId", "CaseWorkflowStatusGuid")
                            WHERE "ConsumedDate" IS NULL;
                        """);

            Insert.IntoTable("PermissionSpecification").Row(new
            {
                Id = 71,
                Name = "Override Case Checklist Gate"
            });

            Insert.IntoTable("RoleRegistryPermission").Row(new
            {
                RoleRegistryId = 1,
                PermissionSpecificationId = 71,
                Active = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = "Administrator",
                Version = 1,
                Guid = Guid.NewGuid()
            });

            Execute.Sql("""
                        INSERT INTO public."RoleRegistryPermission" ("RoleRegistryId", "PermissionSpecificationId", "Active",
                            "CreatedDate", "CreatedUser", "Version", "Guid")
                        SELECT DISTINCT p."RoleRegistryId", 71, 1, now() at time zone 'utc', 'Administrator', 1, gen_random_uuid()
                        FROM public."RoleRegistryPermission" p
                        WHERE p."PermissionSpecificationId" = 18
                          AND p."Active" = 1
                          AND (p."Deleted" = 0 OR p."Deleted" IS NULL)
                          AND NOT EXISTS (SELECT 1 FROM public."RoleRegistryPermission" x
                                          WHERE x."RoleRegistryId" = p."RoleRegistryId" AND x."PermissionSpecificationId" = 71);

                        COMMENT ON TABLE public."CaseChecklistOverride" IS 'A supervisor''s recorded grant that lets the holder of a case leave its current status, or close it, while checklist items that are shown are still not ticked. The grant is consumed by the move that uses it. Joins Case by CaseId. A grant is valid only while the case is still in CaseWorkflowStatusGuid; once the case has left that status the grant is dead and is never consumed. The override does not tick any item. The case holder can never grant one, and the user who granted it can never be the user who consumes it.';
                        COMMENT ON COLUMN public."CaseChecklistOverride"."CaseWorkflowStatusGuid" IS 'The status the case was in when the grant was made; the grant is valid only while the case remains in it.';
                        COMMENT ON COLUMN public."CaseChecklistOverride"."Reason" IS 'The mandatory reason the supervisor gave for overriding the gate.';
                        COMMENT ON COLUMN public."CaseChecklistOverride"."OpenItemsJson" IS 'JSON array of the items, with CaseChecklistItem Id and name, that were shown and not ticked when the grant was made.';
                        COMMENT ON COLUMN public."CaseChecklistOverride"."GrantedUser" IS 'The supervisor who granted the override; needs permission 71 and must not be the holder of the case.';
                        COMMENT ON COLUMN public."CaseChecklistOverride"."ConsumedDate" IS 'When the move that used the grant took place; null while the grant is still open. At most one grant per case and status is open at a time.';
                        COMMENT ON COLUMN public."CaseChecklistOverride"."ConsumedUser" IS 'The user whose status move or close consumed the grant; never the user who granted it.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
