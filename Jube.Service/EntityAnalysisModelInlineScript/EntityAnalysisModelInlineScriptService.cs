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
using Jube.Service.Query.EntityAnalysisModelInvocationContext;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation;
using Jube.Dto.Filter;
using Jube.Dto.RuleExecution;
using Jube.Dto.Query.EntityAnalysisModelInvocationContext;
using Jube.Data.Query;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Validation;
using Jube.Dto.EntityAnalysisModelInlineScript;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.EntityAnalysisModelInlineScript;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Parser.Dependency;
using Jube.Validations.Dependency;
using Jube.Validations.EntityAnalysisModelInlineScript;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.EntityAnalysisModelInlineScript
{
    using InlineScriptPoco = Data.Poco.EntityAnalysisModelInlineScript;

    public sealed class EntityAnalysisModelInlineScriptService
    {
        private const int MaxListTake = 200;
        private static readonly int[] listPermissions = [9];
        private static readonly int[] readPermissions = [9];
        private static readonly int[] writePermissions = [9];
        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelInlineScriptRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly ModelEntityDeleteValidator deleteValidator;
        private readonly EntityAnalysisModelInlineScriptDtoValidator validator;

        private EntityAnalysisModelInlineScriptService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, IStringLocalizer dependencyStrings)
        {
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            repository = new EntityAnalysisModelInlineScriptRepository(dbContext, userName);
            validator = new EntityAnalysisModelInlineScriptDtoValidator(repository,
                new EntityAnalysisInlineScriptRepository(dbContext), strings,
                new EntityAnalysisModelRepository(dbContext, userName));
            deleteValidator = new ModelEntityDeleteValidator(dbContext, tenantRegistryId, userName,
                dependencyStrings);
        }

        public static Task<EntityAnalysisModelInlineScriptService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelInlineScriptService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelInlineScriptResources));
            var dependencyStrings = stringLocalizerFactory.Create(typeof(ModelDependencyResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EntityAnalysisModelInlineScript.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelInlineScriptResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"EntityAnalysisModelInlineScript.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelInlineScriptResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelInlineScriptService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, dependencyStrings);
        }

        [Description("Lists every Inline Script registration visible to the calling user's tenant. Unbounded -- " +
                     "intended for the administrative page, not for agent tooling (use the bounded list operation " +
                     "instead).")]
        public async Task<List<EntityAnalysisModelInlineScriptDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineScript", "List", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelInlineScript.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelInlineScript.List");
                var dtos = EntityAnalysisModelInlineScriptMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelInlineScript.List: {dtos.Count} rows user={userName}");
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
                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelInlineScript.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelInlineScript.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Inline Script registrations belonging to the given Model, ordered by Id, scoped to the " +
                     "calling user's tenant.")]
        [ServiceOperation("EntityAnalysisModelInlineScriptGetByEntityAnalysisModelId", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<EntityAnalysisModelInlineScriptDto>> GetByEntityAnalysisModelIdAsync(
            [Description("Numeric identifier of the parent Model.")]
            int entityAnalysisModelId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineScript", "ListByEntityAnalysisModelId",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelInlineScript.ListByEntityAnalysisModelId: entry entityAnalysisModelId={entityAnalysisModelId} user={userName}");
            }

            try
            {
                EnsurePermitted(readPermissions, "EntityAnalysisModelInlineScript.ListByEntityAnalysisModelId");
                var dtos = EntityAnalysisModelInlineScriptMapper.ToDto(await repository
                    .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"EntityAnalysisModelInlineScript.ListByEntityAnalysisModelId: {dtos.Count} rows entityAnalysisModelId={entityAnalysisModelId} user={userName}");
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
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"EntityAnalysisModelInlineScript.ListByEntityAnalysisModelId: cancelled entityAnalysisModelId={entityAnalysisModelId} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelInlineScript.ListByEntityAnalysisModelId: unexpected failure entityAnalysisModelId={entityAnalysisModelId} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Returns one Inline Script registration by its numeric identifier, scoped to the calling " +
                     "user's tenant. Returns null when the row does not exist or is not visible to the caller.")]
        [ServiceOperation("EntityAnalysisModelInlineScriptGet", OperationKind.Read, Idempotent = true)]
        public async Task<EntityAnalysisModelInlineScriptDto?> GetByIdAsync(
            [Description("Numeric identifier of the Inline Script registration.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineScript", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelInlineScript.Get: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted(readPermissions, "EntityAnalysisModelInlineScript.Get");
                var inlineScript = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (inlineScript == null)
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug(
                            $"EntityAnalysisModelInlineScript.Get: id={id} not found or not visible to tenant user={userName}");
                    }

                    return null;
                }

                op.Entity(inlineScript.Id);
                return EntityAnalysisModelInlineScriptMapper.ToDto(inlineScript);
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelInlineScript.Get: cancelled id={id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelInlineScript.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Inline Script registrations for the caller's tenant, ordered by id, capped at 'take' " +
                     "rows (max 200). If 'more' is true, call again with 'afterId' set to the last returned Id to " +
                     "continue.")]
        [ServiceOperation("EntityAnalysisModelInlineScriptList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<EntityAnalysisModelInlineScriptDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineScript", "ListPaged", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelInlineScript.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelInlineScript.ListPaged");

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);

                return new PagedResult<EntityAnalysisModelInlineScriptDto>(
                    EntityAnalysisModelInlineScriptMapper.ToDto(page));
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelInlineScript.ListPaged: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelInlineScript.ListPaged: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the fields a query builder JSON filter over Inline Scripts may use, " +
                     "with each field's type, the operators allowed for it and what it means. " +
                     "Use them as rule ids in EntityAnalysisModelInlineScriptFilter and " +
                     "EntityAnalysisModelInlineScriptCount.")]
        [ServiceOperation("EntityAnalysisModelInlineScriptFilterFields", OperationKind.Read, Idempotent = true)]
        public async Task<List<FilterFieldDto>> FilterFieldsAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineScript", "FilterFields", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelInlineScript.FilterFields: entry user={userName}");
            }

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelInlineScript.FilterFields");
                await Task.CompletedTask.ConfigureAwait(false);
                var result = DtoFilter.Fields<EntityAnalysisModelInlineScriptDto>();
                op.Rows(result.Count);
                return result;
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
                log.Error($"EntityAnalysisModelInlineScript.FilterFields: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns the Inline Scripts in the caller's tenant matching query builder " +
                     "JSON (the same format as the rule builder, over the fields from " +
                     "EntityAnalysisModelInlineScriptFilterFields), ordered by id and capped at " +
                     "'take' rows (max 200). If 'more' is true, call again with 'afterId' set " +
                     "to the last returned Id to continue. Invalid JSON is not an error: Valid " +
                     "is false and Errors gives each problem with its JSON path.")]
        [ServiceOperation("EntityAnalysisModelInlineScriptFilter", OperationKind.Read, Idempotent = true)]
        public async Task<FilterResultDto<EntityAnalysisModelInlineScriptDto>> FilterAsync(
            [Description(
                "Query builder JSON selecting the Inline Scripts, using the fields from EntityAnalysisModelInlineScriptFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineScript", "Filter", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelInlineScript.Filter: entry take={take} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelInlineScript.Filter");
                var rows = EntityAnalysisModelInlineScriptMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
                var result = DtoFilter.Filter(rows, builderJson, take, afterId, d => d.Id);
                op.Rows(result.Items.Count);
                return result;
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
                log.Error($"EntityAnalysisModelInlineScript.Filter: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Counts the Inline Scripts in the caller's tenant matching query builder " +
                     "JSON (over the fields from EntityAnalysisModelInlineScriptFilterFields; " +
                     "empty counts all), optionally broken down by the values of one field. " +
                     "Invalid JSON is not an error: Valid is false and Errors gives each " +
                     "problem with its JSON path.")]
        [ServiceOperation("EntityAnalysisModelInlineScriptCount", OperationKind.Read, Idempotent = true)]
        public async Task<FilterCountResultDto> CountAsync(
            [Description(
                "Query builder JSON selecting the Inline Scripts, using the fields from EntityAnalysisModelInlineScriptFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description(
                "A field from EntityAnalysisModelInlineScriptFilterFields to count the matching rows by, e.g. Active; empty for a single total.")]
            string? groupBy = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineScript", "Count", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelInlineScript.Count: entry groupBy={groupBy} user={userName}");
            }

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelInlineScript.Count");
                var rows = EntityAnalysisModelInlineScriptMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
                var result = DtoFilter.Count(rows, builderJson, groupBy);
                op.Rows(result.Count);
                return result;
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
                log.Error($"EntityAnalysisModelInlineScript.Count: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Registers a new Inline Script invocation under a Model in the caller's tenant. Not " +
                     "idempotent -- calling twice creates two rows.")]
        [ServiceOperation("EntityAnalysisModelInlineScriptCreate", OperationKind.Write, Idempotent = false)]
        public async Task<InlineScriptPoco> InsertAsync(
            [Description("The Inline Script registration to create.")]
            EntityAnalysisModelInlineScriptDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineScript", "Create", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelInlineScript.Create: entry user={userName} name={model?.Name}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "EntityAnalysisModelInlineScript.Create");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"EntityAnalysisModelInlineScript.Create: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.InsertAsync(EntityAnalysisModelInlineScriptMapper.ToPoco(model), token)
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"EntityAnalysisModelInlineScript.Create: created Id={saved.Id} name={saved.Name} " +
                             $"user={userName}");
                }

                return saved;
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
                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelInlineScript.Create: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelInlineScript.Create: unexpected failure user={userName} " +
                          $"name={model?.Name}", ex);
                throw;
            }
        }

        [Description("Validates an Inline Script without saving it, running every check a create (Id 0) or an " +
                     "update (any other Id) would run, and returns each failure. Nothing is stored or changed.")]
        [ServiceOperation("EntityAnalysisModelInlineScriptValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The Inline Script to validate.")]
            EntityAnalysisModelInlineScriptDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineScript", "Validate", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelInlineScript.Validate: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "EntityAnalysisModelInlineScript.Validate");

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
                log.Error($"EntityAnalysisModelInlineScript.Validate: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Runs a model's inline script against an invocation context outside the engine, without " +
                     "storing anything, and returns the public properties the engine would add to the payload as " +
                     "completion names ready for the context Overlay operation. Disabled unless the " +
                     "EnableInlineScriptExecution setting is True, because inline scripts are not restricted by the " +
                     "rule token allow-list and could call out or have side effects.")]
        [ServiceOperation("EntityAnalysisModelInlineScriptExecute", OperationKind.Read, Idempotent = true)]
        public async Task<InlineScriptExecutionResultDto> ExecuteAsync(
            [Description("The model's inline script to run; only the model id and the inline script id are read.")]
            EntityAnalysisModelInlineScriptDto? model,
            [Description("The invocation context to run it against, built for the same model.")]
            InvocationContextDto? context,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineScript", "Execute", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelInlineScript.Execute: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                ArgumentNullException.ThrowIfNull(context);
                EnsurePermitted(writePermissions, "EntityAnalysisModelInlineScript.Execute");

                if (!InlineScriptExecutionEnabled())
                {
                    return InlineScriptRefused("InlineScriptExecutionDisabled",
                        "Running inline scripts is disabled; set EnableInlineScriptExecution to True to allow it.");
                }

                if (context.EntityAnalysisModelId != model.EntityAnalysisModelId)
                {
                    return InlineScriptRefused(RuleExecutor.ContextModelMismatch,
                        "The context belongs to a different model; build the context for the script's model.");
                }

                if (!await EntityAnalysisModelParentGuard
                        .IsVisibleAsync(dbContext, tenantRegistryId, model.EntityAnalysisModelId, token)
                        .ConfigureAwait(false))
                {
                    return InlineScriptRefused("EntityAnalysisModelIdNotFound", "The model was not found.");
                }

                var script = await new EntityAnalysisInlineScriptRepository(dbContext)
                    .GetByIdAsync(model.EntityAnalysisInlineScriptId, token).ConfigureAwait(false);
                if (script == null)
                {
                    return InlineScriptRefused("EntityAnalysisInlineScriptIdNotFound",
                        "The inline script was not found.");
                }

                var lists = await new GetModelListsQuery(dbContext, tenantRegistryId)
                    .ExecuteAsync(model.EntityAnalysisModelId, token).ConfigureAwait(false);
                var inputs = RuleRunner.ToInputs(EntityAnalysisModelInvocationContextMapper.ToContext(context), lists);
                var binaryPath = Path.GetDirectoryName(typeof(InlineScriptRunner).Assembly.Location) ?? string.Empty;
                var frameworkPath = Path.GetDirectoryName(typeof(object).Assembly.Location) ?? string.Empty;
                var run = await InlineScriptRunner.RunAsync(script.Code, script.LanguageId ?? 1, script.Dependency,
                        binaryPath, frameworkPath, inputs, context.ReferenceDate, TimeSpan.FromSeconds(5), log, token)
                    .ConfigureAwait(false);

                op.Rows(run.Properties.Count);
                return new InlineScriptExecutionResultDto
                {
                    Compiled = run.Compiled,
                    Errors = run.Compiled
                        ? []
                        :
                        [
                            new()
                            {
                                PropertyName = "Code", ErrorCode = "InlineScriptInvalid", Message = run.CompileErrors
                            }
                        ],
                    Succeeded = run.Succeeded,
                    Properties = run.Properties.Select(p => new InvocationContextValueDto
                    {
                        Name = "Payload." + p.Key,
                        Group = "Payload",
                        DataType = p.Value switch
                        {
                            int => "integer", double => "double", DateTime => "datetime", bool => "boolean",
                            _ => "string"
                        },
                        Value = InvocationContextBuilder.FormatValue(p.Value),
                        Origin = "Computed"
                    }).ToList(),
                    RuntimeError = run.Error == null ? null : $"{run.Error.GetType().Name}: {run.Error.Message}",
                    TimedOut = run.TimedOut,
                    DurationMicroseconds = run.DurationMicroseconds,
                    EngineBehaviour = run.Error != null || (run.Compiled && !run.Succeeded)
                        ? "The engine catches this, logs it and adds none of the script's properties to the payload."
                        : null
                };
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
                log.Error($"EntityAnalysisModelInlineScript.Execute: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private static bool InlineScriptExecutionEnabled()
        {
            return string.Equals(Environment.GetEnvironmentVariable("EnableInlineScriptExecution"), "True",
                StringComparison.OrdinalIgnoreCase);
        }

        private static InlineScriptExecutionResultDto InlineScriptRefused(string errorCode, string message)
        {
            return new InlineScriptExecutionResultDto
            {
                Errors = [new() { PropertyName = "Id", ErrorCode = errorCode, Message = message }]
            };
        }

        [Description("Updates an existing Inline Script registration in the caller's tenant, identified by its " +
                     "Id. Idempotent -- repeating the same update has no further effect beyond incrementing " +
                     "Version.")]
        [ServiceOperation("EntityAnalysisModelInlineScriptUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<InlineScriptPoco> UpdateAsync(
            [Description(
                "The Inline Script registration to update. Id selects the row; identity/tenant/audit fields " +
                "are server-owned and ignored.")]
            EntityAnalysisModelInlineScriptDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineScript", "Update", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelInlineScript.Update: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "EntityAnalysisModelInlineScript.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"EntityAnalysisModelInlineScript.Update: validation failed id={model.Id} " +
                                 $"user={userName} props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                InlineScriptPoco saved;
                try
                {
                    saved = await repository.UpdateAsync(EntityAnalysisModelInlineScriptMapper.ToPoco(model), token)
                        .ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelInlineScript.Update: id={model.Id} not found, locked, deleted, or not visible to tenant user={userName}");
                    }

                    throw new NotFoundException("The Inline Script registration was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"EntityAnalysisModelInlineScript.Update: Id={saved.Id} version->{saved.Version} user={userName}");
                }

                return saved;
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
                op.Outcome("notfound");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelInlineScript.Update: cancelled id={model?.Id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelInlineScript.Update: unexpected failure id={model?.Id} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Deletes an Inline Script registration in the caller's tenant by its Id. Reversible at the " +
                     "data level, but treat as destructive -- the Inline Script immediately stops being invoked " +
                     "via the API.")]
        [ServiceOperation("EntityAnalysisModelInlineScriptDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Numeric identifier of the Inline Script registration to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineScript", "Delete", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelInlineScript.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted(writePermissions, "EntityAnalysisModelInlineScript.Delete");

                try
                {
                    var existing = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                    if (existing != null)
                    {
                        var dependents = await deleteValidator
                            .ValidateAsync(
                                new ModelEntityDelete(ModelEntityKind.InlineScriptProperty, id,
                                    existing.EntityAnalysisModelId),
                                token)
                            .ConfigureAwait(false);
                        if (!dependents.IsValid)
                        {
                            throw new DtoValidationException(dependents);
                        }
                    }

                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelInlineScript.Delete: id={id} not found, locked, already deleted, or not visible to tenant user={userName}");
                    }

                    throw new NotFoundException("The Inline Script registration was not found.", ex);
                }

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"EntityAnalysisModelInlineScript.Delete: soft-deleted Id={id} user={userName}");
                }
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
                op.Outcome("notfound");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelInlineScript.Delete: cancelled id={id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelInlineScript.Delete: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        private void EnsurePermitted(int[] specs, string op)
        {
            if (permissionValidation.Validate(specs))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", specs)}]");
            }

            throw new ForbiddenException(strings[EntityAnalysisModelInlineScriptResources.PermissionDenied], specs);
        }
    }
}