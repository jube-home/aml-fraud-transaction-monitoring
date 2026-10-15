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
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Jube.Analytics.Datasets;
using Jube.Analytics.Datasets.Models;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Data.Repository.Interfaces;
using Jube.Dto.ExhaustiveInstanceDataset;
using Jube.Resources;
using Jube.Service.Exceptions.ExhaustiveInstanceDataset;
using Jube.Service.Exceptions.ExhaustiveSearchInstance;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.ExhaustiveInstanceDataset;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.ExhaustiveInstanceDataset
{
    public sealed class ExhaustiveInstanceDatasetService
    {
        private const int RowBatchSize = 1000;
        private const int MaxFileNameLength = 200;
        private const int MaxSourceLabelLength = 256;
        private static readonly int[] permissions = [16];
        private static readonly string[] csvExtensions = [".csv", ".txt"];
        private static readonly string[] jsonExtensions = [".json"];

        private static readonly string[] jsonContentTypes =
        [
            "application/json", "text/json", "text/plain", "application/octet-stream"
        ];

        private static readonly string[] allowedContentTypes =
        [
            "text/csv", "text/plain", "application/csv", "application/vnd.ms-excel", "application/octet-stream",
            "text/comma-separated-values", "application/x-csv", "text/x-csv"
        ];

        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly ExhaustiveInstanceDatasetRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private ExhaustiveInstanceDatasetService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, DatasetLimits limits)
        {
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            Limits = limits;
            repository = new ExhaustiveInstanceDatasetRepository(dbContext, tenantRegistryId, userName);
        }

        public DatasetLimits Limits { get; }

        public static Task<ExhaustiveInstanceDatasetService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, DatasetLimits limits, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, limits,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<ExhaustiveInstanceDatasetService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, DatasetLimits limits, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(ExhaustiveInstanceDatasetResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new NotAuthenticatedException(strings[ExhaustiveInstanceDatasetResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                throw new NotAuthenticatedException(strings[ExhaustiveInstanceDatasetResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new ExhaustiveInstanceDatasetService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, limits);
        }

        [Description(
            "Lists the uploaded Exhaustive training datasets of the caller's tenant, both CSV and JSON unless a kind is given, without column detail. Each row shows its kind.")]
        public async Task<List<ExhaustiveInstanceDatasetDto>> GetAsync(DatasetKind? kind = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("ExhaustiveInstanceDataset", "List", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("ExhaustiveInstanceDataset.List");

                var dtos = (await repository.GetAsync(kind, token).ConfigureAwait(false))
                    .Select(s => ExhaustiveInstanceDatasetMapper.ToDto(s, false)).ToList();
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
                log.Error($"ExhaustiveInstanceDataset.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Returns one uploaded Exhaustive training dataset with its columns and warnings, scoped to " +
                     "the caller's tenant. Returns null when it does not exist or is not visible.")]
        public async Task<ExhaustiveInstanceDatasetDto?> GetByIdAsync(
            [Description("Csv or Json: the store the dataset lives in.")]
            DatasetKind kind,
            [Description("Numeric identifier of the dataset.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("ExhaustiveInstanceDataset", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("ExhaustiveInstanceDataset.Get");
                var dataset = await repository.GetByIdAsync(kind, id, token).ConfigureAwait(false);

                if (dataset == null)
                {
                    return null;
                }

                op.Entity(dataset.Id);

                return ExhaustiveInstanceDatasetMapper.ToDto(dataset, true);
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
                log.Error($"ExhaustiveInstanceDataset.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Step one of the two step upload for a CSV file. Streams a CSV (UTF-8, header row, comma or " +
                     "semicolon) into a pending dataset of the caller's tenant and returns the validation report: " +
                     "row count, column types, missing counts, class balance candidates and warnings. Nothing is " +
                     "usable as training data until the mapping is confirmed. A rejected file is not stored at all.")]
        public Task<ExhaustiveInstanceDatasetDto> UploadCsvAsync(string? name, string? fileName,
            string? contentType, Stream content, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(content);

            return UploadCoreAsync("UploadCsv", DatasetKind.Csv, name, () =>
            {
                var safeFileName = SanitiseFileName(fileName, csvExtensions);

                if (safeFileName == null)
                {
                    throw new UploadRejectedException(
                        strings[ExhaustiveInstanceDatasetResources.FileNameInvalid]);
                }

                if (!IsAcceptableContentType(contentType))
                {
                    throw new UploadRejectedException(
                        strings[ExhaustiveInstanceDatasetResources.ContentTypeInvalid]);
                }

                return (safeFileName, Path.GetFileNameWithoutExtension(safeFileName));
            }, content, DatasetCsvParser.ParseAsync, token);
        }

        [Description("Step one of the two step upload for JSON supplied directly, without a file. Accepts an array " +
                     "of flat objects or the columnar form {\"columns\":[...],\"rows\":[[...]]} as a stream, " +
                     "parses it with the same caps, inspection and two step confirmation as a CSV upload and " +
                     "returns the same validation report. The source label describes where the JSON came from.")]
        public async Task<ExhaustiveInstanceDatasetDto> UploadJsonAsync(string? name, string? sourceLabel,
            string? contentType, Stream content, CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(content);
            using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var hashing = new HashingStream(content, hasher);

            return await UploadCoreAsync("UploadJson", DatasetKind.Json, name, () =>
                {
                    var label = SanitiseSourceLabel(sourceLabel);

                    if (label == null)
                    {
                        throw new UploadRejectedException(
                            strings[ExhaustiveInstanceDatasetResources.SourceLabelInvalid]);
                    }

                    if (!IsAcceptableJsonContentType(contentType))
                    {
                        throw new UploadRejectedException(
                            strings[ExhaustiveInstanceDatasetResources.ContentTypeInvalid]);
                    }

                    return (label, Path.GetFileNameWithoutExtension(label));
                }, hashing, DatasetJsonParser.ParseAsync, token,
                () => Convert.ToHexString(hasher.GetCurrentHash()).ToLowerInvariant()).ConfigureAwait(false);
        }

        [Description("Step one of the two step upload for a JSON file (name ends .json). The file name, reduced to " +
                     "its leaf, becomes the source label. Otherwise identical to the stream form.")]
        public Task<ExhaustiveInstanceDatasetDto> UploadJsonFileAsync(string? name, string? fileName,
            string? contentType, Stream content, CancellationToken token = default)
        {
            var leaf = SanitiseJsonFileName(fileName);

            if (leaf == null)
            {
                throw new UploadRejectedException(strings[ExhaustiveInstanceDatasetResources.FileNameInvalid]);
            }

            return UploadJsonAsync(name, leaf, contentType, content, token);
        }

        [Description("Step one of the two step upload for JSON supplied as text in hand rather than a stream. " +
                     "Identical to the stream form: the same shapes, caps, inspection and confirmation.")]
        public async Task<ExhaustiveInstanceDatasetDto> UploadJsonAsync(string? name, string? sourceLabel,
            string? json, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new UploadRejectedException(strings[ExhaustiveInstanceDatasetResources.JsonRequired]);
            }

            if (json.Length > Limits.MaxBytes)
            {
                throw new UploadRejectedException(strings[ExhaustiveInstanceDatasetResources.UploadTooLarge]);
            }

            byte[] bytes;

            try
            {
                bytes = new UTF8Encoding(false, true).GetBytes(json);
            }
            catch (EncoderFallbackException)
            {
                throw new UploadRejectedException(strings[ExhaustiveInstanceDatasetResources.JsonRequired]);
            }

            await using var stream = new MemoryStream(bytes, false);

            return await UploadJsonAsync(name, sourceLabel, "application/json", stream, token).ConfigureAwait(false);
        }

        private async Task<ExhaustiveInstanceDatasetDto> UploadCoreAsync(string operation, DatasetKind kind,
            string? name, Func<(string Source, string NameBase)> validate, Stream content,
            Func<Stream, DatasetLimits, Func<DatasetRow, CancellationToken, ValueTask>, CancellationToken,
                Task<DatasetParseReport>> parse, CancellationToken token, Func<string>? contentHash = null)
        {
            using var op = OperationScope.Start("ExhaustiveInstanceDataset", operation, userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            var transactionOpen = false;

            try
            {
                EnsurePermitted("ExhaustiveInstanceDataset." + operation);

                var (source, nameBase) = validate();
                var displayName = await ResolveNameAsync(name, nameBase, token).ConfigureAwait(false);

                await dbContext.BeginTransactionAsync(token).ConfigureAwait(false);
                transactionOpen = true;

                var datasetId = kind == DatasetKind.Csv
                    ? (await repository.InsertAsync(new ExhaustiveInstanceCsv
                    {
                        Name = displayName, FileName = source,
                        StatusId = ExhaustiveInstanceDatasetRepository.StatusPending
                    }, token).ConfigureAwait(false)).Id
                    : (await repository.InsertAsync(new ExhaustiveInstanceJson
                    {
                        Name = displayName, SourceLabel = source,
                        StatusId = ExhaustiveInstanceDatasetRepository.StatusPending
                    }, token).ConfigureAwait(false)).Id;

                var batch = new List<IDatasetRow>(RowBatchSize);

                var report = await parse(content, Limits, async (row, ct) =>
                {
                    batch.Add(ExhaustiveInstanceDatasetRepository.NewRow(kind, datasetId, Guid.NewGuid(),
                        DatasetRowJson.Serialise(row.Cells)));

                    if (batch.Count >= RowBatchSize)
                    {
                        await repository.InsertRowsAsync(kind, batch, ct).ConfigureAwait(false);
                        batch.Clear();
                    }
                }, token).ConfigureAwait(false);

                if (!report.Success)
                {
                    await dbContext.RollbackTransactionAsync(token).ConfigureAwait(false);
                    transactionOpen = false;
                    op.Outcome("invalid");

                    if (log.IsWarnEnabled)
                    {
                        log.Warn(
                            $"ExhaustiveInstanceDataset.{operation}: rejected kind={kind} user={userName} code={report.Errors.FirstOrDefault()?.Code}");
                    }

                    throw new DatasetRejectedException(report);
                }

                if (batch.Count > 0)
                {
                    await repository.InsertRowsAsync(kind, batch, token).ConfigureAwait(false);
                }

                await repository.UpdateInspectionAsync(kind, datasetId, JsonSerializer.Serialize(report.Columns),
                        JsonSerializer.Serialize(new ExhaustiveInstanceDatasetMapper.DatasetStoredReport
                        {
                            Warnings = report.Warnings, BlankLineCount = report.BlankLineCount
                        }), report.RowCount, report.BytesRead,
                        contentHash != null ? contentHash() : report.Delimiter.ToString(), token)
                    .ConfigureAwait(false);

                await dbContext.CommitTransactionAsync(token).ConfigureAwait(false);
                transactionOpen = false;

                op.Entity(datasetId);
                op.Created();
                op.Rows((int)Math.Min(report.RowCount, int.MaxValue));

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"ExhaustiveInstanceDataset.{operation}: created kind={kind} Id={datasetId} rows={report.RowCount} columns={report.Columns.Count} user={userName}");
                }

                var stored = await repository.GetByIdAsync(kind, datasetId, token).ConfigureAwait(false);

                return ExhaustiveInstanceDatasetMapper.ToDto(stored, true);
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (DatasetRejectedException)
            {
                throw;
            }
            catch (UploadRejectedException)
            {
                op.Outcome("invalid");
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
                await RollbackQuietlyAsync(transactionOpen).ConfigureAwait(false);
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"ExhaustiveInstanceDataset.{operation}: unexpected failure user={userName}", ex);
                await RollbackQuietlyAsync(transactionOpen).ConfigureAwait(false);
                throw;
            }
        }

        [Description("Step two of the upload. Validates and saves the column mapping (dependent column, included " +
                     "columns, variable names and processing types) and makes the dataset usable as training data.")]
        public async Task<ExhaustiveInstanceDatasetDto> ConfirmAsync(
            [Description("Csv or Json: the store the dataset lives in.")]
            DatasetKind kind,
            [Description("The mapping to confirm.")]
            ExhaustiveInstanceDatasetMappingDto? mapping,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("ExhaustiveInstanceDataset", "Confirm", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(mapping);
                EnsurePermitted("ExhaustiveInstanceDataset.Confirm");

                var dataset = await repository.GetByIdAsync(kind, mapping.Id, token).ConfigureAwait(false);

                if (dataset == null)
                {
                    throw new NotFoundException(strings[ExhaustiveInstanceDatasetResources.NotFound]);
                }

                if (dataset.StatusId == ExhaustiveInstanceDatasetRepository.StatusConfirmed
                    && await repository.IsReferencedAsync(kind, dataset.Id, token).ConfigureAwait(false))
                {
                    throw new DatasetInUseException(
                        strings[ExhaustiveInstanceDatasetResources.AlreadyConfirmed]);
                }

                var columns = ExhaustiveInstanceDatasetMapper.ReadColumns(dataset.ColumnsJson);

                var results = await new ExhaustiveInstanceDatasetMappingDtoValidator(columns, repository, kind, strings)
                    .ValidateAsync(mapping, token).ConfigureAwait(false);

                if (!results.IsValid)
                {
                    throw new DtoValidationException(results);
                }

                foreach (var supplied in mapping.Columns!)
                {
                    var column = columns.First(f => f.Index == supplied.Index);
                    column.Include = supplied.Include;

                    if (column.Include)
                    {
                        column.Name = supplied.Name!;
                        column.ProcessingTypeId = supplied.ProcessingTypeId;
                    }
                }

                var name = string.IsNullOrWhiteSpace(mapping.Name) ? dataset.Name : mapping.Name.Trim();

                if (!await repository.UpdateMappingAsync(kind, dataset.Id, name, JsonSerializer.Serialize(columns),
                        token).ConfigureAwait(false))
                {
                    throw new NotFoundException(strings[ExhaustiveInstanceDatasetResources.NotFound]);
                }

                op.Entity(dataset.Id);
                op.Updated();

                var stored = await repository.GetByIdAsync(kind, dataset.Id, token).ConfigureAwait(false);

                return ExhaustiveInstanceDatasetMapper.ToDto(stored, true);
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
            catch (DatasetInUseException)
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
                log.Error($"ExhaustiveInstanceDataset.Confirm: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Deletes an uploaded Exhaustive training dataset and its rows. Refused while any " +
                     "non-deleted Exhaustive instance still references it, regardless of training status.")]
        public async Task DeleteAsync(
            [Description("Csv or Json: the store the dataset lives in.")]
            DatasetKind kind,
            [Description("Numeric identifier of the dataset to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("ExhaustiveInstanceDataset", "Delete", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);

            var transactionOpen = false;

            try
            {
                EnsurePermitted("ExhaustiveInstanceDataset.Delete");

                if (await repository.GetByIdAsync(kind, id, token).ConfigureAwait(false) == null)
                {
                    throw new NotFoundException(strings[ExhaustiveInstanceDatasetResources.NotFound]);
                }

                if (await repository.IsReferencedAsync(kind, id, token).ConfigureAwait(false))
                {
                    throw new DatasetInUseException(strings[ExhaustiveInstanceDatasetResources.DatasetInUse]);
                }

                await dbContext.BeginTransactionAsync(token).ConfigureAwait(false);
                transactionOpen = true;
                await repository.DeleteAsync(kind, id, token).ConfigureAwait(false);
                await dbContext.CommitTransactionAsync(token).ConfigureAwait(false);
                transactionOpen = false;

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
                op.Outcome("notfound");
                throw;
            }
            catch (DatasetInUseException)
            {
                op.Outcome("conflict");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                await RollbackQuietlyAsync(transactionOpen).ConfigureAwait(false);
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"ExhaustiveInstanceDataset.Delete: unexpected failure id={id} user={userName}", ex);
                await RollbackQuietlyAsync(transactionOpen).ConfigureAwait(false);
                throw;
            }
        }

        internal static string? SanitiseFileName(string? fileName, string[] allowedExtensions)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            var lastSeparator = fileName.LastIndexOfAny(['/', '\\']);
            var leaf = (lastSeparator >= 0 ? fileName[(lastSeparator + 1)..] : fileName).Trim();

            if (leaf.Length == 0 || leaf.Length > MaxFileNameLength || leaf.Any(c =>
                    char.IsControl(c) || c is '<' or '>' or '"' or '|' or '?' or '*' or ':')
                || (leaf.StartsWith('.') && leaf.IndexOf('.', 1) < 0))
            {
                return null;
            }

            var extension = Path.GetExtension(leaf).ToLowerInvariant();

            return allowedExtensions.Contains(extension) ? leaf : null;
        }

        internal static string? SanitiseFileName(string? fileName)
        {
            return SanitiseFileName(fileName, csvExtensions);
        }

        internal static string? SanitiseJsonFileName(string? fileName)
        {
            return SanitiseFileName(fileName, jsonExtensions);
        }

        internal static string? SanitiseSourceLabel(string? label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                return "Direct JSON";
            }

            var trimmed = label.Trim();

            if (trimmed.Length > MaxSourceLabelLength
                || trimmed.Contains("..", StringComparison.Ordinal)
                || trimmed.Any(c => char.IsControl(c) || c is '<' or '>' or '"' or '|' or '?' or '*' or '/' or '\\'))
            {
                return null;
            }

            return trimmed;
        }

        internal static bool IsAcceptableContentType(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return true;
            }

            var mediaType = contentType.Split(';')[0].Trim().ToLowerInvariant();

            return allowedContentTypes.Contains(mediaType);
        }

        internal static bool IsAcceptableJsonContentType(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return true;
            }

            var parts = contentType.Split(';');

            if (!jsonContentTypes.Contains(parts[0].Trim().ToLowerInvariant()))
            {
                return false;
            }

            foreach (var parameter in parts.Skip(1))
            {
                var pair = parameter.Split('=', 2);

                if (pair.Length == 2 && pair[0].Trim().Equals("charset", StringComparison.OrdinalIgnoreCase)
                                     && !pair[1].Trim().Trim('"').Equals("utf-8", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private async Task<string> ResolveNameAsync(string? name, string baseName, CancellationToken token)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                var supplied = name.Trim();

                if (supplied.Length > ExhaustiveInstanceDatasetMappingDtoValidator.MaxNameLength
                    || !NameText.IsAcceptable(supplied))
                {
                    throw new UploadRejectedException(strings[ExhaustiveInstanceDatasetResources.NameRequired]);
                }

                if (await repository.NameExistsAsync(supplied, null, null, token).ConfigureAwait(false))
                {
                    throw new UploadRejectedException(
                        strings[ExhaustiveInstanceDatasetResources.NameAlreadyExists]);
                }

                return supplied;
            }

            if (baseName.Length == 0)
            {
                baseName = "Dataset";
            }

            if (baseName.Length > 200)
            {
                baseName = baseName[..200];
            }

            var candidate = baseName;

            for (var suffix = 2;
                 await repository.NameExistsAsync(candidate, null, null, token).ConfigureAwait(false);
                 suffix++)
            {
                candidate = $"{baseName} ({suffix})";
            }

            return candidate;
        }

        private async Task RollbackQuietlyAsync(bool transactionOpen)
        {
            if (!transactionOpen)
            {
                return;
            }

            try
            {
                await dbContext.RollbackTransactionAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error("ExhaustiveInstanceDataset: rollback failed", ex);
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

            throw new ForbiddenException(strings[ExhaustiveInstanceDatasetResources.PermissionDenied],
                permissions);
        }
    }
}