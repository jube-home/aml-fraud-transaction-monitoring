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
using Jube.Data.Concurrency;
using Jube.Case.Outbound;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Repository.CaseWorkflowReleaseHook;
using Jube.Dto.Validation;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Repository.CaseWorkflowReleaseHook;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.CaseWorkflowReleaseHook;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.CaseWorkflowReleaseHook
{
    public sealed class CaseWorkflowReleaseHookService
    {
        private const string Area = "CaseWorkflowReleaseHook";
        private static readonly int[] permissions = [69];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly CaseWorkflowReleaseHookRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly CaseWorkflowReleaseHookDtoValidator validator;

        private CaseWorkflowReleaseHookService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, OutboundHostPolicy outboundPolicy)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            repository = new CaseWorkflowReleaseHookRepository(dbContext, userName);
            validator = new CaseWorkflowReleaseHookDtoValidator(repository, strings, outboundPolicy);
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseWorkflowReleaseHookService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), dynamicEnvironment, token);
        }

        internal static async Task<CaseWorkflowReleaseHookService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseWorkflowReleaseHookResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{Area}.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowReleaseHookResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{Area}.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseWorkflowReleaseHookResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseWorkflowReleaseHookService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, OutboundHostPolicy.From(dynamicEnvironment));
        }

        [Description("Lists every Case Workflow release hook visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowReleaseHookList", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowReleaseHookDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted($"{Area}.List");

                var dtos = CaseWorkflowReleaseHookMapper.ToDto(await repository.GetAllDescAsync(token)
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

        [Description("Lists the release hooks of a Case Workflow, visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowReleaseHookListByCaseWorkflowId", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseWorkflowReleaseHookDto>> GetByCaseWorkflowIdAsync(
            [Description("Server-assigned identifier of the Case Workflow to list release hooks for.")]
            int caseWorkflowId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "ListByCaseWorkflowId", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted($"{Area}.ListByCaseWorkflowId");

                var dtos = CaseWorkflowReleaseHookMapper.ToDto(await repository
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

        [Description("Returns a single Case Workflow release hook by Id, or null if it doesn't exist or isn't " +
                     "visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowReleaseHookGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseWorkflowReleaseHookDto?> GetByIdAsync(
            [Description("Server-assigned identifier of the release hook.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "Get", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted($"{Area}.Get");

                return CaseWorkflowReleaseHookMapper.ToDto(await repository.GetByIdAsync(id, token)
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

        [Description("Creates a Case Workflow release hook: the web hook called to release, or refuse, a payment " +
                     "held on a case of the Case Workflow, chosen by a field of the payload of the held " +
                     "transaction. The credential is never stored; the name of an environment variable is.")]
        [ServiceOperation("CaseWorkflowReleaseHookCreate", OperationKind.Write, Idempotent = false)]
        public async Task<CaseWorkflowReleaseHookDto> InsertAsync(CaseWorkflowReleaseHookDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "Insert", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted($"{Area}.Insert");

                await ValidateOrThrowAsync(model, "Insert", token).ConfigureAwait(false);

                var workflow = await repository.GetCaseWorkflowByIdAsync(model.CaseWorkflowId, token)
                    .ConfigureAwait(false);

                var poco = CaseWorkflowReleaseHookMapper.ToPoco(model);
                poco.CaseWorkflowGuid = workflow!.Guid;

                var saved = await repository.InsertAsync(poco, token).ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"{Area}.Insert: created Id={saved.Id} user={userName}");
                }

                return CaseWorkflowReleaseHookMapper.ToDto(saved);
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

        [Description("Validates a Case Workflow release hook without saving it, running every check a create " +
                     "(Id 0) or an update (any other Id) would run, and returns each failure. Nothing is stored.")]
        [ServiceOperation("CaseWorkflowReleaseHookValidate", OperationKind.Read, Idempotent = true)]
        public async Task<ValidationResultDto> ValidateAsync(
            [Description("The Case Workflow release hook to validate.")]
            CaseWorkflowReleaseHookDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "Validate", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted($"{Area}.Validate");

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
                log.Error($"{Area}.Validate: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Updates an existing Case Workflow release hook. The row must exist, be unlocked and be " +
                     "visible to the caller's tenant.")]
        [ServiceOperation("CaseWorkflowReleaseHookUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<CaseWorkflowReleaseHookDto> UpdateAsync(CaseWorkflowReleaseHookDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "Update", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted($"{Area}.Update");

                var existing = await repository.GetByIdAsync(model.Id, token).ConfigureAwait(false);

                if (existing == null)
                {
                    throw new KeyNotFoundException();
                }

                if (model.CaseWorkflowId != existing.CaseWorkflowId)
                {
                    throw new DtoValidationException(new FluentValidation.Results.ValidationResult([
                        new FluentValidation.Results.ValidationFailure(
                            nameof(CaseWorkflowReleaseHookDto.CaseWorkflowId),
                            strings[CaseWorkflowReleaseHookResources.CaseWorkflowNotFound])
                        {
                            ErrorCode = "CaseWorkflowIdInvalid"
                        }
                    ]));
                }

                await ValidateOrThrowAsync(model, "Update", token).ConfigureAwait(false);

                var saved = await repository.UpdateAsync(CaseWorkflowReleaseHookMapper.ToPoco(model), token)
                    .ConfigureAwait(false);
                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"{Area}.Update: Id={saved.Id} version={saved.Version} user={userName}");
                }

                return CaseWorkflowReleaseHookMapper.ToDto(saved);
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

                throw new NotFoundException(strings[CaseWorkflowReleaseHookResources.NotFound]);
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

        [Description("Soft-deletes a Case Workflow release hook that is not locked. Reversible only by direct " +
                     "database action.")]
        [ServiceOperation("CaseWorkflowReleaseHookDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Server-assigned identifier of the release hook to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start(Area, "Delete", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted($"{Area}.Delete");

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

                throw new NotFoundException(strings[CaseWorkflowReleaseHookResources.NotFound]);
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

        private async Task ValidateOrThrowAsync(CaseWorkflowReleaseHookDto model, string operation,
            CancellationToken token)
        {
            var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);

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

            throw new ForbiddenException(strings[CaseWorkflowReleaseHookResources.PermissionDenied], permissions);
        }
    }
}
