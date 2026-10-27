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
using System.Text;
using Jube.AskJooby.Security;
using Jube.Data.Context;
using Jube.Data.Repository;
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
    public sealed class AskJoobyChatTranscriptService
    {
        private static readonly int[] permissions = [AskJoobyPermissions.Access];
        private readonly ILog auditLog;
        private readonly AskJoobyCompactionRepository compactionRepository;
        private readonly AskJoobyConversationRepository conversationRepository;
        private readonly AskJoobyFileRepository fileRepository;
        private readonly ILog log;
        private readonly AskJoobyMessageRepository messageRepository;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private AskJoobyChatTranscriptService(DbContext dbContext, string userName, int tenantRegistryId,
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
            messageRepository = new AskJoobyMessageRepository(dbContext);
            fileRepository = new AskJoobyFileRepository(dbContext, tenantRegistryId, userName);
            compactionRepository = new AskJoobyCompactionRepository(dbContext, tenantRegistryId, userName);
        }

        private static string BuildFileName(Data.Poco.AskJoobyConversation conversation, DateTime generatedAtUtc)
        {
            var guidPart = (conversation.Guid ?? Guid.NewGuid()).ToString("N");
            var descriptionPart = SanitizeForFileName(conversation.Title);
            var datePart = generatedAtUtc.ToString("yyyyMMddTHHmmssZ");

            return string.IsNullOrEmpty(descriptionPart)
                ? $"{guidPart}-Transcript-{datePart}.md"
                : $"{guidPart}-{descriptionPart}-{datePart}.md";
        }

        private static string SanitizeForFileName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            foreach (var c in value.Trim())
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
                else if (sb.Length > 0 && sb[^1] != '-')
                {
                    sb.Append('-');
                }
            }

            var result = sb.ToString().Trim('-');

            return result.Length > 60 ? result[..60] : result;
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.ActiveUser)
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied user={userName} landlord={permissionValidation.Landlord}");
            }

            throw new ForbiddenException(strings[AskJoobyConversationResources.PermissionDenied], []);
        }

        public static Task<AskJoobyChatTranscriptService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<AskJoobyChatTranscriptService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(AskJoobyConversationResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("AskJoobyChatTranscript.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[AskJoobyConversationResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"AskJoobyChatTranscript.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[AskJoobyConversationResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new AskJoobyChatTranscriptService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Renders one AskJooby conversation's clean conversational exchange -- what was " +
                     "asked, and what Jooby replied -- as a single Markdown document, and returns it " +
                     "with a ready-to-use download file name.")]
        [ServiceOperation("AskJoobyChatTranscriptGenerate", OperationKind.Read, Idempotent = true)]
        public async Task<(string Markdown, string FileName)> GenerateAsync(
            [Description("The conversation's Id.")]
            int conversationId, CancellationToken token = default)
        {
            using var op = OperationScope.Start("AskJoobyChatTranscript", "Generate", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);

            try
            {
                EnsurePermitted("AskJoobyChatTranscript.Generate");

                var conversation = await conversationRepository.GetByIdAsync(conversationId, token)
                    .ConfigureAwait(false);

                if (conversation is null)
                {
                    throw new NotFoundException(strings[AskJoobyConversationResources.NotFound]);
                }

                if (!string.Equals(conversation.CreatedUser, userName, StringComparison.OrdinalIgnoreCase))
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"AskJoobyChatTranscript.Generate: 403 (not the creator) id={conversationId} " +
                                 $"user={userName} createdUser={conversation.CreatedUser}");
                    }

                    throw new ForbiddenException(strings[AskJoobyConversationResources.PermissionDenied], []);
                }

                var entries = await messageRepository.ListEntriesByConversationAsync(conversationId, token)
                    .ConfigureAwait(false);
                var files = await fileRepository.ListByConversationAsync(conversationId, token)
                    .ConfigureAwait(false);
                var compactions = await compactionRepository.ListByConversationAsync(conversationId, token)
                    .ConfigureAwait(false);

                var generatedAtUtc = DateTime.UtcNow;

                var markdown = AskJoobyChatTranscriptMarkdownBuilder.Build(conversation, entries, files,
                    compactions, generatedAtUtc, userName);

                var fileName = BuildFileName(conversation, generatedAtUtc);

                op.Entity(conversationId);
                op.Rows(entries.Count);

                return (markdown, fileName);
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

                log.Error($"AskJoobyChatTranscript.Generate: unexpected failure id={conversationId} " +
                          $"user={userName}", ex);

                throw;
            }
        }
    }
}