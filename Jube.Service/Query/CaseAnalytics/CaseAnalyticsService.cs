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
using Jube.CaseRisk.Analytics;
using Jube.Data.Context;
using Jube.Data.Query.GetCaseAnalyticsFactsQuery;
using Jube.Data.Query.GetCaseAnalyticsStaysQuery;
using Jube.Data.Repository;
using Jube.Dto.Query.CaseAnalytics;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Query.CaseAnalytics;
using Jube.Service.Observability;
using Jube.Service.Query.CaseSearch;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using SearchNotFound = Jube.Service.Exceptions.Query.CaseSearch.NotFoundException;

namespace Jube.Service.Query.CaseAnalytics
{
    public sealed class CaseAnalyticsService
    {
        private const int DefaultDays = 30;
        private const int MaximumDays = 366;
        private static readonly int[] permissions = [1];
        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly string reportConnectionString;
        private readonly CaseSearchService search;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaseAnalyticsService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            CaseSearchService search)
        {
            this.dbContext = dbContext;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.search = search;

            reportConnectionString = dynamicEnvironment.AppSettings("ReportConnectionString")
                                     ?? dbContext.Connection.ConnectionString;
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseAnalyticsService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseAnalyticsService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseAnalyticsResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new NotAuthenticatedException(strings[CaseAnalyticsResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                throw new NotAuthenticatedException(strings[CaseAnalyticsResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            var search = await CaseSearchService.CreateAsync(dbContext, userName, log, stringLocalizerFactory,
                serviceChangeBus, dynamicEnvironment, auditLog, token).ConfigureAwait(false);

            return new CaseAnalyticsService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, dynamicEnvironment, search);
        }

        [Description("Analyses a Case Workflow over a period for supervisors and for anyone tuning the process: " +
                     "how long cases wait to be looked at, owned and resolved with percentiles; opened, closed and " +
                     "backlog by day with the cases in each status; where time is spent by status and which is " +
                     "the bottleneck; open cases by age and lane; each activation rule's precision and the analyst " +
                     "time its false positives cost; whether the Priority ordering and the likelihood predict " +
                     "confirmed cases better than working the oldest first, with a calibration table; service level " +
                     "attainment and the cases at risk; when cases arrive and how fast the first look is by hour; " +
                     "the caller's throughput beside the team median; and how often cases change hands or reopen. " +
                     "Only cases the caller's roles may see are counted.")]
        [ServiceOperation("CaseAnalyticsGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseAnalyticsDto> GetAsync(CaseAnalyticsRequestDto? request,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseAnalytics", "Get", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(request);
                EnsurePermitted();

                var now = DateTime.UtcNow;
                var to = request.To?.UtcDateTime ?? now;
                var from = request.From?.UtcDateTime ?? to.AddDays(-DefaultDays);

                if (from >= to || (to - from).TotalDays > MaximumDays)
                {
                    throw new DtoValidationException(new FluentValidation.Results.ValidationResult(
                    [
                        new FluentValidation.Results.ValidationFailure(nameof(request.From),
                            strings[CaseAnalyticsResources.PeriodInvalid]) { ErrorCode = "PeriodInvalid" }
                    ]));
                }

                (string WhereSql, List<object> Tokens) scope;

                try
                {
                    scope = await search.EverythingAsync(request.CaseWorkflowGuid, token).ConfigureAwait(false);
                }
                catch (SearchNotFound)
                {
                    throw new NotFoundException(strings[CaseAnalyticsResources.WorkflowNotFound]);
                }

                var factRows = await new GetCaseAnalyticsFactsQuery(reportConnectionString, log)
                    .ExecuteAsync(scope.WhereSql, scope.Tokens, from, to, token).ConfigureAwait(false);

                var stayRows = await new GetCaseAnalyticsStaysQuery(reportConnectionString, log)
                    .ExecuteAsync(scope.WhereSql, scope.Tokens, from, to, token).ConfigureAwait(false);

                var facts = factRows.Rows.Select(ToFact).ToList();
                var stays = stayRows.Rows.Select(s => new StayFact(s.CaseId, s.CaseWorkflowStatusGuid, s.EnteredDate,
                    s.ExitedDate)).ToList();

                var statusGuids = stays.Select(s => s.CaseWorkflowStatusGuid)
                    .Concat(facts.Select(f => f.CaseWorkflowStatusGuid)).Distinct().ToList();

                var statuses = (await dbContext.CaseWorkflowStatus.Where(w => statusGuids.Contains(w.Guid))
                        .Select(s => new { s.Guid, s.Name, s.MaxDwellMinutes, s.Deleted }).ToListAsync(token)
                        .ConfigureAwait(false))
                    .GroupBy(s => s.Guid)
                    .Select(g => g.OrderBy(s => s.Deleted == 1 ? 1 : 0).First())
                    .ToList();

                var cohort = facts.Where(f => f.CreatedDate >= from && f.CreatedDate < to).ToList();
                var closedInPeriod = facts.Where(f => f.ClosedDate >= from && f.ClosedDate < to).ToList();

                var cycle = CycleTimeCalculator.Compute(cohort, now);
                var sla = SlaCalculator.Compute(cohort.Concat(facts.Where(f => !f.Closed && f.CreatedDate < from))
                    .ToList(), now);

                var dailyGoal = await dbContext.CaseWorkflow.Where(w => w.Guid == request.CaseWorkflowGuid && (w.Deleted == null || w.Deleted == 0))
                    .Select(w => w.DailyGoal).FirstOrDefaultAsync(token).ConfigureAwait(false);

                var dto = new CaseAnalyticsDto
                {
                    From = new DateTimeOffset(from, TimeSpan.Zero),
                    To = new DateTimeOffset(to, TimeSpan.Zero),
                    GeneratedAt = new DateTimeOffset(now, TimeSpan.Zero),
                    Truncated = factRows.Truncated || stayRows.Truncated,
                    CasesAnalysed = facts.Count,
                    StatusNames = statuses.ToDictionary(s => s.Guid, s => s.Name),
                    CycleTimes = cycle,
                    Flow = FlowCalculator.Daily(facts, stays, from, to.AddTicks(-1)),
                    StatusDwell = DwellCalculator.ByStatus(stays, now,
                        statuses.ToDictionary(s => s.Guid, s => s.MaxDwellMinutes)),
                    Aging = AgingCalculator.Open(facts, now),
                    Rules = RuleEffectivenessCalculator.Compute(cohort),
                    Ranking = RankingQualityCalculator.Compute(closedInPeriod),
                    QaSample = QaSampleCalculator.Compute(facts),
                    ServiceLevels = sla,
                    Rhythm = RhythmCalculator.Compute(cohort),
                    Workload = WorkloadCalculator.Compute(closedInPeriod.Concat(facts.Where(f => !f.Closed)).ToList(),
                        userName),
                    MyDay = MyDayCalculator.Compute(closedInPeriod, userName, now, dailyGoal),
                    Handoffs = HandoffCalculator.Compute(cohort)
                };

                dto.Headline = Headline(facts, cohort, closedInPeriod, cycle, sla);

                op.Rows(facts.Count);

                return dto;
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
                log.Error($"CaseAnalytics.Get: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private static CaseAnalyticsHeadlineDto Headline(IReadOnlyCollection<CaseFact> facts,
            IReadOnlyCollection<CaseFact> cohort, IReadOnlyCollection<CaseFact> closedInPeriod,
            CycleTimeReport cycle, SlaReport sla)
        {
            var open = facts.Where(f => !f.Closed).ToList();
            var decided = closedInPeriod.Where(f => f.OutcomeId is 1 or 2).ToList();

            return new CaseAnalyticsHeadlineDto
            {
                Opened = cohort.Count,
                Closed = closedInPeriod.Count,
                OpenNow = open.Count,
                NetChange = cohort.Count - closedInPeriod.Count,
                UnassessedOpen = open.Count(f => f.Lane == null),
                UrgentOpen = open.Count(f => f.Lane is >= 1 and <= 3),
                Decided = decided.Count,
                ConfirmedRate = decided.Count > 0 ? (double)decided.Count(f => f.OutcomeId == 1) / decided.Count : null,
                MedianFirstViewMinutes = cycle.FirstView.Count > 0 ? cycle.FirstView.P50 : null,
                MedianResolutionMinutes = cycle.Resolution.Count > 0 ? cycle.Resolution.P50 : null,
                SlaBreachedOpen = sla.OpenBreached,
                SlaAtRiskOpen = sla.OpenAtRisk,
                ReopenRate = cohort.Count > 0 ? (double)cohort.Count(f => f.Reopens > 0) / cohort.Count : 0
            };
        }

        private static CaseFact ToFact(GetCaseAnalyticsFactsQueryDto.Row r) => new(r.CaseId,
            r.CaseWorkflowStatusGuid, r.CreatedDate, r.ClosedDate, r.Closed, r.OutcomeId, r.RuleGuid, r.RuleName,
            r.FirstViewedDate, r.FirstOwnedDate, r.Reopens, r.Handoffs, r.LaneId, r.Priority, r.Likelihood,
            r.ImpactId, r.FirstViewTargetMinutes, r.OwnershipTargetMinutes, r.ResolutionTargetMinutes,
            r.StayEnteredDate, r.MaxDwellMinutes, r.ClosedUser, r.LockedUser, r.QaSample);

        private void EnsurePermitted()
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            throw new ForbiddenException(strings[CaseAnalyticsResources.PermissionDenied], permissions);
        }
    }
}
