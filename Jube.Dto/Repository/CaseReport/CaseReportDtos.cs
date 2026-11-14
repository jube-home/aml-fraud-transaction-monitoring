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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.CaseReport
{
    [Description("One problem the report engine found: where it is and what is wrong.")]
    [MessagePackObject]
    public class CaseReportIssueDto
    {
        [Description("1 Profile, 2 Mapping (a data path), 3 Schema (an element path).")]
        [Key(0)]
        public int Stage { get; set; }

        [Description("A stable code such as MissingRequired or SchemaViolation.")]
        [Key(1)]
        public string Code { get; set; } = string.Empty;

        [Description("A plain explanation.")]
        [Key(2)]
        public string Message { get; set; } = string.Empty;

        [Description("The data path (Mapping) or element path (Schema).")]
        [Key(3)]
        public string? Path { get; set; }

        [Description("The line, when known.")]
        [Key(4)]
        public int? Line { get; set; }

        [Description("The position in the line, when known.")]
        [Key(5)]
        public int? Position { get; set; }
    }

    [Description("One version of a report.")]
    [MessagePackObject]
    public class CaseReportVersionDto
    {
        [Description("The version number.")]
        [Key(0)]
        public int Version { get; set; }

        [Description("The earlier version this corrects.")]
        [Key(1)]
        public int? SupersedesVersion { get; set; }

        [Description("SHA-256 of the generated XML.")]
        [Key(2)]
        public string? PayloadSha256 { get; set; }

        [Description("True once XML has been generated for it.")]
        [Key(3)]
        public bool HasXml { get; set; }

        [Description("Who made the version.")]
        [Key(4)]
        public string CreatedUser { get; set; } = string.Empty;

        [Description("When.")]
        [Key(5)]
        public DateTimeOffset CreatedDate { get; set; }
    }

    [Description("The second person decision on a version.")]
    [MessagePackObject]
    public class CaseReportApprovalDto
    {
        [Description("The version decided.")]
        [Key(0)]
        public int ReportVersion { get; set; }

        [Description("1 Approved, 2 Rejected.")]
        [Key(1)]
        public int DecisionId { get; set; }

        [Description("The reason given.")]
        [Key(2)]
        public string? Note { get; set; }

        [Description("Who decided.")]
        [Key(3)]
        public string CreatedUser { get; set; } = string.Empty;

        [Description("When.")]
        [Key(4)]
        public DateTimeOffset CreatedDate { get; set; }
    }

    [Description("What the FIU said about a submitted report.")]
    [MessagePackObject]
    public class CaseReportFeedbackDto
    {
        [Description("The version it was about.")]
        [Key(0)]
        public int ReportVersion { get; set; }

        [Description("1 Acknowledged, 2 Rejected, 3 Information requested.")]
        [Key(1)]
        public int KindId { get; set; }

        [Description("The FIU message.")]
        [Key(2)]
        public string? Message { get; set; }

        [Description("The field the FIU pointed at.")]
        [Key(3)]
        public string? FieldPath { get; set; }

        [Description("When the FIU responded.")]
        [Key(4)]
        public DateTimeOffset ReceivedDate { get; set; }

        [Description("Who recorded it.")]
        [Key(5)]
        public string RecordedUser { get; set; } = string.Empty;
    }

    [Description("A regulatory report about a case, with its history.")]
    [MessagePackObject]
    public class CaseReportDto
    {
        [Description("The report identifier.")]
        [Key(0)]
        public Guid Guid { get; set; }

        [Description("The case it is about.")]
        [Key(1)]
        public int CaseId { get; set; }

        [Description("The profile used.")]
        [Key(2)]
        public string ProfileName { get; set; } = string.Empty;

        [Description("The profile version.")]
        [Key(3)]
        public int ProfileVersion { get; set; }

        [Description("The FIU report type.")]
        [Key(4)]
        public string ReportCode { get; set; } = string.Empty;

        [Description("1 Draft, 2 Validated, 3 Pending approval, 4 Approved, 5 Submitted, 6 Acknowledged, 7 Rejected by the FIU, 8 Withdrawn.")]
        [Key(5)]
        public int StateId { get; set; }

        [Description("The version in force.")]
        [Key(6)]
        public int CurrentVersion { get; set; }

        [Description("The FIU reference, once submitted.")]
        [Key(7)]
        public string? FiuReference { get; set; }

        [Description("Who started it.")]
        [Key(8)]
        public string CreatedUser { get; set; } = string.Empty;

        [Description("When it was started.")]
        [Key(9)]
        public DateTimeOffset CreatedDate { get; set; }

        [Description("Who recorded it as submitted.")]
        [Key(10)]
        public string? SubmittedUser { get; set; }

        [Description("When.")]
        [Key(11)]
        public DateTimeOffset? SubmittedDate { get; set; }

        [Description("The analyst entered fields of the current version, a JSON object.")]
        [Key(12)]
        public string FieldsJson { get; set; } = "{}";

        [Description("The versions, newest first.")]
        [Key(13)]
        public List<CaseReportVersionDto> Versions { get; set; } = [];

        [Description("The approval decisions, newest first.")]
        [Key(14)]
        public List<CaseReportApprovalDto> Approvals { get; set; } = [];

        [Description("The FIU feedback, newest first.")]
        [Key(15)]
        public List<CaseReportFeedbackDto> Feedback { get; set; } = [];

        [Description("The issues of the last validation of the current version.")]
        [Key(16)]
        public List<CaseReportIssueDto> Issues { get; set; } = [];
    }

    [Description("A request to start a report about a case.")]
    [MessagePackObject]
    public class CaseReportStartDto
    {
        [Description("The case. Required.")]
        [Key(0)]
        public int CaseId { get; set; }

        [Description("The profile name. Required.")]
        [Key(1)]
        public string ProfileName { get; set; } = string.Empty;

        [Description("The FIU report type, at most 50 characters. Required.")]
        [Key(2)]
        public string ReportCode { get; set; } = string.Empty;
    }

    [Description("The fields an analyst entered for the current version.")]
    [MessagePackObject]
    public class CaseReportSaveDto
    {
        [Description("The report. Required.")]
        [Key(0)]
        public Guid Guid { get; set; }

        [Description("A JSON object of the fields, at most 200000 characters. Required.")]
        [Key(1)]
        public string FieldsJson { get; set; } = "{}";
    }

    [Description("A decision, withdrawal or other step on a report, with an optional note.")]
    [MessagePackObject]
    public class CaseReportDecisionDto
    {
        [Description("The report. Required.")]
        [Key(0)]
        public Guid Guid { get; set; }

        [Description("The reason; required when rejecting, at most 1000 characters.")]
        [Key(1)]
        public string? Note { get; set; }
    }

    [Description("Recording that an approved report was filed with the FIU.")]
    [MessagePackObject]
    public class CaseReportSubmissionDto
    {
        [Description("The report. Required.")]
        [Key(0)]
        public Guid Guid { get; set; }

        [Description("The reference the FIU gave, at most 200 characters. Required.")]
        [Key(1)]
        public string FiuReference { get; set; } = string.Empty;
    }

    [Description("Recording what the FIU said about a submitted report.")]
    [MessagePackObject]
    public class CaseReportFeedbackRequestDto
    {
        [Description("The report. Required.")]
        [Key(0)]
        public Guid Guid { get; set; }

        [Description("1 Acknowledged, 2 Rejected, 3 Information requested. Required.")]
        [Key(1)]
        public int KindId { get; set; }

        [Description("The FIU message, at most 2000 characters.")]
        [Key(2)]
        public string? Message { get; set; }

        [Description("The field the FIU pointed at, at most 500 characters.")]
        [Key(3)]
        public string? FieldPath { get; set; }

        [Description("When the FIU responded, in UTC; now when empty.")]
        [Key(4)]
        public DateTime? ReceivedDate { get; set; }
    }

    [Description("A profile a report can be started from.")]
    [MessagePackObject]
    public class CaseReportProfileSummaryDto
    {
        [Description("The profile name.")]
        [Key(0)]
        public string Name { get; set; } = string.Empty;

        [Description("The current version.")]
        [Key(1)]
        public int Version { get; set; }

        [Description("A description.")]
        [Key(2)]
        public string? Description { get; set; }
    }

    [Description("A report profile to import: template, schemas and code lists.")]
    [MessagePackObject]
    public class CaseReportProfileDto
    {
        [Description("The profile name, at most 100 characters. Required.")]
        [Key(0)]
        public string Name { get; set; } = string.Empty;

        [Description("Version; ignored on import.")]
        [Key(1)]
        public int Version { get; set; }

        [Description("A description, at most 500 characters.")]
        [Key(2)]
        public string? Description { get; set; }

        [Description("The report template with its jr: directives. Required.")]
        [Key(3)]
        public string TemplateXml { get; set; } = string.Empty;

        [Description("File name to the XSD text the FIU issued. At least one is required.")]
        [Key(4)]
        public Dictionary<string, string> SchemaDocuments { get; set; } = [];

        [Description("Code list name to its value mappings.")]
        [Key(5)]
        public Dictionary<string, Dictionary<string, string>> CodeLists { get; set; } = [];

        [Description("Who imported it.")]
        [Key(6)]
        public string CreatedUser { get; set; } = string.Empty;

        [Description("When.")]
        [Key(7)]
        public DateTimeOffset CreatedDate { get; set; }
    }

    [Description("The outcome of an import: the new version, or why the profile was refused.")]
    [MessagePackObject]
    public class CaseReportProfileResultDto
    {
        [Description("True when the profile was imported.")]
        [Key(0)]
        public bool Valid { get; set; }

        [Description("The version now current.")]
        [Key(1)]
        public int Version { get; set; }

        [Description("Why it was refused.")]
        [Key(2)]
        public List<CaseReportIssueDto> Issues { get; set; } = [];
    }
}
