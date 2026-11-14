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
using Jube.Dto.Repository.CaseTag;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Repository.CaseTag;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using LinqToDB;
using LinqToDB.Async;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.CaseTag
{
    public sealed class CaseTagService
    {
        public const int MaximumTags = 50;
        private static readonly int[] permissions = [1];
        private readonly ILog auditLog;
        private readonly CaseRepository caseRepository;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly CaseTagRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaseTagService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new CaseTagRepository(dbContext, userName);
            caseRepository = new CaseRepository(dbContext, userName);
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseTagService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseTagService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseTagResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new NotAuthenticatedException(strings[CaseTagResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                throw new NotAuthenticatedException(strings[CaseTagResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseTagService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation, log,
                auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the tags applied to a case from the case manager. Case-level tags seed the graph's " +
                     "proximity-to-tag distances alongside transaction tags. Read-only.")]
        [ServiceOperation("CaseTagListByCaseId", OperationKind.Read, Idempotent = true)]
        public async Task<List<CaseTagDto>> GetByCaseIdAsync([Description("The case id.")] int caseId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseTag", "ListByCaseId", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted();
                await RequireCaseAsync(caseId, token).ConfigureAwait(false);
                var rows = CaseTagMapper.ToDto(await repository.GetByCaseIdAsync(caseId, token).ConfigureAwait(false));
                op.Rows(rows.Count);

                return rows;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("not_found");
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
                log.Error($"CaseTag.ListByCaseId: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the tag names a case may carry: the active tags defined on the case workflow's model, " +
                     "the same vocabulary transactions are tagged with. Read-only.")]
        [ServiceOperation("CaseTagChoices", OperationKind.Read, Idempotent = true)]
        public async Task<List<string>> GetChoicesAsync([Description("The case id.")] int caseId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseTag", "Choices", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted();
                var existing = await RequireCaseAsync(caseId, token).ConfigureAwait(false);
                var choices = await ChoicesAsync(existing, token).ConfigureAwait(false);
                op.Rows(choices.Count);

                return choices;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("not_found");
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
                log.Error($"CaseTag.Choices: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Replaces the tags on a case with the given list: names in the list are applied, names absent " +
                     "are removed, every name must be in the model's tag vocabulary. The next graph distance cycle " +
                     "seeds proximity-to-tag from the case key.")]
        [ServiceOperation("CaseTagMerge", OperationKind.Write, Idempotent = true)]
        public async Task<List<CaseTagDto>> MergeAsync(CaseTagMergeRequestDto? request, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseTag", "Merge", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();
                var existing = await RequireCaseAsync(request.CaseId, token).ConfigureAwait(false);

                if (existing.Locked == 1 && !string.IsNullOrEmpty(existing.LockedUser)
                                         && !string.Equals(existing.LockedUser, userName, StringComparison.Ordinal))
                {
                    throw new ConflictException(strings[CaseTagResources.LockedByAnotherUser]);
                }

                var tags = request.Tags.Select(t => t.Trim()).Where(t => t.Length > 0).Distinct(StringComparer.Ordinal).ToArray();

                if (tags.Length > MaximumTags)
                {
                    throw new ArgumentException(strings[CaseTagResources.TooManyTags]);
                }

                var choices = (await ChoicesAsync(existing, token).ConfigureAwait(false)).ToHashSet(StringComparer.Ordinal);
                var unknown = tags.FirstOrDefault(t => !choices.Contains(t));

                if (unknown != null)
                {
                    throw new ArgumentException(string.Format(strings[CaseTagResources.TagNotInVocabulary], unknown));
                }

                var rows = CaseTagMapper.ToDto(await repository.MergeTagsAsync(request.CaseId, tags, token)
                    .ConfigureAwait(false));
                op.Entity(request.CaseId);
                op.Updated();
                op.Rows(rows.Count);

                return rows;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("not_found");
                throw;
            }
            catch (ConflictException)
            {
                op.Outcome("conflict");
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
                log.Error($"CaseTag.Merge: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private async Task<Data.Poco.Case> RequireCaseAsync(int caseId, CancellationToken token)
        {
            return await caseRepository.GetByIdActiveOnlyAsync(caseId, token).ConfigureAwait(false)
                   ?? throw new NotFoundException(strings[CaseTagResources.CaseNotFound]);
        }

        private async Task<List<string>> ChoicesAsync(Data.Poco.Case existing, CancellationToken token)
        {
            var modelId = await dbContext.CaseWorkflow.Where(w => w.Guid == existing.CaseWorkflowGuid && (w.Deleted == 0 || w.Deleted == null))
                .Select(w => w.EntityAnalysisModelId).FirstOrDefaultAsync(token).ConfigureAwait(false);

            if (modelId == null)
            {
                return [];
            }

            return (await new EntityAnalysisModelTagRepository(dbContext, tenantRegistryId)
                    .GetByEntityAnalysisModelIdOrderByNameAsync(modelId.Value, token).ConfigureAwait(false))
                .Where(t => t.Active == 1 && !string.IsNullOrWhiteSpace(t.Name))
                .Select(t => t.Name!)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private void EnsurePermitted()
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"CaseTag: permission denied user={userName} specs=[{string.Join(",", permissions)}]");
            }

            throw new ForbiddenException(strings[CaseTagResources.PermissionDenied], permissions);
        }
    }
}
