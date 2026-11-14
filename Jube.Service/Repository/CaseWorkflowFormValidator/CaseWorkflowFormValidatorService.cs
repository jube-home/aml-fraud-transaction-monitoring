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
using FluentValidation.Results;
using Jube.Data.Concurrency;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Repository.CaseWorkflowFormValidator;
using Jube.Dto.Validation;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Agent.Models;
using Jube.Service.Charts.Datasource;
using Jube.Service.Charts.Datasource.Template;
using Jube.Service.DynamicSql;
using Jube.Service.Exceptions.Repository.CaseWorkflowFormValidator;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseWorkflowFormValidator.Models;
using Jube.Service.Security;
using Jube.Validations.Repository.CaseWorkflowFormValidator;
using log4net;
using Microsoft.Extensions.Localization;
using RuleRepository = Jube.Data.Repository.CaseWorkflowFormValidatorRepository;

namespace Jube.Service.Repository.CaseWorkflowFormValidator
{
    public sealed class CaseWorkflowFormValidatorService
    {
        private const string Area = "CaseWorkflowFormValidator";
        private static readonly int[] permissions = [21];
        private static readonly int[] sqlPermissions = [67];
        private readonly ILog auditLog;
        private readonly FormValidatorAuthoring authoring;
        private readonly CaseRepository caseRepository;
        private readonly DatasourceChartText chartText;
        private readonly DynamicEnvironment.DynamicEnvironment dynamicEnvironment;
        private readonly DbContext dbContext;
        private readonly IStringLocalizer entryStrings;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly CaseWorkflowFormValidatorDtoValidator validator;

        private CaseWorkflowFormValidatorService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, IStringLocalizer entryStrings, IStringLocalizer chartStrings,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.entryStrings = entryStrings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.dynamicEnvironment = dynamicEnvironment;
            chartText = new DatasourceChartText(chartStrings);
            authoring = new FormValidatorAuthoring(strings, chartText);
            repository = new RuleRepository(dbContext, userName);
            caseRepository = new CaseRepository(dbContext, userName);
            validator = new CaseWorkflowFormValidatorDtoValidator(repository, strings);
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseWorkflowFormValidatorService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                dynamicEnvironment, LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseWorkflowFormValidatorService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseWorkflowFormValidatorResources));
            var entryStrings = stringLocalizerFactory.Create(typeof(CaseWorkflowFormEntryResources));
            var chartStrings = stringLocalizerFactory.Create(typeof(DatasourceChartResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{Area}.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowFormValidatorResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{Area}.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowFormValidatorResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseWorkflowFormValidatorService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, entryStrings, chartStrings,
                dynamicEnvironment);
        }

        [Description("Lists every Case Workflow Form validator visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowFormValidatorList", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowFormValidatorDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted($"{Area}.List", permissions);

                var dtos = CaseWorkflowFormValidatorMapper.ToDto(await repository.GetAllDescAsync(token)
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

        [Description("Lists the validators of the Forms of a Case Workflow, visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowFormValidatorListByCaseWorkflowId", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowFormValidatorDto>> GetByCaseWorkflowIdAsync(
            [Description("Server-assigned identifier of the Case Workflow to list Form validators for.")]
            int caseWorkflowId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "ListByCaseWorkflowId", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted($"{Area}.ListByCaseWorkflowId", permissions);

                var dtos = CaseWorkflowFormValidatorMapper.ToDto(await repository
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

        [Description("Returns a single Case Workflow Form validator by Id, or null if it doesn't exist or isn't " +
                     "visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowFormValidatorGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseWorkflowFormValidatorDto?> GetByIdAsync(
            [Description("Server-assigned identifier of the validator.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "Get", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted($"{Area}.Get", permissions);

                return CaseWorkflowFormValidatorMapper.ToDto(await repository.GetByIdAsync(id, token)
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

        [Description("Creates a Case Workflow Form validator: one server-side rule for one field of the Form, " +
                     "paired to the template element by name. A SQL validator needs its own permission.")]
        [ServiceOperation("CaseWorkflowFormValidatorCreate", OperationKind.Write, Idempotent = false)]
        public async Task<CaseWorkflowFormValidatorDto> InsertAsync(CaseWorkflowFormValidatorDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "Insert", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted($"{Area}.Insert", permissions);

                if (model.KindId == (int)CaseWorkflowFormValidatorKind.Sql)
                {
                    EnsureSqlPermitted($"{Area}.Insert");
                }

                await ValidateOrThrowAsync(model, "Insert", token).ConfigureAwait(false);

                var saved = await repository.InsertAsync(CaseWorkflowFormValidatorMapper.ToPoco(model), token)
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"{Area}.Insert: created Id={saved.Id} kind={saved.KindId} user={userName}");
                }

                return CaseWorkflowFormValidatorMapper.ToDto(saved);
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

        [Description("Validates a Case Workflow Form validator without saving it, running every check a create " +
                     "(Id 0) or an update (any other Id) would run, and returns each failure. Nothing is stored.")]
        [ServiceOperation("CaseWorkflowFormValidatorValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The Case Workflow Form validator to validate.")]
            CaseWorkflowFormValidatorDto? model,
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

        [Description("Updates an existing Case Workflow Form validator. The row must exist and be visible to " +
                     "the caller's tenant. Changing a SQL validator needs the SQL authoring permission.")]
        [ServiceOperation("CaseWorkflowFormValidatorUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<CaseWorkflowFormValidatorDto> UpdateAsync(CaseWorkflowFormValidatorDto? model,
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

                if (model.KindId == (int)CaseWorkflowFormValidatorKind.Sql ||
                    existing.KindId == (byte)CaseWorkflowFormValidatorKind.Sql)
                {
                    EnsureSqlPermitted($"{Area}.Update");
                }

                model.CaseWorkflowFormGuid = existing.CaseWorkflowFormGuid;
                model.CaseWorkflowId = existing.CaseWorkflowId;

                await ValidateOrThrowAsync(model, "Update", token).ConfigureAwait(false);

                var saved = await repository.UpdateAsync(CaseWorkflowFormValidatorMapper.ToPoco(model), token)
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"{Area}.Update: Id={saved.Id} version={saved.Version} kind={saved.KindId} " +
                             $"user={userName}");
                }

                return CaseWorkflowFormValidatorMapper.ToDto(saved);
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

                throw new NotFoundException(strings[CaseWorkflowFormValidatorResources.NotFound]);
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

        [Description("Soft-deletes a Case Workflow Form validator. Deleting a SQL validator needs the SQL " +
                     "authoring permission. Reversible only by direct database action.")]
        [ServiceOperation("CaseWorkflowFormValidatorDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the validator to delete.")]
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

                if (existing.KindId == (byte)CaseWorkflowFormValidatorKind.Sql)
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

                throw new NotFoundException(strings[CaseWorkflowFormValidatorResources.NotFound]);
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

        [Description("Tries a saved Case Workflow Form validator against the values given, without submitting the " +
                     "Form, and reports whether it passed, why not, and how long it took. A SQL validator runs " +
                     "read only against the tenant's own rows with a short timeout. Needs the SQL authoring " +
                     "permission for a SQL validator.")]
        [ServiceOperation("CaseWorkflowFormValidatorTest", OperationKind.Read, Idempotent = true)]
        public async Task<CaseWorkflowFormValidatorTestResultDto> TestAsync(
            [Description("The validator, an optional case and the values to try.")]
            CaseWorkflowFormValidatorTestDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "Test", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted($"{Area}.Test", permissions);

                var existing = await repository.GetByIdAsync(model.Id, token).ConfigureAwait(false);

                if (existing == null)
                {
                    throw new KeyNotFoundException();
                }

                if (existing.KindId == (byte)CaseWorkflowFormValidatorKind.Sql)
                {
                    EnsureSqlPermitted($"{Area}.Test");
                }

                var form = await repository.GetFormAsync(existing.CaseWorkflowFormGuid, token).ConfigureAwait(false);
                var modelGuid = await repository.GetModelGuidOfFormAsync(existing.CaseWorkflowFormGuid, token)
                    .ConfigureAwait(false);

                if (form == null || modelGuid == null)
                {
                    throw new KeyNotFoundException();
                }

                var schema = DatasourceTemplate.CompileForm(form.Html);
                var caseKeyValue = string.Empty;

                if (model.CaseId > 0)
                {
                    var found = await caseRepository.GetByIdActiveOnlyAsync(model.CaseId, token).ConfigureAwait(false);

                    caseKeyValue = found?.CaseKeyValue ?? string.Empty;
                }

                var rule = new FormValidatorRule(existing.Id, existing.FieldName, (CaseWorkflowFormValidatorKind)
                    existing.KindId.GetValueOrDefault(), existing.Argument1, existing.Argument2, existing.Sql,
                    existing.Message);

                var values = (model.Values ?? [])
                    .Where(v => v.Value != null)
                    .ToDictionary(v => v.Key, v => v.Value!, StringComparer.OrdinalIgnoreCase);

                var outcome = await FormValidatorEngine.TestAsync(schema, rule, values, caseKeyValue, model.CaseId,
                    FormValidatorRuntime.Lookups(dbContext, tenantRegistryId, modelGuid.Value, dynamicEnvironment),
                    FormValidatorRuntime.SqlRunner(dynamicEnvironment, userName, log), token).ConfigureAwait(false);

                return new CaseWorkflowFormValidatorTestResultDto
                {
                    Passed = outcome.Passed,
                    Code = outcome.Code,
                    Message = outcome.Passed
                        ? null
                        : FormValidatorMessages.For(outcome.Code ?? string.Empty, existing.Message, entryStrings),
                    Milliseconds = outcome.Milliseconds
                };
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (KeyNotFoundException)
            {
                op.Outcome("not_found");

                throw new NotFoundException(strings[CaseWorkflowFormValidatorResources.NotFound]);
            }
            catch (DatasourceChartException ex)
            {
                op.Outcome("invalid");

                throw new DtoValidationException(new ValidationResult([
                    new ValidationFailure(nameof(CaseWorkflowFormValidatorDto.CaseWorkflowFormGuid),
                        string.Format(strings[CaseWorkflowFormValidatorResources.TemplateInvalid],
                            chartText.Describe(ex))) { ErrorCode = "TemplateInvalid" }
                ]));
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

        private async Task<ValidationResult> FullValidationAsync(CaseWorkflowFormValidatorDto model,
            CancellationToken token)
        {
            var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

            if (!results.IsValid)
            {
                return results;
            }

            var form = await repository.GetFormAsync(model.CaseWorkflowFormGuid, token).ConfigureAwait(false);
            var modelGuid = await repository.GetModelGuidOfFormAsync(model.CaseWorkflowFormGuid, token)
                .ConfigureAwait(false);

            IReadOnlyDictionary<string, string>? tables = null;

            if (model.KindId == (int)CaseWorkflowFormValidatorKind.Sql)
            {
                tables = DynamicSqlTableAccess.BuildVisualisationAllowedTables(userName,
                    permissionValidation.Validate);
            }

            var lookups = FormValidatorRuntime.Lookups(dbContext, tenantRegistryId, modelGuid.GetValueOrDefault(),
                dynamicEnvironment);

            var (_, failures) = await authoring.CheckAsync(model, form?.Html, lookups, tables, token)
                .ConfigureAwait(false);

            return new ValidationResult(failures);
        }

        private async Task ValidateOrThrowAsync(CaseWorkflowFormValidatorDto model, string operation,
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

        private void EnsureSqlPermitted(string op)
        {
            if (permissionValidation.Validate(sqlPermissions))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: SQL authoring permission denied user={userName} specs=[{string.Join(",", sqlPermissions)}]");
            }

            throw new ForbiddenException(strings[CaseWorkflowFormValidatorResources.SqlPermissionDenied],
                sqlPermissions);
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

            throw new ForbiddenException(strings[CaseWorkflowFormValidatorResources.PermissionDenied], specs);
        }
    }
}
