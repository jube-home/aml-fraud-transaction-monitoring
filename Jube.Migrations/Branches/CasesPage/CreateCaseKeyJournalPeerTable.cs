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

    [Migration(20263108010000)]
    public class CreateCaseKeyJournalPeerTable : Migration
    {
        public override void Up()
        {
            Create.Table("CaseKeyJournalPeer")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseWorkflowGuid").AsGuid().NotNullable()
                .WithColumn("CaseKey").AsString().NotNullable()
                .WithColumn("Field").AsString().NotNullable()
                .WithColumn("Segment").AsString().Nullable()
                .WithColumn("KeyCount").AsInt32().NotNullable()
                .WithColumn("RowCount").AsInt32().NotNullable()
                .WithColumn("Median").AsDouble().NotNullable()
                .WithColumn("Scale").AsDouble().NotNullable()
                .WithColumn("Q1").AsDouble().NotNullable()
                .WithColumn("Q3").AsDouble().NotNullable()
                .WithColumn("Minimum").AsDouble().NotNullable()
                .WithColumn("Maximum").AsDouble().NotNullable()
                .WithColumn("Mean").AsDouble().NotNullable()
                .WithColumn("Bins").AsCustom("jsonb").NotNullable()
                .WithColumn("WindowDays").AsInt32().NotNullable()
                .WithColumn("ComputedDate").AsDateTime2().NotNullable()
                .WithColumn("Deleted").AsByte().NotNullable().WithDefaultValue(0)
                .WithColumn("DeletedDate").AsDateTime2().Nullable();

            Execute.Sql("""
                        CREATE UNIQUE INDEX "IX_CaseKeyJournalPeer_Workflow_Field_Segment"
                        ON public."CaseKeyJournalPeer" ("CaseWorkflowGuid", "CaseKey", "Field", COALESCE("Segment", ''));

                        COMMENT ON TABLE public."CaseKeyJournalPeer" IS
                        'How the case keys of a Case Workflow compare with one another on each numeric field of the Case Key Journal, so that a key can be read against its peers as well as against its own history. One row per workflow, case key, field and segment, holding the distribution of the median of the field for each case key in the window. It is rebuilt by the jobs server from the Archive; until it has been, the journal says peers are not available. Each rebuild updates the row for a workflow, case key, field and segment in place, or adds it, and marks as deleted any row it did not rebuild, so that a distribution that no longer qualifies stops being used without being removed. Rows are never edited by hand.';
                        COMMENT ON COLUMN public."CaseKeyJournalPeer"."Id" IS 'Server-assigned identity of the peer distribution.';
                        COMMENT ON COLUMN public."CaseKeyJournalPeer"."CaseWorkflowGuid" IS 'The Case Workflow whose model scored the transactions, matching Guid on CaseWorkflow.';
                        COMMENT ON COLUMN public."CaseKeyJournalPeer"."CaseKey" IS 'The payload field whose values are the case keys being compared, such as AccountId. A workflow can have several, and each is compared only with the others of its own kind.';
                        COMMENT ON COLUMN public."CaseKeyJournalPeer"."Field" IS 'The journal column the distribution is of, named as the Case Key Journal names it, such as Amount or abstraction:Volume.';
                        COMMENT ON COLUMN public."CaseKeyJournalPeer"."Segment" IS 'The value of the workflow''s segment field (a Case Workflow XPath with Segment set) that the peers share, such as a customer tier. Null for all keys of the workflow.';
                        COMMENT ON COLUMN public."CaseKeyJournalPeer"."KeyCount" IS 'How many case keys the distribution is of. A distribution is only stored for at least 20 keys.';
                        COMMENT ON COLUMN public."CaseKeyJournalPeer"."RowCount" IS 'How many transactions those keys had in the window.';
                        COMMENT ON COLUMN public."CaseKeyJournalPeer"."Median" IS 'Median, across the keys, of each key''s median value of the field.';
                        COMMENT ON COLUMN public."CaseKeyJournalPeer"."Scale" IS 'Robust spread of the key medians: the scaled median absolute deviation, falling back to the interquartile range and then the standard deviation. A key is scored against it as a robust z-score.';
                        COMMENT ON COLUMN public."CaseKeyJournalPeer"."Q1" IS 'First quartile of the key medians.';
                        COMMENT ON COLUMN public."CaseKeyJournalPeer"."Q3" IS 'Third quartile of the key medians.';
                        COMMENT ON COLUMN public."CaseKeyJournalPeer"."Minimum" IS 'Smallest key median.';
                        COMMENT ON COLUMN public."CaseKeyJournalPeer"."Maximum" IS 'Largest key median.';
                        COMMENT ON COLUMN public."CaseKeyJournalPeer"."Mean" IS 'Mean of the key medians.';
                        COMMENT ON COLUMN public."CaseKeyJournalPeer"."Bins" IS 'Histogram of the key medians as a JSON array of objects with From, To and Count, used to place a key as a percentile of its peers.';
                        COMMENT ON COLUMN public."CaseKeyJournalPeer"."WindowDays" IS 'How many days of transactions the distribution was built from.';
                        COMMENT ON COLUMN public."CaseKeyJournalPeer"."ComputedDate" IS 'When the distribution was built (UTC).';
                        COMMENT ON COLUMN public."CaseKeyJournalPeer"."Deleted" IS '1 when the last rebuild did not produce this distribution, for example because fewer than 20 keys now have the field; the journal ignores it. A later rebuild that produces it again sets this back to 0.';
                        COMMENT ON COLUMN public."CaseKeyJournalPeer"."DeletedDate" IS 'When the distribution was marked deleted (UTC), null while it is in use.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
