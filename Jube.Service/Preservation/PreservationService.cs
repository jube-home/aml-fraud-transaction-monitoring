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
using Jube.Dto.Repository.PreservationSnapshot;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Preservation;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.PreservationSnapshot;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;
using NotFoundException = Jube.Service.Exceptions.Repository.PreservationSnapshot.NotFoundException;
using PreservationSnapshotDto = Jube.Dto.Preservation.PreservationSnapshotDto;

namespace Jube.Service.Preservation
{
    public sealed class PreservationService
    {
        private static readonly int[] permissions = [38];
        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly bool legacyEncryptionFallback;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly string? preservationSalt;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizerFactory stringLocalizerFactory;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private PreservationService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, IStringLocalizerFactory stringLocalizerFactory, string? preservationSalt,
            bool legacyEncryptionFallback)
        {
            this.dbContext = dbContext;
            this.stringLocalizerFactory = stringLocalizerFactory;
            this.preservationSalt = preservationSalt;
            this.legacyEncryptionFallback = legacyEncryptionFallback;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<PreservationService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            string? preservationSalt, bool legacyEncryptionFallback, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), preservationSalt, legacyEncryptionFallback, token);
        }

        internal static async Task<PreservationService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
            string? preservationSalt, bool legacyEncryptionFallback, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(PreservationResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("Preservation.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[PreservationResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"Preservation.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[PreservationResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new PreservationService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation,
                log, auditLog, serviceChangeBus, strings, stringLocalizerFactory, preservationSalt,
                legacyEncryptionFallback);
        }

        [Description("Takes a point-in-time snapshot of the caller's own tenant configuration, storing it " +
                     "against a row rather than producing a file. Returns the snapshot's identifier and " +
                     "Guid -- quote the Guid to PreservationRecover to roll back to it. Mutating tools take " +
                     "their own snapshot automatically before they run, so this is for taking a deliberate " +
                     "restore point before a sequence of changes, not something each change needs first.")]
        [ServiceOperation("PreservationSnapshot", OperationKind.Write, Idempotent = false)]
        public async Task<PreservationSnapshotDto> SnapshotAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("Preservation", "Snapshot", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                EnsurePermitted("Preservation.Snapshot");

                var snapshot = await (await BuildSnapshotServiceAsync(token).ConfigureAwait(false))
                    .CreateAsync(new PreservationSnapshotRequestDto
                    {
                        Name = "Preservation snapshot",
                        SnapshotSourceId = PreservationSnapshotSource.AskJooby,
                        Exhaustive = true,
                        Lists = true,
                        Dictionaries = true,
                        Visualisations = true,
                        Roles = true,
                        AdvancedAnalytics = true
                    }, token).ConfigureAwait(false);

                var dto = new PreservationSnapshotDto(snapshot.Id, snapshot.Guid, snapshot.Bytes,
                    snapshot.CompletedDate ?? snapshot.CreatedDate ?? DateTimeOffset.UtcNow);

                op.Entity(snapshot.Id);
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"Preservation.Snapshot: created Id={dto.Id} Guid={dto.Guid} user={userName}");
                }

                return dto;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"Preservation.Snapshot: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Restores tenant configuration to a previously taken snapshot, identified by its Guid. " +
                     "Irreversible from the agent's own perspective once it succeeds -- confirm with the " +
                     "caller before invoking this, even though the underlying snapshot file itself is still " +
                     "recoverable. Not bound into any ordinary agent's tool set -- see the agent-creation " +
                     "skill's own note on why.")]
        [ServiceOperation("PreservationRecover", OperationKind.Delete, Idempotent = true, Destructive = true)]
        public async Task RecoverAsync(
            [Description("The Guid returned by a prior PreservationSnapshot call.")]
            Guid snapshotGuid, CancellationToken token = default)
        {
            using var op = OperationScope.Start("Preservation", "Recover", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                EnsurePermitted("Preservation.Recover");

                var existing = await new PreservationSnapshotRepository(dbContext, userName)
                    .GetByGuidAsync(snapshotGuid, token).ConfigureAwait(false);

                if (existing is null)
                {
                    op.Outcome("notfound");
                    throw new NotFoundException("The snapshot was not found.");
                }

                var service = await BuildSnapshotServiceAsync(token).ConfigureAwait(false);
                await service.ImportAsync(existing.Id, token).ConfigureAwait(false);

                op.Entity(existing.Id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"Preservation.Recover: restored Id={existing.Id} Guid={snapshotGuid} user={userName}");
                }
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"Preservation.Recover: unexpected failure Guid={snapshotGuid} user={userName}", ex);
                throw;
            }
        }

        private Task<PreservationSnapshotService> BuildSnapshotServiceAsync(CancellationToken token)
        {
            return PreservationSnapshotService.CreateAsync(dbContext, userName, log, stringLocalizerFactory,
                serviceChangeBus, preservationSalt, legacyEncryptionFallback, token);
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

            throw new ForbiddenException(strings[PreservationResources.PermissionDenied], permissions);
        }
    }
}