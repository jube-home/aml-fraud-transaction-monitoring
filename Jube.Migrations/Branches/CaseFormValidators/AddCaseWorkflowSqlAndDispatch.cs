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

namespace Jube.Migrations.Branches.CaseFormValidators
{
    using FluentMigrator;

    [Migration(20263108140000)]
    public class AddCaseWorkflowSqlAndDispatch : Migration
    {
        private static readonly string[] sources =
            ["CaseWorkflowForm", "CaseWorkflowDisplay", "CaseWorkflowMacro", "CaseWorkflowAction", "CaseWorkflowStatus"];

        public override void Up()
        {
            foreach (var source in sources)
            {
                Execute.Sql($"""
                             ALTER TABLE public."{source}" ADD COLUMN IF NOT EXISTS "Sql" text NULL;
                             ALTER TABLE public."{source}Version" ADD COLUMN IF NOT EXISTS "Sql" text NULL;
                             COMMENT ON COLUMN public."{source}"."Sql" IS 'Optional. A single SELECT run for a case, read only against the tenant''s own rows, whose rows the layout and the notification and HTTP templates of this record are drawn from as rows. It may use the bound parameters @Id, the id of the case, and @CaseKeyValue. When it is empty the record keeps the earlier behaviour of drawing from the case''s stored payload.';
                             COMMENT ON COLUMN public."{source}Version"."Sql" IS 'The value of Sql at the time the version was recorded.';
                             """);
            }

            Execute.Sql("""
                        ALTER TABLE public."CaseWorkflowFormEntry" ADD COLUMN IF NOT EXISTS "Sql" text NULL;
                        ALTER TABLE public."CaseWorkflowFormEntry" ADD COLUMN IF NOT EXISTS "Template" text NULL;
                        ALTER TABLE public."CaseWorkflowFormEntry" ADD COLUMN IF NOT EXISTS "Rows" jsonb NULL;
                        COMMENT ON COLUMN public."CaseWorkflowFormEntry"."Sql" IS 'The SQL of the form as it was when the entry was completed, or null for an entry completed before forms were drawn from SQL.';
                        COMMENT ON COLUMN public."CaseWorkflowFormEntry"."Template" IS 'The template text of the form as it was when the entry was completed, so the entry can be drawn exactly as it was completed.';
                        COMMENT ON COLUMN public."CaseWorkflowFormEntry"."Rows" IS 'The rows the form''s SQL returned for the case at the moment the entry was completed, as a JSON array of objects keyed by column name. The submitted values are in CaseWorkflowFormEntryValue.';
                        """);

            Create.Table("CaseWorkflowDispatch")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("CaseId").AsInt32().Nullable()
                .WithColumn("CaseKey").AsString().Nullable()
                .WithColumn("CaseKeyValue").AsString().Nullable()
                .WithColumn("SourceKindId").AsByte().Nullable()
                .WithColumn("SourceId").AsInt32().Nullable()
                .WithColumn("SourceName").AsString().Nullable()
                .WithColumn("ChannelId").AsByte().Nullable()
                .WithColumn("ModeId").AsByte().Nullable()
                .WithColumn("Destination").AsString(int.MaxValue).Nullable()
                .WithColumn("Subject").AsString(int.MaxValue).Nullable()
                .WithColumn("Body").AsString(int.MaxValue).Nullable()
                .WithColumn("OutcomeId").AsByte().Nullable()
                .WithColumn("Detail").AsString(int.MaxValue).Nullable()
                .WithColumn("ResponseStatus").AsInt32().Nullable()
                .WithColumn("CorrelationId").AsGuid().Nullable()
                .WithColumn("Rows").AsCustom("jsonb").Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable();

            Create.Index().OnTable("CaseWorkflowDispatch").OnColumn("CaseId").Ascending();
            Create.Index().OnTable("CaseWorkflowDispatch")
                .OnColumn("CaseKey").Ascending().OnColumn("CaseKeyValue").Ascending().OnColumn("CreatedDate").Descending();

            Execute.Sql("""
                        UPDATE public."PermissionSpecification" SET "Name" = 'Author Case Workflow SQL' WHERE "Id" = 67;

                        COMMENT ON TABLE public."CaseWorkflowDispatch" IS 'The audit of every email, SMS and HTTP call a case workflow Form, Macro, Action or Status fired for a case: what was rendered and sent, to whom, what came back, and the SQL rows it was drawn from. Written whether the call was sent, failed or was refused before it left. Joins Case by CaseId and by CaseKey and CaseKeyValue.';
                        COMMENT ON COLUMN public."CaseWorkflowDispatch"."SourceKindId" IS 'What fired the call: 1 Form, a form entry was completed; 2 Macro, a macro was run; 3 Action, a note was added with the action; 4 Status, the case moved to the status.';
                        COMMENT ON COLUMN public."CaseWorkflowDispatch"."SourceId" IS 'The id of the Form, Macro, Action or Status named by SourceKindId.';
                        COMMENT ON COLUMN public."CaseWorkflowDispatch"."ChannelId" IS 'How it was sent: 1 Email, 2 SMS, 3 HTTP POST, 4 HTTP GET.';
                        COMMENT ON COLUMN public."CaseWorkflowDispatch"."ModeId" IS 'How it was drawn: 1 Payload, the earlier substitution from the case''s stored payload; 2 SQL, rendered from the rows of the record''s SQL.';
                        COMMENT ON COLUMN public."CaseWorkflowDispatch"."OutcomeId" IS 'What happened: 1 Sent, the provider accepted it; 2 Failed, it was sent but failed, see Detail; 3 Refused, it was not sent because the rendered destination or endpoint was not allowed or the SQL failed, see Detail.';
                        COMMENT ON COLUMN public."CaseWorkflowDispatch"."Destination" IS 'The rendered recipients or, for HTTP, the rendered URL.';
                        COMMENT ON COLUMN public."CaseWorkflowDispatch"."Body" IS 'The rendered message, or for an HTTP POST the JSON body sent.';
                        COMMENT ON COLUMN public."CaseWorkflowDispatch"."Rows" IS 'The rows the SQL returned at the moment of the call, as a JSON array of objects keyed by column name; null when it was drawn from the stored payload.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
