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

namespace Jube.Migrations.Branches.CasesPage
{
    using FluentMigrator;

    [Migration(20263108000000)]
    public class CreateCaseKeyJournalPinTable : Migration
    {
        public override void Up()
        {
            Create.Table("CaseKeyJournalPin")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseId").AsInt32().NotNullable()
                .WithColumn("EntityAnalysisModelInstanceEntryGuid").AsGuid().NotNullable()
                .WithColumn("Pinned").AsByte().NotNullable().WithDefaultValue(1)
                .WithColumn("CreatedUser").AsString().NotNullable()
                .WithColumn("CreatedDate").AsDateTime2().NotNullable()
                .WithColumn("UpdatedUser").AsString().NotNullable()
                .WithColumn("UpdatedDate").AsDateTime2().NotNullable();

            Create.ForeignKey().FromTable("CaseKeyJournalPin").ForeignColumn("CaseId")
                .ToTable("Case").PrimaryColumn("Id").OnDelete(System.Data.Rule.Cascade);

            Create.Index("IX_CaseKeyJournalPin_CaseId_Guid").OnTable("CaseKeyJournalPin")
                .OnColumn("CaseId").Ascending().OnColumn("EntityAnalysisModelInstanceEntryGuid").Ascending()
                .WithOptions().Unique();

            Execute.Sql("""
                        COMMENT ON TABLE public."CaseKeyJournalPin" IS
                        'Transactions an analyst has pinned in the Case Key Journal of a Case. A pinned transaction stays at the top of the journal in every lens and window and travels with the case as a fact, so everyone working the case sees the same ones. One row per case and transaction; unpinning sets Pinned to 0 rather than removing the row.';
                        COMMENT ON COLUMN public."CaseKeyJournalPin"."Id" IS 'Server-assigned identity of the pin.';
                        COMMENT ON COLUMN public."CaseKeyJournalPin"."CaseId" IS 'The Case the transaction is pinned on; the pins are removed with the case.';
                        COMMENT ON COLUMN public."CaseKeyJournalPin"."EntityAnalysisModelInstanceEntryGuid" IS 'The archived transaction that is pinned, matching EntityAnalysisModelInstanceEntryGuid on Archive.';
                        COMMENT ON COLUMN public."CaseKeyJournalPin"."Pinned" IS '1 while the transaction is pinned, 0 once it has been unpinned.';
                        COMMENT ON COLUMN public."CaseKeyJournalPin"."CreatedUser" IS 'The user who first pinned the transaction.';
                        COMMENT ON COLUMN public."CaseKeyJournalPin"."CreatedDate" IS 'When the transaction was first pinned (UTC).';
                        COMMENT ON COLUMN public."CaseKeyJournalPin"."UpdatedUser" IS 'The user who last pinned or unpinned the transaction.';
                        COMMENT ON COLUMN public."CaseKeyJournalPin"."UpdatedDate" IS 'When the transaction was last pinned or unpinned (UTC).';
                        """);
        }

        public override void Down()
        {
        }
    }
}
