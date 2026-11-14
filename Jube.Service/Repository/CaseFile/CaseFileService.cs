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
using System.Text.RegularExpressions;
using FluentValidation.Results;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Repository.CaseFile;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Repository.CaseFile;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseFile.Interfaces;
using Jube.Service.Repository.CaseFile.Models;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;
using RulePoco = Jube.Data.Poco.CaseFile;
using RuleRepository = Jube.Data.Repository.CaseFileRepository;

namespace Jube.Service.Repository.CaseFile
{
    public sealed class CaseFileService
    {
        public const int MaxFileBytes = 25 * 1024 * 1024;
        private const int MaxFileNameLength = 255;
        private const string UploadedDocumentNote = "Uploaded Document";
        private static readonly int[] permissions = [1];
        private static readonly int[] supervisionPermissions = [18];
        private readonly ILog auditLog;
        private readonly CaseNoteRepository caseNoteRepository;
        private readonly CaseResearchRepository caseResearchRepository;
        private readonly CaseRepository caseRepository;
        private readonly ILog log;
        private readonly ICaseFileScanner scanner;
        private readonly CaseFileTypePolicy typePolicy;
        private readonly PermissionValidation permissionValidation;
        private readonly RuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaseFileService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, CaseFileTypePolicy typePolicy, ICaseFileScanner scanner)
        {
            this.typePolicy = typePolicy;
            this.scanner = scanner;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            repository = new RuleRepository(dbContext, userName);
            caseRepository = new CaseRepository(dbContext, userName);
            caseNoteRepository = new CaseNoteRepository(dbContext, userName);
            caseResearchRepository = new CaseResearchRepository(dbContext, userName);
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseFileService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token: token);
        }

        public static Task<CaseFileService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment? dynamicEnvironment, ICaseFileScanner? scanner,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), dynamicEnvironment, scanner, token);
        }

        internal static async Task<CaseFileService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
            DynamicEnvironment.DynamicEnvironment? dynamicEnvironment = null, ICaseFileScanner? scanner = null,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseFileResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("CaseFile.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseFileResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseFile.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[CaseFileResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseFileService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation,
                log, auditLog, serviceChangeBus, strings, CaseFileTypePolicy.FromEnvironment(dynamicEnvironment),
                scanner ?? new NoOpCaseFileScanner());
        }

        [Description("Uploads and attaches a file to a case note of a Case, identified by its CaseKey/CaseKeyValue " +
                     "transaction identity. The note must be one the caller wrote on that Case; when no note is " +
                     "given, a note titled \"Uploaded Document\" is created to carry the file. Only the first " +
                     "non-empty file in the given set is stored; empty files are skipped. Not idempotent -- " +
                     "calling twice with the same file stores it twice.")]
        [ServiceOperation("CaseFileUpload", OperationKind.Write, Idempotent = false)]
        public async Task<CaseFileDto?> UploadAsync(
            [Description("The files to attach; only the first non-empty one is stored.")]
            IEnumerable<UploadedFileContent> files,
            [Description("Name of the key field identifying the transaction the file(s) attach to.")]
            string? caseKey,
            [Description("Value of the key field identifying the transaction the file(s) attach to.")]
            string? caseKeyValue,
            [Description("Identifier of the Case the file(s) attach to.")]
            int caseId,
            [Description("Identifier of the case note, written by the caller on that Case, to attach the file to. " +
                         "Omit or send zero to have a note created for the file.")]
            int? caseNoteId = null,
            [Description("Identifier of the case research, captured by the caller on that Case, to attach the file to. " +
                         "Send either a note or research, never both. Omit both to have a note created for the file.")]
            int? caseResearchId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseFile", "Upload", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseFile.Upload: entry caseId={caseId} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseFile.Upload");

                var failures = new List<ValidationFailure>();

                if (string.IsNullOrWhiteSpace(caseKey))
                {
                    failures.Add(new ValidationFailure(nameof(caseKey), strings[CaseFileResources.CaseKeyRequired])
                        { ErrorCode = "CaseKeyNotEmpty" });
                }

                if (string.IsNullOrWhiteSpace(caseKeyValue))
                {
                    failures.Add(new ValidationFailure(nameof(caseKeyValue),
                        strings[CaseFileResources.CaseKeyValueRequired]) { ErrorCode = "CaseKeyValueNotEmpty" });
                }

                if (caseId <= 0)
                {
                    failures.Add(new ValidationFailure(nameof(caseId), strings[CaseFileResources.CaseIdInvalid])
                        { ErrorCode = "CaseIdGreaterThan" });
                }

                if (failures.Count > 0)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseFile.Upload: validation failed caseId={caseId} user={userName} " +
                                 $"props=[{string.Join(",", failures.Select(f => f.PropertyName).Distinct())}]");
                    }

                    throw new DtoValidationException(new ValidationResult(failures));
                }

                var existingCase = await caseRepository.GetByIdActiveOnlyAsync(caseId, token).ConfigureAwait(false);

                if (existingCase is null)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseFile.Upload: case not found or not visible to tenant caseId={caseId} " +
                                 $"user={userName}");
                    }

                    throw new NotVisibleException(strings[CaseFileResources.NotVisible]);
                }

                if (CaseLockCheck.HeldByAnother(existingCase, userName))
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"CaseFile.Upload: caseId={caseId} is locked to another user user={userName}");
                    }

                    throw new CaseLockedException(strings[CaseFileResources.CaseLocked]);
                }

                Data.Poco.CaseNote? targetNote = null;
                Data.Poco.CaseResearch? targetResearch = null;

                if (caseNoteId is > 0 && caseResearchId is > 0)
                {
                    throw new DtoValidationException(new ValidationResult(
                    [
                        new ValidationFailure(nameof(caseResearchId), strings[CaseFileResources.OneParentOnly])
                            { ErrorCode = "OneParentOnly" }
                    ]));
                }

                if (caseResearchId is > 0)
                {
                    var research = await caseResearchRepository.GetByIdAsync(caseResearchId.Value, token)
                        .ConfigureAwait(false);

                    if (research is null || research.CaseId != caseId || research.CreatedUser != userName
                        || research.SourceId != (int)CaseResearchSource.User)
                    {
                        if (log.IsWarnEnabled)
                        {
                            log.Warn($"CaseFile.Upload: research {caseResearchId} not found, not on case {caseId} or " +
                                     $"not the caller's own user={userName}");
                        }

                        throw new NotVisibleException(strings[CaseFileResources.NotVisible]);
                    }

                    targetResearch = research;
                }

                if (caseNoteId is > 0)
                {
                    var note = await caseNoteRepository.GetByIdAsync(caseNoteId.Value, token).ConfigureAwait(false);

                    if (note is null || note.CaseId != caseId || note.CreatedUser != userName
                        || note.SourceId != (int)CaseNoteSource.User)
                    {
                        if (log.IsWarnEnabled)
                        {
                            log.Warn($"CaseFile.Upload: note {caseNoteId} not found, not on case {caseId} or not " +
                                     $"the caller's own user={userName}");
                        }

                        throw new NotVisibleException(strings[CaseFileResources.NotVisible]);
                    }

                    targetNote = note;
                }

                foreach (var file in files)
                {
                    await using var content = file.Content;

                    if (file.Length <= 0)
                    {
                        continue;
                    }

                    if (file.Length > MaxFileBytes)
                    {
                        throw new DtoValidationException(new ValidationResult(
                        [
                            new ValidationFailure("file", strings[CaseFileResources.FileTooLarge])
                                { ErrorCode = "FileMaximumSize" }
                        ]));
                    }

                    var safeName = SafeFileName(file.Name);

                    if (typePolicy.Evaluate(safeName, []) == CaseFileTypeVerdict.ExtensionNotAllowed)
                    {
                        throw Refused("FileTypeNotAllowed", strings[CaseFileResources.FileTypeNotAllowed]);
                    }

                    using var memoryStream = new MemoryStream();
                    await content.CopyToAsync(memoryStream, token).ConfigureAwait(false);
                    var bytes = memoryStream.ToArray();

                    if (typePolicy.Evaluate(safeName, bytes) != CaseFileTypeVerdict.Accepted)
                    {
                        throw Refused("FileContentNotAllowed", strings[CaseFileResources.FileContentNotAllowed]);
                    }

                    await ScanAsync(safeName, file.ContentType, bytes, token).ConfigureAwait(false);

                    if (targetResearch is null)
                    {
                        targetNote ??= await caseNoteRepository.InsertAsync(new Data.Poco.CaseNote
                        {
                            Note = UploadedDocumentNote,
                            CaseKey = existingCase.CaseKey,
                            CaseKeyValue = existingCase.CaseKeyValue,
                            CaseId = caseId,
                            SourceId = (int)CaseNoteSource.User,
                            KindId = (int)CaseNoteKind.Document,
                            Version = 1
                        }, token).ConfigureAwait(false);
                    }

                    var saved = await repository.InsertAsync(new RulePoco
                    {
                        CaseNoteId = targetNote?.Id,
                        CaseResearchId = targetResearch?.Id,
                        Object = bytes,
                        CaseKey = existingCase.CaseKey,
                        CaseKeyValue = existingCase.CaseKeyValue,
                        CaseId = caseId,
                        Extension = Path.GetExtension(safeName),
                        Size = bytes.Length,
                        Name = safeName,
                        ContentType = SafeContentType(file.ContentType),
                        Hash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes))
                    }, token).ConfigureAwait(false);

                    op.Entity(saved.Id);
                    op.Created();

                    if (log.IsInfoEnabled)
                    {
                        log.Info($"CaseFile.Upload: uploaded Id={saved.Id} name={saved.Name} caseId={caseId} " +
                                 $"user={userName}");
                    }

                    return CaseFileMapper.ToDto(saved);
                }

                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseFile.Upload: no non-empty file in the given set caseId={caseId} user={userName}");
                }

                return null;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotVisibleException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (CaseLockedException)
            {
                op.Outcome("locked");
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
                    log.Debug($"CaseFile.Upload: cancelled caseId={caseId} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseFile.Upload: unexpected failure caseId={caseId} user={userName}", ex);
                throw;
            }
        }

        [Description("Removes a file attachment from a Case (soft delete).")]
        [ServiceOperation("CaseFileRemove", OperationKind.Delete, Idempotent = true, Destructive = true)]
        public async Task RemoveAsync(
            [Description("Identifier of the file attachment to remove.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseFile", "Remove", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseFile.Remove: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseFile.Remove");
                var (existingFile, existingCase) = await EnsureFileVisibleAsync("CaseFile.Remove", id, token)
                    .ConfigureAwait(false);

                EnsureMayRemove(existingFile, existingCase);

                await repository.DeleteAsync(id, token).ConfigureAwait(false);

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"CaseFile.Remove: removed Id={id} name={existingFile.Name} user={userName}");
                }
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotVisibleException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (CaseLockedException)
            {
                op.Outcome("locked");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");

                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseFile.Remove: cancelled id={id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseFile.Remove: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        public async Task<CaseFileContent> GenerateAsync(int id, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseFile", "Generate", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseFile.Generate: entry id={id} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseFile.Generate");

                var (existingFile, _) = await EnsureFileVisibleAsync("CaseFile.Generate", id, token)
                    .ConfigureAwait(false);

                op.Entity(id);
                op.Rows(1);

                return new CaseFileContent(existingFile.Object, existingFile.ContentType, existingFile.Name);
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotVisibleException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");

                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseFile.Generate: cancelled id={id} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseFile.Generate: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the (non-deleted) files attached to the Case identified by the given key field name " +
                     "and value, scoped to Case Workflow roles the caller holds and the caller's tenant.")]
        [ServiceOperation("CaseFileListByCaseKeyValue", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseFileDto>> GetByCaseKeyValueAsync(
            [Description("Name of the key field identifying the transaction, e.g. an account number field.")]
            string key,
            [Description("Value of the key field identifying the transaction.")]
            string value,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseFile", "GetByCaseKeyValue", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"CaseFile.GetByCaseKeyValue: entry key={key} user={userName}");
            }

            try
            {
                EnsurePermitted("CaseFile.GetByCaseKeyValue");

                var dtos = CaseFileMapper.ToDto(
                    await repository.GetByCaseKeyValueActiveOnlyAsync(key, value, token).ConfigureAwait(false));
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

                if (log.IsDebugEnabled)
                {
                    log.Debug($"CaseFile.GetByCaseKeyValue: cancelled key={key} user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseFile.GetByCaseKeyValue: unexpected failure key={key} user={userName}", ex);
                throw;
            }
        }

        private DtoValidationException Refused(string code, string message)
        {
            if (log.IsWarnEnabled)
            {
                log.Warn($"CaseFile.Upload: refused ({code}) user={userName}");
            }

            return new DtoValidationException(new ValidationResult(
                [new ValidationFailure("file", message) { ErrorCode = code }]));
        }

        private async Task ScanAsync(string safeName, string? declaredContentType, byte[] bytes,
            CancellationToken token)
        {
            CaseFileScanResult result;

            try
            {
                result = await scanner.ScanAsync(new CaseFileScanRequest(safeName,
                    SafeContentType(declaredContentType), CaseFileContentSniffer.Sniff(bytes), bytes,
                    tenantRegistryId, userName), token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error($"CaseFile.Upload: the malware scanner failed user={userName}", ex);

                throw Refused("FileScanUnavailable", strings[CaseFileResources.FileScanUnavailable]);
            }

            if (!result.Clean)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseFile.Upload: the malware scanner rejected a file user={userName} " +
                             $"reason={result.Reason}");
                }

                throw Refused("FileRejectedByScanner", strings[CaseFileResources.FileRejectedByScanner]);
            }
        }

        private static string SafeFileName(string? name)
        {
            var candidate = (name ?? string.Empty).Replace('\\', '/');
            candidate = candidate[(candidate.LastIndexOf('/') + 1)..];

            var cleaned = new string(candidate
                .Where(c => !char.IsControl(c) && !IsInvisibleOrBidiControl(c) && c != '"' && c != '<' && c != '>' &&
                            c != '|' && c != ':' && c != '*' && c != '?')
                .ToArray()).Trim().TrimStart('.').TrimEnd('.', ' ');

            if (cleaned.Length > MaxFileNameLength)
            {
                cleaned = cleaned[..MaxFileNameLength];
            }

            return cleaned.Length == 0 ? "file" : cleaned;
        }

        private static bool IsInvisibleOrBidiControl(char c)
        {
            return c is '\u061C' or '\u200B' or '\u200E' or '\u200F' or '\u2028' or '\u2029' or '\u2060' or '\uFEFF'
                   || c is >= '\u202A' and <= '\u202E' or >= '\u2066' and <= '\u2069';
        }

        private static string SafeContentType(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType) || contentType.Length > 127 ||
                !Regex.IsMatch(contentType,
                    "^[A-Za-z0-9][A-Za-z0-9!#$&^_.+-]*/[A-Za-z0-9][A-Za-z0-9!#$&^_.+-]*$"))
            {
                return "application/octet-stream";
            }

            return contentType;
        }

        private void EnsureMayRemove(RulePoco existingFile, Data.Poco.Case existingCase)
        {
            if (permissionValidation.Validate(supervisionPermissions))
            {
                return;
            }

            if (CaseLockCheck.HeldByAnother(existingCase, userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"CaseFile.Remove: caseId={existingCase.Id} is locked to another user user={userName}");
                }

                throw new CaseLockedException(strings[CaseFileResources.CaseLocked]);
            }

            if (string.Equals(existingFile.CreatedUser, userName, StringComparison.Ordinal) ||
                CaseLockCheck.HeldBy(existingCase, userName))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"CaseFile.Remove: id={existingFile.Id} is neither the caller's upload nor on a case the " +
                         $"caller holds user={userName}");
            }

            throw new ForbiddenException(strings[CaseFileResources.RemovalRestricted], supervisionPermissions);
        }

        private async Task<(RulePoco File, Data.Poco.Case Case)> EnsureFileVisibleAsync(string op, int id, CancellationToken token)
        {
            var existingFile = await repository.GetByIdAsync(id, token).ConfigureAwait(false);

            if (existingFile is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{op}: id={id} not found or not visible to tenant user={userName}");
                }

                throw new NotVisibleException(strings[CaseFileResources.NotVisible]);
            }

            if (existingFile.CaseId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{op}: id={id} has no Case user={userName}");
                }

                throw new NotVisibleException(strings[CaseFileResources.NotVisible]);
            }

            var existingCase = await caseRepository.GetByIdActiveOnlyAsync(existingFile.CaseId.Value, token)
                .ConfigureAwait(false);

            if (existingCase is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{op}: id={id} case not found or not visible to tenant caseId=" +
                             $"{existingFile.CaseId} user={userName}");
                }

                throw new NotVisibleException(strings[CaseFileResources.NotVisible]);
            }

            if (existingFile.CaseNoteId.HasValue
                && !await caseNoteRepository.ActionGrantedToUserAsync(existingFile.CaseNoteId.Value, token)
                    .ConfigureAwait(false))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{op}: id={id} belongs to a note whose action the caller's roles do not hold " +
                             $"user={userName}");
                }

                throw new NotVisibleException(strings[CaseFileResources.NotVisible]);
            }

            return (existingFile, existingCase);
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

            throw new ForbiddenException(strings[CaseFileResources.PermissionDenied], permissions);
        }
    }
}