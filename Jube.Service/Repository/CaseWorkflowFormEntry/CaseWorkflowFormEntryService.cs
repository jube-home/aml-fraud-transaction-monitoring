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
using Jube.Dto.Repository.CaseWorkflowFormEntry;
using Jube.Dto.Validation;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.Extensions;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Agent.Models;
using Jube.Service.CaseDispatch;
using Jube.Service.CaseDispatch.Models;
using Jube.Service.Charts.Datasource;
using Jube.Service.Charts.Datasource.Template;
using Jube.Service.Charts.Datasource.Template.Models;
using Jube.Service.Query.CaseWorkflowDisplayExecution;
using Jube.Service.Repository.CaseWorkflowFormValidator;
using Jube.Service.Repository.CaseWorkflowFormValidator.Models;
using Jube.Service.Exceptions.Repository.CaseWorkflowFormEntry;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using FluentValidation.Results;
using Jube.Dto.Options;
using Jube.Validations.Repository.CaseWorkflowFormEntry;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EngineJsonSerializationHelper = Jube.Engine.Helpers.JsonSerializationHelper;
using RulePoco = Jube.Data.Poco.CaseWorkflowFormEntry;
using RuleRepository = Jube.Data.Repository.CaseWorkflowFormEntryRepository;

namespace Jube.Service.Repository.CaseWorkflowFormEntry
{
    using EntryPayload = EntityAnalysisModelInstanceEntryPayload;
    using EntryPayloadExtensions =
        EntityAnalysisModelInstanceEntryPayloadExtensions;

    public sealed class CaseWorkflowFormEntryService
    {
        private static readonly int[] permissions = [1];
        private readonly ILog auditLog;
        private readonly DatasourceChartText chartText;
        private readonly CaseDispatcher dispatcher;
        private readonly DbContext dbContext;
        private readonly CaseWorkflowFormValidatorRepository validatorRepository;
        private readonly CaseRepository caseRepository;
        private readonly CaseWorkflowFormRepository caseWorkflowFormRepository;
        private readonly CaseWorkflowStatusRepository caseWorkflowStatusRepository;
        private readonly DynamicEnvironment.DynamicEnvironment dynamicEnvironment;
        private readonly CaseWorkflowFormEntryValueRepository entryValueRepository;
        private readonly EngineJsonSerializationHelper jsonSerializationHelper;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly CaseWorkflowFormEntryDtoValidator validator;

        private CaseWorkflowFormEntryService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, IStringLocalizer chartStrings,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            EngineJsonSerializationHelper jsonSerializationHelper)
        {
            this.dbContext = dbContext;
            dispatcher = new CaseDispatcher(dbContext, userName, tenantRegistryId, dynamicEnvironment, log);
            chartText = new DatasourceChartText(chartStrings);
            validatorRepository = new CaseWorkflowFormValidatorRepository(dbContext, userName);
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.dynamicEnvironment = dynamicEnvironment;
            this.jsonSerializationHelper = jsonSerializationHelper;
            repository = new RuleRepository(dbContext, userName);
            entryValueRepository = new CaseWorkflowFormEntryValueRepository(dbContext, userName);
            caseRepository = new CaseRepository(dbContext, userName);
            caseWorkflowFormRepository = new CaseWorkflowFormRepository(dbContext, userName);
            caseWorkflowStatusRepository = new CaseWorkflowStatusRepository(dbContext, userName);
            validator = new CaseWorkflowFormEntryDtoValidator(strings);
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseWorkflowFormEntryService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            EngineJsonSerializationHelper jsonSerializationHelper, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment,
                jsonSerializationHelper, LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseWorkflowFormEntryService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            EngineJsonSerializationHelper jsonSerializationHelper, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseWorkflowFormEntryResources));
            var chartStrings = stringLocalizerFactory.Create(typeof(DatasourceChartResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("CaseWorkflowFormEntry.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowFormEntryResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseWorkflowFormEntry.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowFormEntryResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseWorkflowFormEntryService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, chartStrings, dynamicEnvironment,
                jsonSerializationHelper);
        }

        [Description("Submits a CaseWorkflowForm entry for a case -- persists one row per non-null payload field " +
                     "and dispatches the form's notification and/or HTTP endpoint, if either is enabled.")]
        [ServiceOperation("CaseWorkflowFormEntryInsert", OperationKind.Write, Idempotent = false)]
        public async Task<CaseWorkflowFormEntryDto> InsertAsync(CaseWorkflowFormEntryDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowFormEntry", "Insert", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowFormEntry.Insert: entry user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseWorkflowFormEntry.Insert");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseWorkflowFormEntry.Insert: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var existingCase = await caseRepository.GetByIdActiveOnlyAsync(model.CaseId, token)
                    .ConfigureAwait(false);

                if (existingCase is null)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseWorkflowFormEntry.Insert: caseId={model.CaseId} not found or not visible " +
                                 $"user={userName}");
                    }

                    throw new NotFoundException(strings[CaseWorkflowFormEntryResources.CaseNotFound]);
                }

                var caseWorkflowStatus = await caseWorkflowStatusRepository
                    .GetByGuidAsync(existingCase.CaseWorkflowStatusGuid, token).ConfigureAwait(false);

                if (caseWorkflowStatus is null)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseWorkflowFormEntry.Insert: caseWorkflowStatus for caseId={model.CaseId} not " +
                                 $"found user={userName}");
                    }

                    throw new NotFoundException(strings[CaseWorkflowFormEntryResources.CaseWorkflowStatusNotFound]);
                }

                var caseWorkflowForm = await caseWorkflowFormRepository
                    .GetByIdActiveForCaseWorkflowGuidAsync(model.CaseWorkflowFormId, existingCase.CaseWorkflowGuid,
                        token).ConfigureAwait(false);

                if (caseWorkflowForm is null)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseWorkflowFormEntry.Insert: caseWorkflowFormId={model.CaseWorkflowFormId} " +
                                 $"not found user={userName}");
                    }

                    throw new NotFoundException(strings[CaseWorkflowFormEntryResources.CaseWorkflowFormNotFound]);
                }

                var (formPayload, rows) = await AcceptedAsync(model, existingCase, caseWorkflowStatus,
                    caseWorkflowForm, token).ConfigureAwait(false);

                var toInsert = CaseWorkflowFormEntryMapper.ToPoco(model);
                toInsert.CaseKey = existingCase.CaseKey;
                toInsert.CaseKeyValue = existingCase.CaseKeyValue;
                toInsert.Template = caseWorkflowForm.Html;

                if (!string.IsNullOrWhiteSpace(caseWorkflowForm.Sql))
                {
                    toInsert.Sql = caseWorkflowForm.Sql;
                    toInsert.Rows = CaseRowsContext.ToJson(rows);
                }

                var saved = await repository.InsertAsync(toInsert, token).ConfigureAwait(false);

                foreach (var (key, value) in formPayload)
                {
                    if (value is null)
                    {
                        continue;
                    }

                    await entryValueRepository.InsertAsync(new Data.Poco.CaseWorkflowFormEntryValue
                    {
                        CaseWorkflowFormEntryId = saved.Id,
                        Name = key,
                        Value = value.ToString()
                    }, token).ConfigureAwait(false);
                }

                op.Entity(saved.Id);
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"CaseWorkflowFormEntry.Insert: created Id={saved.Id} caseId={model.CaseId} " +
                             $"user={userName}");
                }

                var formSource = new DispatchSource(CaseDispatchSourceKind.Form, caseWorkflowForm.Id,
                    caseWorkflowForm.Name ?? string.Empty, caseWorkflowForm.Sql,
                    caseWorkflowForm.EnableNotification == 1, caseWorkflowForm.NotificationTypeId ?? 1,
                    caseWorkflowForm.NotificationDestination, caseWorkflowForm.NotificationSubject,
                    caseWorkflowForm.NotificationBody, caseWorkflowForm.EnableHttpEndpoint == 1,
                    caseWorkflowForm.HttpEndpoint, caseWorkflowForm.HttpEndpointTypeId ?? 1);

                if (formSource.Wanted)
                {
                    var payload = formSource.UsesSql
                        ? null
                        : JsonConvert.DeserializeObject<EntryPayload>(existingCase.Json,
                            jsonSerializationHelper.DefaultJsonSerializerSettingsSettings);

                    await dispatcher.DispatchAsync(formSource,
                        new DispatchCase(existingCase.Id, existingCase.CaseKey, existingCase.CaseKeyValue),
                        formPayload.ToDictionary(v => v.Key, v => v.Value?.ToString()),
                        payload is null
                            ? null
                            : new LegacyRender(text => EntryPayloadExtensions.ReplaceTokens(payload, text),
                                () => PreparePostBodyString(saved, formPayload, existingCase, caseWorkflowStatus,
                                    caseWorkflowForm, payload)), rows, token).ConfigureAwait(false);
                }

                return CaseWorkflowFormEntryMapper.ToDto(saved);
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
                log.Error($"CaseWorkflowFormEntry.Insert: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private async Task<(Dictionary<string, object?> Accepted, CaseRows Rows)> AcceptedAsync(
            CaseWorkflowFormEntryDto model,
            Data.Poco.Case existingCase, Data.Poco.CaseWorkflowStatus caseWorkflowStatus,
            Data.Poco.CaseWorkflowForm caseWorkflowForm, CancellationToken token)
        {
            var submitted = (model.Payload ?? throw new InvalidOperationException("Payload is validated as required."))
                .Where(p => p.Value is not null)
                .ToDictionary(p => p.Key, p => p.Value is bool flag
                    ? flag ? "true" : "false"
                    : p.Value!.ToString());

            EntryPayload? payload;

            try
            {
                payload = JsonConvert.DeserializeObject<EntryPayload>(existingCase.Json,
                    jsonSerializationHelper.DefaultJsonSerializerSettingsSettings);
            }
            catch (Exception e) when (e is JsonException or InvalidCastException or NullReferenceException
                                          or FormatException)
            {
                payload = null;
            }

            var usesSql = !string.IsNullOrWhiteSpace(caseWorkflowForm.Sql);
            var dialect = usesSql ? TemplateDialect.FormSql : TemplateDialect.Form;
            var rows = CaseRows.None;

            if (usesSql)
            {
                try
                {
                    rows = await new CaseSqlRunner(
                        dynamicEnvironment.AppSettings("ReportConnectionString") ??
                        dynamicEnvironment.AppSettings("ConnectionString"), userName, log).RunAsync(
                        caseWorkflowForm.Sql, existingCase.Id, existingCase.CaseKeyValue, token).ConfigureAwait(false);
                }
                catch (CaseSqlException e)
                {
                    log.Warn($"CaseWorkflowFormEntry.Insert: the form SQL failed user={userName}: {e.Message}");

                    throw new DtoValidationException(new ValidationResult([
                        new ValidationFailure(nameof(model.CaseWorkflowFormId),
                            strings[CaseWorkflowFormEntryResources.FormSqlFailed])
                        {
                            ErrorCode = "FormSqlFailed"
                        }
                    ]));
                }
            }

            var roots = usesSql
                ? CaseRowsContext.Roots(rows)
                : CaseWorkflowTemplateRoots.Build(existingCase, caseWorkflowStatus, payload ?? new EntryPayload());

            FormSubmissionResult result;
            FormSchema schema;
            IReadOnlyList<string> visible;

            try
            {
                schema = DatasourceTemplate.CompileForm(caseWorkflowForm.Html, dialect);
                visible = DatasourceTemplate.RenderForm(caseWorkflowForm.Html, roots, submitted, false, dialect)
                    .VisibleFields;
                result = FormSubmissionValidator.Validate(schema, visible, submitted);
            }
            catch (DatasourceChartException ex)
            {
                throw new DtoValidationException(new ValidationResult([
                    new ValidationFailure(nameof(model.CaseWorkflowFormId), chartText.Describe(ex))
                    {
                        ErrorCode = "FormTemplateInvalid"
                    }
                ]));
            }

            if (!result.Valid)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseWorkflowFormEntry.Insert: submission failed the form template user={userName} " +
                             $"fields=[{string.Join(",", result.Errors.Select(e => e.Name))}]");
                }

                var failures = result.Errors.Where(e => e.Code != FormSubmissionValidator.Unknown).Select(e =>
                    new ValidationFailure($"{nameof(model.Payload)}.{e.Name}", FieldMessage(e.Code))
                    {
                        ErrorCode = e.Code
                    }).ToList();

                if (result.Errors.Any(e => e.Code == FormSubmissionValidator.Unknown))
                {
                    failures.Add(new ValidationFailure(nameof(model.Payload),
                        FieldMessage(FormSubmissionValidator.Unknown))
                    {
                        ErrorCode = FormSubmissionValidator.Unknown
                    });
                }

                throw new DtoValidationException(new ValidationResult(failures));
            }

            var rules = (await validatorRepository
                .GetActiveByCaseWorkflowFormGuidAsync(caseWorkflowForm.Guid, token).ConfigureAwait(false))
                .Select(r => new FormValidatorRule(r.Id, r.FieldName, (CaseWorkflowFormValidatorKind)r.KindId.GetValueOrDefault(),
                    r.Argument1, r.Argument2, r.Sql, r.Message)).ToList();

            if (rules.Count > 0)
            {
                var modelGuid = await validatorRepository.GetModelGuidOfFormAsync(caseWorkflowForm.Guid, token)
                    .ConfigureAwait(false);

                var problems = await FormValidatorEngine.ValidateAsync(schema, visible, result.Accepted, rules,
                    existingCase.CaseKeyValue ?? string.Empty, existingCase.Id,
                    FormValidatorRuntime.Lookups(dbContext, tenantRegistryId, modelGuid.GetValueOrDefault(),
                        dynamicEnvironment),
                    FormValidatorRuntime.SqlRunner(dynamicEnvironment, userName, log), token).ConfigureAwait(false);

                if (problems.Count > 0)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseWorkflowFormEntry.Insert: submission failed the form validators " +
                                 $"user={userName} fields=[{string.Join(",", problems.Select(e => e.Name))}]");
                    }

                    throw new DtoValidationException(new ValidationResult(problems.Select(e =>
                        new ValidationFailure($"{nameof(model.Payload)}.{e.Name}",
                            FormValidatorMessages.For(e.Code, e.Message, strings))
                        {
                            ErrorCode = e.Code
                        })));
                }
            }

            return (result.Accepted.ToDictionary(a => a.Key, a => (object?)a.Value), rows);
        }

        private string FieldMessage(string code)
        {
            return FormValidatorMessages.For(code, null, strings);
        }

        [Description("Validates a Case Workflow Form Entry without saving it, running every check a create " +
                     "would run, and returns each failure. Nothing is stored or changed.")]
        [ServiceOperation("CaseWorkflowFormEntryValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The Case Workflow Form Entry to validate.")]
            CaseWorkflowFormEntryDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowFormEntry", "Validate", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowFormEntry.Validate: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseWorkflowFormEntry.Validate");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                op.Rows(results.Errors.Count);

                return ValidationResultMapper.ToDto(results);
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
                log.Error($"CaseWorkflowFormEntry.Validate: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists case workflow form entries for a Case Key/Value pair, newest first, scoped to the " +
                     "caller's tenant and to Case Workflow/Case Workflow Status roles the caller holds.")]
        [ServiceOperation("CaseWorkflowFormEntryListByCaseKeyValue", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowFormEntryDto>> GetByCaseKeyValueAsync(
            [Description("The case key.")] string key,
            [Description("The case key value.")] string value,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseWorkflowFormEntry", "GetByCaseKeyValue", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseWorkflowFormEntry.GetByCaseKeyValue: entry key={key} value={value} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseWorkflowFormEntry.GetByCaseKeyValue");

                var dtos = CaseWorkflowFormEntryMapper.ToDto(
                    await repository.GetByCaseKeyValueActiveOnlyAsync(key, value, token).ConfigureAwait(false));
                op.Rows(dtos.Count);

                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseWorkflowFormEntry.GetByCaseKeyValue: {dtos.Count} rows key={key} value={value} " +
                              $"user={userName}");
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

                log.Error($"CaseWorkflowFormEntry.GetByCaseKeyValue: unexpected failure key={key} value={value} " +
                          $"user={userName}", ex);

                throw;
            }
        }

        private string PreparePostBodyString(RulePoco entry, Dictionary<string, object?> formPayload,
            Data.Poco.Case existingCase, Data.Poco.CaseWorkflowStatus caseWorkflowStatus,
            Data.Poco.CaseWorkflowForm caseWorkflowForm, EntryPayload payload)
        {
            var jObject = JObject.FromObject(entry,
                jsonSerializationHelper.ArchiveJsonSerializer);

            var payloadJObject = JObject.FromObject(formPayload);
            jObject["payload"] = payloadJObject;
            jObject["caseWorkflowFormName"] = caseWorkflowForm.Name;

            var caseJObject = JObject.FromObject(existingCase,
                jsonSerializationHelper.ArchiveJsonSerializer);
            caseJObject.Remove("json");
            jObject["case"] = caseJObject;

            caseJObject["caseWorkflowStatus"] = caseWorkflowStatus.Name;

            var casePayloadJObject = JObject.FromObject(payload,
                jsonSerializationHelper.ArchiveJsonSerializer);

            caseJObject["payload"] = casePayloadJObject;

            return jObject.ToString();
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

            throw new ForbiddenException(strings[CaseWorkflowFormEntryResources.PermissionDenied], permissions);
        }
    }
}