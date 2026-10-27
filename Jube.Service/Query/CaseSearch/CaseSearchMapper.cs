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

using Jube.CaseRisk;
using Jube.Data.Query.GetCaseTriageEnrichmentQuery;
using Jube.Data.Query.GetCaseTriagePageQuery;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseSearch;

namespace Jube.Service.Query.CaseSearch
{
    internal static class CaseSearchMapper
    {
        private const int RiseThreshold = 10;

        public static CaseSearchRowDto ToDto(GetCaseTriagePageQueryDto.Row row,
            GetCaseTriageEnrichmentQueryDto? enrichment, DateTime now) => new()
        {
            CaseId = row.CaseId,
            CaseKey = row.CaseKey,
            CaseKeyValue = row.CaseKeyValue,
            CreatedDate = Utc(row.CreatedDate),
            AgeMinutes = Math.Max(0, ((row.ClosedStatusId == 3 ? row.ClosedDate : null) ?? now).Subtract(row.CreatedDate)
                .TotalMinutes),
            CaseWorkflowGuid = row.CaseWorkflowGuid,
            CaseWorkflowName = row.WorkflowName,
            CaseWorkflowStatusGuid = row.CaseWorkflowStatusGuid,
            StatusName = row.StatusName,
            ForeColor = row.ForeColor,
            BackColor = row.BackColor,
            StatusEnteredDate = Utc(enrichment?.StayEnteredDate),
            Locked = row.Locked,
            LockedUser = row.LockedUser,
            Mine = row.Mine,
            ClosedStatusId = row.ClosedStatusId,
            Diary = row.Diary,
            DiaryDate = Utc(row.DiaryDate),
            DiaryTypeId = row.DiaryTypeId,
            DiaryType = EnumText.Of<CaseDiaryType>(row.DiaryTypeId),
            QaSample = row.QaSample,
            Value = row.Value,
            Risk = Risk(row),
            Attention = Attention(row),
            ServiceLevel = ServiceLevel(row, enrichment, now),
            FirstViewedDate = Utc(enrichment?.FirstViewedDate),
            FirstViewedBy = enrichment?.FirstViewedBy,
            FirstOwnedDate = Utc(enrichment?.FirstOwnedDate),
            FirstOwnedBy = enrichment?.FirstOwnedBy,
            LastEventTypeId = enrichment?.LastEventTypeId,
            LastEventUser = enrichment?.LastEventUser,
            LastEventDate = Utc(enrichment?.LastEventDate)
        };

        public static CaseRiskSnapshotDto Risk(GetCaseTriagePageQueryDto.Row row) => row.RiskSequence is null
            ? new CaseRiskSnapshotDto { Assessed = false, Lane = (int)CaseRiskLane.Standard,
                LaneName = EnumText.Of<CaseRiskLane>((int)CaseRiskLane.Standard) }
            : new CaseRiskSnapshotDto
            {
                Assessed = true,
                Sequence = row.RiskSequence.Value,
                Lane = row.LaneId.GetValueOrDefault(),
                LaneName = EnumText.Of<CaseRiskLane>(row.LaneId),
                LaneReason = row.LaneReason,
                Likelihood = row.LikelihoodProbability.GetValueOrDefault(),
                LikelihoodBand = row.LikelihoodBandId.GetValueOrDefault(),
                LikelihoodBandName = EnumText.Of<CaseLikelihoodBand>(row.LikelihoodBandId),
                Impact = row.ImpactId.GetValueOrDefault(),
                ImpactName = EnumText.Of<CaseImpactBand>(row.ImpactId),
                Priority = row.Priority.GetValueOrDefault(),
                ExpectedHarm = row.ExpectedHarm.GetValueOrDefault(),
                Method = row.MethodId.GetValueOrDefault(),
                EvidenceCount = row.EvidenceCount,
                NextDeadline = Utc(row.NextDeadlineDate),
                DeadlineBreached = row.DeadlineBreached,
                TopDriver = row.TopDriver,
                TopDriverFamily = row.TopDriverFamily,
                AssessedDate = Utc(row.RiskDate)
            };

        public static CaseAttentionDto Attention(GetCaseTriagePageQueryDto.Row row)
        {
            var reasons = new List<CaseAttentionReasonDto>();

            void Add(CaseAttentionReason reason, string text) =>
                reasons.Add(new CaseAttentionReasonDto { Code = (int)reason, Text = text });

            if (row.Attention)
            {
                if (row.AttReopened)
                {
                    Add(CaseAttentionReason.Reopened, EnumText.Of<CaseAttentionReason>(1)!);
                }

                if (row.AttEscalated)
                {
                    Add(CaseAttentionReason.Escalated, EnumText.Of<CaseAttentionReason>(2)!);
                }

                if (row.AttMoved)
                {
                    Add(CaseAttentionReason.MovedByOther, EnumText.Of<CaseAttentionReason>(3)!);
                }

                if (row.AttDiaryDue)
                {
                    Add(CaseAttentionReason.DiaryDue, EnumText.Of<CaseAttentionReason>(4)!);
                }

                if (row.PriorityRise >= RiseThreshold)
                {
                    Add(CaseAttentionReason.RiskRose, $"Priority has risen by {row.PriorityRise} since you last looked");
                }

                if (row.EventsSince > 0)
                {
                    Add(CaseAttentionReason.NewActivity,
                        $"{row.EventsSince} change{(row.EventsSince == 1 ? string.Empty : "s")} by others since you last looked");
                }
            }

            return new CaseAttentionDto
            {
                Needed = row.Attention,
                Reasons = reasons,
                LastViewedDate = Utc(row.LastViewedDate),
                ChangesSince = row.EventsSince,
                PriorityRise = Math.Max(0, row.PriorityRise)
            };
        }

        public static CaseServiceLevelDto ServiceLevel(GetCaseTriagePageQueryDto.Row row,
            GetCaseTriageEnrichmentQueryDto? enrichment, DateTime now)
        {
            var assessment = ServiceLevelCalculator.Assess(new ServiceLevelInputs(now, row.CreatedDate,
                row.ClosedStatusId == 3 ? row.ClosedDate : null, enrichment?.FirstViewedDate,
                enrichment?.FirstOwnedDate, enrichment?.StayEnteredDate, row.FirstViewTargetMinutes,
                row.OwnershipTargetMinutes, row.ResolutionTargetMinutes, row.MaxDwellMinutes));

            return new CaseServiceLevelDto
            {
                State = (int)assessment.State,
                StateName = StateName(assessment.State),
                Worst = assessment.Worst is null ? null : Measure(assessment.Worst),
                Measures = assessment.Measures.Select(Measure).ToList()
            };
        }

        private static CaseServiceLevelMeasureDto Measure(ServiceLevelMeasure measure) => new()
        {
            Dimension = (int)measure.Dimension,
            DimensionName = measure.Dimension switch
            {
                ServiceLevelDimension.FirstView => "First view",
                ServiceLevelDimension.Ownership => "Ownership",
                ServiceLevelDimension.Resolution => "Resolution",
                _ => "Time in status"
            },
            State = (int)measure.State,
            StateName = StateName(measure.State),
            DueDate = Utc(measure.DueDate),
            ElapsedFraction = measure.ElapsedFraction,
            ActualMinutes = measure.ActualMinutes,
            TargetMinutes = measure.TargetMinutes
        };

        private static string StateName(ServiceLevelState state) => state switch
        {
            ServiceLevelState.Met => "Met",
            ServiceLevelState.OnTrack => "On track",
            ServiceLevelState.AtRisk => "At risk",
            ServiceLevelState.Breached => "Breached",
            _ => "No target"
        };

        public static DateTimeOffset Utc(DateTime value) =>
            new(DateTime.SpecifyKind(value, DateTimeKind.Utc), TimeSpan.Zero);

        public static DateTimeOffset? Utc(DateTime? value) => value.HasValue ? Utc(value.Value) : null;
    }
}
