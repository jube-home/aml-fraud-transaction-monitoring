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
using FluentValidation.Results;
using Jube.Data.Concurrency;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Repository.CaseWorkflowChecklistItem;
using Jube.Dto.Validation;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Agent.Models;
using Jube.Service.DynamicSql;
using Jube.Service.Exceptions.Repository.CaseWorkflowChecklistItem;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseWorkflowFormValidator;
using Jube.Service.Repository.CaseWorkflowFormValidator.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.CaseWorkflowChecklistItem;
using log4net;
using Microsoft.Extensions.Localization;
using ItemRepository = Jube.Data.Repository.CaseWorkflowChecklistItemRepository;

namespace Jube.Service.Repository.CaseWorkflowChecklistItem
{
    public sealed class CaseWorkflowChecklistItemService
    {
        public const int ShowCondition = 1;
        public const int ValidateCondition = 2;

        private const string Area = "CaseWorkflowChecklistItem";
        private static readonly TimeSpan sqlTimeout = TimeSpan.FromSeconds(3);
        private static readonly int[] permissions = [19];
        private static readonly int[] sqlPermissions = [67];
        private readonly ILog auditLog;
        private readonly ChecklistItemSqlAuthoring authoring;
        private readonly CaseRepository caseRepository;
        private readonly DynamicEnvironment.DynamicEnvironment dynamicEnvironment;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly ItemRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly CaseWorkflowChecklistItemDtoValidator validator;

        private CaseWorkflowChecklistItemService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.dynamicEnvironment = dynamicEnvironment;
            authoring = new ChecklistItemSqlAuthoring(strings);
            repository = new ItemRepository(dbContext, userName);
            caseRepository = new CaseRepository(dbContext, userName);
            validator = new CaseWorkflowChecklistItemDtoValidator(repository, strings);
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseWorkflowChecklistItemService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                dynamicEnvironment, LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseWorkflowChecklistItemService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseWorkflowChecklistItemResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{Area}.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowChecklistItemResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{Area}.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowChecklistItemResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseWorkflowChecklistItemService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, dynamicEnvironment);
        }

        [Description("Lists every Case Workflow checklist item visible to the caller's tenant, ordered by priority.")]
        [ServiceOperation("CaseWorkflowChecklistItemList", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowChecklistItemDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted($"{Area}.List", permissions);

                var dtos = CaseWorkflowChecklistItemMapper.ToDto(await repository.GetAllAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);

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
                log.Error($"{Area}.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the checklist items of the statuses of a Case Workflow, visible to the caller's " +
                     "tenant, ordered by priority.")]
        [ServiceOperation("CaseWorkflowChecklistItemListByCaseWorkflowId", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowChecklistItemDto>> GetByCaseWorkflowIdAsync(
            [Description("Server-assigned identifier of the Case Workflow to list checklist items for.")]
            int caseWorkflowId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "ListByCaseWorkflowId", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted($"{Area}.ListByCaseWorkflowId", permissions);

                var dtos = CaseWorkflowChecklistItemMapper.ToDto(await repository
                    .GetByCaseWorkflowIdAsync(caseWorkflowId, token).ConfigureAwait(false));
                op.Rows(dtos.Count);

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
                log.Error($"{Area}.ListByCaseWorkflowId: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the items of one checklist, visible to the caller's tenant, ordered by priority.")]
        [ServiceOperation("CaseWorkflowChecklistItemListByCaseWorkflowChecklistGuid", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<CaseWorkflowChecklistItemDto>> GetByCaseWorkflowChecklistGuidAsync(
            [Description("Globally-unique identifier of the checklist to list the items of.")]
            Guid caseWorkflowChecklistGuid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "ListByCaseWorkflowChecklistGuid", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted($"{Area}.ListByCaseWorkflowChecklistGuid", permissions);

                var dtos = CaseWorkflowChecklistItemMapper.ToDto(await repository
                    .GetByCaseWorkflowChecklistGuidAsync(caseWorkflowChecklistGuid, token).ConfigureAwait(false));
                op.Rows(dtos.Count);

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
                log.Error($"{Area}.ListByCaseWorkflowChecklistGuid: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns a single Case Workflow checklist item by Id, or null if it doesn't exist or isn't " +
                     "visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowChecklistItemGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseWorkflowChecklistItemDto?> GetByIdAsync(
            [Description("Server-assigned identifier of the checklist item.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "Get", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted($"{Area}.Get", permissions);

                return CaseWorkflowChecklistItemMapper.ToDto(await repository.GetByIdAsync(id, token)
                    .ConfigureAwait(false));
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
                log.Error($"{Area}.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Creates a Case Workflow checklist item: a name and priority with an optional show condition " +
                     "(EnableShow and ShowSql), validation condition (EnableValidation and ValidateSql) and SLA in " +
                     "minutes. Any SQL, and switching a condition on, needs its own authoring permission.")]
        [ServiceOperation("CaseWorkflowChecklistItemCreate", OperationKind.Write, Idempotent = false)]
        public async Task<CaseWorkflowChecklistItemDto> InsertAsync(CaseWorkflowChecklistItemDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "Insert", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted($"{Area}.Insert", permissions);

                if (HasSql(model) || model.EnableShow || model.EnableValidation)
                {
                    EnsureSqlPermitted($"{Area}.Insert");
                }

                await ValidateOrThrowAsync(model, "Insert", token).ConfigureAwait(false);

                var saved = await repository.InsertAsync(CaseWorkflowChecklistItemMapper.ToPoco(model), token)
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"{Area}.Insert: created Id={saved.Id} user={userName}");
                }

                return CaseWorkflowChecklistItemMapper.ToDto(saved);
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
                log.Error($"{Area}.Insert: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Validates a Case Workflow checklist item without saving it, running every check a create " +
                     "(Id 0) or an update (any other Id) would run, and returns each failure. Nothing is stored.")]
        [ServiceOperation("CaseWorkflowChecklistItemValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The Case Workflow checklist item to validate.")]
            CaseWorkflowChecklistItemDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "Validate", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted($"{Area}.Validate", permissions);

                var results = await FullValidationAsync(model, token).ConfigureAwait(false);
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
                log.Error($"{Area}.Validate: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Updates an existing Case Workflow checklist item. The row must exist and be visible to the " +
                     "caller's tenant. Changing its show or validate SQL, or switching EnableShow or EnableValidation " +
                     "on, needs the SQL authoring permission; switching them off does not and retains the SQL.")]
        [ServiceOperation("CaseWorkflowChecklistItemUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<CaseWorkflowChecklistItemDto> UpdateAsync(CaseWorkflowChecklistItemDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "Update", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted($"{Area}.Update", permissions);

                var existing = await repository.GetByIdAsync(model.Id, token).ConfigureAwait(false);

                if (existing == null)
                {
                    throw new KeyNotFoundException();
                }

                if (SqlChanged(existing, model))
                {
                    EnsureSqlPermitted($"{Area}.Update");
                }
                else if (SwitchedOn(existing, model))
                {
                    EnsureSqlPermitted($"{Area}.Update", CaseWorkflowChecklistItemResources.SqlEnableNeedsPermission);
                }

                model.CaseWorkflowChecklistGuid = existing.CaseWorkflowChecklistGuid;
                model.CaseWorkflowId = existing.CaseWorkflowId;

                await ValidateOrThrowAsync(model, "Update", token).ConfigureAwait(false);

                var saved = await repository.UpdateAsync(CaseWorkflowChecklistItemMapper.ToPoco(model), token)
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"{Area}.Update: Id={saved.Id} version={saved.Version} user={userName}");
                }

                return CaseWorkflowChecklistItemMapper.ToDto(saved);
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
            catch (KeyNotFoundException)
            {
                op.Outcome("not_found");

                throw new NotFoundException(strings[CaseWorkflowChecklistItemResources.NotFound]);
            }
            catch (VersionConflictException)
            {
                op.Outcome("conflict");
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
                log.Error($"{Area}.Update: unexpected failure id={model?.Id} user={userName}", ex);
                throw;
            }
        }

        [Description("Soft-deletes a Case Workflow checklist item. Deleting an item that carries SQL needs the SQL " +
                     "authoring permission. Reversible only by direct database action.")]
        [ServiceOperation("CaseWorkflowChecklistItemDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the checklist item to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "Delete", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted($"{Area}.Delete", permissions);

                var existing = await repository.GetByIdAsync(id, token).ConfigureAwait(false);

                if (existing == null)
                {
                    throw new KeyNotFoundException();
                }

                if (!string.IsNullOrWhiteSpace(existing.ShowSql) || !string.IsNullOrWhiteSpace(existing.ValidateSql))
                {
                    EnsureSqlPermitted($"{Area}.Delete");
                }

                await repository.DeleteAsync(id, token).ConfigureAwait(false);
                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"{Area}.Delete: soft-deleted Id={id} user={userName}");
                }
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (KeyNotFoundException)
            {
                op.Outcome("not_found");

                throw new NotFoundException(strings[CaseWorkflowChecklistItemResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"{Area}.Delete: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Tries the show or validate SQL of a saved Case Workflow checklist item against a case, " +
                     "without ticking anything, and reports whether it passed, why not and how long it took. The " +
                     "SQL runs read only against the tenant's own rows with a short timeout. It runs whenever the " +
                     "condition has SQL, whether or not its Enable switch is on, so an author can try it before " +
                     "switching it on. A blank condition passes. Needs the SQL authoring permission when the " +
                     "condition has SQL.")]
        [ServiceOperation("CaseWorkflowChecklistItemTest", OperationKind.Read, Idempotent = true)]
        public async Task<CaseWorkflowChecklistItemTestResultDto> TestAsync(
            [Description("The checklist item, the condition (1 show, 2 validate) and an optional case.")]
            CaseWorkflowChecklistItemTestDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "Test", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted($"{Area}.Test", permissions);

                if (model.ConditionId is not (ShowCondition or ValidateCondition))
                {
                    throw new DtoValidationException(new ValidationResult([
                        new ValidationFailure(nameof(CaseWorkflowChecklistItemTestDto.ConditionId),
                            strings[CaseWorkflowChecklistItemResources.ConditionInvalid])
                            { ErrorCode = "ConditionInvalid" }
                    ]));
                }

                var existing = await repository.GetByIdAsync(model.Id, token).ConfigureAwait(false);

                if (existing == null)
                {
                    throw new KeyNotFoundException();
                }

                var sql = CaseWorkflowChecklistItemMapper.Blank(model.ConditionId == ShowCondition
                    ? existing.ShowSql
                    : existing.ValidateSql);

                if (sql == null)
                {
                    return new CaseWorkflowChecklistItemTestResultDto { Passed = true };
                }

                EnsureSqlPermitted($"{Area}.Test");

                var caseKeyValue = string.Empty;

                if (model.CaseId > 0)
                {
                    var found = await caseRepository.GetByIdActiveOnlyAsync(model.CaseId, token).ConfigureAwait(false);

                    caseKeyValue = found?.CaseKeyValue ?? string.Empty;
                }

                var outcome = await RunAsync(sql, model.CaseId, caseKeyValue,
                    FormValidatorRuntime.SqlRunner(dynamicEnvironment, userName, log), token).ConfigureAwait(false);

                return new CaseWorkflowChecklistItemTestResultDto
                {
                    Passed = outcome.Code == null,
                    Code = outcome.Code,
                    Message = outcome.Code switch
                    {
                        null => null,
                        "sqlFailed" => strings[CaseWorkflowChecklistItemResources.SqlFailed],
                        _ => model.ConditionId == ValidateCondition && !string.IsNullOrWhiteSpace(existing.ValidateMessage)
                            ? existing.ValidateMessage
                            : strings[CaseWorkflowChecklistItemResources.SqlNotPassed]
                    },
                    Milliseconds = outcome.Milliseconds
                };
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
            catch (KeyNotFoundException)
            {
                op.Outcome("not_found");

                throw new NotFoundException(strings[CaseWorkflowChecklistItemResources.NotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"{Area}.Test: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private static async Task<(string? Code, int Milliseconds)> RunAsync(string sql, int caseId,
            string caseKeyValue, IFormSqlRunner sqlRunner, CancellationToken token)
        {
            var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = caseId,
                ["CaseKeyValue"] = caseKeyValue
            };

            var stopwatch = Stopwatch.StartNew();

            using var limit = CancellationTokenSource.CreateLinkedTokenSource(token);

            limit.CancelAfter(sqlTimeout);

            try
            {
                var passed = await sqlRunner.PassesAsync(sql, parameters, limit.Token).ConfigureAwait(false);

                return (passed ? null : "sql", (int)stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                return ("sqlFailed", (int)stopwatch.ElapsedMilliseconds);
            }
        }

        private static bool HasSql(CaseWorkflowChecklistItemDto model)
        {
            return !string.IsNullOrWhiteSpace(model.ShowSql) || !string.IsNullOrWhiteSpace(model.ValidateSql);
        }

        private static bool SwitchedOn(Jube.Data.Poco.CaseWorkflowChecklistItem existing,
            CaseWorkflowChecklistItemDto model)
        {
            return (model.EnableShow && existing.EnableShow != 1)
                   || (model.EnableValidation && existing.EnableValidation != 1);
        }

        private static bool SqlChanged(Jube.Data.Poco.CaseWorkflowChecklistItem existing,
            CaseWorkflowChecklistItemDto model)
        {
            return !string.Equals(CaseWorkflowChecklistItemMapper.Blank(existing.ShowSql),
                       CaseWorkflowChecklistItemMapper.Blank(model.ShowSql), StringComparison.Ordinal)
                   || !string.Equals(CaseWorkflowChecklistItemMapper.Blank(existing.ValidateSql),
                       CaseWorkflowChecklistItemMapper.Blank(model.ValidateSql), StringComparison.Ordinal);
        }

        private async Task<ValidationResult> FullValidationAsync(CaseWorkflowChecklistItemDto model,
            CancellationToken token)
        {
            var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

            if (!results.IsValid || !HasSql(model))
            {
                return results;
            }

            var tables = DynamicSqlTableAccess.BuildVisualisationAllowedTables(userName,
                permissionValidation.Validate);

            return new ValidationResult(authoring.Check(model, tables));
        }

        private async Task ValidateOrThrowAsync(CaseWorkflowChecklistItemDto model, string operation,
            CancellationToken token)
        {
            var results = await FullValidationAsync(model, token).ConfigureAwait(false);

            if (results.IsValid)
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{Area}.{operation}: validation failed user={userName} " +
                         $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
            }

            throw new DtoValidationException(results);
        }

        private void EnsureSqlPermitted(string op, string messageKey = CaseWorkflowChecklistItemResources.SqlPermissionDenied)
        {
            if (permissionValidation.Validate(sqlPermissions))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: SQL authoring permission denied user={userName} specs=[{string.Join(",", sqlPermissions)}]");
            }

            throw new ForbiddenException(strings[messageKey], sqlPermissions);
        }

        private void EnsurePermitted(string op, int[] specs)
        {
            if (permissionValidation.Validate(specs))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", specs)}]");
            }

            throw new ForbiddenException(strings[CaseWorkflowChecklistItemResources.PermissionDenied], specs);
        }
    }
}
