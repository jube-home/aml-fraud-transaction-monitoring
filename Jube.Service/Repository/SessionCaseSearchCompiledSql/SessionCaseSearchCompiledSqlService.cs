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
using Jube.Data.Context;
using Jube.Data.Query.DynamicResultsSchema;
using Jube.Data.Reporting;
using Jube.Data.Repository;
using Jube.Dto.Repository.SessionCaseSearchCompiledSql;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.SessionCaseSearchCompiledSql;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.SessionCaseSearchCompiledSql;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;

namespace Jube.Service.Repository.SessionCaseSearchCompiledSql
{
    public sealed class SessionCaseSearchCompiledSqlService
    {
        private static readonly int[] permissions = [1];

        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly SessionCaseSearchCompiledSqlExecutionRepository executionRepository;
        private readonly ILog log;
        private readonly bool parserAssertSelectOnly;
        private readonly PermissionValidation permissionValidation;
        private readonly string reportConnectionString;
        private readonly SessionCaseSearchCompiledSqlRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly SessionCaseSearchCompiledSqlDtoValidator validator;

        private SessionCaseSearchCompiledSqlService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new SessionCaseSearchCompiledSqlRepository(dbContext, userName);
            executionRepository = new SessionCaseSearchCompiledSqlExecutionRepository(dbContext, userName);
            validator = new SessionCaseSearchCompiledSqlDtoValidator(strings);

            var assertSelectOnlySetting = dynamicEnvironment.AppSettings("ParserAssertSelectOnly");
            parserAssertSelectOnly = assertSelectOnlySetting == null ||
                                     assertSelectOnlySetting.Equals("True", StringComparison.OrdinalIgnoreCase);
            reportConnectionString = dynamicEnvironment.AppSettings("ReportConnectionString")
                                     ?? dbContext.Connection.ConnectionString;
        }

        public static Task<SessionCaseSearchCompiledSqlService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                dynamicEnvironment, LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<SessionCaseSearchCompiledSqlService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(SessionCaseSearchCompiledSqlResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("SessionCaseSearchCompiledSql.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[SessionCaseSearchCompiledSqlResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"SessionCaseSearchCompiledSql.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[SessionCaseSearchCompiledSqlResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new SessionCaseSearchCompiledSqlService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, dynamicEnvironment);
        }

        [Description("Executes the caller's own compiled Session Case Search, identified by its Guid, and returns " +
                     "up to 100 result rows with a column schema. SIDE EFFECTS: runs the compiled SQL against the " +
                     "reporting database and records an execution row (record count and response time) against " +
                     "the compiled search; a stale search is recompiled first. Fails as not found when the Guid is " +
                     "unknown or belongs to another user.")]
        public async Task<DynamicResultSchemaDto> ExecuteByGuidAsync(
            [Description("Guid of the compiled Session Case Search.")]
            Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("SessionCaseSearchCompiledSql", "ExecuteByGuid", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"SessionCaseSearchCompiledSql.ExecuteByGuid: entry user={userName} guid={guid}");
            }

            try
            {
                EnsurePermitted("SessionCaseSearchCompiledSql.ExecuteByGuid");
                token.ThrowIfCancellationRequested();

                var model = repository.GetByGuid(guid);
                if (model == null)
                {
                    throw new NotFoundException(strings[SessionCaseSearchCompiledSqlResources.NotFound]);
                }

                model = await CheckRebuildAsync(model, token).ConfigureAwait(false);

                using var postgres = new Postgres(reportConnectionString, log, parserAssertSelectOnly);

                var tokens = JsonConvert.DeserializeObject<List<object>>(model.FilterTokens ?? "[]");

                var sw = Stopwatch.StartNew();

                var value = await postgres.ExecuteByOrderedParametersAsync(model.SelectSqlSearch
                                                                           + " "
                                                                           + model.WhereSql
                                                                           + " " + model.OrderSql + " limit 100",
                    tokens ?? new List<object>(), token).ConfigureAwait(false);

                sw.Stop();

                await executionRepository.InsertAsync(new Data.Poco.SessionCaseSearchCompiledSqlExecution
                {
                    SessionCaseSearchCompiledSqlId = model.Id,
                    Records = value.Count,
                    ResponseTime = (int)sw.Elapsed.TotalMilliseconds
                }, token).ConfigureAwait(false);

                op.Rows(value.Count);
                return DynamicResultSchema.Build(value);
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("not_found");
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
                log.Error($"SessionCaseSearchCompiledSql.ExecuteByGuid: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns the caller's most recently compiled Session Case Search, or a placeholder with " +
                     "NotFound=true when the caller has none yet. Only the caller's own searches are visible. " +
                     "If the compiled search is flagged for rebuild and awaiting its first rebuild it is recompiled " +
                     "(a new compiled row is stored) and that rebuilt search is returned.")]
        [ServiceOperation("SessionCaseSearchCompiledSqlGetLast", OperationKind.Read, Idempotent = true)]
        public async Task<SessionCaseSearchCompiledSqlDto> GetLastAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("SessionCaseSearchCompiledSql", "GetLast", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"SessionCaseSearchCompiledSql.GetLast: entry user={userName}");
            }

            try
            {
                EnsurePermitted("SessionCaseSearchCompiledSql.GetLast");
                token.ThrowIfCancellationRequested();

                var model = await repository.GetByLastAsync(token).ConfigureAwait(false);
                if (model == null)
                {
                    op.Rows(0);
                    return new SessionCaseSearchCompiledSqlDto { NotFound = true };
                }

                model = await CheckRebuildAsync(model, token).ConfigureAwait(false);

                op.Rows(1);
                return SessionCaseSearchCompiledSqlMapper.ToDto(model);
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
                log.Error($"SessionCaseSearchCompiledSql.GetLast: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Compiles a Session Case Search (query-builder select and filter JSON against a Case " +
                     "Workflow) into parameterised SQL, prepares it against the reporting database and stores it " +
                     "for the caller. Creates a new compiled row on every call.")]
        public async Task<SessionCaseSearchCompiledSqlDto> InsertAsync(SessionCaseSearchCompiledSqlDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("SessionCaseSearchCompiledSql", "Insert", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"SessionCaseSearchCompiledSql.Insert: entry user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("SessionCaseSearchCompiledSql.Insert");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"SessionCaseSearchCompiledSql.Insert: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                token.ThrowIfCancellationRequested();

                var saved = await SessionCaseSearchCompiler.CompileAsync(dbContext, model.CaseWorkflowGuid,
                    model.CaseWorkflowFilterGuid, model.SelectJson, model.FilterJson, userName, log,
                    parserAssertSelectOnly, reportConnectionString, token).ConfigureAwait(false);
                op.Entity(saved.Id);

                return SessionCaseSearchCompiledSqlMapper.ToDto(saved);
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (DtoValidationException)
            {
                op.Outcome("invalid");
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
                log.Error($"SessionCaseSearchCompiledSql.Insert: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private async Task<Data.Poco.SessionCaseSearchCompiledSql> CheckRebuildAsync(
            Data.Poco.SessionCaseSearchCompiledSql model, CancellationToken token)
        {
            if (model.Rebuild != 1 || (model.RebuildDate.HasValue && model.RebuildDate.Value != default))
            {
                return model;
            }

            var rebuilt = await SessionCaseSearchCompiler.CompileAsync(dbContext, model.CaseWorkflowGuid,
                model.CaseWorkflowFilterGuid, model.SelectJson, model.FilterJson, userName, log,
                parserAssertSelectOnly, reportConnectionString, token, false).ConfigureAwait(false);

            model.FilterTokens = rebuilt.FilterTokens;
            model.FilterSql = rebuilt.FilterSql;
            model.SelectSqlSearch = rebuilt.SelectSqlSearch;
            model.SelectSqlDisplay = rebuilt.SelectSqlDisplay;
            model.WhereSql = rebuilt.WhereSql;
            model.OrderSql = rebuilt.OrderSql;
            model.Prepared = rebuilt.Prepared;
            model.Error = rebuilt.Error;

            if (rebuilt.Prepared != 1)
            {
                return model;
            }

            model.Rebuild = 0;
            model.RebuildDate = DateTime.UtcNow;

            await repository.UpdateRebuiltAsync(model, token).ConfigureAwait(false);

            return model;
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

            throw new ForbiddenException(strings[SessionCaseSearchCompiledSqlResources.PermissionDenied],
                permissions);
        }
    }
}