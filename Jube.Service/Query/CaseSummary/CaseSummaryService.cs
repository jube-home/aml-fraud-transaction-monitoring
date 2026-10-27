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
using Jube.Data.Poco;
using Jube.Service.Charts;
using Jube.Data.Query.GetCaseKeyOutcomeHistoryQuery;
using Jube.Data.Repository;
using Jube.Dto.Query.CaseSearch;
using Jube.Dto.Query.CaseSummary;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.Query.CaseSummary;
using Jube.Service.Observability;
using Jube.Service.Query.CaseSearch;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json.Linq;

namespace Jube.Service.Query.CaseSummary
{
    public sealed class CaseSummaryService
    {
        public const int PeekEventTypeId = 16;
        private const int HistoryTake = 10;
        private const int EventTake = 10;
        private const int NoteTake = 3;
        private const int NoteLength = 400;
        private const int ChangeTake = 50;
        private static readonly int[] permissions = [1];
        private readonly ILog auditLog;
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly CaseSearchService search;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private CaseSummaryService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, CaseSearchService search)
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
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;

        public static Task<CaseSummaryService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, dynamicEnvironment,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<CaseSummaryService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, ILog auditLog,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(CaseSummaryResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new NotAuthenticatedException(strings[CaseSummaryResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                throw new NotAuthenticatedException(strings[CaseSummaryResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            var search = await CaseSearchService.CreateAsync(dbContext, userName, log, stringLocalizerFactory,
                serviceChangeBus, dynamicEnvironment, auditLog, token).ConfigureAwait(false);

            return new CaseSummaryService(dbContext, userName, resolvedTenantRegistryId.Value, permissionValidation,
                log, auditLog, serviceChangeBus, strings, search);
        }

        [Description("Peeks at a case: everything needed to decide whether to open it, without taking its lock. " +
                     "Returns the case as the triage list shows it, the evidence behind its likelihood and how far " +
                     "each piece moved it, the components of its impact, how its risk has moved, every stay in a " +
                     "status with the time spent, the latest audit events and notes, what happened to earlier " +
                     "cases on the same key, and what changed since the caller last looked. SIDE EFFECTS: records " +
                     "a peek event against the case and that the caller has now looked at it.")]
        [ServiceOperation("CaseSummaryGet", OperationKind.Read, Idempotent = true)]
        public async Task<CaseSummaryDto> GetAsync(
            [Description("Identifier of the case.")]
            int caseId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSummary", "Get", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted();

                var existing = await new CaseRepository(dbContext, userName).GetByIdActiveOnlyAsync(caseId, token)
                    .ConfigureAwait(false);

                if (existing == null)
                {
                    throw new NotFoundException(strings[CaseSummaryResources.NotFound]);
                }

                var row = await search.GetRowAsync(existing.CaseWorkflowGuid, caseId, token).ConfigureAwait(false)
                          ?? throw new NotFoundException(strings[CaseSummaryResources.NotFound]);

                var previousView = await new CaseViewRepository(dbContext, userName).GetAsync(caseId, token)
                    .ConfigureAwait(false);

                var history = await new CaseRiskAssessmentRepository(dbContext)
                    .ListHistoryAsync(caseId, HistoryTake, token).ConfigureAwait(false);

                var summary = new CaseSummaryDto { Case = row };

                var latest = history.FirstOrDefault();

                if (latest != null)
                {
                    summary.BaseRate = latest.BaseRate;
                    summary.Contributions = Contributions(latest.ContributionsJson);
                    summary.Impact = Impact(latest.ImpactComponentsJson);
                }

                summary.RiskHistory = history.Select(h => new CaseRiskHistoryItemDto
                {
                    Sequence = h.Sequence,
                    CreatedDate = CaseSearchMapper.Utc(h.CreatedDate),
                    Trigger = h.TriggerId,
                    Lane = h.LaneId,
                    Likelihood = h.LikelihoodProbability,
                    Impact = h.ImpactId,
                    Priority = h.Priority
                }).ToList();

                summary.Stays = await StaysAsync(caseId, token).ConfigureAwait(false);
                summary.RecentEvents = await EventsAsync(caseId, previousView, token).ConfigureAwait(false);
                summary.Notes = await NotesAsync(caseId, token).ConfigureAwait(false);
                summary.KeyHistory = await KeyHistoryAsync(existing, token).ConfigureAwait(false);
                summary.Since = await SinceAsync(caseId, previousView, row, history, token).ConfigureAwait(false);

                await new CaseEventRepository(dbContext, userName).InsertAsync(new CaseEvent
                {
                    CaseId = caseId,
                    CaseEventTypeId = PeekEventTypeId,
                    CaseKey = existing.CaseKey,
                    CaseKeyValue = existing.CaseKeyValue
                }, token).ConfigureAwait(false);

                await CaseViewRecorder.RecordAsync(dbContext, userName, caseId, token).ConfigureAwait(false);

                op.Entity(caseId);
                op.Rows(1);

                return summary;
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
                log.Error($"CaseSummary.Get: unexpected failure caseId={caseId} user={userName}", ex);
                throw;
            }
        }

        public async Task<string> WaterfallAsync(
            [Description("Identifier of the case.")]
            int caseId,
            [Description("True for the dark theme palette.")]
            bool dark = false,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseSummary", "Waterfall", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);

            try
            {
                EnsurePermitted();

                var existing = await new CaseRepository(dbContext, userName).GetByIdActiveOnlyAsync(caseId, token)
                    .ConfigureAwait(false);

                if (existing == null)
                {
                    throw new NotFoundException(strings[CaseSummaryResources.NotFound]);
                }

                var latest = (await new CaseRiskAssessmentRepository(dbContext).ListHistoryAsync(caseId, 1, token)
                    .ConfigureAwait(false)).FirstOrDefault();

                if (latest?.BaseRate == null)
                {
                    throw new ArgumentException("The case has no risk assessment to chart.");
                }

                op.Entity(caseId);
                op.Rows(1);

                return CaseWaterfallChartRenderer.Render(latest.BaseRate.Value, latest.LikelihoodProbability,
                    Contributions(latest.ContributionsJson), dark);
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
            catch (ArgumentException)
            {
                op.Outcome("invalid");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"CaseSummary.Waterfall: unexpected failure caseId={caseId} user={userName}", ex);
                throw;
            }
        }

        private static List<CaseContributionDto> Contributions(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            return JArray.Parse(json).OfType<JObject>().Select(j => new CaseContributionDto
            {
                Family = (string?)j["family"],
                Label = (string?)j["label"],
                Source = (string?)j["source"],
                Confirmed = (int?)j["confirmed"] ?? 0,
                Closed = (int?)j["closed"] ?? 0,
                Precision = (double?)j["precision"] ?? 0,
                LogLikelihoodRatio = (double?)j["logLikelihoodRatio"] ?? 0,
                Decay = (double?)j["decay"] ?? 1,
                Weight = (double?)j["weight"] ?? 1,
                LogOdds = (double?)j["logOdds"] ?? 0,
                SharesInputs = (bool?)j["sharesInputs"] ?? false
            }).ToList();
        }

        private static CaseImpactComponentsDto? Impact(string? json)
        {
            if (string.IsNullOrWhiteSpace(json) || JObject.Parse(json) is not { HasValues: true } j)
            {
                return null;
            }

            return new CaseImpactComponentsDto
            {
                TypologySeverity = (int?)j["typologySeverity"] ?? 0,
                MoneyBand = (int?)j["moneyBand"],
                Value = (double?)j["value"],
                ExposureUplift = (int?)j["exposureUplift"] ?? 0,
                BreadthUplift = (int?)j["breadthUplift"] ?? 0,
                Floor = (int?)j["floor"] ?? 1
            };
        }

        private async Task<List<CaseStayDto>> StaysAsync(int caseId, CancellationToken token)
        {
            var stays = await new CaseStatusTransitionRepository(dbContext).ListForCaseAsync(caseId, token)
                .ConfigureAwait(false);

            var guids = stays.Select(s => s.CaseWorkflowStatusGuid).Distinct().ToList();

            var statuses = await dbContext.CaseWorkflowStatus.Where(w => guids.Contains(w.Guid))
                .Select(s => new { s.Guid, s.Name, s.MaxDwellMinutes }).ToListAsync(token).ConfigureAwait(false);

            var now = DateTime.UtcNow;

            return stays.Select(s =>
            {
                var status = statuses.FirstOrDefault(x => x.Guid == s.CaseWorkflowStatusGuid);

                return new CaseStayDto
                {
                    CaseWorkflowStatusGuid = s.CaseWorkflowStatusGuid,
                    StatusName = status?.Name,
                    EnterReason = s.EnterReasonId,
                    EnteredDate = CaseSearchMapper.Utc(s.EnteredDate),
                    EnteredUser = s.EnteredUser,
                    ExitedDate = CaseSearchMapper.Utc(s.ExitedDate),
                    ExitReason = s.ExitReasonId,
                    Minutes = s.DurationSeconds.HasValue
                        ? s.DurationSeconds.Value / 60.0
                        : Math.Max(0, (now - s.EnteredDate).TotalMinutes),
                    MaxMinutes = status?.MaxDwellMinutes
                };
            }).ToList();
        }

        private async Task<List<CaseEventSummaryDto>> EventsAsync(int caseId, CaseView? view, CancellationToken token)
        {
            var events = await dbContext.CaseEvent.Where(w => w.CaseId == caseId).OrderByDescending(o => o.Id)
                .Take(EventTake).ToListAsync(token).ConfigureAwait(false);

            return events.Select(e => ToEvent(e, view)).ToList();
        }

        private CaseEventSummaryDto ToEvent(CaseEvent e, CaseView? view) => new()
        {
            EventTypeId = e.CaseEventTypeId.GetValueOrDefault(),
            EventType = CaseEventTypeNames.Of(e.CaseEventTypeId),
            User = e.CreatedUser,
            Date = CaseSearchMapper.Utc(e.CreatedDate),
            Before = e.Before,
            After = e.After,
            New = view != null && e.Id > view.LastViewedCaseEventId.GetValueOrDefault()
                              && e.CreatedUser != userName
                              && CaseEventTypeNames.ChangeTypes.Contains(e.CaseEventTypeId.GetValueOrDefault())
        };

        private async Task<List<CaseNoteSummaryDto>> NotesAsync(int caseId, CancellationToken token)
        {
            var notes = await dbContext.CaseNote.Where(w => w.CaseId == caseId).OrderByDescending(o => o.Id)
                .Take(NoteTake).ToListAsync(token).ConfigureAwait(false);

            return notes.Select(n => new CaseNoteSummaryDto
            {
                Note = n.Note is { Length: > NoteLength } ? n.Note[..NoteLength] : n.Note,
                User = n.CreatedUser,
                Date = CaseSearchMapper.Utc(n.CreatedDate)
            }).ToList();
        }

        private async Task<CaseKeyHistoryDto> KeyHistoryAsync(Data.Poco.Case existing, CancellationToken token)
        {
            var history = await new GetCaseKeyOutcomeHistoryQuery(dbContext).ExecuteAsync(
                existing.CaseWorkflowGuid, existing.CaseKey, existing.CaseKeyValue, existing.Id, token)
                .ConfigureAwait(false);

            var others = await dbContext.Case.CountAsync(w => w.CaseWorkflowGuid == existing.CaseWorkflowGuid
                                                              && w.CaseKey == existing.CaseKey
                                                              && w.CaseKeyValue == existing.CaseKeyValue
                                                              && w.Id != existing.Id
                                                              && (w.ClosedStatusId == 0 || w.ClosedStatusId == 1),
                token).ConfigureAwait(false);

            return new CaseKeyHistoryDto
            {
                Closed = history.Closed,
                Confirmed = history.Confirmed,
                NewestClosedDate = CaseSearchMapper.Utc(history.NewestClosedDate),
                OtherOpenCases = others
            };
        }

        private async Task<CaseDeltaDto> SinceAsync(int caseId, CaseView? view, CaseSearchRowDto row,
            List<CaseRiskAssessment> history, CancellationToken token)
        {
            var delta = new CaseDeltaDto
            {
                FirstLook = view == null,
                LastViewedDate = view == null ? null : CaseSearchMapper.Utc(view.LastViewedDate),
                PriorityNow = row.Risk.Priority
            };

            if (view == null)
            {
                return delta;
            }

            if (view.LastViewedRiskSequence.HasValue)
            {
                delta.PriorityThen = history.FirstOrDefault(h => h.Sequence == view.LastViewedRiskSequence)?.Priority
                                     ?? await dbContext.CaseRiskAssessment.Where(w => w.CaseId == caseId
                                         && w.Sequence == view.LastViewedRiskSequence)
                                         .Select(s => (int?)s.Priority).FirstOrDefaultAsync(token)
                                         .ConfigureAwait(false);
            }

            var since = view.LastViewedCaseEventId.GetValueOrDefault();
            var changeTypes = CaseEventTypeNames.ChangeTypes.Select(t => (int?)t).ToList();

            var changes = await dbContext.CaseEvent.Where(w => w.CaseId == caseId && w.Id > since
                                                               && w.CreatedUser != userName
                                                               && changeTypes.Contains(w.CaseEventTypeId))
                .OrderBy(o => o.Id).Take(ChangeTake).ToListAsync(token).ConfigureAwait(false);

            delta.Changes = changes.Select(c => ToEvent(c, view)).ToList();

            return delta;
        }

        private void EnsurePermitted()
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            throw new ForbiddenException(strings[CaseSummaryResources.PermissionDenied], permissions);
        }
    }
}
