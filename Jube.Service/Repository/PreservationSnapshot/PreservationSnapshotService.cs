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
using Jube.Dto.Repository.Preservation;
using Jube.Dto.Repository.PreservationSnapshot;
using Jube.Service.Repository.Preservation;
using Jube.Preservation.Models;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Repository.Preservation;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;
using NotFoundException = Jube.Service.Exceptions.Repository.PreservationSnapshot.NotFoundException;

namespace Jube.Service.Repository.PreservationSnapshot
{
    using Library = Jube.Preservation.Preservation;

    public sealed class PreservationSnapshotService
    {
        private const string ProcessSourceRequiredMessage =
            "A snapshot taken on behalf of a process must declare a process source.";

        private static readonly int[] permissions = [38];
        private static readonly int[] importPermissions = [63];
        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private PreservationSnapshotService(DbContext dbContext, string userName, int tenantRegistryId,
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
        }

        public static Task<PreservationSnapshotService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<PreservationSnapshotService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(PreservationResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("PreservationSnapshot.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[PreservationResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"PreservationSnapshot.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[PreservationResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new PreservationSnapshotService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the snapshots held for the caller's own tenant, most recent first. Metadata only: " +
                     "the snapshot body is never returned by the list, only its size and the number of Models " +
                     "it holds. The Id of a row is what identifies it for a later rollback.")]
        [ServiceOperation("PreservationSnapshotList", OperationKind.Read, Idempotent = true)]
        public async Task<List<PreservationSnapshotDto>> GetAsync(
            [Description("Maximum number of snapshots to return. Clamped to between 1 and 1000.")]
            int limit = 250,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("PreservationSnapshot", "List", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("PreservationSnapshot.List");

                var repository = new PreservationSnapshotRepository(dbContext, userName);

                var dtos = PreservationSnapshotMapper.ToDto(
                    await repository.GetAsync(Math.Clamp(limit, 1, 1000), token).ConfigureAwait(false));

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
                log.Error($"PreservationSnapshot.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns the metadata of one snapshot belonging to the caller's own tenant. The body is " +
                     "not returned; this confirms that a given Id exists and is importable before a rollback " +
                     "is attempted against it.")]
        [ServiceOperation("PreservationSnapshotGet", OperationKind.Read, Idempotent = true)]
        public async Task<PreservationSnapshotDto> GetByIdAsync(
            [Description("Identifier of the snapshot.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("PreservationSnapshot", "Get", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("PreservationSnapshot.Get");

                var repository = new PreservationSnapshotRepository(dbContext, userName);
                var snapshot = await repository.GetByIdAsync(id, token).ConfigureAwait(false);

                if (snapshot == null)
                {
                    op.Outcome("notfound");
                    throw new NotFoundException("The snapshot was not found.");
                }

                op.Entity(snapshot.Id);
                op.Rows(1);

                return PreservationSnapshotMapper.ToDto(snapshot);
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
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
                log.Error($"PreservationSnapshot.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Returns the full body of one snapshot belonging to the caller's own tenant, as the JSON " +
                     "it is stored in. This is the whole configuration the snapshot captured, so it is guarded " +
                     "by the same permission as exporting, which reveals the same content. Use the list or the " +
                     "metadata call where only the shape of a snapshot is needed.")]
        [ServiceOperation("PreservationSnapshotGetBody", OperationKind.Read, Idempotent = true)]
        public async Task<string> GetBodyAsync(
            [Description("Identifier of the snapshot whose body is wanted.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("PreservationSnapshot", "GetBody", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("PreservationSnapshot.GetBody");

                var repository = new PreservationSnapshotRepository(dbContext, userName);
                var snapshot = await repository.GetByIdAsync(id, token).ConfigureAwait(false);

                if (snapshot == null)
                {
                    op.Outcome("notfound");
                    throw new NotFoundException("The snapshot was not found.");
                }

                if (string.IsNullOrEmpty(snapshot.Json))
                {
                    op.Outcome("notfound");
                    throw new NotFoundException("The snapshot holds no body.");
                }

                op.Entity(snapshot.Id);
                op.Rows(1);

                return snapshot.Json;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
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
                log.Error($"PreservationSnapshot.GetBody: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Takes a snapshot of the caller's own tenant configuration, storing the export body as " +
                     "queryable JSON rather than an encrypted package. Returns the snapshot's metadata, whose " +
                     "Id is the handle for a later rollback by importing it. Nothing in the running " +
                     "configuration is changed by taking a snapshot.")]
        [ServiceOperation("PreservationSnapshotCreate", OperationKind.Write)]
        public async Task<PreservationSnapshotDto> CreateAsync(
            [Description("What to include in the snapshot body, the label to give it, and what is taking it.")]
            PreservationSnapshotRequestDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("PreservationSnapshot", "Create", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("PreservationSnapshot.Create");

                var snapshot = await TakeAsync(model, token).ConfigureAwait(false);

                op.Entity(snapshot.Id);
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"PreservationSnapshot.Create: Id={snapshot.Id} source={model.SnapshotSourceId} user={userName}");
                }

                return PreservationSnapshotMapper.ToDto(snapshot);
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
                log.Error($"PreservationSnapshot.Create: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Compares two snapshots of the caller's own tenant, or a snapshot with the live tenant, and " +
                     "reports what was added, removed and changed between them: the entities by section and, for " +
                     "each changed entity, the properties that differ. Identifiers, audit stamps and references to " +
                     "other entities are left out because they change when a snapshot is restored. Leave one side " +
                     "out to compare against the live tenant. Nothing is changed; the comparison is recorded for " +
                     "audit.")]
        [ServiceOperation("PreservationSnapshotDiff", OperationKind.Read, Idempotent = true)]
        public async Task<PreservationDiffDto> DiffAsync(
            [Description("The snapshot on each side. A side left out is the live tenant; at least one must be a snapshot.")]
            PreservationDiffRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("PreservationSnapshot", "Diff", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted("PreservationSnapshot.Diff");

                var diff = await Comparer().CompareSnapshotsAsync(request.LeftSnapshotId, request.RightSnapshotId,
                    token).ConfigureAwait(false);

                op.Rows(diff.Entries.Count);

                return diff;
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
            catch (ArgumentException)
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
                log.Error($"PreservationSnapshot.Diff: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Works out what rolling the caller's own tenant back to a snapshot would do, without changing " +
                     "anything: the live configuration compared with the snapshot that would replace it, the users " +
                     "who would be left without a role, and anything to know first. Use it before importing a " +
                     "snapshot. The comparison is recorded for audit.")]
        [ServiceOperation("PreservationSnapshotImportPreview", OperationKind.Read, Idempotent = true)]
        public async Task<PreservationImportPreviewDto> ImportPreviewAsync(
            [Description("Identifier of the snapshot that would be rolled back to.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("PreservationSnapshot", "ImportPreview", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("PreservationSnapshot.ImportPreview");

                var preview = await Comparer().PreviewSnapshotAsync(id, token).ConfigureAwait(false);

                op.Entity(id);
                op.Rows(preview.Diff.Entries.Count);

                return preview;
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
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"PreservationSnapshot.ImportPreview: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Rolls the caller's own tenant back to a snapshot, by importing the snapshot body through " +
                     "the same import that a Preservation package uses. This replaces configuration and is not " +
                     "reversible except by importing another snapshot, so take one first if the current state " +
                     "matters. Returns the metadata of the snapshot that was imported.")]
        [ServiceOperation("PreservationSnapshotImport", OperationKind.Write)]
        public async Task<PreservationSnapshotDto> ImportAsync(
            [Description("Identifier of the snapshot to roll back to.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("PreservationSnapshot", "Import", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsureImportPermitted("PreservationSnapshot.Import");

                var snapshot = await RestoreAsync(id, token).ConfigureAwait(false);

                op.Entity(snapshot.Id);
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"PreservationSnapshot.Import: Id={snapshot.Id} user={userName}");
                }

                return PreservationSnapshotMapper.ToDto(snapshot);
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
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
                log.Error($"PreservationSnapshot.Import: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Removes a snapshot belonging to the caller's own tenant. The snapshot is soft-deleted " +
                     "and stops appearing in the list; the configuration it holds is not affected.")]
        [ServiceOperation("PreservationSnapshotDelete", OperationKind.Write, Idempotent = true)]
        public async Task DeleteAsync(
            [Description("Identifier of the snapshot to remove.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("PreservationSnapshot", "Delete", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("PreservationSnapshot.Delete");

                var repository = new PreservationSnapshotRepository(dbContext, userName);

                try
                {
                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    op.Outcome("notfound");
                    throw new NotFoundException("The snapshot was not found.", ex);
                }

                op.Entity(id);
                op.Deleted();
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
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
                log.Error($"PreservationSnapshot.Delete: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Takes a snapshot on behalf of an internal process that has already decided to act, such " +
                     "as a model synchronisation or AskJooby about to change configuration. The permission that " +
                     "guards taking a snapshot directly is deliberately NOT enforced, because the process, not " +
                     "the user, is the actor. The user is still recorded, and the snapshot is still confined to " +
                     "that user's tenant. The source may not be the Preservation page, which always implies a " +
                     "person acting directly and must therefore be permissioned.")]
        public async Task<PreservationSnapshotDto> CreateForProcessAsync(
            PreservationSnapshotRequestDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("PreservationSnapshot", "CreateForProcess", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsureProcessSource(model.SnapshotSourceId);

                var snapshot = await TakeAsync(model, token).ConfigureAwait(false);

                op.Entity(snapshot.Id);
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"PreservationSnapshot.CreateForProcess: Id={snapshot.Id} source={model.SnapshotSourceId} user={userName}");
                }

                return PreservationSnapshotMapper.ToDto(snapshot);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"PreservationSnapshot.CreateForProcess: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Rolls back to a snapshot on behalf of an internal process, such as AskJooby undoing a " +
                     "change it made. The permission that guards rolling back directly is deliberately NOT " +
                     "enforced, because the process is the actor. The rollback is still confined to the user's " +
                     "own tenant, and the user is recorded against the import.")]
        public async Task<PreservationSnapshotDto> ImportForProcessAsync(int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("PreservationSnapshot", "ImportForProcess", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            try
            {
                var snapshot = await RestoreAsync(id, token).ConfigureAwait(false);

                op.Entity(snapshot.Id);
                op.Updated();

                if (log.IsInfoEnabled)
                {
                    log.Info($"PreservationSnapshot.ImportForProcess: Id={snapshot.Id} user={userName}");
                }

                return PreservationSnapshotMapper.ToDto(snapshot);
            }
            catch (NotFoundException)
            {
                op.Outcome("notfound");
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
                log.Error($"PreservationSnapshot.ImportForProcess: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        private void EnsureProcessSource(PreservationSnapshotSource source)
        {
            if (source is PreservationSnapshotSource.ModelSync or PreservationSnapshotSource.AskJooby)
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn(
                    $"PreservationSnapshot.CreateForProcess: source {source} is not a process source; refusing to bypass permission for user={userName}.");
            }

            throw new ArgumentOutOfRangeException(nameof(source), ProcessSourceRequiredMessage);
        }

        private Task<Data.Poco.PreservationSnapshot> TakeAsync(
            PreservationSnapshotRequestDto model, CancellationToken token)
        {
            var library = new Library(dbContext, userName);

            return library.SnapshotAsync(new ExportOptions
            {
                Exhaustive = true,
                Lists = model.Lists,
                Dictionaries = model.Dictionaries,
                Visualisations = model.Visualisations,
                Roles = model.Roles,
                AdvancedAnalytics = model.AdvancedAnalytics
            }, (byte)model.SnapshotSourceId, model.Name, token);
        }

        private async Task<Data.Poco.PreservationSnapshot> RestoreAsync(int id,
            CancellationToken token)
        {
            var library = new Library(dbContext, userName);

            try
            {
                return await library.ImportSnapshotAsync(id, token).ConfigureAwait(false);
            }
            catch (KeyNotFoundException ex)
            {
                throw new NotFoundException("The snapshot was not found.", ex);
            }
        }

        private void EnsureImportPermitted(string op)
        {
            if (permissionValidation.Validate(importPermissions))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn(
                    $"{op}: snapshot import permission denied user={userName} specs=[{string.Join(",", importPermissions)}]");
            }

            throw new ForbiddenException(strings[PreservationResources.PermissionDenied], importPermissions);
        }

        private PreservationComparer Comparer()
        {
            return new PreservationComparer(dbContext, userName, tenantRegistryId, strings, log);
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