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

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Repository.CaseReport;
using Jube.RegulatoryReport;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using LinqToDB;
using LinqToDB.Async;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Service.Repository.CaseReport
{
    public sealed class CaseReportService
    {
        public const int FilePermission = 75;
        public const int ApprovePermission = 76;
        public const int ViewPermission = 77;
        public const int MaximumNote = 1000;
        public const int MaximumReference = 200;
        public const int MaximumMessage = 2000;
        public const int MaximumFieldPath = 500;
        public const int MaximumReportCode = 50;
        public const string HoldReason = "Regulatory report filed";

        private static readonly int[] viewPermissions = [FilePermission, ApprovePermission, ViewPermission];
        private static readonly int[] filePermissions = [FilePermission];
        private static readonly int[] approvePermissions = [ApprovePermission];

        private readonly DbContext dbContext;
        private readonly string userName;
        private readonly int tenantRegistryId;
        private readonly PermissionValidation permissionValidation;
        private readonly ILog log;
        private readonly ILog auditLog;
        private readonly IServiceChangeBus serviceChangeBus;

        private CaseReportService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
        }

        public static async Task<CaseReportService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new CaseExceptions.NotAuthenticatedException("Not authenticated.");
            }

            var tenant = await UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, userName, token)
                .ConfigureAwait(false);

            if (tenant is null)
            {
                throw new CaseExceptions.NotAuthenticatedException("Not authenticated.");
            }

            var validation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseReportService(dbContext, userName, tenant.Value, validation, log,
                LogManager.GetLogger("Jube.Audit"), serviceChangeBus);
        }

        public static async Task<bool> HoldsAnyAsync(DbContext dbContext, string userName, ILog log,
            CancellationToken token = default)
        {
            var validation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return validation.Validate(viewPermissions);
        }

        private void Ensure(int[] required)
        {
            if (!permissionValidation.Validate(required))
            {
                throw new CaseExceptions.ForbiddenException("Permission denied.", required);
            }
        }

        private async Task EnsureVisibleAsync(int caseId, CancellationToken token)
        {
            var visible = await new CaseRepository(dbContext, userName)
                .GetVisibleIdsActiveOnlyAsync([caseId], token).ConfigureAwait(false);

            if (visible.Count == 0)
            {
                throw new CaseExceptions.NotFoundException("The case was not found.");
            }
        }

        private async Task<Data.Poco.CaseReport> RequireAsync(Guid guid, CancellationToken token)
        {
            var report = await new CaseReportRepository(dbContext).GetAsync(tenantRegistryId, guid, token)
                .ConfigureAwait(false);

            if (report is null)
            {
                throw new CaseExceptions.NotFoundException("The report was not found.");
            }

            await EnsureVisibleAsync(report.CaseId, token).ConfigureAwait(false);

            return report;
        }

        public async Task<List<CaseReportDto>> ListAsync(int caseId, CancellationToken token = default)
        {
            Ensure(viewPermissions);
            await EnsureVisibleAsync(caseId, token).ConfigureAwait(false);

            var reports = await new CaseReportRepository(dbContext).ListForCaseAsync(tenantRegistryId, caseId, token)
                .ConfigureAwait(false);

            var list = new List<CaseReportDto>();

            foreach (var report in reports)
            {
                list.Add(await ToDtoAsync(report, token).ConfigureAwait(false));
            }

            return list;
        }

        public async Task<List<CaseReportDto>> ListAwaitingApprovalAsync(CancellationToken token = default)
        {
            Ensure(approvePermissions);

            var reports = await new CaseReportRepository(dbContext).ListAwaitingApprovalAsync(tenantRegistryId, 200,
                token).ConfigureAwait(false);

            var visible = (await new CaseRepository(dbContext, userName)
                .GetVisibleIdsActiveOnlyAsync(reports.Select(r => r.CaseId).Distinct().ToList(), token)
                .ConfigureAwait(false)).ToHashSet();

            var list = new List<CaseReportDto>();

            foreach (var report in reports.Where(r => visible.Contains(r.CaseId)))
            {
                list.Add(await ToDtoAsync(report, token).ConfigureAwait(false));
            }

            return list;
        }

        public async Task<CaseReportDto> GetAsync(Guid guid, CancellationToken token = default)
        {
            Ensure(viewPermissions);

            return await ToDtoAsync(await RequireAsync(guid, token).ConfigureAwait(false), token).ConfigureAwait(false);
        }

        public async Task<CaseReportDto> StartAsync(CaseReportStartDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseReport", "Start", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                Ensure(filePermissions);

                if (model.CaseId <= 0 || string.IsNullOrWhiteSpace(model.ProfileName)
                                      || string.IsNullOrWhiteSpace(model.ReportCode)
                                      || model.ReportCode.Length > MaximumReportCode)
                {
                    throw new ArgumentException("The case, profile and a report code of at most 50 characters are required.");
                }

                await EnsureVisibleAsync(model.CaseId, token).ConfigureAwait(false);

                var profile = await new CaseReportProfileRepository(dbContext)
                    .CurrentAsync(tenantRegistryId, model.ProfileName.Trim(), token).ConfigureAwait(false);

                if (profile is null)
                {
                    throw new CaseExceptions.NotFoundException("The profile was not found.");
                }

                var theCase = await dbContext.Case.Where(c => c.Id == model.CaseId).FirstAsync(token)
                    .ConfigureAwait(false);

                var report = new Data.Poco.CaseReport
                {
                    Guid = Guid.NewGuid(),
                    TenantRegistryId = tenantRegistryId,
                    CaseId = model.CaseId,
                    CaseKey = theCase.CaseKey,
                    CaseKeyValue = theCase.CaseKeyValue,
                    ProfileName = profile.Name,
                    ProfileVersion = profile.Version,
                    ReportCode = model.ReportCode.Trim(),
                    StateId = (short)CaseReportState.Draft,
                    CurrentVersion = 1,
                    CreatedUser = userName
                };

                await new CaseReportRepository(dbContext).InsertAsync(report, token).ConfigureAwait(false);

                await new CaseReportVersionRepository(dbContext).InsertAsync(new CaseReportVersion
                {
                    CaseReportId = report.Id, Version = 1, DataJson = new JsonObject { ["fields"] = new JsonObject() }
                        .ToJsonString(), CreatedUser = userName
                }, token).ConfigureAwait(false);

                op.Entity(model.CaseId);
                op.Created();

                return await ToDtoAsync(report, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        public async Task<CaseReportDto> SaveAsync(CaseReportSaveDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseReport", "Save", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                Ensure(filePermissions);

                var fields = CaseReportDataAssembler.ParseFields(model.FieldsJson);
                var report = await RequireAsync(model.Guid, token).ConfigureAwait(false);
                var versions = new CaseReportVersionRepository(dbContext);
                var reports = new CaseReportRepository(dbContext);
                var current = await versions.GetAsync(report.Id, report.CurrentVersion, token).ConfigureAwait(false);
                var data = new JsonObject { ["fields"] = fields }.ToJsonString();

                if (report.StateId == (short)CaseReportState.Draft && current.Xml is null
                    && string.Equals(current.CreatedUser, userName, StringComparison.OrdinalIgnoreCase))
                {
                    if (await versions.UpdateDataAsync(current.Id, data, token).ConfigureAwait(false) == 0)
                    {
                        throw new CaseExceptions.ConflictException("The report was changed by someone else.");
                    }
                }
                else if (report.StateId is (short)CaseReportState.Draft or (short)CaseReportState.Validated)
                {
                    var next = report.CurrentVersion + 1;

                    await versions.InsertAsync(new CaseReportVersion
                    {
                        CaseReportId = report.Id, Version = next, SupersedesVersion = report.CurrentVersion,
                        DataJson = data, CreatedUser = userName
                    }, token).ConfigureAwait(false);

                    if (await reports.SetVersionAsync(report.Id, report.StateId, (short)CaseReportState.Draft, next,
                            userName, token).ConfigureAwait(false) == 0)
                    {
                        throw new CaseExceptions.ConflictException("The report was changed by someone else.");
                    }
                }
                else
                {
                    throw new CaseExceptions.ConflictException(
                        "The report can only be edited as a draft or validated version; withdraw or correct it first.");
                }

                op.Entity(report.CaseId);
                op.Updated();

                return await ToDtoAsync(await RequireAsync(model.Guid, token).ConfigureAwait(false), token)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        public async Task<CaseReportDto> ValidateAsync(Guid guid, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseReport", "Validate", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                Ensure(filePermissions);

                var report = await RequireAsync(guid, token).ConfigureAwait(false);

                if (report.StateId != (short)CaseReportState.Draft)
                {
                    throw new CaseExceptions.ConflictException("Only a draft report can be validated.");
                }

                var versions = new CaseReportVersionRepository(dbContext);
                var current = await versions.GetAsync(report.Id, report.CurrentVersion, token).ConfigureAwait(false);

                if (current.Xml is not null)
                {
                    throw new CaseExceptions.ConflictException("This version is already validated; edit it to make a new version.");
                }

                if (!string.Equals(current.CreatedUser, userName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new CaseExceptions.ConflictException(
                        "Only the person who last edited this version can validate it; save it yourself to take it over.");
                }

                var profileRow = await new CaseReportProfileRepository(dbContext)
                    .VersionAsync(tenantRegistryId, report.ProfileName, report.ProfileVersion, token)
                    .ConfigureAwait(false);

                var profile = ToProfile(profileRow);
                var theCase = await dbContext.Case.Where(c => c.Id == report.CaseId).FirstAsync(token)
                    .ConfigureAwait(false);

                var workflow = await dbContext.CaseWorkflow
                    .Where(w => w.Guid == theCase.CaseWorkflowGuid && (w.Deleted == 0 || w.Deleted == null))
                    .Select(w => w.Name).FirstOrDefaultAsync(token).ConfigureAwait(false);

                var narrative = (await new CaseNoteRepository(dbContext, userName)
                    .GetLatestNarrativeAsync(report.CaseId, token).ConfigureAwait(false))?.Note;

                var fields = FieldsOf(current.DataJson);
                var data = CaseReportDataAssembler.Assemble(report, theCase, workflow, narrative, fields, userName,
                    DateTime.UtcNow);

                var result = ReportBuilder.Build(profile, data);
                var issuesJson = JsonSerializer.Serialize(result.Issues.Select(CaseReportMapper.ToDto));
                await versions.RecordValidationAsync(current.Id, data.ToJsonString(), issuesJson, result.Succeeded ? result.Xml : null, result.Sha256,
                    token).ConfigureAwait(false);

                if (result.Succeeded && await new CaseReportRepository(dbContext).MoveAsync(report.Id,
                        (short)CaseReportState.Draft, (short)CaseReportState.Validated, userName, token)
                        .ConfigureAwait(false) == 0)
                {
                    throw new CaseExceptions.ConflictException("The report was changed by someone else.");
                }

                op.Entity(report.CaseId);
                op.Updated();

                return await ToDtoAsync(await RequireAsync(guid, token).ConfigureAwait(false), token)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        public async Task<CaseReportDto> RequestApprovalAsync(Guid guid, CancellationToken token = default)
        {
            return await StepAsync("RequestApproval", guid, filePermissions, async report =>
            {
                var latest = await new CaseReportVersionRepository(dbContext)
                    .GetAsync(report.Id, report.CurrentVersion, token).ConfigureAwait(false);

                if (!string.Equals(latest.CreatedUser, userName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new CaseExceptions.ConflictException(
                        "Only the person who last edited this version can request its approval; save it yourself to take it over.");
                }

                await MoveOrConflictAsync(report, CaseReportState.Validated, CaseReportState.PendingApproval, token)
                    .ConfigureAwait(false);

                await NotifyAsync(approvePermissions[0], report, NotificationKind.CaseReportAwaitingApproval,
                    $"case-report-approval:{report.Guid:N}:{report.CurrentVersion}", null, token).ConfigureAwait(false);
            }, token).ConfigureAwait(false);
        }

        public async Task<CaseReportDto> ApproveAsync(CaseReportDecisionDto? model, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            return await StepAsync("Approve", model.Guid, approvePermissions, report =>
                DecideAsync(report, CaseReportDecision.Approved, CaseReportState.Approved, model.Note, token), token)
                .ConfigureAwait(false);
        }

        public async Task<CaseReportDto> RejectAsync(CaseReportDecisionDto? model, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            if (string.IsNullOrWhiteSpace(model.Note))
            {
                throw new ArgumentException("A reason is required to reject a report.");
            }

            return await StepAsync("Reject", model.Guid, approvePermissions, report =>
                DecideAsync(report, CaseReportDecision.Rejected, CaseReportState.Draft, model.Note, token), token)
                .ConfigureAwait(false);
        }

        public async Task<CaseReportDto> WithdrawAsync(Guid guid, CancellationToken token = default)
        {
            return await StepAsync("Withdraw", guid, viewPermissions, async report =>
            {
                var allowed = string.Equals(report.CreatedUser, userName, StringComparison.OrdinalIgnoreCase)
                              || permissionValidation.Validate(approvePermissions);

                if (!allowed || report.StateId > (short)CaseReportState.PendingApproval)
                {
                    throw new CaseExceptions.ConflictException("This report cannot be withdrawn.");
                }

                await MoveOrConflictAsync(report, (CaseReportState)report.StateId, CaseReportState.Withdrawn, token)
                    .ConfigureAwait(false);
            }, token).ConfigureAwait(false);
        }

        public async Task<CaseReportDto> NewVersionAsync(Guid guid, CancellationToken token = default)
        {
            return await StepAsync("NewVersion", guid, filePermissions, async report =>
            {
                if (report.StateId != (short)CaseReportState.Rejected)
                {
                    throw new CaseExceptions.ConflictException("Only a report the FIU rejected can be corrected as a new version.");
                }

                var versions = new CaseReportVersionRepository(dbContext);
                var current = await versions.GetAsync(report.Id, report.CurrentVersion, token).ConfigureAwait(false);
                var next = report.CurrentVersion + 1;

                await versions.InsertAsync(new CaseReportVersion
                {
                    CaseReportId = report.Id, Version = next, SupersedesVersion = report.CurrentVersion,
                    DataJson = new JsonObject { ["fields"] = FieldsOf(current.DataJson) }.ToJsonString(),
                    CreatedUser = userName
                }, token).ConfigureAwait(false);

                if (await new CaseReportRepository(dbContext).SetVersionAsync(report.Id, (short)CaseReportState.Rejected,
                        (short)CaseReportState.Draft, next, userName, token).ConfigureAwait(false) == 0)
                {
                    throw new CaseExceptions.ConflictException("The report was changed by someone else.");
                }
            }, token).ConfigureAwait(false);
        }

        public async Task<CaseReportDto> RecordSubmissionAsync(CaseReportSubmissionDto? model,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            if (string.IsNullOrWhiteSpace(model.FiuReference) || model.FiuReference.Length > MaximumReference)
            {
                throw new ArgumentException($"The FIU reference is required and at most {MaximumReference} characters.");
            }

            return await StepAsync("RecordSubmission", model.Guid, filePermissions, async report =>
            {
                if (await new CaseReportRepository(dbContext).SubmitAsync(report.Id, model.FiuReference.Trim(),
                        userName, token).ConfigureAwait(false) == 0)
                {
                    throw new CaseExceptions.ConflictException("Only an approved report can be recorded as submitted.");
                }

                await dbContext.Case.Where(c => c.Id == report.CaseId && (c.LegalHold == null || c.LegalHold == 0))
                    .Set(s => s.LegalHold, (byte?)1).Set(s => s.LegalHoldReason, HoldReason)
                    .Set(s => s.LegalHoldUser, userName).Set(s => s.LegalHoldDate, DateTime.UtcNow)
                    .UpdateAsync(token).ConfigureAwait(false);
            }, token).ConfigureAwait(false);
        }

        public async Task<CaseReportDto> RecordFeedbackAsync(CaseReportFeedbackRequestDto? model,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            if (!Enum.IsDefined((CaseReportFeedbackKind)model.KindId))
            {
                throw new ArgumentException("The kind must be 1, 2 or 3.");
            }

            if (model.Message is { Length: > MaximumMessage } || model.FieldPath is { Length: > MaximumFieldPath })
            {
                throw new ArgumentException("The message or field path is too long.");
            }

            return await StepAsync("RecordFeedback", model.Guid, filePermissions, async report =>
            {
                if (report.StateId is not ((short)CaseReportState.Submitted or (short)CaseReportState.Acknowledged))
                {
                    throw new CaseExceptions.ConflictException("Only a submitted report can have FIU feedback recorded.");
                }

                var feedbackCount = (await new CaseReportFeedbackRepository(dbContext)
                    .ListAsync(report.Id, token).ConfigureAwait(false)).Count;

                await new CaseReportFeedbackRepository(dbContext).InsertAsync(new CaseReportFeedback
                {
                    CaseReportId = report.Id,
                    ReportVersion = report.CurrentVersion,
                    KindId = (short)model.KindId,
                    Message = string.IsNullOrWhiteSpace(model.Message) ? null : model.Message.Trim(),
                    FieldPath = string.IsNullOrWhiteSpace(model.FieldPath) ? null : model.FieldPath.Trim(),
                    ReceivedDate = BoundedReceived(model.ReceivedDate),
                    RecordedUser = userName
                }, token).ConfigureAwait(false);

                var kind = (CaseReportFeedbackKind)model.KindId;

                if (kind == CaseReportFeedbackKind.Acknowledged)
                {
                    await MoveOrConflictAsync(report, (CaseReportState)report.StateId, CaseReportState.Acknowledged,
                        token).ConfigureAwait(false);
                }
                else if (kind == CaseReportFeedbackKind.Rejected)
                {
                    await MoveOrConflictAsync(report, (CaseReportState)report.StateId, CaseReportState.Rejected, token)
                        .ConfigureAwait(false);
                }

                await NotifyUserAsync(report.CreatedUser, report, NotificationKind.CaseReportFeedback,
                    $"case-report-feedback:{report.Guid:N}:{report.CurrentVersion}:{model.KindId}:{feedbackCount}",
                    (int)kind, token).ConfigureAwait(false);
            }, token).ConfigureAwait(false);
        }

        private static DateTime BoundedReceived(DateTime? received)
        {
            var now = DateTime.UtcNow;

            if (received is null)
            {
                return now;
            }

            var value = received.Value.ToUniversalTime();

            if (value < now.AddYears(-10) || value > now.AddDays(1))
            {
                throw new ArgumentException("The received date must be within the last ten years and not in the future.");
            }

            return value;
        }

        public async Task<(byte[] Bytes, string Name, string Sha256)> DownloadAsync(Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseReport", "Download", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                Ensure([FilePermission, ApprovePermission]);

                var report = await RequireAsync(guid, token).ConfigureAwait(false);

                if (report.StateId is < (short)CaseReportState.Approved or (short)CaseReportState.Withdrawn)
                {
                    throw new CaseExceptions.ConflictException("Only an approved or filed report can be downloaded.");
                }

                var version = await new CaseReportVersionRepository(dbContext)
                    .GetAsync(report.Id, report.CurrentVersion, token).ConfigureAwait(false);

                if (version.Xml is null)
                {
                    throw new CaseExceptions.NotFoundException("The report file was not found.");
                }

                var bytes = Encoding.UTF8.GetBytes(version.Xml);
                var actual = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

                if (!string.Equals(actual, version.PayloadSha256, StringComparison.Ordinal))
                {
                    log.Error($"CaseReport.Download: the stored report {report.Guid} does not match its recorded hash.");

                    throw new InvalidOperationException("The stored report failed its integrity check.");
                }

                op.Entity(report.CaseId);

                return (bytes, $"report-{report.Guid:N}-v{report.CurrentVersion}.xml", actual);
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        private async Task<CaseReportDto> StepAsync(string operation, Guid guid, int[] required,
            Func<Data.Poco.CaseReport, Task> action, CancellationToken token)
        {
            using var op = OperationScope.Start("CaseReport", operation, userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                Ensure(required);

                var report = await RequireAsync(guid, token).ConfigureAwait(false);
                await action(report).ConfigureAwait(false);

                op.Entity(report.CaseId);
                op.Updated();

                return await ToDtoAsync(await RequireAsync(guid, token).ConfigureAwait(false), token)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        private async Task MoveOrConflictAsync(Data.Poco.CaseReport report, CaseReportState from, CaseReportState to,
            CancellationToken token)
        {
            if (report.StateId != (short)from
                || await new CaseReportRepository(dbContext).MoveAsync(report.Id, (short)from, (short)to, userName, token)
                    .ConfigureAwait(false) == 0)
            {
                throw new CaseExceptions.ConflictException("The report is not in the right state for that.");
            }
        }

        private async Task DecideAsync(Data.Poco.CaseReport report, CaseReportDecision decision, CaseReportState to,
            string? noteText, CancellationToken token)
        {
            var note = string.IsNullOrWhiteSpace(noteText) ? null : noteText.Trim();

            if (note is { Length: > MaximumNote })
            {
                throw new ArgumentException($"The note must be at most {MaximumNote} characters.");
            }

            var version = await new CaseReportVersionRepository(dbContext)
                .GetAsync(report.Id, report.CurrentVersion, token).ConfigureAwait(false);

            if (string.Equals(report.CreatedUser, userName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(version.CreatedUser, userName, StringComparison.OrdinalIgnoreCase))
            {
                throw new CaseExceptions.ConflictException("You cannot decide a report you prepared.");
            }

            await MoveOrConflictAsync(report, CaseReportState.PendingApproval, to, token).ConfigureAwait(false);

            await new CaseReportApprovalRepository(dbContext).InsertAsync(new CaseReportApproval
            {
                CaseReportId = report.Id, ReportVersion = report.CurrentVersion, DecisionId = (short)decision,
                Note = note, CreatedUser = userName
            }, token).ConfigureAwait(false);

            await NotifyUserAsync(version.CreatedUser, report, NotificationKind.CaseReportDecided,
                $"case-report-decided:{report.Guid:N}:{report.CurrentVersion}:{(int)decision}", (int)decision,
                token).ConfigureAwait(false);
        }

        private async Task NotifyAsync(int permission, Data.Poco.CaseReport report, NotificationKind kind,
            string dedupe, int? detailId, CancellationToken token)
        {
            foreach (var user in await new SanctionRescreenRepository(dbContext)
                         .ListPermissionHoldersAsync(tenantRegistryId, permission, token).ConfigureAwait(false))
            {
                if (!string.Equals(user, userName, StringComparison.OrdinalIgnoreCase))
                {
                    await NotifyUserAsync(user, report, kind, dedupe, detailId, token).ConfigureAwait(false);
                }
            }
        }

        private async Task NotifyUserAsync(string recipient, Data.Poco.CaseReport report, NotificationKind kind,
            string dedupe, int? detailId, CancellationToken token)
        {
            await new UserNotificationRepository(dbContext).InsertIfNewAsync(new UserNotification
            {
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantRegistryId,
                RecipientUser = recipient,
                KindId = (short)kind,
                PriorityId = (short)NotificationPriority.Normal,
                CaseId = report.CaseId,
                ArgumentsJson = JsonSerializer.Serialize(new { caseId = report.CaseId, detailId }),
                DedupeKey = $"{dedupe}:{recipient}",
                CreatedDate = DateTime.UtcNow
            }, token).ConfigureAwait(false);
        }

        private async Task<CaseReportDto> ToDtoAsync(Data.Poco.CaseReport report, CancellationToken token)
        {
            var versions = await new CaseReportVersionRepository(dbContext).ListAsync(report.Id, token)
                .ConfigureAwait(false);

            var current = versions.FirstOrDefault(v => v.Version == report.CurrentVersion);

            var approvals = await new CaseReportApprovalRepository(dbContext).ListAsync(report.Id, token)
                .ConfigureAwait(false);

            var feedback = await new CaseReportFeedbackRepository(dbContext).ListAsync(report.Id, token)
                .ConfigureAwait(false);

            var issues = new List<CaseReportIssueDto>();

            if (!string.IsNullOrEmpty(current?.IssuesJson))
            {
                try
                {
                    issues = JsonSerializer.Deserialize<List<CaseReportIssueDto>>(current.IssuesJson) ?? [];
                }
                catch (JsonException)
                {
                }
            }

            return CaseReportMapper.ToDto(report, versions, approvals, feedback,
                FieldsOf(current?.DataJson).ToJsonString(), issues);
        }

        private static JsonObject FieldsOf(string? dataJson)
        {
            if (string.IsNullOrWhiteSpace(dataJson))
            {
                return [];
            }

            return (JsonNode.Parse(dataJson) as JsonObject)?["fields"] as JsonObject is { } fields
                ? (JsonObject)fields.DeepClone()
                : [];
        }

        public static ReportProfile ToProfile(CaseReportProfile row)
        {
            var schemas = JsonSerializer.Deserialize<Dictionary<string, string>>(row.SchemaDocumentsJson) ?? [];

            var lists = (JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(
                row.CodeListsJson) ?? []).ToDictionary(k => k.Key, v => (IReadOnlyDictionary<string, string>)v.Value);

            return new ReportProfile(row.Name, row.TemplateXml, schemas, lists, row.Description);
        }
    }
}
