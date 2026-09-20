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
using Jube.Dto.Query.CaseJournal;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.CaseJournal;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.CaseJournal
{
    public sealed class CaseJournalService
    {
        private static readonly int[] permissions = [1];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly global::Jube.Data.Query.GetCaseJournalQuery query;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaseJournalService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, bool parserAssertSelectOnly, string? reportConnectionString)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            query = new global::Jube.Data.Query.GetCaseJournalQuery(dbContext, userName, log, parserAssertSelectOnly,
                reportConnectionString ?? dbContext.Connection.ConnectionString);
        }

        public static Task<CaseJournalService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            bool parserAssertSelectOnly, string? reportConnectionString, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), parserAssertSelectOnly, reportConnectionString, token);
        }

        internal static async Task<CaseJournalService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, bool parserAssertSelectOnly,
            string? reportConnectionString, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseJournalResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("CaseJournal.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseJournalResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseJournal.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseJournalResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseJournalService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, parserAssertSelectOnly,
                reportConnectionString);
        }

        [Description("Returns the Case journal: the most recent Archive entries (newest first, up to 'limit') " +
                     "whose payload holds 'drillValue' at payload key 'drillName', restricted to entries in the " +
                     "caller's tenant, together with a column schema and one extra column per Case Workflow XPath " +
                     "the caller's roles may see for the given Case Workflow. Read-only.")]
        [ServiceOperation("CaseJournalGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseJournalDto> GetAsync(
            [Description("Payload key to match, for example the case key such as 'AccountId'.")]
            string? drillName,
            [Description("Value the payload key must equal.")]
            string? drillValue,
            [Description("Guid of the Case Workflow whose visible XPaths become extra columns.")]
            Guid caseWorkflowGuid,
            [Description("Maximum number of Archive rows to return.")]
            int limit,
            [Description("When true only entries that raised at least one activation are returned.")]
            bool activationsOnly,
            [Description("Minimum response elevation an entry must have.")]
            double responseElevation,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseJournal", "Get", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseJournal.Get: entry user={userName} caseWorkflowGuid={caseWorkflowGuid} limit={limit}");
            }

            try
            {
                EnsurePermitted("CaseJournal.Get");
                var dto = CaseJournalMapper.ToDto(await query.ExecuteAsync(drillName, drillValue, caseWorkflowGuid,
                    limit, activationsOnly ? 1 : 0, responseElevation, token).ConfigureAwait(false));
                op.Rows(dto.Rows?.Count ?? 0);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseJournal.Get: {dto.Rows?.Count ?? 0} rows user={userName}");
                }

                return dto;
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
                log.Error($"CaseJournal.Get: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", permissions)}]");
            }

            throw new ForbiddenException(strings[CaseJournalResources.PermissionDenied], permissions);
        }
    }
}