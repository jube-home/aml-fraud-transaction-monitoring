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
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Repository.SanctionEntrySource;
using Jube.Engine.Sanctions;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Repository.SanctionEntrySource;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.VisualBasic.FileIO;
using SanctionEntrySourcePoco = Jube.Data.Poco.SanctionEntrySource;

namespace Jube.Service.Repository.SanctionEntrySource
{
    public sealed class SanctionEntrySourceService
    {
        private const int MaxListTake = 200;
        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly SanctionEntrySourceRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private SanctionEntrySourceService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new SanctionEntrySourceRepository(dbContext);
        }

        public static Task<SanctionEntrySourceService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<SanctionEntrySourceService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(SanctionEntrySourceResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("SanctionEntrySource.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[SanctionEntrySourceResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"SanctionEntrySource.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[SanctionEntrySourceResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new SanctionEntrySourceService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every configured Sanction Entry Source (e.g. SDN, BOE, EU) -- the specifications the " +
                     "Sanctions Loader uses to poll or manually import a sanctions list. Unbounded -- intended for " +
                     "the administrative dropdown, not for agent tooling (use the bounded list operation " +
                     "instead). Landlord-only.")]
        public async Task<List<SanctionEntrySourceDto>> GetAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("SanctionEntrySource", "List", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"SanctionEntrySource.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("SanctionEntrySource.List");

                var dtos = SanctionEntrySourceMapper.ToDto(await repository.GetAsync(token).ConfigureAwait(false));
                op.Rows(dtos.Count);

                if (log.IsDebugEnabled)
                {
                    log.Debug($"SanctionEntrySource.List: {dtos.Count} rows user={userName}");
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
                if (log.IsDebugEnabled)
                {
                    log.Debug($"SanctionEntrySource.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"SanctionEntrySource.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists configured Sanction Entry Sources, ordered by id, capped at 'take' rows (max 200). " +
                     "If more rows exist, call again with 'afterId' set to the last returned Id to continue. " +
                     "Landlord-only.")]
        [ServiceOperation("SanctionEntrySourceList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<SanctionEntrySourceDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("SanctionEntrySource", "ListPaged", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            if (log.IsDebugEnabled)
            {
                log.Debug($"SanctionEntrySource.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");
            }

            try
            {
                EnsurePermitted("SanctionEntrySource.ListPaged");

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);

                return new PagedResult<SanctionEntrySourceDto>(SanctionEntrySourceMapper.ToDto(page));
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled)
                {
                    log.Debug($"SanctionEntrySource.ListPaged: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"SanctionEntrySource.ListPaged: unexpected failure user={userName}", ex);
                throw;
            }
        }

        public async Task ImportAsync(Stream? file, long fileLength, int sanctionEntrySourceId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("SanctionEntrySource", "Import", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug(
                    $"SanctionEntrySource.Import: entry sanctionEntrySourceId={sanctionEntrySourceId} user={userName}");
            }

            try
            {
                EnsurePermitted("SanctionEntrySource.Import");

                if (file is null || fileLength <= 0)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"SanctionEntrySource.Import: no file supplied sanctionEntrySourceId={sanctionEntrySourceId} user={userName}");
                    }

                    throw new DtoValidationException(new ValidationResult([
                        new ValidationFailure("files", strings[SanctionEntrySourceResources.FileRequired])
                            { ErrorCode = "FilesNotEmpty" }
                    ]));
                }

                var sanctionEntrySource = await repository.GetByIdAsync(sanctionEntrySourceId, token)
                    .ConfigureAwait(false);

                if (sanctionEntrySource is null)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"SanctionEntrySource.Import: sanctionEntrySourceId={sanctionEntrySourceId} not found user={userName}");
                    }

                    throw new DtoValidationException(new ValidationResult([
                        new ValidationFailure("sanctionEntrySourceId",
                                strings[SanctionEntrySourceResources.SanctionEntrySourceNotFound])
                            { ErrorCode = "SanctionEntrySourceIdNotFound" }
                    ]));
                }

                if (sanctionEntrySource.Delimiter is null)
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug(
                            $"SanctionEntrySource.Import: sanctionEntrySourceId={sanctionEntrySourceId} has no " +
                            $"Delimiter configured; skipping import (legacy behaviour preserved) user={userName}");
                    }

                    return;
                }

                using var textFieldParser = new TextFieldParser(file);
                textFieldParser.Delimiters = [sanctionEntrySource.Delimiter.Value.ToString()];
                textFieldParser.TextFieldType = FieldType.Delimited;

                var sanctionEntryImport =
                    await ProcessImportAsync(textFieldParser, sanctionEntrySource, token).ConfigureAwait(false);

                op.Entity(sanctionEntryImport.Id);
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"SanctionEntrySource.Import: sanctionEntrySourceId={sanctionEntrySourceId} " +
                        $"importId={sanctionEntryImport.Id} successful={sanctionEntryImport.Successful} " +
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
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"SanctionEntrySource.Import: cancelled sanctionEntrySourceId={sanctionEntrySourceId} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"SanctionEntrySource.Import: unexpected failure sanctionEntrySourceId={sanctionEntrySourceId} user={userName}",
                    ex);
                throw;
            }
        }

        private async Task<SanctionEntryImport> ProcessImportAsync(TextFieldParser textFieldParser,
            SanctionEntrySourcePoco sanctionEntrySource, CancellationToken token)
        {
            var sanctionsEntryRepository = new SanctionsEntryRepository(dbContext);
            var sanctionEntryImportRepository = new SanctionEntryImportRepository(dbContext);
            var sanctionEntryRejectionRepository = new SanctionEntryRejectionRepository(dbContext);

            var sanctionEntryImport = await sanctionEntryImportRepository.InsertAsync(new SanctionEntryImport
            {
                SanctionEntrySourceId = sanctionEntrySource.Id,
                StartDate = DateTime.UtcNow,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow
            }, token).ConfigureAwait(false);

            var inserted = 0;
            var revived = 0;
            var unchanged = 0;

            try
            {
                var result = await SanctionEntryFileImporter.ImportAsync(textFieldParser, sanctionEntrySource.Id,
                    sanctionEntrySource.MultiPartStringIndex, sanctionEntrySource.ReferenceIndex,
                    sanctionEntrySource.Skip ?? 0,
                    async (record, ct) =>
                    {
                        var sanctionEntry = new SanctionEntry
                        {
                            SanctionEntryElementValue = record.ElementValue,
                            SanctionEntrySourceId = sanctionEntrySource.Id,
                            SanctionPayload = record.Payload,
                            SanctionEntryReference = record.Reference,
                            SanctionEntryHash = record.Hash,
                            CreatedDate = DateTime.UtcNow,
                            CreatedUser = userName
                        };

                        var (_, outcome) = await sanctionsEntryRepository.UpsertAsync(sanctionEntry, ct)
                            .ConfigureAwait(false);

                        switch (outcome)
                        {
                            case SanctionEntryUpsertOutcome.Inserted:
                                inserted++;
                                break;
                            case SanctionEntryUpsertOutcome.Revived:
                                revived++;
                                break;
                            default:
                                unchanged++;
                                break;
                        }
                    },
                    async (rejection, ct) =>
                    {
                        await sanctionEntryRejectionRepository.InsertAsync(new SanctionEntryRejection
                        {
                            SanctionEntryImportId = sanctionEntryImport.Id,
                            SanctionEntrySourceId = sanctionEntrySource.Id,
                            RowNumber = rejection.RowNumber,
                            RawData = rejection.RawData,
                            ReasonId = (int)rejection.ReasonId,
                            CreatedDate = DateTime.UtcNow
                        }, ct).ConfigureAwait(false);
                    },
                    log,
                    token).ConfigureAwait(false);

                var removed = await SanctionEntryFileImporter.ReconcileRemovedAsync(sanctionsEntryRepository,
                    sanctionEntrySource.Id, result.Hashes, userName, log, token).ConfigureAwait(false);

                sanctionEntryImport.EndDate = DateTime.UtcNow;
                sanctionEntryImport.TotalRows = result.TotalRows;
                sanctionEntryImport.InsertedCount = inserted;
                sanctionEntryImport.RevivedCount = revived;
                sanctionEntryImport.UnchangedCount = unchanged;
                sanctionEntryImport.RemovedCount = removed.Count;
                sanctionEntryImport.RejectedCount = result.RejectedRows;
                sanctionEntryImport.Successful = 1;

                await sanctionEntryImportRepository.UpdateAsync(sanctionEntryImport, token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                sanctionEntryImport.EndDate = DateTime.UtcNow;
                sanctionEntryImport.Successful = 0;
                sanctionEntryImport.ErrorMessage = ex.Message;

                await sanctionEntryImportRepository.UpdateAsync(sanctionEntryImport, token).ConfigureAwait(false);

                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"SanctionEntrySource.Import: sanctionEntrySourceId={sanctionEntrySource.Id} " +
                        $"importId={sanctionEntryImport.Id} produced an error and was recorded as unsuccessful " +
                        $"user={userName}", ex);
                }
            }

            return sanctionEntryImport;
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.Landlord)
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied (not landlord) user={userName}");
            }

            throw new ForbiddenException(strings[SanctionEntrySourceResources.PermissionDenied]);
        }
    }
}