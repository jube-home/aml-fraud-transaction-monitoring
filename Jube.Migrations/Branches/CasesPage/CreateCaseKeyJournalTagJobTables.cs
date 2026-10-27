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

    [Migration(20263108040000)]
    public class CreateCaseKeyJournalTagJobTables : Migration
    {
        public override void Up()
        {
            Create.Table("CaseKeyJournalTagJob")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().NotNullable()
                .WithColumn("CaseWorkflowGuid").AsGuid().NotNullable()
                .WithColumn("RequestedUser").AsString().NotNullable()
                .WithColumn("KindId").AsByte().NotNullable()
                .WithColumn("StateId").AsByte().NotNullable()
                .WithColumn("CaseKey").AsString().NotNullable()
                .WithColumn("KeyValue").AsString().NotNullable()
                .WithColumn("FromDate").AsDateTime2().NotNullable()
                .WithColumn("ToDate").AsDateTime2().NotNullable()
                .WithColumn("ActivationsOnly").AsByte().NotNullable().WithDefaultValue(0)
                .WithColumn("ResponseElevation").AsDouble().NotNullable().WithDefaultValue(0)
                .WithColumn("MergeJson").AsString().Nullable()
                .WithColumn("AddJson").AsString().Nullable()
                .WithColumn("RemoveJson").AsString().Nullable()
                .WithColumn("UndoOfGuid").AsGuid().Nullable()
                .WithColumn("Total").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("Processed").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("Changed").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("Skipped").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("Locked").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("CancelRequested").AsByte().NotNullable().WithDefaultValue(0)
                .WithColumn("Error").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().NotNullable()
                .WithColumn("StartedDate").AsDateTime2().Nullable()
                .WithColumn("HeartbeatDate").AsDateTime2().Nullable()
                .WithColumn("FinishedDate").AsDateTime2().Nullable();

            Create.Index("IX_CaseKeyJournalTagJob_Guid").OnTable("CaseKeyJournalTagJob")
                .OnColumn("Guid").Ascending().WithOptions().Unique();
            Create.Index("IX_CaseKeyJournalTagJob_State").OnTable("CaseKeyJournalTagJob")
                .OnColumn("StateId").Ascending().OnColumn("Id").Ascending();
            Create.Index("IX_CaseKeyJournalTagJob_User_Workflow").OnTable("CaseKeyJournalTagJob")
                .OnColumn("RequestedUser").Ascending().OnColumn("CaseWorkflowGuid").Ascending()
                .OnColumn("Id").Descending();

            Create.Table("CaseKeyJournalTagJobRow")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseKeyJournalTagJobId").AsInt64().NotNullable()
                .WithColumn("EntityAnalysisModelInstanceEntryGuid").AsGuid().NotNullable()
                .WithColumn("PreviousTagJson").AsString().NotNullable();

            Create.ForeignKey().FromTable("CaseKeyJournalTagJobRow").ForeignColumn("CaseKeyJournalTagJobId")
                .ToTable("CaseKeyJournalTagJob").PrimaryColumn("Id").OnDelete(System.Data.Rule.Cascade);

            Create.Index("IX_CaseKeyJournalTagJobRow_Job").OnTable("CaseKeyJournalTagJobRow")
                .OnColumn("CaseKeyJournalTagJobId").Ascending().OnColumn("Id").Ascending();

            Execute.Sql("""
                        COMMENT ON TABLE public."CaseKeyJournalTagJob" IS
                        'A request to add Tags to, and remove Tags from, every transaction that matches a view of the Case Key Journal, which is more than the journal shows at once. The jobs server works through the matching transactions in batches as the user who asked, so tenant, workflow, case lock and allowed Tag checks all apply, and records progress here so that the journal can show it. An undo is a job of its own that restores the Tags each changed transaction had, recorded in CaseKeyJournalTagJobRow.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."Id" IS 'Server-assigned identity of the job.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."Guid" IS 'Public identity of the job, used to follow its progress.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."CaseWorkflowGuid" IS 'The Case Workflow whose model scored the transactions.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."RequestedUser" IS 'The user who asked, and as whom the job runs.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."KindId" IS '1 Tag (apply the Tags to add and remove), 2 Undo (restore the Tags recorded by the job named in UndoOfGuid).';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."StateId" IS '1 Queued, 2 Running, 3 Done, 4 Failed (see Error), 5 Cancelled.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."CaseKey" IS 'The payload field of the journal view the job was started from, such as AccountId.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."KeyValue" IS 'The value of that field the view was for.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."FromDate" IS 'Start of the window of the view (UTC).';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."ToDate" IS 'End of the window of the view (UTC).';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."ActivationsOnly" IS '1 when the view showed only transactions that raised an activation.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."ResponseElevation" IS 'The least response elevation the view showed.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."MergeJson" IS 'The extra merged keys of the view, as a JSON array of objects with a key and a value, or null.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."AddJson" IS 'The Tag names to add, as a JSON array, or null.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."RemoveJson" IS 'The Tag names to remove, as a JSON array, or null.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."UndoOfGuid" IS 'For an undo, the job whose changes it reverses.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."Total" IS 'How many transactions matched when the job started.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."Processed" IS 'How many of them have been handled so far.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."Changed" IS 'How many had their Tags changed.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."Skipped" IS 'How many were left alone because they are not in the workflow''s model any more.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."Locked" IS 'How many were left alone because their case is locked to another user.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."CancelRequested" IS '1 once the user has asked for the job to stop; it stops at the end of the batch in hand.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."Error" IS 'Why a failed job failed, null otherwise.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."CreatedDate" IS 'When the job was asked for (UTC).';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."StartedDate" IS 'When the jobs server took the job up (UTC).';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."HeartbeatDate" IS 'When the jobs server last reported progress (UTC). A running job that has not for ten minutes is taken up again by another node.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJob"."FinishedDate" IS 'When the job reached Done, Failed or Cancelled (UTC).';
                        COMMENT ON TABLE public."CaseKeyJournalTagJobRow" IS
                        'The Tags a transaction had before a job changed it, kept so that the job can be undone. One row for each transaction the job changed.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJobRow"."Id" IS 'Server-assigned identity of the record.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJobRow"."CaseKeyJournalTagJobId" IS 'The job that changed the transaction; the rows are removed with the job.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJobRow"."EntityAnalysisModelInstanceEntryGuid" IS 'The archived transaction that was changed.';
                        COMMENT ON COLUMN public."CaseKeyJournalTagJobRow"."PreviousTagJson" IS 'The Tag names it carried before, as a JSON array.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
