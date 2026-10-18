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
using Jube.AskJooby.Security;
using Jube.Data.Context;
using Jube.Data.Query.SearchAskJoobyRagChunkQuery;
using Jube.Data.Repository;
using Jube.Dto.AskJoobyRagChunk;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.AskJoobyRagChunk;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Localization;

namespace Jube.Service.AskJoobyRagChunk
{
    public sealed class AskJoobyRagChunkService
    {
        private static readonly int[] permissions = [AskJoobyPermissions.Access];
        private readonly ILog auditLog;
        private readonly IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly SearchAskJoobyRagChunkQuery ragQuery;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private AskJoobyRagChunkService(DbContext dbContext, string userName, int tenantRegistryId,
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
            ragQuery = new SearchAskJoobyRagChunkQuery(dbContext);
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<AskJoobyRagChunkService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                embeddingGenerator, LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<AskJoobyRagChunkService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(AskJoobyConversationResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("AskJoobyRagChunk.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[AskJoobyConversationResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"AskJoobyRagChunk.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[AskJoobyConversationResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new AskJoobyRagChunkService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, embeddingGenerator);
        }

        [Description("Searches the Pasture (Jube documentation, rule taxonomy, code and database schema) and " +
                     "returns the most relevant Bites, ranked by reciprocal-rank fusion of full-text and " +
                     "vector search.")]
        [ServiceOperation("AskJoobyRagChunkSearch", OperationKind.Read, Idempotent = true)]
        public async Task<List<AskJoobyRagChunkDto>> SearchAsync(
            [Description("A natural-language description of what you need to know.")]
            string query,
            [Description("Maximum Bites to return.")]
            int take = 10,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("AskJoobyRagChunk", "Search", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"AskJoobyRagChunk.Search: entry user={userName}");
            }

            try
            {
                EnsurePermitted("AskJoobyRagChunk.Search");
                var boundedTake = Math.Clamp(take, 1, 50);

                var embeddingText = string.Empty;

                if (embeddingGenerator is not null && !string.IsNullOrWhiteSpace(query))
                {
                    var embedding = await embeddingGenerator.GenerateAsync(query, cancellationToken: token)
                        .ConfigureAwait(false);

                    embeddingText = AskJoobyRagClient.ToPgVectorText(embedding.Vector.Span);
                }

                var rows = await ragQuery.ExecuteAsync(query, embeddingText, boundedTake, token: token)
                    .ConfigureAwait(false);

                var dtos = rows.Select(row => new AskJoobyRagChunkDto
                {
                    Id = row.Id,
                    Hash = row.Hash,
                    ParentHash = row.ParentHash,
                    Category = AskJoobyRagClient.CategoryName(row.CategoryId),
                    Kind = AskJoobyRagClient.KindName(row.KindId),
                    SourceFile = row.SourceFile,
                    StartLine = row.StartLine,
                    EndLine = row.EndLine,
                    QualifiedName = row.QualifiedName,
                    Content = row.Content,
                    Score = row.Score
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
                log.Error($"AskJoobyRagChunk.Search: unexpected failure user={userName}", ex);
                throw;
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

            throw new ForbiddenException(strings[AskJoobyConversationResources.PermissionDenied], permissions);
        }
    }
}