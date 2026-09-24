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
using Jube.Dto.Validation;
using Jube.Dto.Repository.CaseWorkflowFormEntry;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.CaseWorkflowFormEntry;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.CaseWorkflowFormEntry;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using EngineJsonSerializationHelper = Jube.Engine.Helpers.JsonSerializationHelper;
using EngineNotification = global::Jube.Case.Notification;
using EngineSendHttpEndpoint = global::Jube.Case.SendHttpEndpoint;
using RulePoco = Jube.Data.Poco.CaseWorkflowFormEntry;
using RuleRepository = Jube.Data.Repository.CaseWorkflowFormEntryRepository;

namespace Jube.Service.Repository.CaseWorkflowFormEntry
{
    using EntryPayload = Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload
        .EntityAnalysisModelInstanceEntryPayload;
    using EntryPayloadExtensions =
        Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload
        .Extensions.EntityAnalysisModelInstanceEntryPayloadExtensions;

    public sealed class CaseWorkflowFormEntryService
    {
        private static readonly int[] permissions = [1];

        private readonly ILog auditLog;
        private readonly RuleRepository repository;
        private readonly CaseWorkflowFormEntryValueRepository entryValueRepository;
        private readonly CaseRepository caseRepository;
        private readonly CaseWorkflowFormRepository caseWorkflowFormRepository;
        private readonly CaseWorkflowStatusRepository caseWorkflowStatusRepository;
        private readonly DynamicEnvironment.DynamicEnvironment dynamicEnvironment;
        private readonly EngineJsonSerializationHelper jsonSerializationHelper;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly CaseWorkflowFormEntryDtoValidator validator;

        private CaseWorkflowFormEntryService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            EngineJsonSerializationHelper jsonSerializationHelper)
        {
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
                permissionValidation, log, auditLog, serviceChangeBus, strings, dynamicEnvironment,
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

                var toInsert = CaseWorkflowFormEntryMapper.ToPoco(model);
                toInsert.CaseKey = existingCase.CaseKey;
                toInsert.CaseKeyValue = existingCase.CaseKeyValue;
                var saved = await repository.InsertAsync(toInsert, token).ConfigureAwait(false);

                var formPayload = model.Payload ??
                                  throw new InvalidOperationException("Payload is validated as required.");

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

                if (caseWorkflowForm.EnableNotification == 1 || caseWorkflowForm.EnableHttpEndpoint == 1)
                {
                    var payload = JsonConvert.DeserializeObject<EntryPayload>(
                        existingCase.Json, jsonSerializationHelper.DefaultJsonSerializerSettingsSettings);

                    if (caseWorkflowForm.EnableNotification == 1 && payload is not null)
                    {
                        var notificationSubject =
                            EntryPayloadExtensions.ReplaceTokens(payload, caseWorkflowForm.NotificationSubject);
                        var notificationDestination =
                            EntryPayloadExtensions.ReplaceTokens(payload, caseWorkflowForm.NotificationDestination);
                        var notificationBody =
                            EntryPayloadExtensions.ReplaceTokens(payload, caseWorkflowForm.NotificationBody);

                        var notification = new EngineNotification(log, dynamicEnvironment);

                        await notification.SendAsync(caseWorkflowForm.NotificationTypeId ?? 1,
                                notificationDestination, notificationSubject, notificationBody, token)
                            .ConfigureAwait(false);
                    }

                    if (caseWorkflowForm.EnableHttpEndpoint == 1 && payload is not null)
                    {
                        var endpoint = EntryPayloadExtensions.ReplaceTokens(payload, caseWorkflowForm.HttpEndpoint);

                        if (caseWorkflowStatus.HttpEndpointTypeId == 1)
                        {
                            await EngineSendHttpEndpoint.PostAsync(endpoint,
                                PreparePostBodyString(saved, formPayload, existingCase, caseWorkflowStatus,
                                    caseWorkflowForm, payload), log).ConfigureAwait(false);
                        }
                        else
                        {
                            await EngineSendHttpEndpoint.GetAsync(endpoint, log).ConfigureAwait(false);
                        }
                    }
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
            var jObject = Newtonsoft.Json.Linq.JObject.FromObject(entry,
                jsonSerializationHelper.ArchiveJsonSerializer);
            var payloadJObject = Newtonsoft.Json.Linq.JObject.FromObject(formPayload);
            jObject["payload"] = payloadJObject;
            jObject["caseWorkflowFormName"] = caseWorkflowForm.Name;

            var caseJObject = Newtonsoft.Json.Linq.JObject.FromObject(existingCase,
                jsonSerializationHelper.ArchiveJsonSerializer);
            caseJObject.Remove("json");
            jObject["case"] = caseJObject;

            caseJObject["caseWorkflowStatus"] = caseWorkflowStatus.Name;

            var casePayloadJObject = Newtonsoft.Json.Linq.JObject.FromObject(payload,
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