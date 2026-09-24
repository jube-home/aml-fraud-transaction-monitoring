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
using Jube.Dto.EntityAnalysisModelAbstractionCalculation;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.EntityAnalysisModelAbstractionCalculation;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Parser.Dependency;
using Jube.Validations.Dependency;
using Jube.Validations.EntityAnalysisModelAbstractionCalculation;
using Jube.Validations.RuleScript;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.EntityAnalysisModelAbstractionCalculation
{
    using AbstractionCalculationPoco = Data.Poco.EntityAnalysisModelAbstractionCalculation;

    public sealed class EntityAnalysisModelAbstractionCalculationService
    {
        private const int MaxListTake = 200;
        private static readonly int[] listPermissions = [14];
        private static readonly int[] readPermissions = [14];
        private static readonly int[] writePermissions = [14];
        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelAbstractionCalculationRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly ModelEntityDeleteValidator deleteValidator;
        private readonly EntityAnalysisModelAbstractionCalculationDtoValidator validator;

        private EntityAnalysisModelAbstractionCalculationService(DbContext dbContext, string userName,
            int tenantRegistryId, PermissionValidation permissionValidation, ILog log, ILog auditLog,
            IServiceChangeBus serviceChangeBus, IStringLocalizer strings, IStringLocalizer dependencyStrings)
        {
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            repository = new EntityAnalysisModelAbstractionCalculationRepository(dbContext, userName);
            validator = new EntityAnalysisModelAbstractionCalculationDtoValidator(repository, strings,
                new RuleScriptParser(dbContext, tenantRegistryId));
            deleteValidator = new ModelEntityDeleteValidator(dbContext, tenantRegistryId, userName,
                dependencyStrings);
        }

        public static Task<EntityAnalysisModelAbstractionCalculationService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelAbstractionCalculationService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelAbstractionCalculationResources));
            var dependencyStrings = stringLocalizerFactory.Create(typeof(ModelDependencyResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EntityAnalysisModelAbstractionCalculation.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelAbstractionCalculationResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"EntityAnalysisModelAbstractionCalculation.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelAbstractionCalculationResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelAbstractionCalculationService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings,
                dependencyStrings);
        }

        [Description("Lists every Abstraction Calculation visible to the calling user's tenant. Unbounded -- " +
                     "intended for the administrative page, not for agent tooling (use the bounded list " +
                     "operation instead).")]
        public async Task<List<EntityAnalysisModelAbstractionCalculationDto>> GetAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelAbstractionCalculation", "List", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelAbstractionCalculation.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelAbstractionCalculation.List");
                var dtos = EntityAnalysisModelAbstractionCalculationMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"EntityAnalysisModelAbstractionCalculation.List: {dtos.Count} rows user={userName}");
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
                    log.Debug($"EntityAnalysisModelAbstractionCalculation.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelAbstractionCalculation.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Abstraction Calculations belonging to the given Model, ordered by Id, scoped to the " +
                     "calling user's tenant.")]
        [ServiceOperation("EntityAnalysisModelAbstractionCalculationGetByEntityAnalysisModelId", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<EntityAnalysisModelAbstractionCalculationDto>> GetByEntityAnalysisModelIdAsync(
            [Description("Numeric identifier of the parent Model.")]
            int entityAnalysisModelId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelAbstractionCalculation",
                "ListByEntityAnalysisModelId", userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelAbstractionCalculation.ListByEntityAnalysisModelId: entry entityAnalysisModelId={entityAnalysisModelId} user={userName}");
            }

            try
            {
                EnsurePermitted(readPermissions,
                    "EntityAnalysisModelAbstractionCalculation.ListByEntityAnalysisModelId");
                var dtos = EntityAnalysisModelAbstractionCalculationMapper.ToDto(await repository
                    .GetByEntityAnalysisModelIdOrderByIdDescAsync(entityAnalysisModelId, token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"EntityAnalysisModelAbstractionCalculation.ListByEntityAnalysisModelId: {dtos.Count} rows entityAnalysisModelId={entityAnalysisModelId} user={userName}");
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
                        $"EntityAnalysisModelAbstractionCalculation.ListByEntityAnalysisModelId: cancelled entityAnalysisModelId={entityAnalysisModelId} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelAbstractionCalculation.ListByEntityAnalysisModelId: unexpected failure entityAnalysisModelId={entityAnalysisModelId} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Returns one Abstraction Calculation by its numeric identifier, scoped to the calling " +
                     "user's tenant. Returns null when the row does not exist or is not visible to the caller.")]
        [ServiceOperation("EntityAnalysisModelAbstractionCalculationGet", OperationKind.Read, Idempotent = true)]
        public async Task<EntityAnalysisModelAbstractionCalculationDto?> GetByIdAsync(
            [Description("Numeric identifier of the Abstraction Calculation.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelAbstractionCalculation", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelAbstractionCalculation.Get: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted(readPermissions, "EntityAnalysisModelAbstractionCalculation.Get");
                var abstractionCalculation = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (abstractionCalculation == null)
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug(
                            $"EntityAnalysisModelAbstractionCalculation.Get: id={id} not found or not visible to tenant user={userName}");
                    }

                    return null;
                }

                op.Entity(abstractionCalculation.Id);
                return EntityAnalysisModelAbstractionCalculationMapper.ToDto(abstractionCalculation);
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
                    log.Debug($"EntityAnalysisModelAbstractionCalculation.Get: cancelled id={id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelAbstractionCalculation.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Abstraction Calculations for the caller's tenant, ordered by id, capped at 'take' " +
                     "rows (max 200). If 'more' is true, call again with 'afterId' set to the last returned Id " +
                     "to continue.")]
        [ServiceOperation("EntityAnalysisModelAbstractionCalculationList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<EntityAnalysisModelAbstractionCalculationDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelAbstractionCalculation", "ListPaged", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelAbstractionCalculation.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelAbstractionCalculation.ListPaged");

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);

                return new PagedResult<EntityAnalysisModelAbstractionCalculationDto>(
                    EntityAnalysisModelAbstractionCalculationMapper.ToDto(page));
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
                    log.Debug($"EntityAnalysisModelAbstractionCalculation.ListPaged: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelAbstractionCalculation.ListPaged: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Lists the fields a query builder JSON filter over Abstraction " +
                     "Calculations may use, with each field's type, the operators allowed for " +
                     "it and what it means. Use them as rule ids in " +
                     "EntityAnalysisModelAbstractionCalculationFilter and " +
                     "EntityAnalysisModelAbstractionCalculationCount.")]
        [ServiceOperation("EntityAnalysisModelAbstractionCalculationFilterFields", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<FilterFieldDto>> FilterFieldsAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelAbstractionCalculation", "FilterFields", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelAbstractionCalculation.FilterFields: entry user={userName}");
            }

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelAbstractionCalculation.FilterFields");
                await Task.CompletedTask.ConfigureAwait(false);
                var result = DtoFilter.Fields<EntityAnalysisModelAbstractionCalculationDto>();
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
                log.Error($"EntityAnalysisModelAbstractionCalculation.FilterFields: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Returns the Abstraction Calculations in the caller's tenant matching " +
                     "query builder JSON (the same format as the rule builder, over the fields " +
                     "from EntityAnalysisModelAbstractionCalculationFilterFields), ordered by " +
                     "id and capped at 'take' rows (max 200). If 'more' is true, call again " +
                     "with 'afterId' set to the last returned Id to continue. Invalid JSON is " +
                     "not an error: Valid is false and Errors gives each problem with its JSON " +
                     "path.")]
        [ServiceOperation("EntityAnalysisModelAbstractionCalculationFilter", OperationKind.Read, Idempotent = true)]
        public async Task<FilterResultDto<EntityAnalysisModelAbstractionCalculationDto>> FilterAsync(
            [Description(
                "Query builder JSON selecting the Abstraction Calculations, using the fields from EntityAnalysisModelAbstractionCalculationFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelAbstractionCalculation", "Filter", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelAbstractionCalculation.Filter: entry take={take} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelAbstractionCalculation.Filter");
                var rows = EntityAnalysisModelAbstractionCalculationMapper.ToDto(await repository.GetAsync(token)
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
                log.Error($"EntityAnalysisModelAbstractionCalculation.Filter: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Counts the Abstraction Calculations in the caller's tenant matching query " +
                     "builder JSON (over the fields from " +
                     "EntityAnalysisModelAbstractionCalculationFilterFields; empty counts all), " +
                     "optionally broken down by the values of one field. Invalid JSON is not an " +
                     "error: Valid is false and Errors gives each problem with its JSON path.")]
        [ServiceOperation("EntityAnalysisModelAbstractionCalculationCount", OperationKind.Read, Idempotent = true)]
        public async Task<FilterCountResultDto> CountAsync(
            [Description(
                "Query builder JSON selecting the Abstraction Calculations, using the fields from EntityAnalysisModelAbstractionCalculationFilterFields; empty selects all.")]
            string? builderJson = null,
            [Description(
                "A field from EntityAnalysisModelAbstractionCalculationFilterFields to count the matching rows by, e.g. Active; empty for a single total.")]
            string? groupBy = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelAbstractionCalculation", "Count", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelAbstractionCalculation.Count: entry groupBy={groupBy} user={userName}");
            }

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelAbstractionCalculation.Count");
                var rows = EntityAnalysisModelAbstractionCalculationMapper.ToDto(await repository.GetAsync(token)
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
                log.Error($"EntityAnalysisModelAbstractionCalculation.Count: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Registers a new Abstraction Calculation under a Model in the caller's tenant. Not " +
                     "idempotent -- calling twice creates two rows.")]
        [ServiceOperation("EntityAnalysisModelAbstractionCalculationCreate", OperationKind.Write, Idempotent = false)]
        public async Task<AbstractionCalculationPoco> InsertAsync(
            [Description("The Abstraction Calculation to create.")]
            EntityAnalysisModelAbstractionCalculationDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelAbstractionCalculation", "Create", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"EntityAnalysisModelAbstractionCalculation.Create: entry user={userName} name={model?.Name}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                model.Id = 0;
                EnsurePermitted(writePermissions, "EntityAnalysisModelAbstractionCalculation.Create");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelAbstractionCalculation.Create: validation failed user={userName} " +
                            $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var saved = await repository
                    .InsertAsync(EntityAnalysisModelAbstractionCalculationMapper.ToPoco(model), token)
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"EntityAnalysisModelAbstractionCalculation.Create: created Id={saved.Id} name={saved.Name} " +
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
                    log.Debug($"EntityAnalysisModelAbstractionCalculation.Create: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelAbstractionCalculation.Create: unexpected failure user={userName} " +
                          $"name={model?.Name}", ex);
                throw;
            }
        }

        [Description("Validates an Abstraction Calculation without saving it, running every check a create (Id " +
                     "0) or an update (any other Id) would run, and returns each failure. Nothing is stored or " +
                     "changed.")]
        [ServiceOperation("EntityAnalysisModelAbstractionCalculationValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The Abstraction Calculation to validate.")]
            EntityAnalysisModelAbstractionCalculationDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelAbstractionCalculation", "Validate", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelAbstractionCalculation.Validate: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "EntityAnalysisModelAbstractionCalculation.Validate");

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
                log.Error($"EntityAnalysisModelAbstractionCalculation.Validate: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description(
            "Parses and compiles the rule text of an Abstraction Calculation against its Entity Analysis Model " +
            "as the engine would, without saving anything, and returns each error with its line " +
            "and position in the rule text. Cheaper than a full validation; use it to iterate on " +
            "rule text.")]
        [ServiceOperation("EntityAnalysisModelAbstractionCalculationParseRule", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ParseRuleAsync(
            [Description(
                "The Abstraction Calculation whose rule text is parsed; only the model id, the rule script type and the " +
                "rule text are read.")]
            EntityAnalysisModelAbstractionCalculationDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelAbstractionCalculation", "ParseRule", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelAbstractionCalculation.ParseRule: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "EntityAnalysisModelAbstractionCalculation.ParseRule");

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
                log.Error($"EntityAnalysisModelAbstractionCalculation.ParseRule: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description(
            "Runs an Abstraction Calculation against an invocation context exactly as the engine would compile and " +
            "call it, without saving the rule or storing anything, and returns the result, any runtime " +
            "error, how long it took and which names it read, flagging any the context leaves unset. " +
            "Build the context with the EntityAnalysisModelInvocationContext operations.")]
        [ServiceOperation("EntityAnalysisModelAbstractionCalculationExecute", OperationKind.Read, Idempotent = true)]
        public async Task<RuleExecutionResultDto> ExecuteAsync(
            [Description(
                "The Abstraction Calculation to run; only the model id, the rule script type and the rule text are read.")]
            EntityAnalysisModelAbstractionCalculationDto? model,
            [Description("The invocation context to run it against, built for the same model.")]
            InvocationContextDto? context,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelAbstractionCalculation", "Execute", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelAbstractionCalculation.Execute: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                ArgumentNullException.ThrowIfNull(context);
                EnsurePermitted(writePermissions, "EntityAnalysisModelAbstractionCalculation.Execute");

                var result = await RuleExecutor.ExecuteAsync(dbContext, tenantRegistryId,
                    model.EntityAnalysisModelId, RuleParse.AbstractionCalculation,
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
                log.Error($"EntityAnalysisModelAbstractionCalculation.Execute: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Updates an existing Abstraction Calculation in the caller's tenant, identified by its Id. " +
                     "Idempotent -- repeating the same update has no further effect beyond incrementing Version.")]
        [ServiceOperation("EntityAnalysisModelAbstractionCalculationUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<AbstractionCalculationPoco> UpdateAsync(
            [Description("The Abstraction Calculation to update. Id selects the row; identity/tenant/audit " +
                         "fields are server-owned and ignored.")]
            EntityAnalysisModelAbstractionCalculationDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelAbstractionCalculation", "Update", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelAbstractionCalculation.Update: entry id={model?.Id} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "EntityAnalysisModelAbstractionCalculation.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelAbstractionCalculation.Update: validation failed id={model.Id} " +
                            $"user={userName} props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                AbstractionCalculationPoco saved;
                try
                {
                    saved = await repository
                        .UpdateAsync(EntityAnalysisModelAbstractionCalculationMapper.ToPoco(model), token)
                        .ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"EntityAnalysisModelAbstractionCalculation.Update: id={model.Id} not found, locked, deleted, or not visible to tenant user={userName}");
                    }

                    throw new NotFoundException("The Abstraction Calculation was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"EntityAnalysisModelAbstractionCalculation.Update: Id={saved.Id} version->{saved.Version} user={userName}");
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
                    log.Debug(
                        $"EntityAnalysisModelAbstractionCalculation.Update: cancelled id={model?.Id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelAbstractionCalculation.Update: unexpected failure id={model?.Id} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Deletes an Abstraction Calculation in the caller's tenant by its Id. Reversible at the " +
                     "data level, but treat as destructive -- the Abstraction Calculation immediately stops " +
                     "being evaluated on transaction invocation.")]
        [ServiceOperation("EntityAnalysisModelAbstractionCalculationDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Numeric identifier of the Abstraction Calculation to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelAbstractionCalculation", "Delete", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelAbstractionCalculation.Delete: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted(writePermissions, "EntityAnalysisModelAbstractionCalculation.Delete");

                try
                {
                    var existing = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                    if (existing != null)
                    {
                        var dependents = await deleteValidator
                            .ValidateAsync(
                                new ModelEntityDelete(ModelEntityKind.AbstractionCalculation, id,
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
                            $"EntityAnalysisModelAbstractionCalculation.Delete: id={id} not found, locked, already deleted, or not visible to tenant user={userName}");
                    }

                    throw new NotFoundException("The Abstraction Calculation was not found.", ex);
                }

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"EntityAnalysisModelAbstractionCalculation.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Debug($"EntityAnalysisModelAbstractionCalculation.Delete: cancelled id={id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelAbstractionCalculation.Delete: unexpected failure id={id} user={userName}",
                    ex);
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

            throw new ForbiddenException(
                strings[EntityAnalysisModelAbstractionCalculationResources.PermissionDenied], specs);
        }
    }
}