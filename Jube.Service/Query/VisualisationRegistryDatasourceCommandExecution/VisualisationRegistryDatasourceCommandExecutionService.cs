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
using System.Diagnostics;
using System.Globalization;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.VisualisationRegistryDatasourceCommandExecution;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json.Linq;

namespace Jube.Service.Query.VisualisationRegistryDatasourceCommandExecution
{
    public sealed class VisualisationRegistryDatasourceCommandExecutionService
    {
        private static readonly int[] permissions = [28, 1];

        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly DynamicEnvironment.DynamicEnvironment dynamicEnvironment;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly global::Jube.Data.Query.GetByVisualisationRegistryDatasourceCommandExecutionQuery query;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private VisualisationRegistryDatasourceCommandExecutionService(DbContext dbContext, string userName,
            int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.dynamicEnvironment = dynamicEnvironment;
            query = new global::Jube.Data.Query.GetByVisualisationRegistryDatasourceCommandExecutionQuery(dbContext,
                userName, log,
                dynamicEnvironment.ParserAssertSelectOnly(),
                dynamicEnvironment.AppSettings("ReportConnectionString") ??
                dynamicEnvironment.AppSettings("ConnectionString"));
        }

        public static Task<VisualisationRegistryDatasourceCommandExecutionService> CreateAsync(DbContext dbContext,
            string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                dynamicEnvironment, LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<VisualisationRegistryDatasourceCommandExecutionService> CreateAsync(
            DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog auditLog,
            CancellationToken token = default)
        {
            var strings =
                stringLocalizerFactory.Create(typeof(VisualisationRegistryDatasourceCommandExecutionResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        "VisualisationRegistryDatasourceCommandExecution.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[VisualisationRegistryDatasourceCommandExecutionResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"VisualisationRegistryDatasourceCommandExecution.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[VisualisationRegistryDatasourceCommandExecutionResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new VisualisationRegistryDatasourceCommandExecutionService(dbContext, userName,
                resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, dynamicEnvironment);
        }

        [Description("Executes the SQL command of a Visualisation Registry Datasource the caller may see and " +
                     "returns its rows as dictionaries of column name to value; the columns are defined by the " +
                     "command and are not fixed. Each parameter is {\"id\": <Visualisation Registry Parameter id>, " +
                     "\"value\": <value>}; a missing value falls back to the parameter's default. Every " +
                     "execution, successful or failed, is recorded in the datasource execution log together with " +
                     "the supplied parameter values. A command that throws is logged and returns no rows. A datasource the " +
                     "caller cannot see returns no rows and is not logged.")]
        [ServiceOperation("VisualisationRegistryDatasourceCommandExecutionExecute", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<IDictionary<string, object>>> ExecuteAsync(
            [Description("Id of the Visualisation Registry Datasource to execute.")]
            int id,
            [Description("JSON array of {id, value} parameter objects.")]
            JArray parameters,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("VisualisationRegistryDatasourceCommandExecution", "Execute", userName,
                tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"VisualisationRegistryDatasourceCommandExecution.Execute: entry user={userName} id={id}");
            }

            try
            {
                EnsurePermitted("VisualisationRegistryDatasourceCommandExecution.Execute");
                ArgumentNullException.ThrowIfNull(parameters);

                var visibleDatasource = await new VisualisationRegistryDatasourceRepository(dbContext, userName)
                    .GetByIdActiveOnlyAsync(id, token).ConfigureAwait(false);
                if (visibleDatasource == null)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"VisualisationRegistryDatasourceCommandExecution.Execute: datasource {id} not visible; returning empty without logging user={userName}");
                    }

                    op.Rows(0);
                    return [];
                }

                var parametersByParsedId = parameters.ToDictionary(
                    parameter => (int)(parameter.SelectToken("id") ?? throw new ArgumentException("A parameter has no id.")),
                    parameter => parameter.SelectToken("value"));
                var parametersByName = await ParametersByNameAsync(id, parametersByParsedId, token)
                    .ConfigureAwait(false);

                string? error = null;
                var values = new List<IDictionary<string, object>>();
                var sw = new Stopwatch();
                try
                {
                    sw.Start();
                    values = await query.ExecuteAsync(id, parametersByName, token).ConfigureAwait(false);
                    sw.Stop();
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    error = ex.ToString();
                }

                await StoreAuditAsync(id, values, error, sw, parametersByParsedId, token).ConfigureAwait(false);

                op.Rows(values.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"VisualisationRegistryDatasourceCommandExecution.Execute: {values.Count} rows error={error != null} user={userName}");
                }

                return values;
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
                log.Error(
                    $"VisualisationRegistryDatasourceCommandExecution.Execute: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private async Task StoreAuditAsync(int id, List<IDictionary<string, object>> values, string? error,
            Stopwatch sw, Dictionary<int, JToken?> parametersByParsedId, CancellationToken token)
        {
            var executionLog = await new VisualisationRegistryDatasourceExecutionLogRepository(dbContext)
                .InsertAsync(new VisualisationRegistryDatasourceExecutionLog
                {
                    Records = values.Count,
                    Error = error?.Replace("\0", string.Empty),
                    ResponseTime = (int)sw.ElapsedMilliseconds,
                    VisualisationRegistryDatasourceId = id,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = userName
                }, token).ConfigureAwait(false);

            var parameterRepository = new VisualisationRegistryDatasourceExecutionLogParameterRepository(dbContext);
            var knownParameterIds = (await new VisualisationRegistryParameterRepository(dbContext, userName)
                    .GetByVisualisationRegistryDatasourceIdAsync(id, token).ConfigureAwait(false))
                .Select(p => p.Id).ToHashSet();
            foreach (var parameter in parametersByParsedId.Where(p => knownParameterIds.Contains(p.Key)))
            {
                await parameterRepository.InsertAsync(new VisualisationRegistryDatasourceExecutionLogParameter
                {
                    Value = parameter.Value?.ToString().Replace("\0", string.Empty),
                    VisualisationRegistryDatasourceExecutionLogId = executionLog.Id,
                    VisualisationRegistryParameterId = parameter.Key
                }, token).ConfigureAwait(false);
            }
        }

        private async Task<Dictionary<string, object>> ParametersByNameAsync(int id,
            Dictionary<int, JToken?> parametersByParsedId, CancellationToken token)
        {
            var registryParameters = await new VisualisationRegistryParameterRepository(dbContext, userName)
                .GetByVisualisationRegistryDatasourceIdAsync(id, token).ConfigureAwait(false);

            var parametersByName = new Dictionary<string, object>();
            foreach (var parameter in parametersByParsedId)
            {
                var registryParameter = registryParameters.FirstOrDefault(f => f.Id == parameter.Key);
                if (registryParameter == null)
                {
                    continue;
                }

                var cleanName = registryParameter.Name.Replace(" ", "_");
                var value = parameter.Value;

                switch (registryParameter.DataTypeId)
                {
                    case 2:
                        parametersByName.Add(cleanName, value == null
                            ? int.Parse(registryParameter.DefaultValue, CultureInfo.InvariantCulture)
                            : int.Parse(value.ToString(), CultureInfo.InvariantCulture));
                        break;
                    case 3:
                        parametersByName.Add(cleanName, value == null
                            ? double.Parse(registryParameter.DefaultValue, CultureInfo.InvariantCulture)
                            : double.Parse(value.ToString(), CultureInfo.InvariantCulture));
                        break;
                    case 4:
                        parametersByName.Add(cleanName, value == null
                            ? DateTime.UtcNow.AddDays(int.Parse(registryParameter.DefaultValue,
                                CultureInfo.InvariantCulture) * -1)
                            : DateTimeOffset.TryParse(value.ToString(), CultureInfo.InvariantCulture,
                                dynamicEnvironment.AppSettings("AssumeLocalDateInPayloadExtraction")
                                    .Equals("True", StringComparison.CurrentCultureIgnoreCase)
                                    ? DateTimeStyles.AssumeLocal
                                    : DateTimeStyles.AssumeUniversal,
                                out var dto)
                                ? dto.UtcDateTime
                                : DateTime.UtcNow);
                        break;
                    case 5:
                        parametersByName.Add(cleanName, value == null
                            ? byte.Parse(registryParameter.DefaultValue, CultureInfo.InvariantCulture)
                            : byte.Parse(value.ToString(), CultureInfo.InvariantCulture));
                        break;
                    default:
                        parametersByName.Add(cleanName,
                            value == null ? registryParameter.DefaultValue : value.ToString());
                        break;
                }
            }

            return parametersByName;
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

            throw new ForbiddenException(
                strings[VisualisationRegistryDatasourceCommandExecutionResources.PermissionDenied], permissions);
        }
    }
}