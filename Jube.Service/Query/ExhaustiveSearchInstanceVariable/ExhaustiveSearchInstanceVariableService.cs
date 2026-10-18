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
using Jube.Analytics.Datasets;
using Jube.Data.Context;
using Jube.Data.Query.GetExhaustiveSearchInstanceVariableQuery;
using Jube.Data.Repository;
using Jube.Dto.Query.ExhaustiveSearchInstanceVariable;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Query.ExhaustiveSearchInstanceVariable;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.ExhaustiveSearchInstanceVariable;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.ExhaustiveSearchInstanceVariable
{
    public sealed class ExhaustiveSearchInstanceVariableService
    {
        private static readonly int[] permissions = [16];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly GetExhaustiveSearchInstanceVariableQuery query;
        private readonly ExhaustiveSearchInstanceVariableRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private ExhaustiveSearchInstanceVariableService(DbContext dbContext, string userName,
            int tenantRegistryId, PermissionValidation permissionValidation, ILog log, ILog auditLog,
            IServiceChangeBus serviceChangeBus, IStringLocalizer strings)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;

            query = new GetExhaustiveSearchInstanceVariableQuery(
                dbContext, userName);

            repository = new ExhaustiveSearchInstanceVariableRepository(dbContext);
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<ExhaustiveSearchInstanceVariableService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<ExhaustiveSearchInstanceVariableService> CreateAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(ExhaustiveSearchInstanceVariableResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("ExhaustiveSearchInstanceVariable.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[ExhaustiveSearchInstanceVariableResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"ExhaustiveSearchInstanceVariable.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[ExhaustiveSearchInstanceVariableResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new ExhaustiveSearchInstanceVariableService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description(
            "Lists the variables of one Exhaustive Search Instance with distribution statistics and histogram bins. " +
            "Read-only and scoped to the caller's tenant: an Exhaustive Search Instance " +
            "belonging to another tenant yields an empty list.")]
        [ServiceOperation("ExhaustiveSearchInstanceVariableGet", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<ExhaustiveSearchInstanceVariableDto>> GetAsync(
            [Description("Integer Id of the Exhaustive Search Instance.")]
            int exhaustiveSearchInstanceId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("ExhaustiveSearchInstanceVariable", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"ExhaustiveSearchInstanceVariable.Get: entry user={userName} id={exhaustiveSearchInstanceId}");
            }

            try
            {
                EnsurePermitted("ExhaustiveSearchInstanceVariable.Get");

                var dtos = ExhaustiveSearchInstanceVariableMapper.ToDto(
                    await query.ExecuteAsync(exhaustiveSearchInstanceId, token)
                        .ConfigureAwait(false));
                op.Rows(dtos.Count);

                if (log.IsDebugEnabled)
                {
                    log.Debug($"ExhaustiveSearchInstanceVariable.Get: {dtos.Count} rows user={userName}");
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
                log.Error($"ExhaustiveSearchInstanceVariable.Get: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description(
            "Reallocates a trained variable's live name and processing type, without retraining. Recall resolves " +
            "variables by name and processing type only, so this changes what live field the network's fixed " +
            "weight position reads at the next recall; the learned weight itself is unchanged, so retarget to a " +
            "field with a similar scale and distribution to the one the model was trained on.")]
        [ServiceOperation("ExhaustiveSearchInstanceVariableReallocate", OperationKind.Write)]
        public async Task<ExhaustiveSearchInstanceVariableReallocateDto> ReallocateAsync(
            [Description("Integer Id of the Exhaustive Search Instance Variable.")]
            int id,
            [Description("The new name and processing type.")]
            ExhaustiveSearchInstanceVariableReallocateDto model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("ExhaustiveSearchInstanceVariable", "Reallocate", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"ExhaustiveSearchInstanceVariable.Reallocate: entry user={userName} id={id}");
            }

            try
            {
                token.ThrowIfCancellationRequested();
                EnsurePermitted("ExhaustiveSearchInstanceVariable.Reallocate");

                var validator = new ExhaustiveSearchInstanceVariableReallocateDtoValidator(strings);
                var result = await validator.ValidateAsync(model, token).ConfigureAwait(false);

                if (!result.IsValid)
                {
                    throw new DtoValidationException(result);
                }

                var name = DatasetProcessingTypes.Prefix(model.ProcessingTypeId) + "." + model.Name;

                var found = await repository.ReallocateAsync(id, tenantRegistryId, name,
                    (byte)model.ProcessingTypeId, token).ConfigureAwait(false);

                if (!found)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"ExhaustiveSearchInstanceVariable.Reallocate: {id} not found user={userName}");
                    }

                    throw new NotFoundException(strings[ExhaustiveSearchInstanceVariableResources.NotFound]);
                }

                op.Rows(1);
                op.Entity(id);
                op.Updated();

                return model;
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
                log.Error($"ExhaustiveSearchInstanceVariable.Reallocate: unexpected failure user={userName}", ex);
                throw;
            }
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

            throw new ForbiddenException(
                strings[ExhaustiveSearchInstanceVariableResources.PermissionDenied], permissions);
        }
    }
}