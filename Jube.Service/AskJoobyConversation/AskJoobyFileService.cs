/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.ComponentModel;
using FluentValidation.Results;
using Jube.AskJooby.Security;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.AskJoobyConversation;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.AskJoobyConversation;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.AskJoobyConversation
{
    public sealed class AskJoobyFileService
    {
        public const long MaxFileSizeBytes = 10 * 1024 * 1024;
        public static readonly string[] TextExtensions = [".txt", ".md", ".csv", ".json", ".log"];
        public static readonly string[] AllowedExtensions = TextExtensions;
        private static readonly int[] permissions = [AskJoobyPermissions.Access];
        private readonly ILog auditLog;
        private readonly AskJoobyConversationRepository conversationRepository;
        private readonly AskJoobyFileRepository fileRepository;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private AskJoobyFileService(DbContext dbContext, string userName, int tenantRegistryId,
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
            conversationRepository = new AskJoobyConversationRepository(dbContext, tenantRegistryId, userName);
            fileRepository = new AskJoobyFileRepository(dbContext, tenantRegistryId, userName);
        }

        private static ValidationResult ToValidationResult(string propertyName, string message)
        {
            return new ValidationResult([new ValidationFailure(propertyName, message)]);
        }

        private static AskJoobyFileDto ToDto(AskJoobyFile file)
        {
            return new AskJoobyFileDto
            {
                Id = file.Id,
                AskJoobyConversationId = file.AskJoobyConversationId ?? 0,
                Name = file.Name,
                ContentType = file.ContentType,
                Size = file.Size,
                CreatedUser = file.CreatedUser,
                CreatedDate = file.CreatedDate.HasValue
                    ? new DateTimeOffset(DateTime.SpecifyKind(file.CreatedDate.Value, DateTimeKind.Utc))
                    : null
            };
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.ActiveUser)
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", permissions)}]");
            }

            throw new ForbiddenException(strings[AskJoobyConversationResources.PermissionDenied], permissions);
        }

        public static Task<AskJoobyFileService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<AskJoobyFileService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(AskJoobyConversationResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("AskJoobyFile.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[AskJoobyConversationResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"AskJoobyFile.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[AskJoobyConversationResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new AskJoobyFileService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation,
                log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the files attached to one AskJooby conversation.")]
        [ServiceOperation("AskJoobyFileList", OperationKind.Read, Idempotent = true)]
        public async Task<List<AskJoobyFileDto>> ListAsync(
            [Description("The conversation's Id.")]
            int conversationId, CancellationToken token = default)
        {
            using var op = OperationScope.Start("AskJoobyFile", "List", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("AskJoobyFile.List");

                var files = await fileRepository.ListByConversationAsync(conversationId, token).ConfigureAwait(false);
                var dtos = files.Select(ToDto).ToList();
                op.Entity(conversationId);
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
                log.Error($"AskJoobyFile.List: unexpected failure id={conversationId} user={userName}", ex);
                throw;
            }
        }

        [Description("Uploads a file, attaching it to an AskJooby conversation. Not idempotent.")]
        [ServiceOperation("AskJoobyFileUpload", OperationKind.Write, Idempotent = false)]
        public async Task<AskJoobyFileDto> UploadAsync(
            [Description("The conversation's Id.")]
            int conversationId,
            [Description("The file's raw bytes.")] Stream content,
            [Description("The original file name.")]
            string fileName,
            [Description("The uploaded file's content type.")]
            string contentType,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("AskJoobyFile", "Upload", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("AskJoobyFile.Upload");

                var conversation = await conversationRepository.GetByIdAsync(conversationId, token)
                    .ConfigureAwait(false);

                if (conversation is null)
                {
                    throw new NotFoundException(strings[AskJoobyConversationResources.NotFound]);
                }

                var extension = Path.GetExtension(fileName).ToLowerInvariant();

                if (!AllowedExtensions.Contains(extension))
                {
                    throw new DtoValidationException(ToValidationResult(
                        nameof(fileName), strings[AskJoobyConversationResources.FileTypeNotAllowed]));
                }

                await using var buffer = new MemoryStream();
                await content.CopyToAsync(buffer, token).ConfigureAwait(false);

                if (buffer.Length > MaxFileSizeBytes)
                {
                    throw new DtoValidationException(ToValidationResult(nameof(fileName),
                        strings[AskJoobyConversationResources.FileTooLarge, MaxFileSizeBytes / (1024 * 1024)]));
                }

                var saved = await fileRepository.InsertAsync(new AskJoobyFile
                {
                    AskJoobyConversationId = conversationId,
                    Object = buffer.ToArray(),
                    Name = fileName,
                    ContentType = contentType,
                    Extension = extension,
                    Size = buffer.Length
                }, token).ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"AskJoobyFile.Upload: uploaded id={saved.Id} conversationId={conversationId} " +
                             $"user={userName}");
                }

                return ToDto(saved);
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
            catch (DtoValidationException)
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

                log.Error(
                    $"AskJoobyFile.Upload: unexpected failure conversationId={conversationId} user={userName}", ex);

                throw;
            }
        }

        [Description("Downloads one previously-uploaded file's raw bytes.")]
        [ServiceOperation("AskJoobyFileDownload", OperationKind.Read, Idempotent = true)]
        public async Task<(byte[] Content, string ContentType, string Name)> DownloadAsync(
            [Description("The file's Id.")] int fileId, CancellationToken token = default)
        {
            using var op = OperationScope.Start("AskJoobyFile", "Download", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                EnsurePermitted("AskJoobyFile.Download");

                var file = await fileRepository.GetByIdAsync(fileId, token).ConfigureAwait(false);

                if (file is null)
                {
                    throw new NotFoundException(strings[AskJoobyConversationResources.FileNotFound]);
                }

                op.Entity(fileId);

                return (file.Object, file.ContentType ?? "application/octet-stream", file.Name ?? "file");
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
                log.Error($"AskJoobyFile.Download: unexpected failure id={fileId} user={userName}", ex);
                throw;
            }
        }

        [Description("Deletes one previously-uploaded file. Not idempotent.")]
        [ServiceOperation("AskJoobyFileDelete", OperationKind.Delete, Idempotent = false, Destructive = true)]
        public async Task DeleteAsync(
            [Description("The file's Id.")] int fileId, CancellationToken token = default)
        {
            using var op = OperationScope.Start("AskJoobyFile", "Delete", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted("AskJoobyFile.Delete");

                await fileRepository.DeleteAsync(fileId, token).ConfigureAwait(false);

                op.Entity(fileId);
                op.Deleted();

                if (log.IsInfoEnabled)
                {
                    log.Info($"AskJoobyFile.Delete: deleted id={fileId} user={userName}");
                }
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (KeyNotFoundException)
            {
                op.Outcome("notfound");
                throw new NotFoundException(strings[AskJoobyConversationResources.FileNotFound]);
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"AskJoobyFile.Delete: unexpected failure id={fileId} user={userName}", ex);
                throw;
            }
        }
    }
}