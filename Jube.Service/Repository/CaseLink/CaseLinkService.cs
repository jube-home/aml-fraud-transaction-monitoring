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
using Jube.Data.Repository;
using Jube.Dto.Repository.CaseLink;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using LinqToDB;
using LinqToDB.Async;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Service.Repository.CaseLink
{
    public sealed class CaseLinkService
    {
        public const int CasePermission = 1;
        public const int MaximumReason = 500;
        private static readonly int[] permissions = [CasePermission];

        private readonly DbContext dbContext;
        private readonly string userName;
        private readonly int tenantRegistryId;
        private readonly PermissionValidation permissionValidation;
        private readonly ILog log;
        private readonly ILog auditLog;
        private readonly IServiceChangeBus serviceChangeBus;

        private CaseLinkService(DbContext dbContext, string userName, int tenantRegistryId,
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

        public static async Task<CaseLinkService> CreateAsync(DbContext dbContext, string? userName, ILog log,
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

            return new CaseLinkService(dbContext, userName, tenant.Value, validation, log,
                LogManager.GetLogger("Jube.Audit"), serviceChangeBus);
        }

        private void Ensure()
        {
            if (!permissionValidation.Validate(permissions))
            {
                throw new CaseExceptions.ForbiddenException("Permission denied.", permissions);
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

        private async Task WriteEventsAsync(int caseId, int otherCaseId, int caseEventTypeId, string? reason,
            CancellationToken token)
        {
            var cases = await dbContext.Case.Where(c => c.Id == caseId || c.Id == otherCaseId)
                .Select(c => new { c.Id, c.CaseKey, c.CaseKeyValue })
                .ToListAsync(token).ConfigureAwait(false);

            var events = new CaseEventRepository(dbContext, userName);

            foreach (var linked in cases)
            {
                var other = linked.Id == caseId ? otherCaseId : caseId;
                var detail = reason == null ? $"Case {other}" : $"Case {other}: {reason}";

                await events.InsertAsync(new Data.Poco.CaseEvent
                {
                    CaseId = linked.Id,
                    CaseKey = linked.CaseKey,
                    CaseKeyValue = linked.CaseKeyValue,
                    CaseEventTypeId = caseEventTypeId,
                    Before = caseEventTypeId == CaseEventRepository.CaseUnlinkedEventTypeId ? detail : null,
                    After = caseEventTypeId == CaseEventRepository.CaseLinkedEventTypeId ? detail : null
                }, token).ConfigureAwait(false);
            }
        }

        public async Task<List<CaseLinkDto>> ListAsync(int caseId, CancellationToken token = default)
        {
            Ensure();
            await EnsureVisibleAsync(caseId, token).ConfigureAwait(false);

            var links = await new CaseLinkRepository(dbContext).ListForCaseAsync(caseId, token)
                .ConfigureAwait(false);

            var otherIds = links.Select(l => l.CaseIdLow == caseId ? l.CaseIdHigh : l.CaseIdLow).Distinct().ToList();

            var visible = (await new CaseRepository(dbContext, userName)
                .GetVisibleIdsActiveOnlyAsync(otherIds, token).ConfigureAwait(false)).ToHashSet();

            var cases = await dbContext.Case.Where(c => visible.Contains(c.Id))
                .Select(c => new { c.Id, c.CaseKey, c.CaseKeyValue, c.ClosedStatusId })
                .ToListAsync(token).ConfigureAwait(false);

            return
            [
                .. from link in links
                let otherId = link.CaseIdLow == caseId ? link.CaseIdHigh : link.CaseIdLow
                join other in cases on otherId equals other.Id
                select new CaseLinkDto
                {
                    OtherCaseId = otherId,
                    CaseKey = other.CaseKey,
                    CaseKeyValue = other.CaseKeyValue,
                    ClosedStatusId = other.ClosedStatusId.GetValueOrDefault(),
                    Reason = link.Reason,
                    CreatedUser = link.CreatedUser,
                    CreatedDate = new DateTimeOffset(DateTime.SpecifyKind(link.CreatedDate, DateTimeKind.Utc))
                }
            ];
        }

        public async Task LinkAsync(CaseLinkRequestDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseLink", "Link", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                Ensure();
                Check(model);

                var reason = string.IsNullOrWhiteSpace(model.Reason) ? null : model.Reason.Trim();

                if (reason is { Length: > MaximumReason })
                {
                    throw new ArgumentException($"The reason must be at most {MaximumReason} characters.");
                }

                await EnsureVisibleAsync(model.CaseId, token).ConfigureAwait(false);
                await EnsureVisibleAsync(model.OtherCaseId, token).ConfigureAwait(false);

                if (!await new CaseLinkRepository(dbContext)
                        .InsertAsync(model.CaseId, model.OtherCaseId, reason, userName, token).ConfigureAwait(false))
                {
                    throw new CaseExceptions.ConflictException("The cases are already linked.");
                }

                await WriteEventsAsync(model.CaseId, model.OtherCaseId, CaseEventRepository.CaseLinkedEventTypeId,
                    reason, token).ConfigureAwait(false);

                op.Entity(model.CaseId);
                op.Created();
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        public async Task UnlinkAsync(CaseLinkRequestDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseLink", "Unlink", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                Ensure();
                Check(model);
                await EnsureVisibleAsync(model.CaseId, token).ConfigureAwait(false);
                await EnsureVisibleAsync(model.OtherCaseId, token).ConfigureAwait(false);

                if (await new CaseLinkRepository(dbContext).DeleteAsync(model.CaseId, model.OtherCaseId, token)
                        .ConfigureAwait(false) == 0)
                {
                    throw new CaseExceptions.NotFoundException("The link was not found.");
                }

                await WriteEventsAsync(model.CaseId, model.OtherCaseId, CaseEventRepository.CaseUnlinkedEventTypeId,
                    null, token).ConfigureAwait(false);

                op.Entity(model.CaseId);
                op.Updated();
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        private static void Check(CaseLinkRequestDto model)
        {
            if (model.CaseId <= 0 || model.OtherCaseId <= 0)
            {
                throw new ArgumentException("Both cases are required.");
            }

            if (model.CaseId == model.OtherCaseId)
            {
                throw new ArgumentException("A case cannot be linked to itself.");
            }
        }
    }
}
