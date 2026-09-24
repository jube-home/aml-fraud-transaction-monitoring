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
using FluentValidation;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Parser;
using Jube.Dto.Filter;
using Jube.Dto.RuleExecution;
using Jube.Dto.Query.EntityAnalysisModelInvocationContext;
using Jube.Dto.Validation;
using Jube.Dto.EntityAnalysisModelInlineFunction;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.EntityAnalysisModelInlineFunction;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Parser.Dependency;
using Jube.Validations.Dependency;
using Jube.Validations.EntityAnalysisModelInlineFunction;
using Jube.Validations.RuleScript;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.EntityAnalysisModelInlineFunction
{
    using InlineFunctionPoco = Data.Poco.EntityAnalysisModelInlineFunction;

    public sealed class EntityAnalysisModelInlineFunctionService
    {
        private const int MaxListTake = 200;
        private static readonly int[] listPermissions = [8];
        private static readonly int[] readPermissions = [8];
        private static readonly int[] writePermissions = [8];
        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelInlineFunctionRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly ModelEntityDeleteValidator deleteValidator;
        private readonly EntityAnalysisModelInlineFunctionDtoValidator validator;

        private EntityAnalysisModelInlineFunctionService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new EntityAnalysisModelInlineFunctionRepository(dbContext, userName);
            validator = new EntityAnalysisModelInlineFunctionDtoValidator(repository, strings,
                new EntityAnalysisModelRepository(dbContext, userName),
                new RuleScriptParser(dbContext, tenantRegistryId));
            deleteValidator = new ModelEntityDeleteValidator(dbContext, tenantRegistryId, userName,
                dependencyStrings);
        }

        public static Task<EntityAnalysisModelInlineFunctionService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelInlineFunctionService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelInlineFunctionResources));
            var dependencyStrings = stringLocalizerFactory.Create(typeof(ModelDependencyResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EntityAnalysisModelInlineFunction.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelInlineFunctionResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"EntityAnalysisModelInlineFunction.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelInlineFunctionResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelInlineFunctionService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, dependencyStrings);
        }

        [Description("Lists every Inline Function visible to the calling user's tenant. Unbounded -- intended for " +
                     "the administrative page, not for agent tooling (use the bounded list operation instead).")]
        public async Task<List<EntityAnalysisModelInlineFunctionDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineFunction", "List", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelInlineFunction.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelInlineFunction.List");
                var dtos = EntityAnalysisModelInlineFunctionMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelInlineFunction.List: {dtos.Count} rows user={userName}");
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
                    log.Debug($"EntityAnalysisModelInlineFunction.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelInlineFunction.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Inline Functions belonging to the given Model, ordered by Id, scoped to the calling " +
                     "user's tenant.")]
        [ServiceOperation("EntityAnalysisModelInlineFunctionGetByEntityAnalysisModelId", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<EntityAnalysisModelInlineFunctionDto>> GetByEntityAnalysisModelIdAsync(
            [Description("Numeric identifier of the parent Model.")]
            int entityAnalysisModelId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineFunction", "ListByEntityAnalysisModelId",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelInlineFunction.ListByEntityAnalysisModelId: entry entityAnalysisModelId={entityAnalysisModelId} user={userName}");
            }

            try
            {
                EnsurePermitted(readPermissions, "EntityAnalysisModelInlineFunction.ListByEntityAnalysisModelId");
                var dtos = EntityAnalysisModelInlineFunctionMapper.ToDto(await repository
                    .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"EntityAnalysisModelInlineFunction.ListByEntityAnalysisModelId: {dtos.Count} rows entityAnalysisModelId={entityAnalysisModelId} user={userName}");
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
                        $"EntityAnalysisModelInlineFunction.ListByEntityAnalysisModelId: cancelled entityAnalysisModelId={entityAnalysisModelId} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelInlineFunction.ListByEntityAnalysisModelId: unexpected failure entityAnalysisModelId={entityAnalysisModelId} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Returns one Inline Function by its numeric identifier, scoped to the calling user's tenant. " +
                     "Returns null when the row does not exist or is not visible to the caller.")]
        [ServiceOperation("EntityAnalysisModelInlineFunctionGet", OperationKind.Read, Idempotent = true)]
        public async Task<EntityAnalysisModelInlineFunctionDto?> GetByIdAsync(
            [Description("Numeric identifier of the Inline Function.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineFunction", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelInlineFunction.Get: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted(readPermissions, "EntityAnalysisModelInlineFunction.Get");
                var inlineFunction = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (inlineFunction == null)
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug(
                            $"EntityAnalysisModelInlineFunction.Get: id={id} not found or not visible to tenant user={userName}");
                    }

                    return null;
                }

                op.Entity(inlineFunction.Id);
                return EntityAnalysisModelInlineFunctionMapper.ToDto(inlineFunction);
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
                    log.Debug($"EntityAnalysisModelInlineFunction.Get: cancelled id={id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelInlineFunction.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Inline Functions for the caller's tenant, ordered by id, capped at 'take' rows " +
                     "(max 200). If 'more' is true, call again with 'afterId' set to the last returned Id to " +
                     "continue.")]
        [ServiceOperation("EntityAnalysisModelInlineFunctionList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<EntityAnalysisModelInlineFunctionDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineFunction", "ListPaged", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelInlineFunction.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelInlineFunction.ListPaged");

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);

                return new PagedResult<EntityAnalysisModelInlineFunctionDto>(
                    EntityAnalysisModelInlineFunctionMapper.ToDto(page));
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
                    log.Debug($"EntityAnalysisModelInlineFunction.ListPaged: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelInlineFunction.ListPaged: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the fields a query builder JSON filter over Inline Functions may " +
                     "use, with each field's type, the operators allowed for it and what it " +
                     "means. Use them as rule ids in EntityAnalysisModelInlineFunctionFilter " +
                     "and EntityAnalysisModelInlineFunctionCount.")]
        [ServiceOperation("EntityAnalysisModelInlineFunctionFilterFields", OperationKind.Read, Idempotent = true)]
        public async Task<List<FilterFieldDto>> FilterFieldsAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineFunction", "FilterFields", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelInlineFunction.FilterFields: entry user={userName}");
            }

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelInlineFunction.FilterFields");
                await Task.CompletedTask.ConfigureAwait(false);
                var result = DtoFilter.Fields<EntityAnalysisModelInlineFunctionDto>();
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
                log.Error($"EntityAnalysisModelInlineFunction.FilterFields: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns the Inline Functions in the caller's tenant matching query " +
                     "builder JSON (the same format as the rule builder, over the fields from " +
                     "EntityAnalysisModelInlineFunctionFilterFields), ordered by id and capped " +
                     "at 'take' rows (max 200). If 'more' is true, call again with 'afterId' " +
                     "set to the last returned Id to continue. Invalid JSON is not an error: " +
                     "Valid is false and Errors gives each problem with its JSON path.")]
        [ServiceOperation("EntityAnalysisModelInlineFunctionFilter", OperationKind.Read, Idempotent = true)]
        public async Task<FilterResultDto<EntityAnalysisModelInlineFunctionDto>> FilterAsync(
            [Description(
                "Query builder JSON selecting the Inline Functions, using the fields from EntityAnalysisModelInlineFunctionFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineFunction", "Filter", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelInlineFunction.Filter: entry take={take} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelInlineFunction.Filter");
                var rows = EntityAnalysisModelInlineFunctionMapper.ToDto(await repository.GetAsync(token)
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
                log.Error($"EntityAnalysisModelInlineFunction.Filter: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Counts the Inline Functions in the caller's tenant matching query builder " +
                     "JSON (over the fields from EntityAnalysisModelInlineFunctionFilterFields; " +
                     "empty counts all), optionally broken down by the values of one field. " +
                     "Invalid JSON is not an error: Valid is false and Errors gives each " +
                     "problem with its JSON path.")]
        [ServiceOperation("EntityAnalysisModelInlineFunctionCount", OperationKind.Read, Idempotent = true)]
        public async Task<FilterCountResultDto> CountAsync(
            [Description(
                "Query builder JSON selecting the Inline Functions, using the fields from EntityAnalysisModelInlineFunctionFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description(
                "A field from EntityAnalysisModelInlineFunctionFilterFields to count the matching rows by, e.g. Active; empty for a single total.")]
            string? groupBy = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineFunction", "Count", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelInlineFunction.Count: entry groupBy={groupBy} user={userName}");
            }

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelInlineFunction.Count");
                var rows = EntityAnalysisModelInlineFunctionMapper.ToDto(await repository.GetAsync(token)
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
                log.Error($"EntityAnalysisModelInlineFunction.Count: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Creates a new Inline Function under a Model in the caller's tenant. Not idempotent -- " +
                     "calling twice creates two rows.")]
        [ServiceOperation("EntityAnalysisModelInlineFunctionCreate", OperationKind.Write, Idempotent = false)]
        public async Task<InlineFunctionPoco> InsertAsync(
            [Description("The Inline Function to create.")]
            EntityAnalysisModelInlineFunctionDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineFunction", "Create", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelInlineFunction.Create: entry user={userName} name={model?.Name}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "EntityAnalysisModelInlineFunction.Create");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"EntityAnalysisModelInlineFunction.Create: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.InsertAsync(EntityAnalysisModelInlineFunctionMapper.ToPoco(model), token)
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"EntityAnalysisModelInlineFunction.Create: created Id={saved.Id} name={saved.Name} " +
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
                    log.Debug($"EntityAnalysisModelInlineFunction.Create: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelInlineFunction.Create: unexpected failure user={userName} " +
                          $"name={model?.Name}", ex);
                throw;
            }
        }

        [Description("Validates an Inline Function without saving it, running every check a create (Id 0) or " +
                     "an update (any other Id) would run, and returns each failure. Nothing is stored or " +
                     "changed.")]
        [ServiceOperation("EntityAnalysisModelInlineFunctionValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The Inline Function to validate.")]
            EntityAnalysisModelInlineFunctionDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineFunction", "Validate", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelInlineFunction.Validate: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "EntityAnalysisModelInlineFunction.Validate");

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
                log.Error($"EntityAnalysisModelInlineFunction.Validate: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Parses and compiles the rule text of an Inline Function against its Entity Analysis Model " +
                     "as the engine would, without saving anything, and returns each error with its line " +
                     "and position in the rule text. Cheaper than a full validation; use it to iterate on " +
                     "rule text.")]
        [ServiceOperation("EntityAnalysisModelInlineFunctionParseRule", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ParseRuleAsync(
            [Description(
                "The Inline Function whose rule text is parsed; only the model id, the rule script type and the " +
                "rule text are read.")]
            EntityAnalysisModelInlineFunctionDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineFunction", "ParseRule", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelInlineFunction.ParseRule: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "EntityAnalysisModelInlineFunction.ParseRule");

                var results = await validator.ValidateAsync(model,
                    o => o.IncludeRuleSets(RuleScriptParser.RuleSetName), token).ConfigureAwait(false);
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
                log.Error($"EntityAnalysisModelInlineFunction.ParseRule: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Runs an Inline Function against an invocation context exactly as the engine would compile and " +
                     "call it, without saving the rule or storing anything, and returns the result, any runtime " +
                     "error, how long it took and which names it read, flagging any the context leaves unset. " +
                     "Build the context with the EntityAnalysisModelInvocationContext operations.")]
        [ServiceOperation("EntityAnalysisModelInlineFunctionExecute", OperationKind.Read, Idempotent = true)]
        public async Task<RuleExecutionResultDto> ExecuteAsync(
            [Description(
                "The Inline Function to run; only the model id, the rule script type and the rule text are read.")]
            EntityAnalysisModelInlineFunctionDto? model,
            [Description("The invocation context to run it against, built for the same model.")]
            InvocationContextDto? context,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineFunction", "Execute", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelInlineFunction.Execute: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                ArgumentNullException.ThrowIfNull(context);
                EnsurePermitted(writePermissions, "EntityAnalysisModelInlineFunction.Execute");

                var result = await RuleExecutor.ExecuteAsync(dbContext, tenantRegistryId,
                    model.EntityAnalysisModelId, RuleParse.InlineFunction,
                    model.FunctionScript,
                    "FunctionScript",
                    null, false, context, token).ConfigureAwait(false);
                op.Rows(result.Errors.Count);
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
                log.Error($"EntityAnalysisModelInlineFunction.Execute: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Updates an existing Inline Function in the caller's tenant, identified by its Id. " +
                     "Idempotent -- repeating the same update has no further effect beyond incrementing Version.")]
        [ServiceOperation("EntityAnalysisModelInlineFunctionUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<InlineFunctionPoco> UpdateAsync(
            [Description(
                "The Inline Function to update. Id selects the row; identity/tenant/audit fields are " +
                "server-owned and ignored.")]
            EntityAnalysisModelInlineFunctionDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineFunction", "Update", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelInlineFunction.Update: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "EntityAnalysisModelInlineFunction.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"EntityAnalysisModelInlineFunction.Update: validation failed id={model.Id} " +
                                 $"user={userName} props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                InlineFunctionPoco saved;
                try
                {
                    saved = await repository.UpdateAsync(EntityAnalysisModelInlineFunctionMapper.ToPoco(model), token)
                        .ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelInlineFunction.Update: id={model.Id} not found, locked, deleted, or not visible to tenant user={userName}");
                    }

                    throw new NotFoundException("The Inline Function was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"EntityAnalysisModelInlineFunction.Update: Id={saved.Id} version->{saved.Version} user={userName}");
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
                    log.Debug($"EntityAnalysisModelInlineFunction.Update: cancelled id={model?.Id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelInlineFunction.Update: unexpected failure id={model?.Id} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Deletes an Inline Function in the caller's tenant by its Id. Reversible at the data level, " +
                     "but treat as destructive -- the field immediately stops being evaluated via the API.")]
        [ServiceOperation("EntityAnalysisModelInlineFunctionDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Numeric identifier of the Inline Function to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelInlineFunction", "Delete", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelInlineFunction.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted(writePermissions, "EntityAnalysisModelInlineFunction.Delete");

                try
                {
                    var existing = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                    if (existing != null)
                    {
                        var dependents = await deleteValidator
                            .ValidateAsync(
                                new ModelEntityDelete(ModelEntityKind.InlineFunction, id,
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
                            $"EntityAnalysisModelInlineFunction.Delete: id={id} not found, locked, already deleted, or not visible to tenant user={userName}");
                    }

                    throw new NotFoundException("The Inline Function was not found.", ex);
                }

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"EntityAnalysisModelInlineFunction.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Debug($"EntityAnalysisModelInlineFunction.Delete: cancelled id={id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelInlineFunction.Delete: unexpected failure id={id} user={userName}", ex);
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

            throw new ForbiddenException(strings[EntityAnalysisModelInlineFunctionResources.PermissionDenied], specs);
        }
    }
}