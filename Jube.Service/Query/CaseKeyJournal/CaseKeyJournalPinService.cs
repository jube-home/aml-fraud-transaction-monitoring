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

using System.ComponentModel;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Query.CaseKeyJournal;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Query.CaseKeyJournal;
using Jube.Service.Observability;
using Jube.Service.Query.CaseSearch;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using SearchNotFound = Jube.Service.Exceptions.Query.CaseSearch.NotFoundException;

namespace Jube.Service.Query.CaseKeyJournal
{
    public sealed class CaseKeyJournalPinService
    {
        public const int MaximumPins = 50;

        private static readonly int[] permissions = [1];
        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly CaseSearchService search;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaseKeyJournalPinService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, CaseSearchService search)
        {
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.search = search;
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseKeyJournalPinService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseKeyJournalPinService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseKeyJournalResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new NotAuthenticatedException(strings[CaseKeyJournalResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                throw new NotAuthenticatedException(strings[CaseKeyJournalResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            var search = await CaseSearchService.CreateAsync(dbContext, userName, log, stringLocalizerFactory,
                serviceChangeBus, dynamicEnvironment, auditLog, token).ConfigureAwait(false);

            return new CaseKeyJournalPinService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, search);
        }

        [Description("Lists the transactions pinned in the journal of a case. A pinned transaction stays at the top " +
                     "of the journal in every lens and window and is shared by everyone working the case. " +
                     "Read-only.")]
        [ServiceOperation("CaseKeyJournalPinList", OperationKind.Read, Idempotent = true)]
        public async Task<CaseKeyJournalPinsDto> ListAsync(
            [Description("Id of the case.")] int caseId, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "PinList", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                EnsurePermitted();

                await ResolveAsync(caseId, token).ConfigureAwait(false);

                var pinned = await new CaseKeyJournalPinRepository(dbContext, userName)
                    .GetPinnedAsync(caseId, token).ConfigureAwait(false);

                op.Rows(pinned.Count);

                return new CaseKeyJournalPinsDto { Transactions = [.. pinned] };
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseKeyJournal.PinList: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Pins or unpins up to 50 journal transactions on a case, so that they stay at the top of the " +
                     "journal in every lens and window for everyone working the case. Only transactions scored by " +
                     "the case workflow's model can be pinned, a case can hold 50 pins, and a case locked to " +
                     "another user cannot be changed. SIDE EFFECTS: stores the pin against the case and records who " +
                     "made the change. Returns the transactions now pinned.")]
        [ServiceOperation("CaseKeyJournalPinSet", OperationKind.Write, Idempotent = true)]
        public async Task<CaseKeyJournalPinsDto> SetAsync(CaseKeyJournalPinRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseKeyJournal", "PinSet", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();

                var distinct = request.Transactions.Distinct().ToList();

                if (distinct.Count is < 1 or > MaximumPins || distinct.Count != request.Transactions.Length)
                {
                    throw Invalid(nameof(request.Transactions), CaseKeyJournalResources.PinTransactionsInvalid,
                        "PinTransactionsInvalid");
                }

                var (existing, modelId) = await ResolveAsync(request.CaseId, token).ConfigureAwait(false);

                if (existing.Locked == 1 && !string.IsNullOrEmpty(existing.LockedUser)
                                         && !string.Equals(existing.LockedUser, userName, StringComparison.Ordinal))
                {
                    throw new ConflictException(strings[CaseKeyJournalResources.CaseLocked]);
                }

                var repository = new CaseKeyJournalPinRepository(dbContext, userName);

                if (request.Pinned)
                {
                    var requested = distinct;
                    var known = await dbContext.Archive
                        .Where(a => a.EntityAnalysisModelId == modelId
                                    && requested.Contains(a.EntityAnalysisModelInstanceEntryGuid))
                        .Select(a => a.EntityAnalysisModelInstanceEntryGuid).ToListAsync(token)
                        .ConfigureAwait(false);

                    distinct = distinct.Where(known.Contains).ToList();

                    var current = await repository.GetPinnedAsync(request.CaseId, token).ConfigureAwait(false);

                    if (current.Union(distinct).Count() > MaximumPins)
                    {
                        throw Invalid(nameof(request.Transactions), CaseKeyJournalResources.PinLimitInvalid,
                            "PinLimitInvalid");
                    }
                }

                await repository.SetAsync(request.CaseId, distinct, request.Pinned, token).ConfigureAwait(false);

                var pinned = await repository.GetPinnedAsync(request.CaseId, token).ConfigureAwait(false);

                op.Rows(distinct.Count);

                return new CaseKeyJournalPinsDto { Transactions = [.. pinned] };
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseKeyJournal.PinSet: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private async Task<(Data.Poco.Case Case, int ModelId)> ResolveAsync(int caseId, CancellationToken token)
        {
            var existing = await dbContext.Case.FirstOrDefaultAsync(c => c.Id == caseId, token)
                .ConfigureAwait(false) ?? throw new NotFoundException(strings[CaseKeyJournalResources.CaseNotFound]);

            try
            {
                await search.EverythingAsync(existing.CaseWorkflowGuid, token).ConfigureAwait(false);
            }
            catch (SearchNotFound)
            {
                throw new NotFoundException(strings[CaseKeyJournalResources.CaseNotFound]);
            }

            var modelId = await dbContext.CaseWorkflow.Where(w => w.Guid == existing.CaseWorkflowGuid && (w.Deleted == null || w.Deleted == 0))
                .Select(w => w.EntityAnalysisModelId).FirstOrDefaultAsync(token).ConfigureAwait(false);

            return (existing, modelId ?? throw new NotFoundException(strings[CaseKeyJournalResources.CaseNotFound]));
        }

        private DtoValidationException Invalid(string property, string resource, string code)
        {
            return new DtoValidationException(new FluentValidation.Results.ValidationResult(
                [new FluentValidation.Results.ValidationFailure(property, strings[resource]) { ErrorCode = code }]));
        }

        private void EnsurePermitted()
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            throw new ForbiddenException(strings[CaseKeyJournalResources.PermissionDenied], permissions);
        }
    }
}
