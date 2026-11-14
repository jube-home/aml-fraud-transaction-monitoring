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

namespace Jube.Migrations.Branches.CaseReports
{
    using FluentMigrator;

    [Migration(2026111107000000)]
    public class CreateCaseReports : Migration
    {
        public override void Up()
        {
            Create.Table("CaseReportProfile")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("Name").AsString(100).NotNullable()
                .WithColumn("Version").AsInt32().NotNullable()
                .WithColumn("Description").AsString(500).Nullable()
                .WithColumn("TemplateXml").AsCustom("text").NotNullable()
                .WithColumn("SchemaDocumentsJson").AsCustom("text").NotNullable()
                .WithColumn("CodeListsJson").AsCustom("text").NotNullable()
                .WithColumn("CreatedUser").AsString().NotNullable()
                .WithColumn("CreatedDate").AsDateTime().NotNullable();

            Create.Index("IX_CaseReportProfile_Version").OnTable("CaseReportProfile")
                .OnColumn("TenantRegistryId").Ascending().OnColumn("Name").Ascending().OnColumn("Version").Ascending()
                .WithOptions().Unique();

            Create.Table("CaseReport")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().NotNullable().Unique()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("CaseId").AsInt32().NotNullable().ForeignKey("FK_CaseReport_CaseId_Case_Id", "Case", "Id")
                .WithColumn("CaseKey").AsString().Nullable()
                .WithColumn("CaseKeyValue").AsString().Nullable()
                .WithColumn("ProfileName").AsString(100).NotNullable()
                .WithColumn("ProfileVersion").AsInt32().NotNullable()
                .WithColumn("ReportCode").AsString(50).NotNullable()
                .WithColumn("StateId").AsInt16().NotNullable()
                .WithColumn("CurrentVersion").AsInt32().NotNullable()
                .WithColumn("FiuReference").AsString(200).Nullable()
                .WithColumn("CreatedUser").AsString().NotNullable()
                .WithColumn("CreatedDate").AsDateTime().NotNullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("SubmittedUser").AsString().Nullable()
                .WithColumn("SubmittedDate").AsDateTime().Nullable();

            Create.Index("IX_CaseReport_CaseId").OnTable("CaseReport").OnColumn("CaseId").Ascending();
            Create.Index("IX_CaseReport_State").OnTable("CaseReport").OnColumn("TenantRegistryId").Ascending()
                .OnColumn("StateId").Ascending();

            Create.Table("CaseReportVersion")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseReportId").AsInt64().NotNullable()
                    .ForeignKey("FK_CaseReportVersion_CaseReportId_CaseReport_Id", "CaseReport", "Id")
                .WithColumn("Version").AsInt32().NotNullable()
                .WithColumn("SupersedesVersion").AsInt32().Nullable()
                .WithColumn("DataJson").AsCustom("text").NotNullable()
                .WithColumn("Xml").AsCustom("text").Nullable()
                .WithColumn("PayloadSha256").AsString(64).Nullable()
                .WithColumn("IssuesJson").AsCustom("text").Nullable()
                .WithColumn("CreatedUser").AsString().NotNullable()
                .WithColumn("CreatedDate").AsDateTime().NotNullable();

            Create.Index("IX_CaseReportVersion_Version").OnTable("CaseReportVersion")
                .OnColumn("CaseReportId").Ascending().OnColumn("Version").Ascending().WithOptions().Unique();

            Create.Table("CaseReportApproval")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseReportId").AsInt64().NotNullable()
                    .ForeignKey("FK_CaseReportApproval_CaseReportId_CaseReport_Id", "CaseReport", "Id")
                .WithColumn("ReportVersion").AsInt32().NotNullable()
                .WithColumn("DecisionId").AsInt16().NotNullable()
                .WithColumn("Note").AsString(1000).Nullable()
                .WithColumn("CreatedUser").AsString().NotNullable()
                .WithColumn("CreatedDate").AsDateTime().NotNullable();

            Create.Table("CaseReportFeedback")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseReportId").AsInt64().NotNullable()
                    .ForeignKey("FK_CaseReportFeedback_CaseReportId_CaseReport_Id", "CaseReport", "Id")
                .WithColumn("ReportVersion").AsInt32().NotNullable()
                .WithColumn("KindId").AsInt16().NotNullable()
                .WithColumn("Message").AsString(2000).Nullable()
                .WithColumn("FieldPath").AsString(500).Nullable()
                .WithColumn("ReceivedDate").AsDateTime().NotNullable()
                .WithColumn("RecordedUser").AsString().NotNullable()
                .WithColumn("CreatedDate").AsDateTime().NotNullable();

            Execute.Sql("""
                        INSERT INTO public."PermissionSpecification" ("Id", "Name") VALUES (75, 'File Case Reports');
                        INSERT INTO public."PermissionSpecification" ("Id", "Name") VALUES (76, 'Approve Case Reports');
                        INSERT INTO public."PermissionSpecification" ("Id", "Name") VALUES (77, 'View Case Reports');
                        INSERT INTO public."RoleRegistryPermission" ("RoleRegistryId", "PermissionSpecificationId", "Active", "CreatedDate", "CreatedUser", "Version", "Guid")
                        SELECT 1, s."Id", 1, now() at time zone 'utc', 'Administrator', 1, gen_random_uuid()
                        FROM public."PermissionSpecification" s WHERE s."Id" IN (75, 76, 77)
                          AND NOT EXISTS (SELECT 1 FROM public."RoleRegistryPermission" x WHERE x."RoleRegistryId" = 1
                              AND x."PermissionSpecificationId" = s."Id" AND (x."Deleted" = 0 OR x."Deleted" IS NULL));

                        COMMENT ON TABLE public."CaseReportProfile" IS 'How a regulatory report is built for one FIU: the template, the schema or schemas the FIU issued, and the code lists. Append only: each import is a new version and the highest version is current. Built and validated by the Jube.RegulatoryReport engine.';
                        COMMENT ON COLUMN public."CaseReportProfile"."TenantRegistryId" IS 'The tenant the profile belongs to.';
                        COMMENT ON COLUMN public."CaseReportProfile"."Name" IS 'The profile name, for example the FIU and report type.';
                        COMMENT ON COLUMN public."CaseReportProfile"."Version" IS 'Starts at 1 and rises with each import; the highest is current.';
                        COMMENT ON COLUMN public."CaseReportProfile"."TemplateXml" IS 'The report template with its jr: directives.';
                        COMMENT ON COLUMN public."CaseReportProfile"."SchemaDocumentsJson" IS 'JSON object of file name to the XSD text the FIU issued.';
                        COMMENT ON COLUMN public."CaseReportProfile"."CodeListsJson" IS 'JSON object of code list name to its value mappings.';
                        COMMENT ON TABLE public."CaseReport" IS 'A regulatory report filed about a case. It exists only as part of a case. Visible only to holders of the File, Approve or View Case Reports permissions, so that the existence of a report is not disclosed to anyone who does not need to know.';
                        COMMENT ON COLUMN public."CaseReport"."TenantRegistryId" IS 'The tenant the case belongs to.';
                        COMMENT ON COLUMN public."CaseReport"."CaseId" IS 'The case the report is about. Joins Case on Id.';
                        COMMENT ON COLUMN public."CaseReport"."CaseKey" IS 'The case key name, copied from the case.';
                        COMMENT ON COLUMN public."CaseReport"."CaseKeyValue" IS 'The case key value, copied from the case.';
                        COMMENT ON COLUMN public."CaseReport"."ProfileName" IS 'The profile used. Joins CaseReportProfile on Name.';
                        COMMENT ON COLUMN public."CaseReport"."ProfileVersion" IS 'The profile version the report was started with.';
                        COMMENT ON COLUMN public."CaseReport"."ReportCode" IS 'The FIU report type, for example a suspicious activity or suspicious transaction report code.';
                        COMMENT ON COLUMN public."CaseReport"."StateId" IS 'Enumeration (CaseReportState): 1 Draft, 2 Validated, 3 Pending approval, 4 Approved, 5 Submitted, 6 Acknowledged, 7 Rejected by the FIU, 8 Withdrawn.';
                        COMMENT ON COLUMN public."CaseReport"."CurrentVersion" IS 'The version of the report now in force. Versions are never edited once validated.';
                        COMMENT ON COLUMN public."CaseReport"."FiuReference" IS 'The reference the FIU gave on receipt, recorded by hand.';
                        COMMENT ON COLUMN public."CaseReport"."SubmittedUser" IS 'Who recorded the report as submitted.';
                        COMMENT ON COLUMN public."CaseReport"."SubmittedDate" IS 'UTC. When it was recorded as submitted.';
                        COMMENT ON TABLE public."CaseReportVersion" IS 'One version of a report, append only. Holds the exact data given to the report engine, the hash of the XML it produced and the result of its last validation.';
                        COMMENT ON COLUMN public."CaseReportVersion"."Version" IS 'The version number, from 1.';
                        COMMENT ON COLUMN public."CaseReportVersion"."SupersedesVersion" IS 'The earlier version this one corrects, when it follows an FIU rejection.';
                        COMMENT ON COLUMN public."CaseReportVersion"."DataJson" IS 'JSON: the case facts, the narrative and the fields the analyst entered, as given to the engine.';
                        COMMENT ON COLUMN public."CaseReportVersion"."Xml" IS 'The generated report XML, once validated. Kept here and not as a case file, because a case file hangs off a case note or research and so would reveal the report to everyone who can read the case.';
                        COMMENT ON COLUMN public."CaseReportVersion"."PayloadSha256" IS 'SHA-256 of the generated XML, as stored and as downloaded.';
                        COMMENT ON COLUMN public."CaseReportVersion"."IssuesJson" IS 'JSON list of the issues the last validation found, with data and element paths.';
                        COMMENT ON TABLE public."CaseReportApproval" IS 'The second person''s decision on a report version: approved, or rejected with a note. Never the author.';
                        COMMENT ON COLUMN public."CaseReportApproval"."DecisionId" IS 'Enumeration (CaseReportDecision): 1 Approved, 2 Rejected.';
                        COMMENT ON COLUMN public."CaseReportApproval"."Note" IS 'The reason given with the decision.';
                        COMMENT ON TABLE public."CaseReportFeedback" IS 'What the FIU said about a submitted report, recorded by hand: acknowledgement, rejection or a request for information.';
                        COMMENT ON COLUMN public."CaseReportFeedback"."KindId" IS 'Enumeration (CaseReportFeedbackKind): 1 Acknowledged, 2 Rejected, 3 Information requested.';
                        COMMENT ON COLUMN public."CaseReportFeedback"."Message" IS 'The FIU''s message.';
                        COMMENT ON COLUMN public."CaseReportFeedback"."FieldPath" IS 'The field the FIU said was wrong, when it said.';
                        COMMENT ON COLUMN public."CaseReportFeedback"."ReceivedDate" IS 'UTC. When the FIU responded.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
