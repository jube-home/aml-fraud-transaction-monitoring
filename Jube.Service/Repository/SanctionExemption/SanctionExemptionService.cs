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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Repository.SanctionExemption;
using Jube.Engine.Sanctions;
using Jube.Service.Exceptions.Repository.SanctionExemption;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using LinqToDB;
using LinqToDB.Async;

namespace Jube.Service.Repository.SanctionExemption
{
    public sealed class SanctionExemptionService
    {
        public const int RequestPermission = 29;
        public const int DecisionPermission = 44;
        public const int MaximumName = 500;
        public const int MaximumReason = 1000;
        private const int PageSize = 500;

        private static readonly int[] requestPermissions = [RequestPermission];
        private static readonly int[] decisionPermissions = [DecisionPermission];
        private static readonly int[] readPermissions = [RequestPermission, DecisionPermission];

        private readonly DbContext dbContext;
        private readonly string userName;
        private readonly int tenantRegistryId;
        private readonly PermissionValidation permissionValidation;
        private readonly ILog log;
        private readonly ILog auditLog;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly SanctionExemptionRepository repository;

        private SanctionExemptionService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            repository = new SanctionExemptionRepository(dbContext);
        }

        public static async Task<SanctionExemptionService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new NotAuthenticatedException("Not authenticated.");
            }

            var tenant = await UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, userName, token)
                .ConfigureAwait(false);

            if (tenant is null)
            {
                throw new NotAuthenticatedException("Not authenticated.");
            }

            var permissions = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new SanctionExemptionService(dbContext, userName, tenant.Value, permissions, log,
                LogManager.GetLogger("Jube.Audit"), serviceChangeBus);
        }

        private void Ensure(int[] required)
        {
            if (!permissionValidation.Validate(required))
            {
                throw new ForbiddenException("Permission denied.", required);
            }
        }

        private static SanctionExemptionDto ToDto(Jube.Data.Poco.SanctionExemption row)
        {
            return new SanctionExemptionDto
            {
                Id = row.Id,
                ScreenedName = row.ScreenedName,
                SanctionEntrySourceId = row.SanctionEntrySourceId,
                SanctionEntryReference = row.SanctionEntryReference,
                Reason = row.Reason,
                ExpiryDate = row.ExpiryDate.HasValue
                    ? new DateTimeOffset(DateTime.SpecifyKind(row.ExpiryDate.Value, DateTimeKind.Utc))
                    : null,
                StateId = row.StateId,
                RequestedUser = row.RequestedUser,
                RequestedDate = new DateTimeOffset(DateTime.SpecifyKind(row.RequestedDate, DateTimeKind.Utc)),
                DecidedUser = row.DecidedUser,
                DecidedDate = row.DecidedDate.HasValue
                    ? new DateTimeOffset(DateTime.SpecifyKind(row.DecidedDate.Value, DateTimeKind.Utc))
                    : null,
                DecisionNote = row.DecisionNote
            };
        }

        public async Task<List<SanctionExemptionDto>> ListAsync(short? stateId, CancellationToken token = default)
        {
            Ensure(readPermissions);

            return (await repository.ListAsync(tenantRegistryId, stateId, PageSize, token).ConfigureAwait(false))
                .Select(ToDto).ToList();
        }

        public async Task<SanctionExemptionDto> RequestAsync(SanctionExemptionRequestDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("SanctionExemption", "Request", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                Ensure(requestPermissions);

                var name = model.ScreenedName?.Trim();
                var reference = model.SanctionEntryReference?.Trim();
                var reason = model.Reason?.Trim();

                if (string.IsNullOrEmpty(name) || name.Length > MaximumName)
                {
                    throw new ArgumentException($"The screened name is required and at most {MaximumName} characters.");
                }

                if (string.IsNullOrEmpty(reference) || reference.Length > MaximumName)
                {
                    throw new ArgumentException(
                        $"The entry reference is required and at most {MaximumName} characters.");
                }

                if (string.IsNullOrEmpty(reason) || reason.Length > MaximumReason)
                {
                    throw new ArgumentException($"The reason is required and at most {MaximumReason} characters.");
                }

                var nameKey = SanctionExemptionSet.NameKey(name);

                if (nameKey.Length == 0)
                {
                    throw new ArgumentException("The screened name has nothing to match on.");
                }

                if (model.ExpiryDate.HasValue && model.ExpiryDate.Value.UtcDateTime <= DateTime.UtcNow)
                {
                    throw new ArgumentException("The expiry date must be in the future.");
                }

                if (!await dbContext.SanctionEntrySource.AnyAsync(w => w.Id == model.SanctionEntrySourceId, token)
                        .ConfigureAwait(false))
                {
                    throw new ArgumentException("The sanction list does not exist.");
                }

                var inserted = await repository.InsertAsync(new Jube.Data.Poco.SanctionExemption
                {
                    Guid = Guid.NewGuid(),
                    TenantRegistryId = tenantRegistryId,
                    ScreenedName = name,
                    NameKey = nameKey,
                    SanctionEntrySourceId = model.SanctionEntrySourceId,
                    SanctionEntryReference = reference,
                    Reason = reason,
                    ExpiryDate = model.ExpiryDate?.UtcDateTime,
                    StateId = SanctionExemptionRepository.Pending,
                    RequestedUser = userName,
                    RequestedDate = DateTime.UtcNow
                }, token).ConfigureAwait(false);

                if (inserted == null)
                {
                    throw new ConflictException("An exemption for this name and entry is already pending or approved.");
                }

                op.Entity(inserted.Id);
                op.Created();

                return ToDto(inserted);
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        public Task DecideAsync(SanctionExemptionDecisionDto? model, bool approve, CancellationToken token = default)
        {
            return DecideCoreAsync(model, approve, token);
        }

        private async Task DecideCoreAsync(SanctionExemptionDecisionDto? model, bool approve,
            CancellationToken token)
        {
            using var op = OperationScope.Start("SanctionExemption", approve ? "Approve" : "Reject", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                Ensure(decisionPermissions);

                if (model.Note is { Length: > MaximumReason })
                {
                    throw new ArgumentException($"The note must be at most {MaximumReason} characters.");
                }

                var existing = await repository.GetAsync(tenantRegistryId, model.Id, token).ConfigureAwait(false)
                               ?? throw new NotFoundException("The exemption was not found.");

                if (existing.StateId != SanctionExemptionRepository.Pending)
                {
                    throw new ConflictException("The exemption has already been decided.");
                }

                if (string.Equals(existing.RequestedUser, userName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ForbiddenException("Nobody can decide their own request.", decisionPermissions);
                }

                var decided = await repository.DecideAsync(tenantRegistryId, existing.Id,
                    SanctionExemptionRepository.Pending,
                    approve ? SanctionExemptionRepository.Approved : SanctionExemptionRepository.Rejected, userName,
                    model.Note?.Trim(), token).ConfigureAwait(false);

                if (decided == 0)
                {
                    throw new ConflictException("The exemption was decided meanwhile.");
                }

                op.Entity(existing.Id);
                op.Updated();
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        public async Task RetractAsync(int id, CancellationToken token = default)
        {
            using var op = OperationScope.Start("SanctionExemption", "Retract", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                Ensure(readPermissions);

                var existing = await repository.GetAsync(tenantRegistryId, id, token).ConfigureAwait(false)
                               ?? throw new NotFoundException("The exemption was not found.");

                var mayRetract = string.Equals(existing.RequestedUser, userName, StringComparison.OrdinalIgnoreCase)
                                 || permissionValidation.Holds(DecisionPermission);

                if (!mayRetract)
                {
                    throw new ForbiddenException("Only the requester or an approver can retract.", decisionPermissions);
                }

                var retracted = await repository.DecideAsync(tenantRegistryId, id,
                    SanctionExemptionRepository.Pending, SanctionExemptionRepository.Retracted, userName, null, token)
                    .ConfigureAwait(false);

                if (retracted == 0)
                {
                    retracted = await repository.DecideAsync(tenantRegistryId, id,
                        SanctionExemptionRepository.Approved, SanctionExemptionRepository.Retracted, userName, null,
                        token).ConfigureAwait(false);
                }

                if (retracted == 0)
                {
                    throw new ConflictException("The exemption is no longer pending or approved.");
                }

                op.Entity(id);
                op.Updated();
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }
    }
}
