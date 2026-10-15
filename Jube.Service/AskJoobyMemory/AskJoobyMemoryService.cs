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
using Jube.Data.Query.SearchAskJoobyMemoryQuery;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Dto.AskJoobyMemory;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.AskJoobyRagChunk;
using Jube.Service.Exceptions.AskJoobyMemory;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.AskJoobyMemory;
using log4net;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Localization;

namespace Jube.Service.AskJoobyMemory
{
    public sealed class AskJoobyMemoryService
    {
        private static readonly int[] permissions = [AskJoobyPermissions.Access];
        private readonly ILog auditLog;
        private readonly IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator;
        private readonly AskJoobyAgentGuardRejectionRepository guardRejectionRepository;
        private readonly ILog log;
        private readonly SearchAskJoobyMemoryQuery memoryQuery;
        private readonly AskJoobyMemoryRepository memoryRepository;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly AskJoobyMemoryDtoValidator validator;

        private AskJoobyMemoryService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.embeddingGenerator = embeddingGenerator;
            memoryRepository = new AskJoobyMemoryRepository(dbContext, tenantRegistryId, userName);
            memoryQuery = new SearchAskJoobyMemoryQuery(dbContext);
            guardRejectionRepository = new AskJoobyAgentGuardRejectionRepository(dbContext);
            validator = new AskJoobyMemoryDtoValidator(strings);
        }

        private Task<string> EmbedQueryAsync(string text, CancellationToken token)
        {
            if (embeddingGenerator is null || string.IsNullOrWhiteSpace(text))
            {
                return Task.FromResult(string.Empty);
            }

            return EmbedAsync(text, token);
        }

        private async Task<string> EmbedAsync(string text, CancellationToken token)
        {
            if (embeddingGenerator is null || string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var embedding = await embeddingGenerator.GenerateAsync(text, cancellationToken: token)
                .ConfigureAwait(false);

            return AskJoobyRagClient.ToPgVectorText(embedding.Vector.Span);
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

            throw new ForbiddenException(strings[AskJoobyMemoryResources.PermissionDenied], permissions);
        }

        public static Task<AskJoobyMemoryService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                embeddingGenerator, LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<AskJoobyMemoryService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(AskJoobyMemoryResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("AskJoobyMemory.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[AskJoobyMemoryResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"AskJoobyMemory.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[AskJoobyMemoryResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new AskJoobyMemoryService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, embeddingGenerator);
        }

        [Description("Remembers a fact for later recall. \"User\" scope is private to you; \"System\" scope is " +
                     "shared with every user in this tenant -- use System deliberately, only for something " +
                     "genuinely useful to anyone working on this platform, not personal context.")]
        [ServiceOperation("AskJoobyMemoryRemember", OperationKind.Write, Idempotent = false)]
        public async Task<AskJoobyMemoryDto> RememberAsync(
            [Description("The fact to remember, in plain language.")]
            string content,
            [Description("\"User\" (private to you) or \"System\" (shared tenant-wide).")]
            string scope = "User",
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("AskJoobyMemory", "Remember", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                EnsurePermitted("AskJoobyMemory.Remember");

                var dto = new AskJoobyMemoryDto { Content = content, Scope = scope };
                var validation = await validator.ValidateAsync(dto, token).ConfigureAwait(false);

                if (!validation.IsValid)
                {
                    throw new DtoValidationException(validation);
                }

                var sensitiveFinding = await AskJoobySensitiveDataGuard.CheckAsync(content).ConfigureAwait(false);

                if (sensitiveFinding is not null)
                {
                    await guardRejectionRepository.InsertAsync(new AskJoobyAgentGuardRejection
                    {
                        TenantRegistryId = tenantRegistryId,
                        PhaseId = Enum.TryParse<AskJoobyGuardPhase>(sensitiveFinding.Phase, true, out var phaseId)
                            ? phaseId
                            : null,
                        PipelineName = sensitiveFinding.PipelineName,
                        RuleName = sensitiveFinding.RuleName,
                        Reason = sensitiveFinding.Reason,
                        ActionId = Enum.TryParse<AskJoobyGuardAction>(sensitiveFinding.Action, true,
                            out var actionId)
                            ? actionId
                            : null,
                        CreatedUser = userName
                    }, token).ConfigureAwait(false);

                    throw new DtoValidationException(new ValidationResult(
                        [new ValidationFailure("Content", sensitiveFinding.Message)]));
                }

                var embeddingText = await EmbedAsync(content, token).ConfigureAwait(false);

                var scopeId = Enum.TryParse<AskJoobyMemoryScope>(scope, out var parsedScope)
                    ? (byte)parsedScope
                    : (byte)AskJoobyMemoryScope.User;

                var saved = await memoryRepository.InsertAsync(content, scopeId, embeddingText, token: token)
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Created();

                if (log.IsInfoEnabled)
                {
                    log.Info($"AskJoobyMemory.Remember: created Id={saved.Id} scope={scope} user={userName}");
                }

                return new AskJoobyMemoryDto
                {
                    Id = saved.Id, Scope = ((AskJoobyMemoryScope)saved.ScopeId).ToString(),
                    CreatedUser = saved.CreatedUser, Content = saved.Content
                };
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
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"AskJoobyMemory.Remember: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Recalls remembered facts relevant to a query, ranked by reciprocal-rank fusion of " +
                     "full-text and vector search. Always searches both your own private memories and " +
                     "everything shared tenant-wide -- there is no separate scope switch for recall.")]
        [ServiceOperation("AskJoobyMemoryRecall", OperationKind.Read, Idempotent = true)]
        public async Task<List<AskJoobyMemoryDto>> RecallAsync(
            [Description("A natural-language description of what you want to recall.")]
            string query,
            [Description("Maximum memories to return.")]
            int take = 5,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("AskJoobyMemory", "Recall", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                EnsurePermitted("AskJoobyMemory.Recall");
                var boundedTake = Math.Clamp(take, 1, 20);

                var embeddingText = await EmbedQueryAsync(query, token).ConfigureAwait(false);

                var rows = await memoryQuery
                    .ExecuteAsync(query, embeddingText, tenantRegistryId, userName, boundedTake, token: token)
                    .ConfigureAwait(false);

                var dtos = rows.Select(row => new AskJoobyMemoryDto
                {
                    Id = row.Id, Scope = ((AskJoobyMemoryScope)row.ScopeId).ToString(),
                    CreatedUser = row.CreatedUser, Content = row.Content, Score = row.Score
                }).ToList();

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
                log.Error($"AskJoobyMemory.Recall: unexpected failure user={userName}", ex);
                throw;
            }
        }
    }
}