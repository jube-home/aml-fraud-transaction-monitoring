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

namespace Jube.Migrations.Branches.PreservationSnapshot
{
    using FluentMigrator;

    [Migration(20263107470000)]
    public class AddPreservationSnapshot : Migration
    {
        public override void Up()
        {
            Create.Table("PreservationSnapshot")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("SnapshotSourceId").AsByte().NotNullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Json").AsCustom("jsonb").Nullable()
                .WithColumn("ExportVersion").AsInt32().Nullable()
                .WithColumn("ExportGuid").AsGuid().Nullable()
                .WithColumn("EntityAnalysisModelCount").AsInt32().Nullable()
                .WithColumn("Bytes").AsInt64().Nullable()
                .WithColumn("InError").AsByte().Nullable()
                .WithColumn("ErrorStack").AsString().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CompletedDate").AsDateTime2().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable();

            Create.Index("IX_PreservationSnapshot_TenantRegistryId_CreatedDate")
                .OnTable("PreservationSnapshot")
                .OnColumn("TenantRegistryId").Ascending()
                .OnColumn("CreatedDate").Descending();

            Create.Index("IX_PreservationSnapshot_TenantRegistryId_SnapshotSourceId")
                .OnTable("PreservationSnapshot")
                .OnColumn("TenantRegistryId").Ascending()
                .OnColumn("SnapshotSourceId").Ascending();

            Execute.Sql("""
                        CREATE INDEX "IX_PreservationSnapshot_Json"
                            ON "PreservationSnapshot" USING gin ("Json" jsonb_path_ops);
                        """);
        }

        public override void Down()
        {
        }
    }
}