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
using Jube.Dto.EntityAnalysisModelGatewayRule;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.EntityAnalysisModelGatewayRule;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.EntityAnalysisModelGatewayRule;
using Jube.Validations.RuleScript;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.EntityAnalysisModelGatewayRule
{
    using GatewayRulePoco = Data.Poco.EntityAnalysisModelGatewayRule;

    public sealed class EntityAnalysisModelGatewayRuleService
    {
        private const int MaxListTake = 200;
        private static readonly int[] listPermissions = [10];
        private static readonly int[] readPermissions = [10];
        private static readonly int[] writePermissions = [10];
        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelGatewayRuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly EntityAnalysisModelGatewayRuleDtoValidator validator;

        private EntityAnalysisModelGatewayRuleService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings)
        {
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            repository = new EntityAnalysisModelGatewayRuleRepository(dbContext, userName);
            validator = new EntityAnalysisModelGatewayRuleDtoValidator(repository, strings,
                new RuleScriptParser(dbContext, tenantRegistryId));
        }

        public static Task<EntityAnalysisModelGatewayRuleService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelGatewayRuleService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelGatewayRuleResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EntityAnalysisModelGatewayRule.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelGatewayRuleResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"EntityAnalysisModelGatewayRule.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelGatewayRuleResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelGatewayRuleService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Gateway Rule visible to the calling user's tenant. Unbounded -- intended for " +
                     "the administrative page, not for agent tooling (use the bounded list operation instead).")]
        public async Task<List<EntityAnalysisModelGatewayRuleDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGatewayRule", "List", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelGatewayRule.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelGatewayRule.List");
                var dtos = EntityAnalysisModelGatewayRuleMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelGatewayRule.List: {dtos.Count} rows user={userName}");
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
                    log.Debug($"EntityAnalysisModelGatewayRule.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelGatewayRule.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Gateway Rules belonging to the given Model, ordered by Id, scoped to the calling " +
                     "user's tenant.")]
        [ServiceOperation("EntityAnalysisModelGatewayRuleGetByEntityAnalysisModelId", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<EntityAnalysisModelGatewayRuleDto>> GetByEntityAnalysisModelIdAsync(
            [Description("Numeric identifier of the parent Model.")]
            int entityAnalysisModelId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGatewayRule", "ListByEntityAnalysisModelId",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelGatewayRule.ListByEntityAnalysisModelId: entry entityAnalysisModelId={entityAnalysisModelId} user={userName}");
            }

            try
            {
                EnsurePermitted(readPermissions, "EntityAnalysisModelGatewayRule.ListByEntityAnalysisModelId");
                var dtos = EntityAnalysisModelGatewayRuleMapper.ToDto(await repository
                    .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"EntityAnalysisModelGatewayRule.ListByEntityAnalysisModelId: {dtos.Count} rows entityAnalysisModelId={entityAnalysisModelId} user={userName}");
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
                        $"EntityAnalysisModelGatewayRule.ListByEntityAnalysisModelId: cancelled entityAnalysisModelId={entityAnalysisModelId} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelGatewayRule.ListByEntityAnalysisModelId: unexpected failure entityAnalysisModelId={entityAnalysisModelId} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Returns one Gateway Rule by its numeric identifier, scoped to the calling user's tenant. " +
                     "Returns null when the row does not exist or is not visible to the caller.")]
        [ServiceOperation("EntityAnalysisModelGatewayRuleGet", OperationKind.Read, Idempotent = true)]
        public async Task<EntityAnalysisModelGatewayRuleDto?> GetByIdAsync(
            [Description("Numeric identifier of the Gateway Rule.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGatewayRule", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelGatewayRule.Get: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted(readPermissions, "EntityAnalysisModelGatewayRule.Get");
                var gatewayRule = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (gatewayRule == null)
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug(
                            $"EntityAnalysisModelGatewayRule.Get: id={id} not found or not visible to tenant user={userName}");
                    }

                    return null;
                }

                op.Entity(gatewayRule.Id);
                return EntityAnalysisModelGatewayRuleMapper.ToDto(gatewayRule);
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
                    log.Debug($"EntityAnalysisModelGatewayRule.Get: cancelled id={id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelGatewayRule.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Gateway Rules for the caller's tenant, ordered by id, capped at 'take' rows (max " +
                     "200). If 'more' is true, call again with 'afterId' set to the last returned Id to continue.")]
        [ServiceOperation("EntityAnalysisModelGatewayRuleList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<EntityAnalysisModelGatewayRuleDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGatewayRule", "ListPaged", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelGatewayRule.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelGatewayRule.ListPaged");

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);

                return new PagedResult<EntityAnalysisModelGatewayRuleDto>(
                    EntityAnalysisModelGatewayRuleMapper.ToDto(page));
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
                    log.Debug($"EntityAnalysisModelGatewayRule.ListPaged: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelGatewayRule.ListPaged: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the fields a query builder JSON filter over Gateway Rules may use, " +
                     "with each field's type, the operators allowed for it and what it means. " +
                     "Use them as rule ids in EntityAnalysisModelGatewayRuleFilter and " +
                     "EntityAnalysisModelGatewayRuleCount.")]
        [ServiceOperation("EntityAnalysisModelGatewayRuleFilterFields", OperationKind.Read, Idempotent = true)]
        public async Task<List<FilterFieldDto>> FilterFieldsAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGatewayRule", "FilterFields", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelGatewayRule.FilterFields: entry user={userName}");
            }

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelGatewayRule.FilterFields");
                await Task.CompletedTask.ConfigureAwait(false);
                var result = DtoFilter.Fields<EntityAnalysisModelGatewayRuleDto>();
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
                log.Error($"EntityAnalysisModelGatewayRule.FilterFields: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns the Gateway Rules in the caller's tenant matching query builder " +
                     "JSON (the same format as the rule builder, over the fields from " +
                     "EntityAnalysisModelGatewayRuleFilterFields), ordered by id and capped at " +
                     "'take' rows (max 200). If 'more' is true, call again with 'afterId' set " +
                     "to the last returned Id to continue. Invalid JSON is not an error: Valid " +
                     "is false and Errors gives each problem with its JSON path.")]
        [ServiceOperation("EntityAnalysisModelGatewayRuleFilter", OperationKind.Read, Idempotent = true)]
        public async Task<FilterResultDto<EntityAnalysisModelGatewayRuleDto>> FilterAsync(
            [Description(
                "Query builder JSON selecting the Gateway Rules, using the fields from EntityAnalysisModelGatewayRuleFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGatewayRule", "Filter", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelGatewayRule.Filter: entry take={take} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelGatewayRule.Filter");
                var rows = EntityAnalysisModelGatewayRuleMapper.ToDto(await repository.GetAsync(token)
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
                log.Error($"EntityAnalysisModelGatewayRule.Filter: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Counts the Gateway Rules in the caller's tenant matching query builder " +
                     "JSON (over the fields from EntityAnalysisModelGatewayRuleFilterFields; " +
                     "empty counts all), optionally broken down by the values of one field. " +
                     "Invalid JSON is not an error: Valid is false and Errors gives each " +
                     "problem with its JSON path.")]
        [ServiceOperation("EntityAnalysisModelGatewayRuleCount", OperationKind.Read, Idempotent = true)]
        public async Task<FilterCountResultDto> CountAsync(
            [Description(
                "Query builder JSON selecting the Gateway Rules, using the fields from EntityAnalysisModelGatewayRuleFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description(
                "A field from EntityAnalysisModelGatewayRuleFilterFields to count the matching rows by, e.g. Active; empty for a single total.")]
            string? groupBy = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGatewayRule", "Count", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelGatewayRule.Count: entry groupBy={groupBy} user={userName}");
            }

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelGatewayRule.Count");
                var rows = EntityAnalysisModelGatewayRuleMapper.ToDto(await repository.GetAsync(token)
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
                log.Error($"EntityAnalysisModelGatewayRule.Count: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Registers a new Gateway Rule under a Model in the caller's tenant. Not idempotent -- " +
                     "calling twice creates two rows.")]
        [ServiceOperation("EntityAnalysisModelGatewayRuleCreate", OperationKind.Write, Idempotent = false)]
        public async Task<GatewayRulePoco> InsertAsync(
            [Description("The Gateway Rule to create.")]
            EntityAnalysisModelGatewayRuleDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGatewayRule", "Create", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelGatewayRule.Create: entry user={userName} name={model?.Name}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                model.Id = 0;
                EnsurePermitted(writePermissions, "EntityAnalysisModelGatewayRule.Create");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"EntityAnalysisModelGatewayRule.Create: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository.InsertAsync(EntityAnalysisModelGatewayRuleMapper.ToPoco(model), token)
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"EntityAnalysisModelGatewayRule.Create: created Id={saved.Id} name={saved.Name} " +
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
                    log.Debug($"EntityAnalysisModelGatewayRule.Create: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelGatewayRule.Create: unexpected failure user={userName} " +
                          $"name={model?.Name}", ex);
                throw;
            }
        }

        [Description("Validates a Gateway Rule without saving it, running every check a create (Id 0) or an " +
                     "update (any other Id) would run, and returns each failure. Nothing is stored or changed.")]
        [ServiceOperation("EntityAnalysisModelGatewayRuleValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The Gateway Rule to validate.")]
            EntityAnalysisModelGatewayRuleDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGatewayRule", "Validate", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelGatewayRule.Validate: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "EntityAnalysisModelGatewayRule.Validate");

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
                log.Error($"EntityAnalysisModelGatewayRule.Validate: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Parses and compiles the rule text of a Gateway Rule against its Entity Analysis Model " +
                     "as the engine would, without saving anything, and returns each error with its line " +
                     "and position in the rule text. Cheaper than a full validation; use it to iterate on " +
                     "rule text.")]
        [ServiceOperation("EntityAnalysisModelGatewayRuleParseRule", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ParseRuleAsync(
            [Description(
                "The Gateway Rule whose rule text is parsed; only the model id, the rule script type and the " +
                "rule text are read.")]
            EntityAnalysisModelGatewayRuleDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGatewayRule", "ParseRule", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelGatewayRule.ParseRule: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "EntityAnalysisModelGatewayRule.ParseRule");

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
                log.Error($"EntityAnalysisModelGatewayRule.ParseRule: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Runs a Gateway Rule against an invocation context exactly as the engine would compile and " +
                     "call it, without saving the rule or storing anything, and returns the result, any runtime " +
                     "error, how long it took and which names it read, flagging any the context leaves unset. " +
                     "Build the context with the EntityAnalysisModelInvocationContext operations.")]
        [ServiceOperation("EntityAnalysisModelGatewayRuleExecute", OperationKind.Read, Idempotent = true)]
        public async Task<RuleExecutionResultDto> ExecuteAsync(
            [Description(
                "The Gateway Rule to run; only the model id, the rule script type and the rule text are read.")]
            EntityAnalysisModelGatewayRuleDto? model,
            [Description("The invocation context to run it against, built for the same model.")]
            InvocationContextDto? context,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGatewayRule", "Execute", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelGatewayRule.Execute: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                ArgumentNullException.ThrowIfNull(context);
                EnsurePermitted(writePermissions, "EntityAnalysisModelGatewayRule.Execute");

                var result = await RuleExecutor.ExecuteAsync(dbContext, tenantRegistryId,
                    model.EntityAnalysisModelId, RuleParse.GatewayRule,
                    model.RuleScriptTypeId == 1 ? model.BuilderRuleScript : model.CoderRuleScript,
                    model.RuleScriptTypeId == 1 ? "BuilderRuleScript" : "CoderRuleScript",
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
                log.Error($"EntityAnalysisModelGatewayRule.Execute: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Turns query builder JSON (the rule builder's own format: a group with condition AND or OR, " +
                     "an optional not, and rules of id, operator and value) into the rule text the browser's rule " +
                     "builder would produce for a Gateway Rule, and checks it parses and compiles against the model. " +
                     "Field ids are completion names from CompletionsGetByEntityAnalysisModelIdParseTypeId with " +
                     "parse type 2. Returns the rule text to save as BuilderRuleScript with the JSON as Json and " +
                     "RuleScriptTypeId 1, or each problem with its JSON path. Nothing is saved.")]
        [ServiceOperation("EntityAnalysisModelGatewayRuleBuildRuleFromBuilderJson", OperationKind.Read,
            Idempotent = true)]
        public async Task<BuilderRuleResultDto> BuildRuleFromBuilderJsonAsync(
            [Description("Id of the Entity Analysis Model the rule belongs to.")]
            int entityAnalysisModelId,
            [Description("The query builder JSON, e.g. {\"condition\":\"AND\",\"rules\":[{\"id\":\"Payload.Amount\"," +
                         "\"operator\":\"greater\",\"value\":100}]}.")]
            string? builderJson,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGatewayRule", "BuildRuleFromBuilderJson", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            try
            {
                EnsurePermitted(writePermissions, "EntityAnalysisModelGatewayRule.BuildRuleFromBuilderJson");
                var result = await BuilderRuleComposer.ComposeAsync(dbContext, tenantRegistryId,
                    entityAnalysisModelId, RuleParse.GatewayRule, builderJson, token).ConfigureAwait(false);
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
                log.Error(
                    $"EntityAnalysisModelGatewayRule.BuildRuleFromBuilderJson: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Updates an existing Gateway Rule in the caller's tenant, identified by its Id. Idempotent " +
                     "-- repeating the same update has no further effect beyond incrementing Version. Note: the " +
                     "counter fields are overwritten with whatever the caller supplies (or their defaults if " +
                     "omitted) -- see the migration report for this pre-existing quirk.")]
        [ServiceOperation("EntityAnalysisModelGatewayRuleUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<GatewayRulePoco> UpdateAsync(
            [Description("The Gateway Rule to update. Id selects the row; identity/tenant/audit fields are " +
                         "server-owned and ignored.")]
            EntityAnalysisModelGatewayRuleDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGatewayRule", "Update", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelGatewayRule.Update: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "EntityAnalysisModelGatewayRule.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"EntityAnalysisModelGatewayRule.Update: validation failed id={model.Id} " +
                                 $"user={userName} props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                GatewayRulePoco saved;
                try
                {
                    saved = await repository.UpdateAsync(EntityAnalysisModelGatewayRuleMapper.ToPoco(model), token)
                        .ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelGatewayRule.Update: id={model.Id} not found, locked, deleted, or not visible to tenant user={userName}");
                    }

                    throw new NotFoundException("The Gateway Rule was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"EntityAnalysisModelGatewayRule.Update: Id={saved.Id} version->{saved.Version} user={userName}");
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
                    log.Debug($"EntityAnalysisModelGatewayRule.Update: cancelled id={model?.Id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelGatewayRule.Update: unexpected failure id={model?.Id} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Deletes a Gateway Rule in the caller's tenant by its Id. Reversible at the data level, but " +
                     "treat as destructive -- the Gateway Rule immediately stops being evaluated on transaction " +
                     "invocation.")]
        [ServiceOperation("EntityAnalysisModelGatewayRuleDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Numeric identifier of the Gateway Rule to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGatewayRule", "Delete", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelGatewayRule.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted(writePermissions, "EntityAnalysisModelGatewayRule.Delete");

                try
                {
                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelGatewayRule.Delete: id={id} not found, locked, already deleted, or not visible to tenant user={userName}");
                    }

                    throw new NotFoundException("The Gateway Rule was not found.", ex);
                }

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"EntityAnalysisModelGatewayRule.Delete: soft-deleted Id={id} user={userName}");
                }
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
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelGatewayRule.Delete: cancelled id={id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelGatewayRule.Delete: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Resets the activation and evaluation counters for a Gateway Rule in the caller's tenant " +
                     "back to zero. Destructive to the counter history -- the running totals cannot be recovered.")]
        [ServiceOperation("EntityAnalysisModelGatewayRuleResetCounter", OperationKind.Write, Idempotent = true,
            Destructive = true)]
        public async Task ResetCounterAsync(
            [Description("Numeric identifier of the Gateway Rule whose counters should be reset.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelGatewayRule", "ResetCounter", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelGatewayRule.ResetCounter: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted(writePermissions, "EntityAnalysisModelGatewayRule.ResetCounter");

                try
                {
                    await repository.ResetCounterAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelGatewayRule.ResetCounter: id={id} not found, deleted, or not visible to tenant user={userName}");
                    }

                    throw new NotFoundException("The Gateway Rule was not found.", ex);
                }

                op.Entity(id);
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"EntityAnalysisModelGatewayRule.ResetCounter: reset counters Id={id} user={userName}");
                }
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
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelGatewayRule.ResetCounter: cancelled id={id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelGatewayRule.ResetCounter: unexpected failure id={id} user={userName}", ex);
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

            throw new ForbiddenException(strings[EntityAnalysisModelGatewayRuleResources.PermissionDenied], specs);
        }
    }
}