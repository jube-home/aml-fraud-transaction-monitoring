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

namespace Jube.Migrations.Branches.SanctionRescreen
{
    using FluentMigrator;

    [Migration(2026111021000000)]
    public class CreateSanctionRescreen : Migration
    {
        public override void Up()
        {
            Alter.Table("SanctionEntry").AddColumn("LastImportId").AsInt32().Nullable();

            Create.Table("SanctionRescreenRun")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("UpToImportId").AsInt32().NotNullable()
                .WithColumn("StartDate").AsDateTime().NotNullable()
                .WithColumn("EndDate").AsDateTime().Nullable()
                .WithColumn("ChecksScreened").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("NamesScreened").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("EntriesConsidered").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("Hits").AsInt32().NotNullable().WithDefaultValue(0);

            Create.Table("SanctionRescreenHit")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("RunId").AsInt32().NotNullable()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().NotNullable()
                .WithColumn("ScreenedName").AsString(500).NotNullable()
                .WithColumn("NameKey").AsString(500).NotNullable()
                .WithColumn("SanctionEntryId").AsInt32().NotNullable()
                .WithColumn("SanctionEntrySourceId").AsInt32().NotNullable()
                .WithColumn("SanctionEntryReference").AsString(500).NotNullable()
                .WithColumn("ListedName").AsString(500).NotNullable()
                .WithColumn("Distance").AsInt32().NotNullable()
                .WithColumn("CreatedDate").AsDateTime().NotNullable();

            Create.Index("IX_SanctionRescreenHit_Once")
                .OnTable("SanctionRescreenHit")
                .OnColumn("TenantRegistryId").Ascending()
                .OnColumn("NameKey").Ascending()
                .OnColumn("SanctionEntryId").Ascending()
                .WithOptions().Unique();

            Execute.Sql("""
                        COMMENT ON COLUMN public."SanctionEntry"."LastImportId" IS 'The SanctionEntryImport in which this entry was last inserted or revived. Entries changed after the last rescreen are the ones rescreened.';
                        COMMENT ON TABLE public."SanctionRescreenRun" IS 'One pass of the rescreen that checks names already screened in recent transactions against the sanction entries added or revived by a list import. UpToImportId is the watermark: entries from imports up to it have been rescreened.';
                        COMMENT ON TABLE public."SanctionRescreenHit" IS 'A name that was screened earlier and matches a sanction entry that arrived with a later list update, found by the rescreen. One row per tenant, name and entry.';
                        COMMENT ON COLUMN public."SanctionRescreenHit"."NameKey" IS 'The screened name folded and tokenised the way the matcher does, tokens sorted.';
                        COMMENT ON COLUMN public."SanctionRescreenHit"."ListedName" IS 'The listed name of the new entry as held by the matcher, folded and lower case.';
                        COMMENT ON COLUMN public."SanctionRescreenHit"."Distance" IS 'The edit distance of the match, the worst distance across its tokens.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
