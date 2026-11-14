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
    using FluentMigrator;

    [Migration(20263108430000)]
    public class CreateCaseChecklistRuntime : Migration
    {
        public override void Up()
        {
            Create.Table("CaseChecklistItem")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseId").AsInt32().NotNullable()
                .WithColumn("CaseWorkflowChecklistItemGuid").AsGuid().NotNullable()
                .WithColumn("CaseWorkflowChecklistGuid").AsGuid().NotNullable()
                .WithColumn("CaseWorkflowStatusGuid").AsGuid().NotNullable()
                .WithColumn("ChecklistName").AsString().Nullable()
                .WithColumn("ItemName").AsString().Nullable()
                .WithColumn("Priority").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("StateId").AsByte().NotNullable()
                .WithColumn("FirstShownDate").AsDateTime2().NotNullable()
                .WithColumn("ShownDate").AsDateTime2().NotNullable()
                .WithColumn("DueDate").AsDateTime2().Nullable()
                .WithColumn("CompletedDate").AsDateTime2().Nullable()
                .WithColumn("CompletedUser").AsString().Nullable()
                .WithColumn("LastEvaluatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().NotNullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable();

            Create.Index("UX_CaseChecklistItem_CaseId_CaseWorkflowChecklistItemGuid").OnTable("CaseChecklistItem")
                .OnColumn("CaseId").Ascending()
                .OnColumn("CaseWorkflowChecklistItemGuid").Ascending()
                .WithOptions().Unique();

            Create.Table("CaseChecklistEvent")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseId").AsInt32().NotNullable()
                .WithColumn("CaseChecklistItemId").AsInt64().Nullable()
                .WithColumn("CaseWorkflowChecklistItemGuid").AsGuid().Nullable()
                .WithColumn("ActionId").AsByte().NotNullable()
                .WithColumn("Actor").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().NotNullable()
                .WithColumn("Detail").AsString(int.MaxValue).Nullable()
                .WithColumn("CaseEventId").AsInt32().Nullable();

            Create.Index().OnTable("CaseChecklistEvent")
                .OnColumn("CaseId").Ascending()
                .OnColumn("CreatedDate").Ascending();

            Alter.Table("Case").AddColumn("ChecklistReconciledDate").AsDateTime2().Nullable();

            Execute.Sql("""
                        COMMENT ON TABLE public."CaseChecklistItem" IS 'One checklist item as it stands on one case. A row is created only when the item is first shown on the case, from a CaseWorkflowChecklistItem of the checklists carried by the status the case is in, and is never deleted: an item whose show condition later fails is marked hidden, and a ticked item stays ticked. Names and Priority are copied from the definition when the item is first shown so that the case keeps its history if the definition is edited.';
                        COMMENT ON COLUMN public."CaseChecklistItem"."CaseId" IS 'The case the item is on. Joins Case on Id.';
                        COMMENT ON COLUMN public."CaseChecklistItem"."CaseWorkflowChecklistItemGuid" IS 'The definition the item came from. Joins CaseWorkflowChecklistItem on Guid. A case holds at most one row per definition.';
                        COMMENT ON COLUMN public."CaseChecklistItem"."CaseWorkflowChecklistGuid" IS 'The checklist the definition belonged to when the item was first shown. Joins CaseWorkflowChecklist on Guid.';
                        COMMENT ON COLUMN public."CaseChecklistItem"."CaseWorkflowStatusGuid" IS 'The status the case was in when the item was first shown. Only the items of the status the case is currently in count towards leaving it; rows of earlier statuses are kept as history.';
                        COMMENT ON COLUMN public."CaseChecklistItem"."ChecklistName" IS 'The name of the checklist when the item was first shown.';
                        COMMENT ON COLUMN public."CaseChecklistItem"."ItemName" IS 'The name of the item when it was first shown.';
                        COMMENT ON COLUMN public."CaseChecklistItem"."Priority" IS 'The Priority of the item when it was first shown; a lower number is listed first.';
                        COMMENT ON COLUMN public."CaseChecklistItem"."StateId" IS 'Where the item stands on the case: 1 Shown, the item is required and not yet ticked; 2 Hidden, the item is not required because its show condition is false or its definition is no longer in force; 3 Done, the item has been ticked and stays visible and ticked even if its show condition later fails.';
                        COMMENT ON COLUMN public."CaseChecklistItem"."FirstShownDate" IS 'UTC. When the item was first shown on the case. The SLA clock starts here and is not restarted if the item is hidden and shown again.';
                        COMMENT ON COLUMN public."CaseChecklistItem"."ShownDate" IS 'UTC. When the item was most recently shown on the case.';
                        COMMENT ON COLUMN public."CaseChecklistItem"."DueDate" IS 'UTC. FirstShownDate plus the SlaMinutes of the definition, fixed when the item is first shown. Blank when the item has no SLA. An item that is not Done after this time has breached its SLA; one that is Done after it was completed late.';
                        COMMENT ON COLUMN public."CaseChecklistItem"."CompletedDate" IS 'UTC. When the item was ticked, blank unless StateId is 3.';
                        COMMENT ON COLUMN public."CaseChecklistItem"."CompletedUser" IS 'Who ticked the item, blank unless StateId is 3.';
                        COMMENT ON COLUMN public."CaseChecklistItem"."LastEvaluatedDate" IS 'UTC. When the show condition of the item was last evaluated for the case.';
                        COMMENT ON TABLE public."CaseChecklistEvent" IS 'Append-only audit of everything that happens to the checklist items of a case: shown, hidden, ticked, unticked, failed validation, the gate blocking or being overridden or bypassed, and SLA breach. A row is never deleted and is never changed after it is written, apart from CaseEventId being set once when the matching timeline row has been written. Shown and hidden rows are also written to the case timeline as CaseEvent rows, linked by CaseEventId.';
                        COMMENT ON COLUMN public."CaseChecklistEvent"."CaseId" IS 'The case the action happened on. Joins Case on Id.';
                        COMMENT ON COLUMN public."CaseChecklistEvent"."CaseChecklistItemId" IS 'The CaseChecklistItem the action was on. Blank for actions that are about the case as a whole, such as the gate blocking a move.';
                        COMMENT ON COLUMN public."CaseChecklistEvent"."CaseWorkflowChecklistItemGuid" IS 'The definition of the item the action was on, when there is one.';
                        COMMENT ON COLUMN public."CaseChecklistEvent"."ActionId" IS 'What happened: 1 Shown, 2 Hidden, 3 Checked, 4 Unchecked, 5 ValidationFailed, 6 GateBlocked, 7 GateOverridden, 8 GateBypassed, 9 SlaBreached.';
                        COMMENT ON COLUMN public."CaseChecklistEvent"."Actor" IS 'Who did it: a user name, or System, Case creation, Case escalation or AskJooby when no person did.';
                        COMMENT ON COLUMN public."CaseChecklistEvent"."CreatedDate" IS 'UTC. When the action happened.';
                        COMMENT ON COLUMN public."CaseChecklistEvent"."Detail" IS 'Why or how: the outcome of the show or validate SQL, the reason a supervisor gave for an override, or the items that were still open when the gate acted.';
                        COMMENT ON COLUMN public."CaseChecklistEvent"."CaseEventId" IS 'The CaseEvent row written to the case timeline for this action, when there is one. Joins CaseEvent on Id.';
                        COMMENT ON COLUMN public."Case"."ChecklistReconciledDate" IS 'UTC. When the checklist items of the case were last brought up to date against the show conditions of the status the case is in.';
                        COMMENT ON COLUMN public."CaseEvent"."CaseEventTypeId" IS
                        'What happened to the case. 1=Automatic Unlock, 2=Skim Case, 3=Automatic Lock, 4=Full Case Fetch, 5=Closed, 6=Manual Lock, 7=Diary (set, or its type changed; After holds the diary type), 8=Workflow Change, 9=Status Change, 10=Diary Date Change, 11=Rating Change, 12=Suspend Closed, 13=Suspend Open, 14=Allocate Lock, 15=Process Expired Case, 16=Peek (the case summary was read without taking the lock), 17=Automatic Escalation (the escalation rule of the status moved the case on), 18=Lock Broken (a supervisor released the lock; Before holds the user who held it and After the reason when one was given), 19=Diary Cleared (Before holds the diary type that was cleared), 20=QA Sampled (the case was picked at random for a quality review; After holds the sample rate), 21=Checklist Item Shown, 22=Checklist Item Hidden, 23=Checklist Item Checked, 24=Checklist Item Unchecked, 25=Checklist Gate Blocked, 26=Checklist Gate Overridden, 27=Checklist Gate Bypassed, 28=Checklist Item SLA Breached (for 21 to 28 Before holds the item name and After the Id of the CaseChecklistEvent row with the detail). Anything else renders as Unknown. Before and After carry the old and new values for the change the event records. The time to first view of a case is measured from creation to the first event of type 2, 4 or 16, and the time to ownership to the first event of type 2, 3, 6 or 14.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
