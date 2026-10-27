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

namespace Jube.Data.Query.GetCaseTriagePageQuery
{
    using Dto.Options;

    public static class CaseTriageSql
    {
        public const string OpenCondition = "(\"Case\".\"ClosedStatusId\" = 0 or \"Case\".\"ClosedStatusId\" = 1)";

        public static string OrderBy(CaseSearchSort sort) => sort switch
        {
            CaseSearchSort.Priority => "\"Priority\" desc nulls last, \"CreatedDate\", \"CaseId\"",
            CaseSearchSort.Age => "\"CreatedDate\", \"CaseId\"",
            CaseSearchSort.Due => "\"NextDeadlineDate\" nulls last, \"Priority\" desc nulls last, \"CaseId\"",
            CaseSearchSort.Value => "\"Value\" desc nulls last, \"CaseId\"",
            CaseSearchSort.Likelihood => "\"LikelihoodProbability\" desc nulls last, \"CaseId\"",
            CaseSearchSort.Newest => "\"CreatedDate\" desc, \"CaseId\"",
            _ => "case when \"LaneId\" in (1, 2) then \"LaneId\" when \"Attention\" then 2.5 " +
                 "when \"QaSample\" and coalesce(\"LaneId\", 4) = 4 then 3.5 " +
                 "else coalesce(\"LaneId\", 4) end, \"Priority\" desc nulls last, \"CreatedDate\", \"CaseId\""
        };

        public static string Resolve(string extraCondition, int mePosition) =>
            string.IsNullOrEmpty(extraCondition) ? extraCondition : extraCondition.Replace("{me}", "(@" + mePosition + ")");

        public static string Matched(string whereSql, string extraCondition) =>
            "\"Matched\" as (select distinct \"Case\".\"Id\" as \"CaseId\" " + whereSql +
            (string.IsNullOrWhiteSpace(extraCondition) ? string.Empty : " and " + extraCondition) + ")";

        public const string Latest =
            "\"Latest\" as (select distinct on (a.\"CaseId\") a.* from \"CaseRiskAssessment\" a " +
            "join \"Matched\" m on m.\"CaseId\" = a.\"CaseId\" order by a.\"CaseId\", a.\"Sequence\" desc)";

        public static string Rows(int mePosition)
        {
            var me = "@" + mePosition;

            return "\"Rows\" as (select c.\"Id\" as \"CaseId\", c.\"CaseKey\", c.\"CaseKeyValue\", c.\"CreatedDate\", " +
                   "c.\"Locked\", c.\"LockedUser\", c.\"LockedDate\", c.\"ClosedStatusId\", c.\"ClosedDate\", " +
                   "c.\"Diary\", c.\"DiaryDate\", c.\"DiaryTypeId\", (c.\"QaSampleStatusId\" = 2 and c.\"ClosedStatusId\" in (0, 1)) as \"QaSample\", c.\"CaseWorkflowGuid\", c.\"CaseWorkflowStatusGuid\", c.\"Rating\", " +
                   "s.\"Name\" as \"StatusName\", s.\"ForeColor\", s.\"BackColor\", s.\"MaxDwellMinutes\", " +
                   "w.\"Name\" as \"WorkflowName\", w.\"FirstViewTargetMinutes\", w.\"OwnershipTargetMinutes\", " +
                   "w.\"ResolutionTargetMinutes\", l.\"Sequence\" as \"RiskSequence\", l.\"LaneId\", l.\"Priority\", " +
                   "l.\"LikelihoodProbability\", l.\"LikelihoodBandId\", l.\"ImpactId\", l.\"ExpectedHarm\", " +
                   "l.\"NextDeadlineDate\", l.\"DeadlineBreached\", l.\"LaneReason\", l.\"MethodId\", " +
                   "l.\"EvidenceCount\", l.\"CreatedDate\" as \"RiskDate\", " +
                   "l.\"ContributionsJson\"->0->>'label' as \"TopDriver\", " +
                   "l.\"ContributionsJson\"->0->>'family' as \"TopDriverFamily\", " +
                   "(l.\"ImpactComponentsJson\"->>'value')::double precision as \"Value\", " +
                   "(v.\"CaseId\" is not null) as \"Viewed\", v.\"LastViewedDate\", v.\"LastViewedRiskSequence\", " +
                   "(c.\"Locked\" = 1 and c.\"LockedUser\" = " + me + ") as \"Mine\", " +
                   "coalesce(v.\"CaseId\" is not null and exists (select 1 from \"CaseStatusTransition\" t " +
                   "where t.\"CaseId\" = c.\"Id\" and t.\"EnteredDate\" > v.\"LastViewedDate\" " +
                   "and t.\"EnterReasonId\" = 3), false) as \"AttReopened\", " +
                   "coalesce(v.\"CaseId\" is not null and exists (select 1 from \"CaseStatusTransition\" t " +
                   "where t.\"CaseId\" = c.\"Id\" and t.\"EnteredDate\" > v.\"LastViewedDate\" " +
                   "and t.\"EnterReasonId\" = 4), false) as \"AttEscalated\", " +
                   "coalesce(v.\"CaseId\" is not null and exists (select 1 from \"CaseStatusTransition\" t " +
                   "where t.\"CaseId\" = c.\"Id\" and t.\"EnteredDate\" > v.\"LastViewedDate\" " +
                   "and t.\"EnterReasonId\" = 2 and t.\"EnteredUser\" is distinct from " + me + "), false) " +
                   "as \"AttMoved\", " +
                   "(c.\"Diary\" = 1 and c.\"DiaryDate\" <= (now() at time zone 'utc') " +
                   "and (v.\"CaseId\" is not null or (c.\"Locked\" = 1 and c.\"LockedUser\" = " + me + "))) " +
                   "as \"AttDiaryDue\", " +
                   "case when v.\"CaseId\" is not null and l.\"Sequence\" > coalesce(v.\"LastViewedRiskSequence\", 0) " +
                   "then coalesce(l.\"Priority\", 0) - coalesce((select a.\"Priority\" from \"CaseRiskAssessment\" a " +
                   "where a.\"CaseId\" = c.\"Id\" and a.\"Sequence\" = v.\"LastViewedRiskSequence\"), 0) " +
                   "else 0 end as \"PriorityRise\", " +
                   "case when v.\"CaseId\" is not null then (select count(*) from \"CaseEvent\" e " +
                   "where e.\"CaseId\" = c.\"Id\" and e.\"Id\" > coalesce(v.\"LastViewedCaseEventId\", 0) " +
                   "and e.\"CaseEventTypeId\" in (5, 7, 8, 9, 10, 11, 12, 13, 14) " +
                   "and e.\"CreatedUser\" is distinct from " + me + ") else 0 end as \"EventsSince\" " +
                   "from \"Matched\" m join \"Case\" c on c.\"Id\" = m.\"CaseId\" " +
                   "join \"CaseWorkflowStatus\" s on s.\"Guid\" = c.\"CaseWorkflowStatusGuid\" " +
                   "join \"CaseWorkflow\" w on w.\"Guid\" = c.\"CaseWorkflowGuid\" " +
                   "left join \"Latest\" l on l.\"CaseId\" = c.\"Id\" " +
                   "left join \"CaseView\" v on v.\"CaseId\" = c.\"Id\" and v.\"User\" = " + me + ")";
        }

        public const string Ranked =
            "\"Ranked\" as (select r.*, (r.\"ClosedStatusId\" in (0, 1) and (r.\"Viewed\" or r.\"Mine\") " +
            "and (r.\"AttReopened\" or r.\"AttEscalated\" or r.\"AttMoved\" or r.\"AttDiaryDue\" " +
            "or r.\"PriorityRise\" >= 10 or r.\"EventsSince\" > 0)) as \"Attention\" from \"Rows\" r)";

        public static string Page(string whereSql, string extraCondition, int mePosition, CaseSearchSort sort,
            int take, int skip) =>
            "with " + Matched(whereSql, extraCondition) + ", " + Latest + ", " + Rows(mePosition) + ", " + Ranked +
            " select x.*, count(*) over () as \"Total\" from \"Ranked\" x order by " + OrderBy(sort) +
            " limit " + take + " offset " + skip;

        public static string Count(string whereSql, string extraCondition) =>
            "select count(*) as \"Total\" from (select distinct \"Case\".\"Id\" " + whereSql +
            (string.IsNullOrWhiteSpace(extraCondition) ? string.Empty : " and " + extraCondition) + ") x";

        public static string Matrix(string whereSql, string extraCondition) =>
            "with " + Matched(whereSql, extraCondition) + ", " + Latest +
            " select coalesce(l.\"LikelihoodBandId\", 0) as \"LikelihoodBandId\", " +
            "coalesce(l.\"ImpactId\", 0) as \"ImpactId\", count(*) as \"Cases\" " +
            "from \"Matched\" m left join \"Latest\" l on l.\"CaseId\" = m.\"CaseId\" group by 1, 2";
    }
}
