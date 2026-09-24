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
using Jube.Dto.Repository.CaseNote;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.CaseNote;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.CaseNote;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using EngineJsonSerializationHelper = Jube.Engine.Helpers.JsonSerializationHelper;
using EngineNotification = global::Jube.Case.Notification;
using EngineSendHttpEndpoint = global::Jube.Case.SendHttpEndpoint;
using RulePoco = Jube.Data.Poco.CaseNote;
using RuleRepository = Jube.Data.Repository.CaseNoteRepository;

namespace Jube.Service.Repository.CaseNote
{
    using EntryPayload = Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload
        .EntityAnalysisModelInstanceEntryPayload;
    using EntryPayloadExtensions =
        Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload
        .Extensions.EntityAnalysisModelInstanceEntryPayloadExtensions;

    public sealed class CaseNoteService
    {
        private static readonly int[] permissions = [1];

        private readonly ILog auditLog;
        private readonly RuleRepository repository;
        private readonly CaseRepository caseRepository;
        private readonly CaseWorkflowActionRepository caseWorkflowActionRepository;
        private readonly CaseWorkflowStatusRepository caseWorkflowStatusRepository;
        private readonly DynamicEnvironment.DynamicEnvironment dynamicEnvironment;
        private readonly EngineJsonSerializationHelper jsonSerializationHelper;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly CaseNoteDtoValidator validator;

        private CaseNoteService(DbContext dbContext, string userName, int tenantRegistryId,
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
            caseRepository = new CaseRepository(dbContext, userName);
            caseWorkflowActionRepository = new CaseWorkflowActionRepository(dbContext, userName);
            caseWorkflowStatusRepository = new CaseWorkflowStatusRepository(dbContext, userName);
            validator = new CaseNoteDtoValidator(strings);
        }

        public static Task<CaseNoteService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            EngineJsonSerializationHelper jsonSerializationHelper, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment,
                jsonSerializationHelper, LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseNoteService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            EngineJsonSerializationHelper jsonSerializationHelper, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseNoteResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("CaseNote.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseNoteResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseNote.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseNoteResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseNoteService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation, log,
                auditLog, serviceChangeBus, strings, dynamicEnvironment, jsonSerializationHelper);
        }

        [Description("Adds a note to a case, filed against a CaseWorkflowAction; dispatches that action's " +
                     "notification and/or HTTP endpoint, if either is enabled. Notes are append-only -- there is " +
                     "no update or delete.")]
        [ServiceOperation("CaseNoteInsert", OperationKind.Write, Idempotent = false)]
        public async Task<CaseNoteDto> InsertAsync(CaseNoteDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseNote", "Insert", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseNote.Insert: entry user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseNote.Insert");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseNote.Insert: validation failed user={userName} " +
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
                        log.Warn($"CaseNote.Insert: caseId={model.CaseId} not found or not visible user={userName}");
                    }

                    throw new NotFoundException(strings[CaseNoteResources.CaseNotFound]);
                }

                var caseWorkflowAction = await caseWorkflowActionRepository
                    .GetByIdActiveOnlyAsync(model.ActionId, token).ConfigureAwait(false);
                if (caseWorkflowAction is null)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseNote.Insert: actionId={model.ActionId} not found user={userName}");
                    }

                    throw new NotFoundException(strings[CaseNoteResources.CaseWorkflowActionNotFound]);
                }

                var caseWorkflowStatus = await caseWorkflowStatusRepository
                    .GetByGuidAsync(existingCase.CaseWorkflowStatusGuid, token).ConfigureAwait(false);
                if (caseWorkflowStatus is null)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseNote.Insert: caseWorkflowStatus for caseId={model.CaseId} not found " +
                                 $"user={userName}");
                    }

                    throw new NotFoundException(strings[CaseNoteResources.CaseWorkflowStatusNotFound]);
                }

                if (caseWorkflowAction.CaseWorkflowId != caseWorkflowStatus.CaseWorkflowId)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseNote.Insert: action {model.ActionId} does not belong to the workflow of " +
                                 $"case {model.CaseId} user={userName}");
                    }

                    throw new NotFoundException(strings[CaseNoteResources.CaseWorkflowActionNotFound]);
                }

                var poco = CaseNoteMapper.ToPoco(model);
                poco.CaseKey = existingCase.CaseKey;
                poco.CaseKeyValue = existingCase.CaseKeyValue;
                poco.Note = HtmlSanitiser.Sanitise(poco.Note);
                var saved = await repository.InsertAsync(poco, token).ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"CaseNote.Insert: created Id={saved.Id} caseId={model.CaseId} user={userName}");
                }

                if (model.Payload is not null &&
                    (caseWorkflowAction.EnableNotification == 1 || caseWorkflowAction.EnableHttpEndpoint == 1))
                {
                    var payload = JsonConvert.DeserializeObject<EntryPayload>(
                        existingCase.Json, jsonSerializationHelper.DefaultJsonSerializerSettingsSettings);

                    if (caseWorkflowAction.EnableNotification == 1 && payload is not null)
                    {
                        var notificationSubject =
                            EntryPayloadExtensions.ReplaceTokens(payload, caseWorkflowAction.NotificationSubject);
                        var notificationDestination =
                            EntryPayloadExtensions.ReplaceTokens(payload, caseWorkflowAction.NotificationDestination);
                        var notificationBody =
                            EntryPayloadExtensions.ReplaceTokens(payload, caseWorkflowAction.NotificationBody);

                        var notification = new EngineNotification(log, dynamicEnvironment);

                        await notification.SendAsync(caseWorkflowAction.NotificationTypeId ?? 1,
                                notificationDestination, notificationSubject, notificationBody, token)
                            .ConfigureAwait(false);
                    }

                    if (caseWorkflowAction.EnableHttpEndpoint == 1 && payload is not null)
                    {
                        var endpoint = EntryPayloadExtensions.ReplaceTokens(payload, caseWorkflowAction.HttpEndpoint);

                        if (caseWorkflowAction.HttpEndpointTypeId == 1)
                        {
                            await EngineSendHttpEndpoint.PostAsync(endpoint,
                                PreparePostBodyString(saved, existingCase, caseWorkflowStatus, caseWorkflowAction,
                                    payload), log).ConfigureAwait(false);
                        }
                        else
                        {
                            await EngineSendHttpEndpoint.GetAsync(endpoint, log).ConfigureAwait(false);
                        }
                    }
                }

                return CaseNoteMapper.ToDto(saved);
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
                log.Error($"CaseNote.Insert: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Validates a Case Note without saving it, running every check a create would run, and " +
                     "returns each failure. Nothing is stored or changed.")]
        [ServiceOperation("CaseNoteValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The Case Note to validate.")]
            CaseNoteDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseNote", "Validate", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseNote.Validate: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("CaseNote.Validate");

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
                log.Error($"CaseNote.Validate: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists case notes for a Case Key/Value pair, newest first -- notes roll up to the key/value " +
                     "combination, not the current Case Id, so they remain visible across the key's full case " +
                     "history.")]
        [ServiceOperation("CaseNoteListByCaseKeyValue", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseNoteDto>> GetByCaseKeyValueAsync(
            [Description("The case key.")] string key,
            [Description("The case key value.")] string value,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseNote", "GetByCaseKeyValue", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseNote.GetByCaseKeyValue: entry key={key} value={value} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseNote.GetByCaseKeyValue");
                var dtos = CaseNoteMapper.ToDto(
                    await repository.GetByCaseKeyValueAsync(key, value, token).ConfigureAwait(false));
                foreach (var dto in dtos)
                {
                    dto.Note = HtmlSanitiser.Sanitise(dto.Note);
                }

                op.Rows(dtos.Count);

                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseNote.GetByCaseKeyValue: {dtos.Count} rows key={key} value={value} " +
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
                log.Error($"CaseNote.GetByCaseKeyValue: unexpected failure key={key} value={value} " +
                          $"user={userName}", ex);
                throw;
            }
        }

        private string PreparePostBodyString(RulePoco caseNote, Data.Poco.Case existingCase,
            Data.Poco.CaseWorkflowStatus caseWorkflowStatus, Data.Poco.CaseWorkflowAction caseWorkflowAction,
            EntryPayload payload)
        {
            var jObject = Newtonsoft.Json.Linq.JObject.FromObject(caseNote,
                jsonSerializationHelper.ArchiveJsonSerializer);
            jObject["caseWorkflowActionName"] = caseWorkflowAction.Name;

            var caseJObject = Newtonsoft.Json.Linq.JObject.FromObject(existingCase,
                jsonSerializationHelper.ArchiveJsonSerializer);
            caseJObject.Remove("json");
            jObject["case"] = caseJObject;

            caseJObject["caseWorkflowStatus"] = caseWorkflowStatus.Name;

            var payloadJObject = Newtonsoft.Json.Linq.JObject.FromObject(payload,
                jsonSerializationHelper.ArchiveJsonSerializer);
            caseJObject["payload"] = payloadJObject;

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

            throw new ForbiddenException(strings[CaseNoteResources.PermissionDenied], permissions);
        }
    }
}