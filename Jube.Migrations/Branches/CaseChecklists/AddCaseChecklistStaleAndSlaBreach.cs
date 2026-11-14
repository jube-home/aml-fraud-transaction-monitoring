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

    [Migration(20263108450000)]
    public class AddCaseChecklistStaleAndSlaBreach : Migration
    {
        public override void Up()
        {
            Alter.Table("Case")
                .AddColumn("ChecklistStaleDate").AsDateTime2().Nullable();

            Alter.Table("CaseChecklistItem")
                .AddColumn("SlaBreachedDate").AsDateTime2().Nullable();

            Execute.Sql("""
                        CREATE INDEX "IX_CaseChecklistItem_OpenSlaDue" ON public."CaseChecklistItem" ("DueDate", "Id")
                        WHERE "StateId" = 1 AND "SlaBreachedDate" IS NULL AND "DueDate" IS NOT NULL;

                        COMMENT ON COLUMN public."Case"."ChecklistStaleDate" IS 'Set to the current UTC time whenever something happens to the case that could change which checklist items are shown: any case timeline event other than the checklist''s own, and a new activation attached to an existing case. The case is stale, and its checklist is reconciled the next time it is read, when this is later than ChecklistReconciledDate. Only a timestamp is written on the hot path; no show condition runs.';
                        COMMENT ON COLUMN public."Case"."ChecklistReconciledDate" IS 'The UTC time at which the last reconcile of the checklist of the case began, not ended, so that an event arriving while it ran leaves the case stale. Null means the checklist has never been reconciled and is reconciled on the next read.';
                        COMMENT ON COLUMN public."CaseChecklistItem"."SlaBreachedDate" IS 'The UTC time at which the SLA breach of the item was first recorded, either by a reconcile, by the engine sweep or when the item was ticked after its DueDate. It is set once and never cleared, so the breach is recorded on the case timeline exactly once. Null means no breach has been recorded.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
