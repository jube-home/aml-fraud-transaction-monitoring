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
using Jube.Dto.Repository.VisualisationRegistryDatasourceSeries;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.VisualisationRegistryDatasourceSeries;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;
using RuleRepository = Jube.Data.Repository.VisualisationRegistryDatasourceSeriesRepository;

namespace Jube.Service.Repository.VisualisationRegistryDatasourceSeries
{
    public sealed class VisualisationRegistryDatasourceSeriesService
    {
        private static readonly int[] permissions = [28, 1];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private VisualisationRegistryDatasourceSeriesService(DbContext dbContext, string userName,
            int tenantRegistryId, PermissionValidation permissionValidation, ILog log, ILog auditLog,
            IServiceChangeBus serviceChangeBus, IStringLocalizer strings)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            repository = new RuleRepository(dbContext, userName);
        }

        public static Task<VisualisationRegistryDatasourceSeriesService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<VisualisationRegistryDatasourceSeriesService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(VisualisationRegistryDatasourceSeriesResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("VisualisationRegistryDatasourceSeries.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[VisualisationRegistryDatasourceSeriesResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"VisualisationRegistryDatasourceSeries.Create: user '{userName}' resolves to no tenant; " +
                        "refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[VisualisationRegistryDatasourceSeriesResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new VisualisationRegistryDatasourceSeriesService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the series definitions of a Visualisation Registry Datasource, visible to the " +
                     "caller's tenant. Each series is one column of the Datasource's SQL command result, with a " +
                     "Name and inferred DataTypeId, derived automatically in the background when the Datasource " +
                     "is created or updated -- there is no way to create, edit or delete a series directly. The " +
                     "browser uses this list to build the field schema for a Kendo Chart or Grid before executing " +
                     "the Datasource.")]
        [ServiceOperation("VisualisationRegistryDatasourceSeriesListByVisualisationRegistryDatasourceId",
            OperationKind.Read, Idempotent = true)]
        public async Task<List<VisualisationRegistryDatasourceSeriesDto>> GetByVisualisationRegistryDatasourceIdAsync(
            [Description("Server-assigned identifier of the Visualisation Registry Datasource to list series " +
                         "definitions for.")]
            int id, CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryDatasourceSeries",
                "ListByVisualisationRegistryDatasourceId", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug("VisualisationRegistryDatasourceSeries.ListByVisualisationRegistryDatasourceId: entry " +
                          $"id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("VisualisationRegistryDatasourceSeries.ListByVisualisationRegistryDatasourceId");
                var dtos = VisualisationRegistryDatasourceSeriesMapper.ToDto(
                    await repository.GetByVisualisationRegistryDatasourceIdAsync(id, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug("VisualisationRegistryDatasourceSeries.ListByVisualisationRegistryDatasourceId: " +
                              $"{dtos.Count} rows id={id} user={userName}");
                }

                return dtos;
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
                log.Error("VisualisationRegistryDatasourceSeries.ListByVisualisationRegistryDatasourceId: " +
                          $"unexpected failure id={id} user={userName}", ex);
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

            throw new ForbiddenException(strings[VisualisationRegistryDatasourceSeriesResources.PermissionDenied],
                permissions);
        }
    }
}