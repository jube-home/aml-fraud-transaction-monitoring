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

namespace Jube.Migrations.Branches.MakerChecker
{
    using FluentMigrator;

    [Migration(20260929120100)]
    public class AddEntityApprovalTable : Migration
    {
        public override void Up()
        {
            Create.Table("EntityApproval")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("TenantRegistryId").AsInt32().Nullable()
                .WithColumn("EntityApprovalKindId").AsInt32().Nullable()
                .WithColumn("EntityId").AsInt32().Nullable()
                .WithColumn("EntityVersion").AsInt32().Nullable()
                .WithColumn("StateId").AsInt32().Nullable()
                .WithColumn("Note").AsCustom("text").Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Guid").AsGuid().Nullable();

            Create.ForeignKey().FromTable("EntityApproval").ForeignColumn("TenantRegistryId")
                .ToTable("TenantRegistry").PrimaryColumn("Id");

            Create.Index("IX_EntityApproval_Entity")
                .OnTable("EntityApproval")
                .OnColumn("EntityApprovalKindId").Ascending()
                .OnColumn("EntityId").Ascending()
                .OnColumn("EntityVersion").Ascending()
                .OnColumn("CreatedUser").Ascending()
                .WithOptions().Unique();

            Create.Index("IX_EntityApproval_TenantKind")
                .OnTable("EntityApproval")
                .OnColumn("TenantRegistryId").Ascending()
                .OnColumn("EntityApprovalKindId").Ascending()
                .OnColumn("EntityId").Ascending()
                .OnColumn("EntityVersion").Ascending();

            Create.Index("IX_EntityApproval_TenantId")
                .OnTable("EntityApproval")
                .OnColumn("TenantRegistryId").Ascending()
                .OnColumn("Id").Ascending();
        }

        public override void Down()
        {
        }
    }
}