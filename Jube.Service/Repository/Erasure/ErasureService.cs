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

using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Query.GetErasureSubjectBlockersQuery;
using Jube.Data.Query.GetSubjectArchiveExportQuery;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Repository.Erasure;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Service.Repository.Erasure
{
    public sealed class ErasureService
    {
        public const int RequestPermission = 78;
        public const int ApprovePermission = 79;
        public const int MaximumReason = 1000;
        public const int MaximumKeyName = 200;
        public const int MaximumKeyValue = 500;
        public const int ExportLimit = 10000;
        public const int ExportByteBudget = 64 * 1024 * 1024;

        private static readonly int[] requestPermissions = [RequestPermission];
        private static readonly int[] approvePermissions = [ApprovePermission];
        private static readonly int[] viewPermissions = [RequestPermission, ApprovePermission];

        private readonly DbContext dbContext;
        private readonly string userName;
        private readonly int tenantRegistryId;
        private readonly int retentionMinimumDays;
        private readonly PermissionValidation permissionValidation;
        private readonly ILog log;
        private readonly ILog auditLog;
        private readonly IServiceChangeBus serviceChangeBus;

        private ErasureService(DbContext dbContext, string userName, int tenantRegistryId, int retentionMinimumDays,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.retentionMinimumDays = retentionMinimumDays;
            this.permissionValidation = permissionValidation;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
        }

        public static async Task<ErasureService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IServiceChangeBus serviceChangeBus, int retentionMinimumDays, CancellationToken token = default)
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

            return new ErasureService(dbContext, userName, tenant.Value, retentionMinimumDays, validation, log,
                LogManager.GetLogger("Jube.Audit"), serviceChangeBus);
        }

        private void Ensure(int[] required)
        {
            if (!permissionValidation.Validate(required))
            {
                throw new CaseExceptions.ForbiddenException("Permission denied.", required);
            }
        }

        private static (string Name, string Value) Subject(string? keyName, string? keyValue)
        {
            var name = keyName?.Trim();
            var value = keyValue?.Trim();

            if (string.IsNullOrEmpty(name) || name.Length > MaximumKeyName)
            {
                throw new ArgumentException($"The key name is required and at most {MaximumKeyName} characters.");
            }

            if (string.IsNullOrEmpty(value) || value.Length > MaximumKeyValue)
            {
                throw new ArgumentException($"The key value is required and at most {MaximumKeyValue} characters.");
            }

            return (name, value);
        }

        private Task<GetErasureSubjectBlockersQueryDto> BlockersAsync(string name, string value,
            CancellationToken token)
        {
            return new GetErasureSubjectBlockersQuery(dbContext).ExecuteAsync(tenantRegistryId, name, value,
                retentionMinimumDays, DateTime.UtcNow, token);
        }

        private async Task<ErasureRequest> RequireAsync(Guid guid, CancellationToken token)
        {
            var request = await new ErasureRequestRepository(dbContext).GetAsync(tenantRegistryId, guid, token)
                .ConfigureAwait(false);

            return request ?? throw new CaseExceptions.NotFoundException("The erasure request was not found.");
        }

        public async Task<List<ErasureRequestDto>> ListAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("Erasure", "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                Ensure(viewPermissions);

                var rows = await new ErasureRequestRepository(dbContext).ListAsync(tenantRegistryId, 200, token)
                    .ConfigureAwait(false);

                op.Rows(rows.Count);

                return rows.Select(ToDto).ToList();
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        public async Task<ErasurePreviewDto> PreviewAsync(ErasureSubjectDto? model, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            using var op = OperationScope.Start("Erasure", "Preview", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                Ensure(requestPermissions);

                var (name, value) = Subject(model.KeyName, model.KeyValue);
                var blockers = await BlockersAsync(name, value, token).ConfigureAwait(false);

                return new ErasurePreviewDto
                {
                    ArchiveRows = (int)blockers.ArchiveRows, RefusalReasonId = (int?)blockers.Refusal()
                };
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        public async Task<ErasureRequestDto> RequestAsync(ErasureRequestCreateDto? model,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            using var op = OperationScope.Start("Erasure", "Request", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                Ensure(requestPermissions);

                var (name, value) = Subject(model.KeyName, model.KeyValue);
                var reason = model.Reason?.Trim();

                if (string.IsNullOrEmpty(reason) || reason.Length > MaximumReason)
                {
                    throw new ArgumentException($"A reason is required, at most {MaximumReason} characters.");
                }

                var repository = new ErasureRequestRepository(dbContext);

                if (await repository.CountOpenForSubjectAsync(tenantRegistryId, name, value, token)
                        .ConfigureAwait(false) > 0)
                {
                    throw new CaseExceptions.ConflictException("There is already an open erasure request for this subject.");
                }

                var blockers = await BlockersAsync(name, value, token).ConfigureAwait(false);
                var refusal = blockers.Refusal();

                var created = await repository.InsertAsync(new ErasureRequest
                {
                    TenantRegistryId = tenantRegistryId, KeyName = name, KeyValue = value, Reason = reason,
                    StateId = (short)(refusal is null ? ErasureState.Pending : ErasureState.Refused),
                    RefusalReasonId = refusal is null ? null : (short)refusal.Value,
                    ArchiveRowsFound = (int)blockers.ArchiveRows, RequestedUser = userName
                }, token).ConfigureAwait(false);

                op.Created();

                return ToDto(created);
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        public Task<ErasureRequestDto> ApproveAsync(ErasureDecisionDto? model, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            return DecideAsync("Approve", model.Guid, approvePermissions, ErasureState.Approved, model.Note, false,
                token);
        }

        public Task<ErasureRequestDto> RejectAsync(ErasureDecisionDto? model, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            return DecideAsync("Reject", model.Guid, approvePermissions, ErasureState.Rejected, model.Note, true,
                token);
        }

        public Task<ErasureRequestDto> RetractAsync(Guid guid, CancellationToken token = default)
        {
            return DecideAsync("Retract", guid, requestPermissions, ErasureState.Retracted, null, false, token);
        }

        private async Task<ErasureRequestDto> DecideAsync(string operation, Guid guid, int[] required,
            ErasureState to, string? noteText, bool noteRequired, CancellationToken token)
        {
            using var op = OperationScope.Start("Erasure", operation, userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                Ensure(required);

                var note = string.IsNullOrWhiteSpace(noteText) ? null : noteText.Trim();

                if (noteRequired && note is null)
                {
                    throw new ArgumentException("A reason is required to reject an erasure request.");
                }

                if (note is { Length: > MaximumReason })
                {
                    throw new ArgumentException($"The note must be at most {MaximumReason} characters.");
                }

                var request = await RequireAsync(guid, token).ConfigureAwait(false);
                var own = string.Equals(request.RequestedUser, userName, StringComparison.OrdinalIgnoreCase);

                if (request.StateId != (short)ErasureState.Pending)
                {
                    throw new CaseExceptions.ConflictException("Only a pending request can be decided.");
                }

                if (to == ErasureState.Retracted ? !own : own)
                {
                    throw new CaseExceptions.ConflictException(to == ErasureState.Retracted
                        ? "Only the person who asked can retract a request."
                        : "A request cannot be decided by the person who made it.");
                }

                if (await new ErasureRequestRepository(dbContext)
                        .DecideAsync(request.Id, ErasureState.Pending, to, userName, note, token)
                        .ConfigureAwait(false) == 0)
                {
                    throw new CaseExceptions.ConflictException("The request is no longer pending.");
                }

                op.Updated();

                return ToDto(await RequireAsync(guid, token).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        public async Task<SubjectExportDto> ExportAsync(ErasureSubjectDto? model, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(model);

            using var op = OperationScope.Start("Erasure", "Export", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                Ensure(requestPermissions);

                var (name, value) = Subject(model.KeyName, model.KeyValue);
                var rows = await new GetSubjectArchiveExportQuery(dbContext)
                    .ExecuteAsync(tenantRegistryId, name, value, ExportLimit + 1, token).ConfigureAwait(false);

                var transactions = new List<SubjectExportTransactionDto>();
                long bytes = 0;
                var truncated = rows.Count > ExportLimit;

                foreach (var row in rows.Take(ExportLimit))
                {
                    var json = row.Json ?? string.Empty;
                    bytes += json.Length;

                    if (bytes > ExportByteBudget)
                    {
                        truncated = true;

                        break;
                    }

                    transactions.Add(new SubjectExportTransactionDto
                    {
                        EntryGuid = row.EntryGuid, ModelId = row.ModelId, ReferenceDate = row.ReferenceDate,
                        CreatedDate = row.CreatedDate, Json = json
                    });
                }

                op.Rows(transactions.Count);

                return new SubjectExportDto
                {
                    KeyName = name, KeyValue = value, GeneratedDate = DateTime.UtcNow, GeneratedUser = userName,
                    Truncated = truncated, Transactions = transactions
                };
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        private static ErasureRequestDto ToDto(ErasureRequest request)
        {
            return new ErasureRequestDto
            {
                Guid = request.Guid, KeyName = request.KeyName, KeyValue = request.KeyValue, Reason = request.Reason,
                StateId = request.StateId, RefusalReasonId = request.RefusalReasonId,
                ArchiveRowsFound = request.ArchiveRowsFound, ArchiveRowsRemoved = request.ArchiveRowsRemoved,
                GraphNodesRemoved = request.GraphNodesRemoved, GraphEdgesRemoved = request.GraphEdgesRemoved,
                CasePayloadsNulled = request.CasePayloadsNulled, RequestedUser = request.RequestedUser,
                RequestedDate = request.RequestedDate, DecidedUser = request.DecidedUser,
                DecidedDate = request.DecidedDate, DecisionNote = request.DecisionNote,
                ExecutedDate = request.ExecutedDate
            };
        }
    }
}
