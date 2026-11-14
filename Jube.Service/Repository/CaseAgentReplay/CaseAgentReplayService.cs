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

using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Repository.CaseAgentReplay;
using Jube.Service.Observability;
using Jube.Service.Query.CaseTriage;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using LinqToDB;
using LinqToDB.Async;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Service.Repository.CaseAgentReplay
{
    public sealed class CaseAgentReplayService
    {
        public const int WorkflowPermission = 18;
        public const int MaximumActiveReplays = 2;
        public const int MaximumPrompt = 4000;
        public const int MaximumWindowDays = 366;
        private static readonly int[] permissions = [WorkflowPermission];

        private readonly DbContext dbContext;
        private readonly string userName;
        private readonly int tenantRegistryId;
        private readonly PermissionValidation permissionValidation;
        private readonly ILog log;
        private readonly ILog auditLog;
        private readonly IServiceChangeBus serviceChangeBus;

        private CaseAgentReplayService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
        }

        public static async Task<CaseAgentReplayService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new CaseExceptions.NotAuthenticatedException("Not authenticated.");
            }

            var tenant = await UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, userName, token)
                .ConfigureAwait(false);

            if (tenant is null)
            {
                throw new CaseExceptions.NotAuthenticatedException("Not authenticated.");
            }

            var validation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseAgentReplayService(dbContext, userName, tenant.Value, validation, log,
                LogManager.GetLogger("Jube.Audit"), serviceChangeBus);
        }

        private void Ensure()
        {
            if (!permissionValidation.Validate(permissions))
            {
                throw new CaseExceptions.ForbiddenException("Permission denied.", permissions);
            }
        }

        public async Task<Guid> StartAsync(CaseAgentReplayRequestDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseAgentReplay", "Start", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                Ensure();
                Check(model);

                if (model.CaseWorkflowGuid is { } workflow && (!await WorkflowInTenantAsync(workflow, token)
                        .ConfigureAwait(false) || await new CaseSearchFilterRepository(dbContext, userName,
                            tenantRegistryId).GetAccessibleCaseWorkflowIdAsync(workflow, token)
                            .ConfigureAwait(false) == null))
                {
                    throw new CaseExceptions.NotFoundException("The workflow was not found.");
                }

                if (await new CaseAgentReplayRepository(dbContext).CountActiveAsync(tenantRegistryId, token)
                        .ConfigureAwait(false) >= MaximumActiveReplays)
                {
                    throw new CaseExceptions.ConflictException(
                        "Replays are already waiting or running; start another when they finish.");
                }

                var prompt = string.IsNullOrWhiteSpace(model.Prompt) ? null : model.Prompt.Trim();
                var guid = Guid.NewGuid();

                await new CaseAgentReplayRepository(dbContext).InsertAsync(new Data.Poco.CaseAgentReplay
                {
                    Guid = guid,
                    TenantRegistryId = tenantRegistryId,
                    StatusId = (short)CaseAgentReplayStatus.Pending,
                    PromptText = prompt,
                    AgentDefinitionHash = CaseTriagePrompt.DefinitionHash(prompt ?? CaseTriagePrompt.Replay),
                    FromDate = DateTime.SpecifyKind(model.FromDate, DateTimeKind.Unspecified),
                    ToDate = DateTime.SpecifyKind(model.ToDate, DateTimeKind.Unspecified),
                    MaximumCases = model.MaximumCases,
                    AsOfMinutes = model.AsOfMinutes,
                    CaseWorkflowGuid = model.CaseWorkflowGuid,
                    CreatedUser = userName,
                    CreatedDate = DateTime.UtcNow
                }, token).ConfigureAwait(false);

                op.Created();

                return guid;
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }

        public async Task<List<CaseAgentReplayDto>> ListAsync(CancellationToken token = default)
        {
            Ensure();

            var repository = new CaseAgentReplayRepository(dbContext);
            var replays = await repository.ListAsync(tenantRegistryId, 20, token).ConfigureAwait(false);
            var list = new List<CaseAgentReplayDto>();

            foreach (var replay in replays)
            {
                list.Add(ToDto(replay, await repository.ResultsAsync(replay.Guid, token).ConfigureAwait(false)));
            }

            return list;
        }

        public async Task<CaseAgentReplayDto> GetAsync(Guid guid, CancellationToken token = default)
        {
            Ensure();

            var repository = new CaseAgentReplayRepository(dbContext);
            var replay = await RequireAsync(repository, guid, token).ConfigureAwait(false);

            return ToDto(replay, await repository.ResultsAsync(guid, token).ConfigureAwait(false));
        }

        public async Task<CaseAgentReplayComparisonDto> CompareAsync(Guid first, Guid second,
            CancellationToken token = default)
        {
            Ensure();

            var repository = new CaseAgentReplayRepository(dbContext);
            var a = await RequireAsync(repository, first, token).ConfigureAwait(false);
            var b = await RequireAsync(repository, second, token).ConfigureAwait(false);
            var aResults = await repository.ResultsAsync(first, token).ConfigureAwait(false);
            var bResults = await repository.ResultsAsync(second, token).ConfigureAwait(false);

            var common = (from x in aResults.Where(r => r.RecommendedOutcomeId is not null)
                join y in bResults.Where(r => r.RecommendedOutcomeId is not null) on x.CaseId equals y.CaseId
                select (x, y)).ToList();

            return new CaseAgentReplayComparisonDto
            {
                First = ToDto(a, aResults),
                Second = ToDto(b, bResults),
                CommonCases = common.Count,
                SameRecommendation = common.Count(c => c.x.RecommendedOutcomeId == c.y.RecommendedOutcomeId),
                FirstAgreed = common.Count(c => c.x.RecommendedOutcomeId == c.x.HumanOutcomeId),
                SecondAgreed = common.Count(c => c.y.RecommendedOutcomeId == c.y.HumanOutcomeId)
            };
        }

        private async Task<Data.Poco.CaseAgentReplay> RequireAsync(CaseAgentReplayRepository repository, Guid guid,
            CancellationToken token)
        {
            var replay = await repository.GetByGuidAsync(guid, token).ConfigureAwait(false);

            if (replay is null || replay.TenantRegistryId != tenantRegistryId)
            {
                throw new CaseExceptions.NotFoundException("The replay was not found.");
            }

            return replay;
        }

        private Task<bool> WorkflowInTenantAsync(Guid workflow, CancellationToken token)
        {
            return (from w in dbContext.CaseWorkflow
                from m in dbContext.EntityAnalysisModel.InnerJoin(m => m.Id == w.EntityAnalysisModelId)
                where w.Guid == workflow && (w.Deleted == 0 || w.Deleted == null)
                                         && m.TenantRegistryId == tenantRegistryId
                select w.Id).AnyAsync(token);
        }

        private static CaseAgentReplayDto ToDto(Data.Poco.CaseAgentReplay replay, List<CaseAgentReplayResult> results)
        {
            var metrics = ReplayMetrics.Compute(results.Select(r => ((int)r.HumanOutcomeId,
                r.RecommendedOutcomeId is null ? (int?)null : r.RecommendedOutcomeId.Value)));

            return new CaseAgentReplayDto
            {
                Guid = replay.Guid,
                StatusId = replay.StatusId,
                AgentDefinitionHash = replay.AgentDefinitionHash,
                ModelName = replay.ModelName,
                FromDate = DateTime.SpecifyKind(replay.FromDate, DateTimeKind.Utc),
                ToDate = DateTime.SpecifyKind(replay.ToDate, DateTimeKind.Utc),
                AsOfMinutes = replay.AsOfMinutes,
                CasesSelected = replay.CasesSelected,
                Scored = metrics.Scored,
                Unusable = metrics.Unusable,
                Agreed = metrics.Agreed,
                MissedRisk = metrics.MissedRisk,
                OverEscalation = metrics.OverEscalation,
                Inconclusive = metrics.Inconclusive,
                AgreementRate = metrics.AgreementRate,
                Error = replay.Error,
                CreatedDate = new DateTimeOffset(DateTime.SpecifyKind(replay.CreatedDate, DateTimeKind.Utc)),
                CreatedUser = replay.CreatedUser
            };
        }

        private static void Check(CaseAgentReplayRequestDto model)
        {
            if (model.ToDate <= model.FromDate)
            {
                throw new ArgumentException("The end of the window must be after its start.");
            }

            if ((model.ToDate - model.FromDate).TotalDays > MaximumWindowDays)
            {
                throw new ArgumentException($"The window must be at most {MaximumWindowDays} days.");
            }

            if (model.MaximumCases is < 1 or > 200)
            {
                throw new ArgumentException("The maximum cases must be between 1 and 200.");
            }

            if (model.AsOfMinutes is < 0 or > 10080)
            {
                throw new ArgumentException("The cut-off must be between 0 and 10080 minutes.");
            }

            if (model.Prompt is { Length: > MaximumPrompt })
            {
                throw new ArgumentException($"The prompt must be at most {MaximumPrompt} characters.");
            }
        }
    }
}
