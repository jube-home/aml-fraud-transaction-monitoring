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

namespace Jube.Migrations.Branches.ArchiveKeyIndexing
{
    using FluentMigrator;

    [Migration(20263108480000, TransactionBehavior.None)]
    public class AddCaseAndCasesPageIndexes : Migration
    {
        public static readonly string[] Statements =
        [
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_CaseEvent_CaseId_Id" ON public."CaseEvent" ("CaseId", "Id")""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_CaseEvent_Type_CreatedDate" ON public."CaseEvent" ("CaseEventTypeId", "CreatedDate") INCLUDE ("CaseId", "CreatedUser")""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Case_Key_Value_Workflow_Closed" ON public."Case" ("CaseKey", "CaseKeyValue", "CaseWorkflowGuid", "ClosedStatusId")""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Case_EntityAnalysisModelInstanceEntryGuid" ON public."Case" ("EntityAnalysisModelInstanceEntryGuid")""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Case_Workflow_Created" ON public."Case" ("CaseWorkflowGuid", "CreatedDate")""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Case_Workflow_Open" ON public."Case" ("CaseWorkflowGuid", "CreatedDate") WHERE "ClosedStatusId" IN (0, 1)""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Case_Workflow_LockedUser_Open" ON public."Case" ("CaseWorkflowGuid", "LockedUser") WHERE "ClosedStatusId" IN (0, 1) AND "Locked" = 1""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Case_Workflow_Diary_Open" ON public."Case" ("CaseWorkflowGuid", "DiaryDate") WHERE "ClosedStatusId" IN (0, 1) AND "Diary" = 1""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Case_Workflow_ClosedDate" ON public."Case" ("CaseWorkflowGuid", "ClosedDate") WHERE "ClosedDate" IS NOT NULL""",
            """DROP INDEX CONCURRENTLY IF EXISTS public."IX_Case_CaseWorkflowGuid" """,
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Case_Workflow_Triage" ON public."Case" ("CaseWorkflowGuid", "RiskLaneId", "RiskPriority" DESC NULLS LAST, "CreatedDate", "Id") WHERE "ClosedStatusId" IN (0, 1)""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Case_Workflow_Deadline" ON public."Case" ("CaseWorkflowGuid", "RiskNextDeadlineDate") WHERE "ClosedStatusId" IN (0, 1)""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_CaseNote_CaseId_Id" ON public."CaseNote" ("CaseId", "Id")""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_CaseResearch_CaseId_Id" ON public."CaseResearch" ("CaseId", "Id")""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_CaseWorkflowFormEntry_CaseId" ON public."CaseWorkflowFormEntry" ("CaseId")""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_CaseWorkflowDispatch_Key_Value_Id" ON public."CaseWorkflowDispatch" ("CaseKey", "CaseKeyValue", "Id" DESC)""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_CaseChecklistItem_CaseId_Status_Shown" ON public."CaseChecklistItem" ("CaseId", "CaseWorkflowStatusGuid") WHERE "StateId" = 1""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_CaseKeyJournalTagJob_UndoOfGuid" ON public."CaseKeyJournalTagJob" ("UndoOfGuid") WHERE "UndoOfGuid" IS NOT NULL""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_SessionCaseJournal_CaseWorkflowGuid_CreatedUser" ON public."SessionCaseJournal" ("CaseWorkflowGuid", "CreatedUser")""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_EntityAnalysisModelOverride_Key_Value_Model" ON public."EntityAnalysisModelOverride" ("OverrideKey", "OverrideKeyValue", "EntityAnalysisModelGuid")""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_EntityAnalysisModelActivationRuleOverride_Key_Value_Model" ON public."EntityAnalysisModelActivationRuleOverride" ("OverrideKey", "OverrideKeyValue", "EntityAnalysisModelGuid", "EntityAnalysisModelActivationRuleName")""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_CaseWorkflowChecklist_Guid" ON public."CaseWorkflowChecklist" ("Guid")""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_CaseWorkflowChecklistItem_Guid" ON public."CaseWorkflowChecklistItem" ("Guid")""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_CaseWorkflowVersion_CaseWorkflowId" ON public."CaseWorkflowVersion" ("CaseWorkflowId")""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_CaseWorkflowStatusVersion_CaseWorkflowStatusId" ON public."CaseWorkflowStatusVersion" ("CaseWorkflowStatusId")""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_CaseWorkflowXPathVersion_CaseWorkflowXPathId" ON public."CaseWorkflowXPathVersion" ("CaseWorkflowXPathId")""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_CaseWorkflowActionVersion_CaseWorkflowActionId" ON public."CaseWorkflowActionVersion" ("CaseWorkflowActionId")""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_CaseWorkflowDisplayVersion_CaseWorkflowDisplayId" ON public."CaseWorkflowDisplayVersion" ("CaseWorkflowDisplayId")""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_CaseWorkflowFilterVersion_CaseWorkflowFilterId" ON public."CaseWorkflowFilterVersion" ("CaseWorkflowFilterId")""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_CaseWorkflowFormVersion_CaseWorkflowFormId" ON public."CaseWorkflowFormVersion" ("CaseWorkflowFormId")""",
            """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_CaseWorkflowMacroVersion_CaseWorkflowMacroId" ON public."CaseWorkflowMacroVersion" ("CaseWorkflowMacroId")"""
        ];

        public override void Up()
        {
            foreach (var statement in Statements)
            {
                Execute.Sql(statement);
            }
        }

        public override void Down()
        {
        }
    }
}
