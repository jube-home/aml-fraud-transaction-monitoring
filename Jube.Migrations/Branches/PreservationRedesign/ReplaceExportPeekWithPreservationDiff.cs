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


namespace Jube.Migrations.Branches.PreservationRedesign
{
    using FluentMigrator;

    [Migration(20263107820000)]
    public class ReplaceExportPeekWithPreservationDiff : Migration
    {
        public override void Up()
        {
            Rename.Table("ExportPeek").To("PreservationDiff");

            Alter.Table("PreservationDiff")
                .AddColumn("DiffKindId").AsByte().Nullable()
                .AddColumn("LeftSnapshotId").AsInt32().Nullable()
                .AddColumn("RightSnapshotId").AsInt32().Nullable()
                .AddColumn("RightLabel").AsString().Nullable()
                .AddColumn("AddedCount").AsInt32().Nullable()
                .AddColumn("RemovedCount").AsInt32().Nullable()
                .AddColumn("ChangedCount").AsInt32().Nullable();

            Create.Index("IX_PreservationDiff_TenantRegistryId_CreatedDate")
                .OnTable("PreservationDiff")
                .OnColumn("TenantRegistryId").Ascending()
                .OnColumn("CreatedDate").Descending();

            Execute.Sql("""
                        COMMENT ON TABLE public."PreservationDiff" IS
                        'One comparison of a tenant''s configuration, kept as an audit of what was compared and what differed: two snapshots, a snapshot against the live tenant, or the live tenant against a .jemp package about to be imported. Json holds the full result as plain json so it reads exactly as it was produced. Replaces the former ExportPeek table: rows written before DiffKindId existed have a NULL DiffKindId and are peeks, a readable rendering of what an export would contain, with no left or right side.';
                        COMMENT ON COLUMN public."PreservationDiff"."DiffKindId" IS
                        'What was compared, an enumeration id: 1=Two snapshots, 2=A snapshot against the live tenant, 3=The live tenant against a .jemp package an import was previewed for, 4=The live tenant against a snapshot a restore was previewed for. NULL means a legacy peek that predates the diff.';
                        COMMENT ON COLUMN public."PreservationDiff"."LeftSnapshotId" IS
                        'The snapshot on the left (the earlier state) of the comparison. NULL when the left side is the live tenant or the row is a legacy peek.';
                        COMMENT ON COLUMN public."PreservationDiff"."RightSnapshotId" IS
                        'The snapshot on the right (the later state, or the state being moved to) of the comparison. NULL when the right side is the live tenant or a .jemp package, or the row is a legacy peek.';
                        COMMENT ON COLUMN public."PreservationDiff"."RightLabel" IS
                        'A human label for a right side that is not a snapshot, such as the guid of the .jemp package a preview was made for. NULL otherwise.';
                        COMMENT ON COLUMN public."PreservationDiff"."AddedCount" IS
                        'How many entities exist on the right but not the left, counting every nested entity of an added entity. NULL for a legacy peek.';
                        COMMENT ON COLUMN public."PreservationDiff"."RemovedCount" IS
                        'How many entities exist on the left but not the right, counting every nested entity of a removed entity. NULL for a legacy peek.';
                        COMMENT ON COLUMN public."PreservationDiff"."ChangedCount" IS
                        'How many entities exist on both sides with at least one property differing, ignoring identifiers, audit stamps and references to other entities. NULL for a legacy peek.';
                        COMMENT ON COLUMN public."PreservationSnapshot"."SnapshotSourceId" IS
                        'What caused the snapshot. 1=Preservation page, a person acting directly, which is the only source that requires the snapshot permission; 2=Model synchronisation; 3=AskJooby, taken automatically before the assistant changes configuration; 4=Before import, taken automatically before a .jemp package or a snapshot is imported, so the import can be undone.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
