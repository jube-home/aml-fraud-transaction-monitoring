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
using Jube.Dto.Query.CaseWorkflowDisplayExecution;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.CaseDispatch;
using Jube.Service.CaseDispatch.Models;
using Jube.Service.Charts.Datasource;
using Jube.Service.Charts.Datasource.Template;
using Jube.Service.Charts.Datasource.Template.Models;
using Jube.Service.Exceptions.Query.CaseWorkflowDisplayExecution;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using EngineJsonSerializationHelper = Jube.Engine.Helpers.JsonSerializationHelper;

namespace Jube.Service.Query.CaseWorkflowDisplayExecution
{
    public sealed class CaseWorkflowDisplayExecutionService
    {
        private const int MaximumEntries = 50;
        private static readonly int[] permissions = [1];
        private readonly ILog auditLog;
        private readonly CaseRepository caseRepository;
        private readonly CaseWorkflowDisplayRepository caseWorkflowDisplayRepository;
        private readonly CaseWorkflowFormRepository caseWorkflowFormRepository;
        private readonly DynamicEnvironment.DynamicEnvironment dynamicEnvironment;
        private readonly CaseSqlGuard sqlGuard;
        private readonly CaseWorkflowFormEntryRepository caseWorkflowFormEntryRepository;
        private readonly CaseWorkflowDispatchRepository caseWorkflowDispatchRepository;
        private readonly CaseWorkflowFormEntryValueRepository caseWorkflowFormEntryValueRepository;
        private readonly DatasourceChartText chartText;
        private readonly CaseWorkflowStatusRepository caseWorkflowStatusRepository;
        private readonly EngineJsonSerializationHelper jsonSerializationHelper;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaseWorkflowDisplayExecutionService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, IStringLocalizer chartStrings, IStringLocalizer sqlStrings,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            EngineJsonSerializationHelper jsonSerializationHelper)
        {
            this.dynamicEnvironment = dynamicEnvironment;
            sqlGuard = new CaseSqlGuard(sqlStrings, permissionValidation, userName);
            chartText = new DatasourceChartText(chartStrings);
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.jsonSerializationHelper = jsonSerializationHelper;
            caseRepository = new CaseRepository(dbContext, userName);
            caseWorkflowDisplayRepository = new CaseWorkflowDisplayRepository(dbContext, userName);
            caseWorkflowFormRepository = new CaseWorkflowFormRepository(dbContext, userName);
            caseWorkflowFormEntryRepository = new CaseWorkflowFormEntryRepository(dbContext, userName);
            caseWorkflowDispatchRepository = new CaseWorkflowDispatchRepository(dbContext, userName);
            caseWorkflowFormEntryValueRepository = new CaseWorkflowFormEntryValueRepository(dbContext, userName);
            caseWorkflowStatusRepository = new CaseWorkflowStatusRepository(dbContext, userName);
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseWorkflowDisplayExecutionService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            EngineJsonSerializationHelper jsonSerializationHelper, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                dynamicEnvironment, jsonSerializationHelper, LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseWorkflowDisplayExecutionService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            EngineJsonSerializationHelper jsonSerializationHelper, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseWorkflowDisplayExecutionResources));
            var chartStrings = stringLocalizerFactory.Create(typeof(DatasourceChartResources));
            var sqlStrings = stringLocalizerFactory.Create(typeof(CaseWorkflowSqlResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("CaseWorkflowDisplayExecution.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowDisplayExecutionResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"CaseWorkflowDisplayExecution.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowDisplayExecutionResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseWorkflowDisplayExecutionService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, chartStrings, sqlStrings, dynamicEnvironment,
                jsonSerializationHelper);
        }

        [Description("Renders a case workflow display: loads the case, the active display template and the case's " +
                     "workflow status (each restricted to the caller's tenant and roles), then returns the display " +
                     "as an escaped HTML fragment drawn from the template, with [@ Case.x @] and [@ Payload.x @] " +
                     "style tokens, v-if, v-for and filters evaluated against the case's stored payload. Read only.")]
        [ServiceOperation("CaseWorkflowDisplayExecutionExecute", OperationKind.Read, Idempotent = true)]
        public async Task<string> ExecuteAsync(
            [Description("The case and display to render.")]
            CaseWorkflowDisplayExecutionDto model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowDisplayExecution", "Execute", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"CaseWorkflowDisplayExecution.Execute: entry user={userName} caseId={model.CaseId} displayId={model.CaseWorkflowDisplayId}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowDisplayExecution.Execute");

                var (existingCase, caseWorkflowStatus, payload) = await LoadAsync(model.CaseId, token)
                    .ConfigureAwait(false);

                var caseWorkflowDisplay = await caseWorkflowDisplayRepository
                    .GetByIdActiveOnlyAsync(model.CaseWorkflowDisplayId, token).ConfigureAwait(false);

                if (caseWorkflowDisplay == null)
                {
                    throw NotFound("display", model.CaseWorkflowDisplayId);
                }

                var (roots, dialect, _) = await RootsAsync(caseWorkflowDisplay.Sql, existingCase,
                    caseWorkflowStatus, payload, token).ConfigureAwait(false);

                var rendered = Template(() => DatasourceTemplate.RenderForm(caseWorkflowDisplay.Html, roots,
                    new Dictionary<string, string?>(), true, dialect).Html);

                op.Rows(1);

                return rendered;
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
            catch (TemplateInvalidException)
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
                log.Error($"CaseWorkflowDisplayExecution.Execute: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Renders a case workflow form: loads the case and the active form template (each restricted " +
                     "to the caller's tenant and roles), then returns the form as an escaped HTML fragment with the " +
                     "values entered so far filled in, the names of the fields that are shown for those values, " +
                     "and every field the template declares. Read only.")]
        [ServiceOperation("CaseWorkflowDisplayExecutionRenderForm", OperationKind.Read, Idempotent = true)]
        public async Task<CaseWorkflowFormRenderResultDto> RenderFormAsync(
            [Description("The case, form and values entered so far.")]
            CaseWorkflowFormRenderDto model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowDisplayExecution", "RenderForm", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"CaseWorkflowDisplayExecution.RenderForm: entry user={userName} caseId={model.CaseId} formId={model.CaseWorkflowFormId}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowDisplayExecution.RenderForm");

                var (existingCase, caseWorkflowStatus, payload) = await LoadAsync(model.CaseId, token)
                    .ConfigureAwait(false);

                var caseWorkflowForm = await caseWorkflowFormRepository
                    .GetByIdActiveForCaseWorkflowGuidAsync(model.CaseWorkflowFormId, existingCase.CaseWorkflowGuid,
                        token).ConfigureAwait(false);

                if (caseWorkflowForm == null)
                {
                    throw NotFound("form", model.CaseWorkflowFormId);
                }

                var (roots, dialect, _) = await RootsAsync(caseWorkflowForm.Sql, existingCase, caseWorkflowStatus,
                    payload, token).ConfigureAwait(false);

                var result = Template(() =>
                {
                    var schema = DatasourceTemplate.CompileForm(caseWorkflowForm.Html, dialect);
                    var render = DatasourceTemplate.RenderForm(caseWorkflowForm.Html, roots,
                        model.Values ?? new Dictionary<string, string?>(), model.ReadOnly, dialect);

                    return new CaseWorkflowFormRenderResultDto
                    {
                        Html = render.Html,
                        VisibleFields = [.. render.VisibleFields],
                        Fields =
                        [
                            .. schema.Fields.Select(f => new CaseWorkflowFormFieldDto
                            {
                                Name = f.Name,
                                Kind = char.ToLowerInvariant(f.Kind.ToString()[0]) + f.Kind.ToString()[1..],
                                Options = [.. f.Options]
                            })
                        ]
                    };
                });

                op.Rows(1);

                return result;
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
            catch (TemplateInvalidException)
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
                log.Error($"CaseWorkflowDisplayExecution.RenderForm: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Draws the submitted form entries for a case's key and key value, newest first, each read " +
                     "only from its form's current template with the submitted values filled in. Entries are " +
                     "limited to the caller's tenant, workflow and status roles and the forms the caller's role " +
                     "holds; values the template has no field for come back beside it. Read only.")]
        [ServiceOperation("CaseWorkflowDisplayExecutionRenderFormEntries", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowFormEntryViewDto>> RenderFormEntriesAsync(
            [Description("The case whose key selects the entries.")]
            CaseWorkflowFormEntriesRenderDto model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowDisplayExecution", "RenderFormEntries", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("CaseWorkflowDisplayExecution.RenderFormEntries");

                var (current, _, _) = await LoadAsync(model.CaseId, token).ConfigureAwait(false);

                var entries = (await caseWorkflowFormEntryRepository
                        .GetByCaseKeyValueActiveOnlyAsync(current.CaseKey, current.CaseKeyValue, token)
                        .ConfigureAwait(false)).Take(MaximumEntries).ToList();

                var views = new List<CaseWorkflowFormEntryViewDto>();

                foreach (var entry in entries)
                {
                    var view = await ViewAsync(entry, token).ConfigureAwait(false);

                    if (view != null)
                    {
                        views.Add(view);
                    }
                }

                op.Rows(views.Count);

                return views;
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
                log.Error($"CaseWorkflowDisplayExecution.RenderFormEntries: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the audit of the emails, SMS and HTTP calls fired for a case's key and key value, newest " +
                     "first (200 at most): what each rendered, where it went, whether it was sent, failed or was " +
                     "refused, and the SQL rows it was drawn from. Limited to the caller's tenant and the workflow " +
                     "and status roles the caller holds. Read only.")]
        [ServiceOperation("CaseWorkflowDisplayExecutionListDispatches", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowDispatchDto>> ListDispatchesAsync(
            [Description("The case whose key selects the dispatches.")]
            CaseWorkflowDispatchesDto model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowDisplayExecution", "ListDispatches", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("CaseWorkflowDisplayExecution.ListDispatches");

                var (current, _, _) = await LoadAsync(model.CaseId, token).ConfigureAwait(false);

                var dispatches = (await caseWorkflowDispatchRepository
                    .GetByCaseKeyValueAsync(current.CaseKey, current.CaseKeyValue, 200, token)
                    .ConfigureAwait(false)).Select(d => new CaseWorkflowDispatchDto
                {
                    Id = d.Id,
                    CaseId = d.CaseId.GetValueOrDefault(),
                    SourceKindId = d.SourceKindId.GetValueOrDefault(),
                    SourceName = d.SourceName ?? string.Empty,
                    ChannelId = d.ChannelId.GetValueOrDefault(),
                    ModeId = d.ModeId.GetValueOrDefault(),
                    Destination = d.Destination,
                    Subject = d.Subject,
                    Body = d.Body,
                    OutcomeId = d.OutcomeId.GetValueOrDefault(),
                    Detail = d.Detail,
                    ResponseStatus = d.ResponseStatus,
                    CorrelationId = d.CorrelationId,
                    Rows = d.Rows,
                    CreatedUser = d.CreatedUser ?? string.Empty,
                    CreatedDate = d.CreatedDate.HasValue
                        ? new DateTimeOffset(DateTime.SpecifyKind(d.CreatedDate.Value, DateTimeKind.Utc))
                        : null
                }).ToList();

                op.Rows(dispatches.Count);

                return dispatches;
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
                log.Error($"CaseWorkflowDisplayExecution.ListDispatches: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Tries the SQL of a case workflow Form, Display, Macro, Action or Status against a case without " +
                     "saving it: checks it as saving would, then runs it read only for the case, returning the " +
                     "columns and the first rows, or why it was refused. Needs the case workflow SQL authoring " +
                     "permission. Read only.")]
        [ServiceOperation("CaseWorkflowDisplayExecutionTestSql", OperationKind.Read, Idempotent = true)]
        public async Task<CaseWorkflowSqlTestResultDto> TestSqlAsync(
            [Description("The SQL to try and the case to run it for.")]
            CaseWorkflowSqlTestDto model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowDisplayExecution", "TestSql", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("CaseWorkflowDisplayExecution.TestSql");

                if (!permissionValidation.Validate(CaseSqlGuard.RequiredPermissions.ToArray()))
                {
                    throw new ForbiddenException(strings[CaseWorkflowDisplayExecutionResources.PermissionDenied],
                        CaseSqlGuard.RequiredPermissions.ToArray());
                }

                var (existingCase, _, _) = await LoadAsync(model.CaseId, token).ConfigureAwait(false);

                var checks = new FluentValidation.Results.ValidationResult();

                sqlGuard.Append(checks, model.Sql);

                if (string.IsNullOrWhiteSpace(model.Sql) || checks.Errors.Count > 0)
                {
                    return new CaseWorkflowSqlTestResultDto
                    {
                        Error = checks.Errors.Count > 0
                            ? string.Join(" ", checks.Errors.Select(e => e.ErrorMessage))
                            : string.Empty
                    };
                }

                try
                {
                    var rows = await new CaseSqlRunner(
                        dynamicEnvironment.AppSettings("ReportConnectionString") ??
                        dynamicEnvironment.AppSettings("ConnectionString"), userName, log).RunAsync(model.Sql,
                        existingCase.Id, existingCase.CaseKeyValue, token).ConfigureAwait(false);

                    op.Rows(rows.Rows.Count);

                    return new CaseWorkflowSqlTestResultDto
                    {
                        Ok = true,
                        Columns = [.. rows.Columns.Select(c => new CaseWorkflowSqlColumnDto
                        {
                            Name = c.Name, DataType = c.DataType
                        })],
                        Rows = [.. rows.Rows.Take(20).Select(r => r.ToDictionary(p => p.Key, p => p.Value))],
                        RowCount = rows.Rows.Count,
                        Truncated = rows.Truncated,
                        Milliseconds = rows.Milliseconds
                    };
                }
                catch (CaseSqlException e)
                {
                    return new CaseWorkflowSqlTestResultDto { Error = e.Message };
                }
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
                log.Error($"CaseWorkflowDisplayExecution.TestSql: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private async Task<CaseWorkflowFormEntryViewDto?> ViewAsync(Data.Poco.CaseWorkflowFormEntry entry,
            CancellationToken token)
        {
            var caseId = entry.CaseId.GetValueOrDefault();
            Data.Poco.Case entryCase;
            Data.Poco.CaseWorkflowStatus status;
            EntityAnalysisModelInstanceEntryPayload payload;

            try
            {
                (entryCase, status, payload) = await LoadAsync(caseId, token).ConfigureAwait(false);
            }
            catch (NotFoundException)
            {
                return null;
            }

            var form = await caseWorkflowFormRepository.GetByIdActiveForCaseWorkflowGuidAsync(
                entry.CaseWorkflowFormId.GetValueOrDefault(), entryCase.CaseWorkflowGuid, token).ConfigureAwait(false);

            if (form == null)
            {
                return null;
            }

            var stored = (await caseWorkflowFormEntryValueRepository
                    .GetByCaseWorkflowFormEntryIdActiveOnlyAsync(entry.Id, token).ConfigureAwait(false))
                .Where(v => v.Name != null)
                .GroupBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => (string?)g.First().Value, StringComparer.OrdinalIgnoreCase);

            var view = new CaseWorkflowFormEntryViewDto
            {
                Id = entry.Id,
                CaseId = caseId,
                FormName = form.Name ?? string.Empty,
                CreatedUser = entry.CreatedUser ?? string.Empty,
                CreatedDate = entry.CreatedDate.HasValue
                    ? new DateTimeOffset(DateTime.SpecifyKind(entry.CreatedDate.Value, DateTimeKind.Utc))
                    : null
            };

            var template = string.IsNullOrWhiteSpace(entry.Template) ? form.Html : entry.Template;
            var snapshot = !string.IsNullOrWhiteSpace(entry.Sql);
            var dialect = snapshot ? TemplateDialect.FormSql : TemplateDialect.Form;

            try
            {
                var schema = DatasourceTemplate.CompileForm(template, dialect);

                view.Html = DatasourceTemplate.RenderForm(template,
                    snapshot
                        ? CaseRowsContext.Roots(CaseRowsContext.FromJson(entry.Rows))
                        : CaseWorkflowTemplateRoots.Build(entryCase, status, payload), stored, true, dialect).Html;

                var drawn = new HashSet<string>(schema.Fields.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);

                view.OtherValues = [.. stored.Where(v => !drawn.Contains(v.Key))
                    .Select(v => new CaseWorkflowFormEntryValueDto { Name = v.Key, Value = v.Value })];
            }
            catch (DatasourceChartException ex)
            {
                view.Error = chartText.Describe(ex);
                view.OtherValues = [.. stored.Select(v => new CaseWorkflowFormEntryValueDto
                {
                    Name = v.Key, Value = v.Value
                })];
            }

            return view;
        }

        private async Task<(Data.Poco.Case Case, Data.Poco.CaseWorkflowStatus Status,
            EntityAnalysisModelInstanceEntryPayload Payload)> LoadAsync(int caseId, CancellationToken token)
        {
            var existingCase = await caseRepository.GetByIdActiveOnlyAsync(caseId, token).ConfigureAwait(false);

            if (existingCase == null)
            {
                throw NotFound("case", caseId);
            }

            var caseWorkflowStatus = await caseWorkflowStatusRepository
                .GetByGuidAsync(existingCase.CaseWorkflowStatusGuid, token).ConfigureAwait(false);

            if (caseWorkflowStatus == null)
            {
                throw NotFound("status", caseId);
            }

            EntityAnalysisModelInstanceEntryPayload? payload;

            try
            {
                payload = JsonConvert.DeserializeObject<EntityAnalysisModelInstanceEntryPayload>(
                    existingCase.Json, jsonSerializationHelper.DefaultJsonSerializerSettingsSettings);
            }
            catch (Exception e) when (e is JsonException or InvalidCastException or NullReferenceException
                                          or FormatException)
            {
                payload = null;
            }

            return (existingCase, caseWorkflowStatus, payload ?? new EntityAnalysisModelInstanceEntryPayload());
        }

        private async Task<(IReadOnlyDictionary<string, object?> Roots, TemplateDialect Dialect, CaseRows Rows)>
            RootsAsync(string? sql, Data.Poco.Case existingCase, Data.Poco.CaseWorkflowStatus status,
                EntityAnalysisModelInstanceEntryPayload payload, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return (CaseWorkflowTemplateRoots.Build(existingCase, status, payload), TemplateDialect.Form,
                    CaseRows.None);
            }

            try
            {
                var rows = await new CaseSqlRunner(
                    dynamicEnvironment.AppSettings("ReportConnectionString") ??
                    dynamicEnvironment.AppSettings("ConnectionString"), userName, log).RunAsync(sql, existingCase.Id,
                    existingCase.CaseKeyValue, token).ConfigureAwait(false);

                return (CaseRowsContext.Roots(rows), TemplateDialect.FormSql, rows);
            }
            catch (CaseSqlException e)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseWorkflowDisplayExecution: the SQL failed user={userName}: {e.Message}");
                }

                throw new TemplateInvalidException(string.Format(strings[CaseWorkflowDisplayExecutionResources.SqlFailed],
                    e.Message));
            }
        }

        private T Template<T>(Func<T> render)
        {
            try
            {
                return render();
            }
            catch (DatasourceChartException ex)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseWorkflowDisplayExecution: template invalid user={userName}: {ex.Message}");
                }

                throw new TemplateInvalidException(chartText.Describe(ex));
            }
        }

        private NotFoundException NotFound(string what, int id)
        {
            if (log.IsWarnEnabled)
            {
                log.Warn($"CaseWorkflowDisplayExecution.Execute: {what} {id} not found or not visible user={userName}");
            }

            return new NotFoundException(strings[CaseWorkflowDisplayExecutionResources.NotFound]);
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

            throw new ForbiddenException(strings[CaseWorkflowDisplayExecutionResources.PermissionDenied], permissions);
        }
    }
}