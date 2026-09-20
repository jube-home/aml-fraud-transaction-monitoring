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
using Jube.Dto.Repository.ExhaustiveSearchInstancePromotedTrialInstance;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.ExhaustiveSearchInstancePromotedTrialInstance;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.ExhaustiveSearchInstancePromotedTrialInstance;
using log4net;
using Microsoft.Extensions.Localization;
using RuleRepository = Jube.Data.Repository.ExhaustiveSearchInstancePromotedTrialInstanceRepository;

namespace Jube.Service.Repository.ExhaustiveSearchInstancePromotedTrialInstance
{
    public sealed class ExhaustiveSearchInstancePromotedTrialInstanceService
    {
        private static readonly int[] writePermissions = [14];

        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly ExhaustiveSearchInstancePromotedTrialInstanceDtoValidator validator;

        private ExhaustiveSearchInstancePromotedTrialInstanceService(DbContext dbContext, string userName,
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
            repository = new RuleRepository(dbContext, userName);
            validator = new ExhaustiveSearchInstancePromotedTrialInstanceDtoValidator(strings);
        }

        public static Task<ExhaustiveSearchInstancePromotedTrialInstanceService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<ExhaustiveSearchInstancePromotedTrialInstanceService> CreateAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(ExhaustiveSearchInstancePromotedTrialInstanceResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("ExhaustiveSearchInstancePromotedTrialInstance.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[ExhaustiveSearchInstancePromotedTrialInstanceResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"ExhaustiveSearchInstancePromotedTrialInstance.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(
                    strings[ExhaustiveSearchInstancePromotedTrialInstanceResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new ExhaustiveSearchInstancePromotedTrialInstanceService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Sets whether a promoted trial instance (the outcome of one Exhaustive Adaptation training " +
                     "trial) is promoted for production use, scoped to the caller's tenant. When set to false, " +
                     "the trial instance is rejected/deactivated and the next highest-performing promoted trial " +
                     "instance for its parent Exhaustive Adaptation takes its place. Idempotent -- setting the " +
                     "same value twice has no further effect.")]
        [ServiceOperation("ExhaustiveSearchInstancePromotedTrialInstanceUpdate", OperationKind.Write,
            Idempotent = true)]
        public async Task UpdateActiveAsync(
            [Description("The promoted trial instance Id and the desired Active state.")]
            ExhaustiveSearchInstancePromotedTrialInstanceDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("ExhaustiveSearchInstancePromotedTrialInstance", "Update", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"ExhaustiveSearchInstancePromotedTrialInstance.Update: entry id={model?.Id} active={model?.Active} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "ExhaustiveSearchInstancePromotedTrialInstance.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"ExhaustiveSearchInstancePromotedTrialInstance.Update: validation failed id={model.Id} " +
                            $"user={userName} props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                try
                {
                    await repository.UpdateActiveAsync(model.Id, model.Active, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"ExhaustiveSearchInstancePromotedTrialInstance.Update: id={model.Id} not found or not " +
                            $"visible to tenant user={userName}");
                    }

                    throw new NotFoundException(
                        strings[ExhaustiveSearchInstancePromotedTrialInstanceResources.NotFound], ex);
                }

                op.Entity(model.Id);
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"ExhaustiveSearchInstancePromotedTrialInstance.Update: Id={model.Id} active={model.Active} " +
                        $"user={userName}");
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
                    log.Debug(
                        $"ExhaustiveSearchInstancePromotedTrialInstance.Update: cancelled id={model?.Id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"ExhaustiveSearchInstancePromotedTrialInstance.Update: unexpected failure id={model?.Id} user={userName}",
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
                strings[ExhaustiveSearchInstancePromotedTrialInstanceResources.PermissionDenied], specs);
        }
    }
}