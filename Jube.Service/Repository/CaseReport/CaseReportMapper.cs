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

using Jube.Data.Poco;
using Jube.Dto.Repository.CaseReport;
using Jube.RegulatoryReport;

namespace Jube.Service.Repository.CaseReport
{
    public static class CaseReportMapper
    {
        public static DateTimeOffset Utc(DateTime value)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
        }

        public static CaseReportIssueDto ToDto(ReportIssue issue)
        {
            return new CaseReportIssueDto
            {
                Stage = (int)issue.Stage, Code = issue.Code, Message = issue.Message, Path = issue.Path,
                Line = issue.Line, Position = issue.Position
            };
        }

        public static CaseReportDto ToDto(Data.Poco.CaseReport report, List<CaseReportVersion> versions,
            List<CaseReportApproval> approvals, List<CaseReportFeedback> feedback, string fieldsJson,
            List<CaseReportIssueDto> issues)
        {
            return new CaseReportDto
            {
                Guid = report.Guid,
                CaseId = report.CaseId,
                ProfileName = report.ProfileName,
                ProfileVersion = report.ProfileVersion,
                ReportCode = report.ReportCode,
                StateId = report.StateId,
                CurrentVersion = report.CurrentVersion,
                FiuReference = report.FiuReference,
                CreatedUser = report.CreatedUser,
                CreatedDate = Utc(report.CreatedDate),
                SubmittedUser = report.SubmittedUser,
                SubmittedDate = report.SubmittedDate is null ? null : Utc(report.SubmittedDate.Value),
                FieldsJson = fieldsJson,
                Issues = issues,
                Versions =
                [
                    .. versions.Select(v => new CaseReportVersionDto
                    {
                        Version = v.Version, SupersedesVersion = v.SupersedesVersion, PayloadSha256 = v.PayloadSha256,
                        HasXml = v.Xml is not null, CreatedUser = v.CreatedUser, CreatedDate = Utc(v.CreatedDate)
                    })
                ],
                Approvals =
                [
                    .. approvals.Select(a => new CaseReportApprovalDto
                    {
                        ReportVersion = a.ReportVersion, DecisionId = a.DecisionId, Note = a.Note,
                        CreatedUser = a.CreatedUser, CreatedDate = Utc(a.CreatedDate)
                    })
                ],
                Feedback =
                [
                    .. feedback.Select(f => new CaseReportFeedbackDto
                    {
                        ReportVersion = f.ReportVersion, KindId = f.KindId, Message = f.Message,
                        FieldPath = f.FieldPath, ReceivedDate = Utc(f.ReceivedDate), RecordedUser = f.RecordedUser
                    })
                ]
            };
        }
    }
}
