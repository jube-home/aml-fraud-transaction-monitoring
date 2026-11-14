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

    [Migration(20263107900000)]
    public class CreateCaseStatusTransitionTable : Migration
    {
        public static string BackfillSql(string caseCondition) => $$"""
            INSERT INTO public."CaseStatusTransition"
                ("CaseId", "CaseWorkflowGuid", "CaseWorkflowStatusGuid", "PreviousCaseWorkflowStatusGuid",
                 "EnterReasonId", "EnteredDate", "EnteredUser", "ExitReasonId", "ExitedDate", "ExitedUser",
                 "DurationSeconds")
            WITH "Moves" AS (
                SELECT e."CaseId", e."CreatedDate", e."CreatedUser",
                       CASE WHEN e."Before" ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$'
                            THEN e."Before"::uuid END AS "FromGuid",
                       CASE WHEN e."After" ~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$'
                            THEN e."After"::uuid END AS "ToGuid",
                       ROW_NUMBER() OVER (PARTITION BY e."CaseId" ORDER BY e."CreatedDate", e."Id") AS "Position"
                FROM public."CaseEvent" e
                WHERE e."CaseEventTypeId" = 9 AND e."CaseId" IS NOT NULL
            ),
            "Starts" AS (
                SELECT c."Id" AS "CaseId", c."CaseWorkflowGuid",
                       COALESCE(m."FromGuid", c."CaseWorkflowStatusGuid") AS "StatusGuid",
                       NULL::uuid AS "PreviousGuid", 1 AS "EnterReasonId",
                       COALESCE(c."CreatedDate", m."CreatedDate") AS "EnteredDate", NULL::text AS "EnteredUser",
                       0 AS "Position"
                FROM public."Case" c
                LEFT JOIN "Moves" m ON m."CaseId" = c."Id" AND m."Position" = 1
                WHERE c."CaseWorkflowStatusGuid" IS NOT NULL AND ({{caseCondition}})
                  AND NOT EXISTS (SELECT 1 FROM public."CaseStatusTransition" t WHERE t."CaseId" = c."Id")
                UNION ALL
                SELECT c."Id", c."CaseWorkflowGuid", m."ToGuid", m."FromGuid", 2, m."CreatedDate", m."CreatedUser",
                       m."Position"
                FROM "Moves" m
                JOIN public."Case" c ON c."Id" = m."CaseId"
                WHERE m."ToGuid" IS NOT NULL AND ({{caseCondition}})
                  AND NOT EXISTS (SELECT 1 FROM public."CaseStatusTransition" t WHERE t."CaseId" = c."Id")
            ),
            "Spans" AS (
                SELECT s.*, LEAD(s."EnteredDate") OVER (PARTITION BY s."CaseId" ORDER BY s."Position") AS "NextEntered",
                       LEAD(s."EnteredUser") OVER (PARTITION BY s."CaseId" ORDER BY s."Position") AS "NextUser"
                FROM "Starts" s
            )
            SELECT sp."CaseId", sp."CaseWorkflowGuid", sp."StatusGuid", sp."PreviousGuid", sp."EnterReasonId",
                   sp."EnteredDate", sp."EnteredUser",
                   CASE WHEN sp."NextEntered" IS NOT NULL THEN 1
                        WHEN c."ClosedStatusId" = 3 AND c."ClosedDate" IS NOT NULL THEN 2 END,
                   CASE WHEN sp."NextEntered" IS NOT NULL THEN sp."NextEntered"
                        WHEN c."ClosedStatusId" = 3 AND c."ClosedDate" IS NOT NULL THEN c."ClosedDate" END,
                   CASE WHEN sp."NextEntered" IS NOT NULL THEN sp."NextUser"
                        WHEN c."ClosedStatusId" = 3 AND c."ClosedDate" IS NOT NULL THEN c."ClosedUser" END,
                   CASE WHEN COALESCE(sp."NextEntered",
                                      CASE WHEN c."ClosedStatusId" = 3 THEN c."ClosedDate" END) IS NOT NULL
                        THEN GREATEST(0, EXTRACT(EPOCH FROM (COALESCE(sp."NextEntered", c."ClosedDate")
                                                             - sp."EnteredDate")))::int END
            FROM "Spans" sp
            JOIN public."Case" c ON c."Id" = sp."CaseId"
            WHERE sp."EnteredDate" IS NOT NULL
            ORDER BY sp."CaseId", sp."Position";
            """;

        public override void Up()
        {
            Create.Table("CaseStatusTransition")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseId").AsInt32().NotNullable()
                .WithColumn("CaseWorkflowGuid").AsGuid().NotNullable()
                .WithColumn("CaseWorkflowStatusGuid").AsGuid().NotNullable()
                .WithColumn("PreviousCaseWorkflowStatusGuid").AsGuid().Nullable()
                .WithColumn("EnterReasonId").AsByte().NotNullable()
                .WithColumn("EnteredDate").AsDateTime2().NotNullable()
                .WithColumn("EnteredUser").AsString().Nullable()
                .WithColumn("ExitReasonId").AsByte().Nullable()
                .WithColumn("ExitedDate").AsDateTime2().Nullable()
                .WithColumn("ExitedUser").AsString().Nullable()
                .WithColumn("DurationSeconds").AsInt32().Nullable();

            Create.ForeignKey().FromTable("CaseStatusTransition").ForeignColumn("CaseId")
                .ToTable("Case").PrimaryColumn("Id").OnDelete(System.Data.Rule.Cascade);

            Execute.Sql("""
                        CREATE UNIQUE INDEX "IX_CaseStatusTransition_CaseId_Open"
                        ON public."CaseStatusTransition" ("CaseId") WHERE "ExitedDate" IS NULL;
                        CREATE INDEX "IX_CaseStatusTransition_CaseId_EnteredDate"
                        ON public."CaseStatusTransition" ("CaseId", "EnteredDate");
                        CREATE INDEX "IX_CaseStatusTransition_Workflow_Status_Entered"
                        ON public."CaseStatusTransition" ("CaseWorkflowGuid", "CaseWorkflowStatusGuid", "EnteredDate");
                        """);

            Execute.Sql("""
                        COMMENT ON TABLE public."CaseStatusTransition" IS
                        'One row for each stay of a case in a Case Workflow Status: when it entered the status, why, who moved it, and when and why it left, with the time spent. The row still open (no exit) is the status the case is in now; at most one row per case is open. It is what makes service levels measurable, as the time taken to move between statuses cannot be recovered from the case itself, and it feeds the cumulative flow, bottleneck and status dwell analysis. Written when a case is created, changes status, is escalated, is closed or is reopened.';
                        COMMENT ON COLUMN public."CaseStatusTransition"."Id" IS 'Server-assigned identity of the stay.';
                        COMMENT ON COLUMN public."CaseStatusTransition"."CaseId" IS 'The case; the stays are removed with the case.';
                        COMMENT ON COLUMN public."CaseStatusTransition"."CaseWorkflowGuid" IS 'The Case Workflow of the case, repeated here so stays can be analysed per workflow without joining the case.';
                        COMMENT ON COLUMN public."CaseStatusTransition"."CaseWorkflowStatusGuid" IS 'The Case Workflow Status the case entered and stayed in.';
                        COMMENT ON COLUMN public."CaseStatusTransition"."PreviousCaseWorkflowStatusGuid" IS 'The status the case came from; null when the case was created in this status.';
                        COMMENT ON COLUMN public."CaseStatusTransition"."EnterReasonId" IS 'Why the case entered the status. 1 Created, 2 Status changed (by a person or an agent), 3 Reopened (a closed case was reopened into this status), 4 Escalated (the engine moved it to a higher priority status when another activation fired).';
                        COMMENT ON COLUMN public."CaseStatusTransition"."EnteredDate" IS 'When the case entered the status (UTC).';
                        COMMENT ON COLUMN public."CaseStatusTransition"."EnteredUser" IS 'The user who moved the case into the status; null when the engine did.';
                        COMMENT ON COLUMN public."CaseStatusTransition"."ExitReasonId" IS 'Why the case left the status. 1 Moved to another status, 2 Closed. Null while the case is still in the status.';
                        COMMENT ON COLUMN public."CaseStatusTransition"."ExitedDate" IS 'When the case left the status (UTC); null while it is still there.';
                        COMMENT ON COLUMN public."CaseStatusTransition"."ExitedUser" IS 'The user who moved or closed the case out of the status; null when the engine did or the case is still there.';
                        COMMENT ON COLUMN public."CaseStatusTransition"."DurationSeconds" IS 'Seconds spent in the status, set when the case leaves it; null while the case is still there.';
                        """);

            Execute.Sql(BackfillSql("TRUE"));
        }

        public override void Down()
        {
        }
    }
}
