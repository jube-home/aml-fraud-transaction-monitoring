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
using Jube.Data.Query;
using Jube.Data.Repository;
using Jube.Dto.Query.EntityAnalysisModelInvocationContext;
using Jube.Engine.EntityAnalysisModelInvoke.Extraction;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.EntityAnalysisModelInvocationContext;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jube.Service.Query.EntityAnalysisModelInvocationContext
{
    public sealed class EntityAnalysisModelInvocationContextService
    {
        public const int MaximumRequestJsonLength = 1_048_576;
        private const int ContextParseType = 5;

        private static readonly int[] authoringPermissions = [8, 10, 13, 14, 16, 17, 25, 26];
        private static readonly int[] archivePermissions = [1, 40];

        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private EntityAnalysisModelInvocationContextService(DbContext dbContext, string userName,
            int tenantRegistryId, PermissionValidation permissionValidation, ILog log, ILog auditLog,
            IServiceChangeBus serviceChangeBus, IStringLocalizer strings)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
        }

        public static Task<EntityAnalysisModelInvocationContextService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelInvocationContextService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelInvocationContextResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EntityAnalysisModelInvocationContext.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelInvocationContextResources
                    .NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"EntityAnalysisModelInvocationContext.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelInvocationContextResources
                    .NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelInvocationContextService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Returns a blank rule-evaluation context for an Entity Analysis Model: every name a rule can " +
                     "use (by completion name) with only the request XPath default values filled in, and every " +
                     "pipeline stage marked not computed. Fill it with the Overlay operation to build a context " +
                     "by hand. Nothing is stored or invoked.")]
        [ServiceOperation("EntityAnalysisModelInvocationContextBlank", OperationKind.Read, Idempotent = true)]
        public Task<InvocationContextDto> BlankAsync(
            [Description("Id of the Entity Analysis Model; must belong to the caller's tenant.")]
            int entityAnalysisModelId,
            CancellationToken token = default)
        {
            return RunAsync("Blank", authoringPermissions, async () =>
            {
                await RequireModelAsync(entityAnalysisModelId, token).ConfigureAwait(false);
                var fields = await FieldsAsync(entityAnalysisModelId, token).ConfigureAwait(false);
                var requestFields = await RequestFieldsAsync(entityAnalysisModelId, token).ConfigureAwait(false);

                return EntityAnalysisModelInvocationContextMapper.ToDto(InvocationContextBuilder.Blank(
                    entityAnalysisModelId, fields, requestFields, AssumeLocalDates(), DateTime.UtcNow));
            }, token);
        }

        [Description("Builds a rule-evaluation context from a transaction's request JSON exactly as the model's " +
                     "invocation would extract it: request XPaths are read, typed and defaulted by the engine's " +
                     "own extraction code, and the reference date and entry id are resolved. Later stages (inline " +
                     "functions, TTL counters, abstractions, sanctions, calculations, adaptations, activations) " +
                     "are not computed and are listed as such; set their values with the Overlay operation. Stages " +
                     "that store or notify are never performed. Nothing is stored or invoked.")]
        [ServiceOperation("EntityAnalysisModelInvocationContextFromRequestJson", OperationKind.Read,
            Idempotent = true)]
        public Task<InvocationContextDto> FromRequestJsonAsync(
            [Description("The model and the request JSON.")]
            InvocationContextFromRequestJsonDto? request,
            CancellationToken token = default)
        {
            return RunAsync("FromRequestJson", authoringPermissions, async () =>
            {
                ArgumentNullException.ThrowIfNull(request);
                var json = ParseRequestJson(request.RequestJson);
                var model = await RequireModelAsync(request.EntityAnalysisModelId, token).ConfigureAwait(false);
                var fields = await FieldsAsync(model.Id, token).ConfigureAwait(false);
                var requestFields = await RequestFieldsAsync(model.Id, token).ConfigureAwait(false);

                return EntityAnalysisModelInvocationContextMapper.ToDto(InvocationContextBuilder.FromRequestJson(
                    model.Id, json, fields, requestFields,
                    new RequestReferences(model.EntryXPath ?? string.Empty, model.ReferenceDateXPath ?? string.Empty,
                        model.ReferenceDatePayloadLocationTypeId ?? 1),
                    AssumeLocalDates(), DateTime.UtcNow));
            }, token);
        }

        [Description("Builds a rule-evaluation context from a transaction the model has already invoked and " +
                     "archived, with every value (payload, TTL counters, abstractions, sanctions, calculations, " +
                     "adaptations, dictionary values and matched activation rules) as it was at the time. Use it " +
                     "to ask whether a new or changed rule would have fired on a real transaction. Nothing is " +
                     "stored or invoked.")]
        [ServiceOperation("EntityAnalysisModelInvocationContextFromArchive", OperationKind.Read, Idempotent = true)]
        public Task<InvocationContextDto> FromArchiveAsync(
            [Description("The model and the archived transaction's Guid.")]
            InvocationContextFromArchiveDto? request,
            CancellationToken token = default)
        {
            return RunAsync("FromArchive", archivePermissions, async () =>
            {
                ArgumentNullException.ThrowIfNull(request);
                var model = await RequireModelAsync(request.EntityAnalysisModelId, token).ConfigureAwait(false);
                var archive = await new ArchiveRepository(dbContext)
                    .GetByEntityAnalysisModelInstanceEntryGuidAsync(request.EntityAnalysisModelInstanceEntryGuid,
                        model.Id, tenantRegistryId, token).ConfigureAwait(false);

                if (archive?.Json == null)
                {
                    throw new NotFoundException(strings[EntityAnalysisModelInvocationContextResources
                        .ArchiveNotFound]);
                }

                var fields = await FieldsAsync(model.Id, token).ConfigureAwait(false);
                return EntityAnalysisModelInvocationContextMapper.ToDto(InvocationContextBuilder.FromArchive(
                    model.Id, request.EntityAnalysisModelInstanceEntryGuid, JObject.Parse(archive.Json), fields));
            }, token);
        }

        [Description("Sets values in a rule-evaluation context by completion name and returns the changed " +
                     "context, e.g. Abstraction.CountLastHour = 6 to test a rule against a high count. Each value " +
                     "is checked against the name's data type; unknown names and unreadable values are listed in " +
                     "Errors and left unchanged. Nothing is stored.")]
        [ServiceOperation("EntityAnalysisModelInvocationContextOverlay", OperationKind.Read, Idempotent = true)]
        public Task<InvocationContextDto> OverlayAsync(
            [Description("The context and the values to set.")]
            InvocationContextOverlayDto? request,
            CancellationToken token = default)
        {
            return RunAsync("Overlay", authoringPermissions, async () =>
            {
                ArgumentNullException.ThrowIfNull(request);
                ArgumentNullException.ThrowIfNull(request.Context);
                await RequireModelAsync(request.Context.EntityAnalysisModelId, token).ConfigureAwait(false);

                var context = EntityAnalysisModelInvocationContextMapper.ToContext(request.Context);
                var errors = InvocationContextBuilder.Overlay(context, request.Values);
                return EntityAnalysisModelInvocationContextMapper.ToDto(context, errors);
            }, token);
        }

        private async Task<InvocationContextDto> RunAsync(string operation, int[] permissions,
            Func<Task<InvocationContextDto>> body, CancellationToken token)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInvocationContext", operation, userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelInvocationContext.{operation}: entry user={userName}");
            }

            try
            {
                token.ThrowIfCancellationRequested();
                EnsurePermitted($"EntityAnalysisModelInvocationContext.{operation}", permissions);
                var result = await body().ConfigureAwait(false);
                op.Entity(result.EntityAnalysisModelId);
                op.Rows(result.Values.Count);
                return result;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("notfound");
                throw;
            }
            catch (InvalidRequestException)
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
                log.Error($"EntityAnalysisModelInvocationContext.{operation}: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        private JObject ParseRequestJson(string? requestJson)
        {
            if (requestJson is { Length: > MaximumRequestJsonLength })
            {
                throw new InvalidRequestException(string.Format(
                    strings[EntityAnalysisModelInvocationContextResources.RequestJsonTooLarge],
                    MaximumRequestJsonLength));
            }

            try
            {
                return RequestFieldExtraction.Parse(new StringReader(requestJson ?? string.Empty));
            }
            catch (JsonReaderException ex)
            {
                throw new InvalidRequestException(string.Format(
                    strings[EntityAnalysisModelInvocationContextResources.RequestJsonInvalid], ex.Message), ex);
            }
        }

        private async Task<Data.Poco.EntityAnalysisModel> RequireModelAsync(int entityAnalysisModelId,
            CancellationToken token)
        {
            var model = await new EntityAnalysisModelRepository(dbContext, tenantRegistryId)
                .GetByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false);

            if (model == null || model.TenantRegistryId != tenantRegistryId)
            {
                throw new NotFoundException(strings[EntityAnalysisModelInvocationContextResources.ModelNotFound]);
            }

            return model;
        }

        private async Task<List<InvocationContextField>> FieldsAsync(int entityAnalysisModelId,
            CancellationToken token)
        {
            return (await new GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery(dbContext,
                        tenantRegistryId)
                    .ExecuteAsync(entityAnalysisModelId, ContextParseType, false, token).ConfigureAwait(false))
                .Where(f => f.JQueryBuilderDataType != "list" && f.Name.Contains('.'))
                .GroupBy(f => f.Name)
                .Select(g => new InvocationContextField(g.Key, g.First().Group, g.First().JQueryBuilderDataType))
                .ToList();
        }

        private async Task<List<RequestField>> RequestFieldsAsync(int entityAnalysisModelId, CancellationToken token)
        {
            return (await new EntityAnalysisModelRequestXPathRepository(dbContext, tenantRegistryId)
                    .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false))
                .Where(x => x.Active == 1 && !string.IsNullOrEmpty(x.Name))
                .Select(x => new RequestField(x.Name, x.XPath, x.DataTypeId ?? 1, x.DefaultValue,
                    x.EncryptionId is 1 or 2))
                .ToList();
        }

        private static bool AssumeLocalDates()
        {
            return !string.Equals(Environment.GetEnvironmentVariable("AssumeLocalDateInPayloadExtraction"), "False",
                StringComparison.OrdinalIgnoreCase);
        }

        private void EnsurePermitted(string op, int[] permissions)
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", permissions)}]");
            }

            throw new ForbiddenException(strings[EntityAnalysisModelInvocationContextResources.PermissionDenied],
                permissions);
        }
    }
}