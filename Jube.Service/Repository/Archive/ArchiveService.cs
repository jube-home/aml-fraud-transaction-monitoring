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
using Jube.Dto.Repository.Archive;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.Archive;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Repository.Archive;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.Archive
{
    public sealed class ArchiveService
    {
        private static readonly int[] permissions = [1];
        private readonly ArchiveTagRepository archiveTagRepository;
        private readonly ILog auditLog;
        private readonly CaseRepository caseRepository;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly ArchiveRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly ArchiveTagDtoValidator validator;

        private ArchiveService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            repository = new ArchiveRepository(dbContext);
            caseRepository = new CaseRepository(dbContext);
            archiveTagRepository = new ArchiveTagRepository(dbContext, userName);
            validator = new ArchiveTagDtoValidator(strings);
        }

        public static Task<ArchiveService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<ArchiveService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(ArchiveResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("Archive.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[ArchiveResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"Archive.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[ArchiveResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new ArchiveService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation, log,
                auditLog, serviceChangeBus, strings);
        }

        [Description("Replaces the full set of Tags applied to an archived transaction, identified by its " +
                     "EntityAnalysisModelInstanceEntryGuid, scoped to the caller's tenant. Not a delta -- the " +
                     "given Tag array becomes the complete set; an empty array clears all tags. Idempotent.")]
        [ServiceOperation("ArchiveTag", OperationKind.Write, Idempotent = true)]
        public async Task TagAsync(
            [Description("The Tag update to apply.")]
            ArchiveTagDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("Archive", "Tag", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"Archive.Tag: entry guid={model?.EntityAnalysisModelInstanceEntryGuid} user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("Archive.Tag");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Archive.Tag: validation failed guid={model.EntityAnalysisModelInstanceEntryGuid} " +
                                 $"user={userName} props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(results);
                }

                var ownerTenantRegistryId = await repository
                    .GetTenantRegistryIdByEntityAnalysisModelInstanceEntryGuidAsync(
                        model.EntityAnalysisModelInstanceEntryGuid, token).ConfigureAwait(false);

                if (ownerTenantRegistryId is null || ownerTenantRegistryId.Value != tenantRegistryId)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"Archive.Tag: guid={model.EntityAnalysisModelInstanceEntryGuid} not found or not " +
                            $"visible to tenant user={userName}");
                    }

                    throw new NotFoundException(strings[ArchiveResources.NotFound]);
                }

                Data.Poco.Archive archive;
                try
                {
                    archive = await repository.UpdateTagsByEntityAnalysisModelInstanceEntryGuidAsync(
                        model.EntityAnalysisModelInstanceEntryGuid, model.Tag ?? [], token).ConfigureAwait(false);
                }
                catch (InvalidOperationException ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"Archive.Tag: guid={model.EntityAnalysisModelInstanceEntryGuid} not found at update " +
                            $"time (race with the tenant check) user={userName}");
                    }

                    throw new NotFoundException(strings[ArchiveResources.NotFound], ex);
                }

                await caseRepository.UpdateArchiveJsonAsync(model.EntityAnalysisModelInstanceEntryGuid, archive.Json,
                    token).ConfigureAwait(false);

                await archiveTagRepository.MergeTagsAsync(model.EntityAnalysisModelInstanceEntryGuid,
                    model.Tag ?? [], token).ConfigureAwait(false);

                op.Entity((int)archive.Id);
                op.Version(archive.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"Archive.Tag: tagged guid={model.EntityAnalysisModelInstanceEntryGuid} " +
                             $"tags=[{string.Join(",", model.Tag ?? [])}] user={userName}");
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
                        $"Archive.Tag: cancelled guid={model?.EntityAnalysisModelInstanceEntryGuid} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"Archive.Tag: unexpected failure guid={model?.EntityAnalysisModelInstanceEntryGuid} user={userName}",
                    ex);
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

            throw new ForbiddenException(strings[ArchiveResources.PermissionDenied], permissions);
        }
    }
}