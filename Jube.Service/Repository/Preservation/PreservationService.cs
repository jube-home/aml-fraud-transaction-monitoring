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
using Jube.Cryptography;
using Jube.Cryptography.Exceptions;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Repository.Preservation;
using Jube.Preservation.Exceptions;
using Jube.Preservation.Models;
using Jube.Resources;
using Jube.Service.Exceptions.Repository.Preservation;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Reactivity.Models;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.Preservation
{
    using Library = Jube.Preservation.Preservation;

    public sealed class PreservationService
    {
        private static readonly int[] permissions = [38];

        private static readonly string[] importedAreas =
            ["Preservation", "EntityAnalysisModel", "RoleRegistry", "VisualisationRegistry", "CaseWorkflow"];

        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private PreservationService(DbContext dbContext, string userName, int tenantRegistryId,
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

        public static Task<PreservationService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<PreservationService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
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
                permissionValidation, log, auditLog, serviceChangeBus, strings);
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

                var library = new Library(dbContext, userName);

                await library.ImportAsync(bytes, new ImportOptions
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
            catch (Exception ex) when (ex is InvalidDecryptionException or InvalidAdvancedAnalyticsPreservationException)
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

        [Description("Works out what importing an encrypted Jube export package (.jemp) would do to the caller's own " +
                     "tenant, without changing anything: the live configuration compared with what would replace " +
                     "it, the users who would be left without a role, and anything the caller should know first. " +
                     "The comparison is recorded for audit.")]
        public async Task<PreservationImportPreviewDto> ImportPreviewAsync(
            [Description("The uploaded package file(s); only the first is read.")]
            IReadOnlyList<PreservationImportFileDto> files,
            [Description("The password the package was exported with.")]
            string? password,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("Preservation", "ImportPreview", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("Preservation.ImportPreview");

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

                var preview = await Comparer().PreviewPackageAsync(bytes, password, token).ConfigureAwait(false);

                op.Rows(preview.Diff.Entries.Count);

                return preview;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NoFileException)
            {
                throw;
            }
            catch (Exception ex) when (ex is InvalidDecryptionException or InvalidAdvancedAnalyticsPreservationException)
            {
                op.Outcome("invalid-file");

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
                log.Error($"Preservation.ImportPreview: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Describes what an export of the caller's own tenant would carry, section by section, with how " +
                     "many entities and how many bytes each include switch adds. Nothing is changed.")]
        public async Task<PreservationSummaryDto> SummaryAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("Preservation", "Summary", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("Preservation.Summary");

                var summary = await Comparer().SummaryAsync(token).ConfigureAwait(false);

                summary.MinimumPasswordLength = JempAesEncryption.MinimumPasswordLength;

                op.Rows(summary.Sections.Count);

                return summary;
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
                log.Error($"Preservation.Summary: unexpected failure user={userName}", ex);
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

                var library = new Library(dbContext, userName);

                var export = await library.ExportAsync(new ExportOptions
                {
                    Password = model.Password,
                    Exhaustive = model.Exhaustive,
                    Lists = model.Lists,
                    Dictionaries = model.Dictionaries,
                    Visualisations = model.Visualisations,
                    Roles = model.Roles,
                    AdvancedAnalytics = model.AdvancedAnalytics
                }, token).ConfigureAwait(false);

                op.Rows(1);

                return new PreservationExportDto { Guid = export.Guid, EncryptedBytes = export.EncryptedBytes };
            }
            catch (InvalidPasswordException ex)
            {
                op.Outcome("weak-password");

                throw new WeakPasswordException(strings[PreservationResources.PasswordTooShort], ex);
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

        private PreservationComparer Comparer()
        {
            return new PreservationComparer(dbContext, userName, tenantRegistryId, strings, log);
        }

        private async Task PublishImportedAsync(CancellationToken token)
        {
            foreach (var area in importedAreas)
            {
                try
                {
                    await serviceChangeBus.PublishAsync(new ServiceChangeEvent(area, tenantRegistryId,
                        ServiceChangeKind.Updated, null, null, userName, DateTimeOffset.UtcNow,
                        Activity.Current?.TraceId.ToString() ?? "-"), token).ConfigureAwait(false);
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