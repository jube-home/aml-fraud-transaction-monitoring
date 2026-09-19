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
using Jube.Cryptography.Exceptions;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Repository.Preservation;
using Jube.Resources;
using Jube.Service.Exceptions.Repository.Preservation;
using Jube.Service.Observability;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.Preservation
{
    using Library = global::Jube.Preservation.Preservation;

    public sealed class PreservationService
    {
        private static readonly int[] permissions = [38];

        private static readonly string[] importedAreas =
            ["Preservation", "EntityAnalysisModel", "RoleRegistry", "VisualisationRegistry", "CaseWorkflow"];

        private readonly ILog auditLog;
        private readonly bool legacyFallback;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly string? salt;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private PreservationService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, string? salt, bool legacyEncryptionFallback)
        {
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.salt = salt;
            legacyFallback = legacyEncryptionFallback;
        }

        public static Task<PreservationService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            string? preservationSalt, bool legacyEncryptionFallback, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), preservationSalt, legacyEncryptionFallback, token);
        }

        internal static async Task<PreservationService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, string? preservationSalt,
            bool legacyEncryptionFallback, CancellationToken token = default)
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

            return new PreservationService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, preservationSalt,
                legacyEncryptionFallback);
        }

        [Description("Imports an encrypted Jube export package (.jemp) into the caller's own tenant, REPLACING the " +
                     "tenant's existing configuration. The first file supplied is used. Runs in a single " +
                     "transaction; on any failure nothing is changed.")]
        public async Task ImportAsync(
            [Description("The uploaded package file(s); only the first is read.")]
            IReadOnlyList<PreservationImportFileDto> files,
            [Description("The password the package was exported with.")]
            string? password,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("Preservation", "Import", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"Preservation.Import: entry user={userName}");
            }

            try
            {
                EnsurePermitted("Preservation.Import");

                if (files.Count <= 0)
                {
                    op.Outcome("no-file");
                    throw new NoFileException(strings[PreservationResources.NoFileSupplied]);
                }

                byte[] bytes;
                await using (var ms = new MemoryStream())
                {
                    await files[0].Content.CopyToAsync(ms, token).ConfigureAwait(false);
                    bytes = ms.ToArray();
                }

                var library = new Library(dbContext, userName, salt, legacyFallback);
                await library.ImportAsync(bytes, new global::Jube.Preservation.ImportOptions
                {
                    Password = password
                }, token).ConfigureAwait(false);

                op.Rows(1);
                await PublishImportedAsync(token).ConfigureAwait(false);
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NoFileException)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"Preservation.Import: no file supplied user={userName}");
                }

                throw;
            }
            catch (Exception ex) when (ex is InvalidHmacException or InvalidDecryptionException)
            {
                op.Outcome("invalid-file");
                if (log.IsWarnEnabled)
                {
                    log.Warn($"Preservation.Import: package could not be decrypted user={userName}");
                }

                throw new InvalidPreservationFileException(strings[PreservationResources.InvalidPreservationFile],
                    ex);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"Preservation.Import: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Previews, as YAML text, what an export with the given options would contain for the " +
                     "caller's own tenant. Nothing is changed. Roles are never included in a preview.")]
        public async Task<string> ExportPeekAsync(
            [Description("Include Exhaustive Search Instances.")]
            bool exhaustive,
            [Description("Include Suppressions.")] bool suppressions,
            [Description("Include Lists.")] bool lists,
            [Description("Include Dictionaries.")] bool dictionaries,
            [Description("Include Visualisations.")]
            bool visualisations,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("Preservation", "ExportPeek", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"Preservation.ExportPeek: entry user={userName}");
            }

            try
            {
                EnsurePermitted("Preservation.ExportPeek");

                var library = new Library(dbContext, userName);
                var peek = await library.ExportPeekAsync(new global::Jube.Preservation.ExportOptions
                {
                    Exhaustive = exhaustive,
                    Suppressions = suppressions,
                    Lists = lists,
                    Dictionaries = dictionaries,
                    Visualisations = visualisations
                }, token).ConfigureAwait(false);

                op.Rows(1);
                return peek.Yaml;
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
                log.Error($"Preservation.ExportPeek: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Exports the caller's own tenant configuration as an encrypted Jube package (.jemp).")]
        public async Task<PreservationExportDto> ExportAsync(
            [Description("Export options, including the encryption password.")]
            ImportExportOptionsDto model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("Preservation", "Export", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"Preservation.Export: entry user={userName}");
            }

            try
            {
                EnsurePermitted("Preservation.Export");

                var library = new Library(dbContext, userName, salt, legacyFallback);
                var export = await library.ExportAsync(new global::Jube.Preservation.ExportOptions
                {
                    Password = model.Password,
                    Exhaustive = model.Exhaustive,
                    Suppressions = model.Suppressions,
                    Lists = model.Lists,
                    Dictionaries = model.Dictionaries,
                    Visualisations = model.Visualisations,
                    Roles = model.Roles
                }, token).ConfigureAwait(false);

                op.Rows(1);
                return new PreservationExportDto { Guid = export.Guid, EncryptedBytes = export.EncryptedBytes };
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
                log.Error($"Preservation.Export: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private async Task PublishImportedAsync(CancellationToken token)
        {
            foreach (var area in importedAreas)
            {
                try
                {
                    await serviceChangeBus.PublishAsync(new ServiceChangeEvent(area, tenantRegistryId,
                        ServiceChangeKind.Updated, null, null, userName, DateTimeOffset.UtcNow,
                        System.Diagnostics.Activity.Current?.TraceId.ToString() ?? "-"), token).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Preservation.Import: change-event publish failed for {area}", ex);
                    }
                }
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

            throw new ForbiddenException(strings[PreservationResources.PermissionDenied], permissions);
        }
    }
}