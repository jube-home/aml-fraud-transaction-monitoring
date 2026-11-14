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
    using System.Collections.Generic;
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
            "\"Matched\" as (select distinct on (\"Case\".\"Id\") \"Case\".\"Id\" as \"CaseId\", \"Case\".\"CreatedDate\", " +
            "\"Case\".\"RiskLaneId\", \"Case\".\"RiskPriority\", \"Case\".\"RiskLikelihoodProbability\", " +
            "\"Case\".\"RiskNextDeadlineDate\", \"Case\".\"RiskValue\", \"Case\".\"Locked\", \"Case\".\"LockedUser\", " +
            "\"Case\".\"QaSampleStatusId\", \"Case\".\"ClosedStatusId\" " + whereSql +
            (string.IsNullOrWhiteSpace(extraCondition) ? string.Empty : " and " + extraCondition) + ")";

        public static string PreOrderBy(CaseSearchSort sort) => sort switch
        {
            CaseSearchSort.Priority => "\"RiskPriority\" desc nulls last, \"CreatedDate\", \"CaseId\"",
            CaseSearchSort.Age => "\"CreatedDate\", \"CaseId\"",
            CaseSearchSort.Due => "\"RiskNextDeadlineDate\" nulls last, \"RiskPriority\" desc nulls last, \"CaseId\"",
            CaseSearchSort.Value => "\"RiskValue\" desc nulls last, \"CaseId\"",
            CaseSearchSort.Likelihood => "\"RiskLikelihoodProbability\" desc nulls last, \"CaseId\"",
            CaseSearchSort.Newest => "\"CreatedDate\" desc, \"CaseId\"",
            _ => "coalesce(\"RiskLaneId\", 4), \"RiskPriority\" desc nulls last, \"CreatedDate\", \"CaseId\""
        };

        public static string Pre(int mePosition, CaseSearchSort sort, int reach)
        {
            var me = "@" + mePosition;
            var top = "(select \"CaseId\" from \"Matched\" order by " + PreOrderBy(sort) + " limit " + reach + ")";

            return "\"Pre\" as (" + (sort == CaseSearchSort.Triage
                ? "select m.\"CaseId\" from \"Matched\" m join \"CaseView\" v on v.\"CaseId\" = m.\"CaseId\" and v.\"User\" = " + me + " " +
                  "union select \"CaseId\" from \"Matched\" where \"Locked\" = 1 and \"LockedUser\" = " + me + " " +
                  "union select \"CaseId\" from \"Matched\" where \"QaSampleStatusId\" = 2 and \"ClosedStatusId\" in (0, 1) " +
                  "union " + top
                : "select \"CaseId\" from " + top + " t") + ")";
        }

        public const string LatestOfMatched =
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
                   "w.\"ResolutionTargetMinutes\", l.\"Sequence\" as \"RiskSequence\", c.\"RiskLaneId\" as \"LaneId\", " +
                   "c.\"RiskPriority\" as \"Priority\", c.\"RiskLikelihoodProbability\" as \"LikelihoodProbability\", " +
                   "l.\"LikelihoodBandId\", l.\"ImpactId\", l.\"ExpectedHarm\", " +
                   "c.\"RiskNextDeadlineDate\" as \"NextDeadlineDate\", l.\"DeadlineBreached\", l.\"LaneReason\", l.\"MethodId\", " +
                   "l.\"EvidenceCount\", l.\"CreatedDate\" as \"RiskDate\", " +
                   "l.\"ContributionsJson\"->0->>'label' as \"TopDriver\", " +
                   "l.\"ContributionsJson\"->0->>'family' as \"TopDriverFamily\", " +
                   "c.\"RiskValue\" as \"Value\", " +
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
                   "and e.\"CreatedUser\" is distinct from " + me + ") else 0 end as \"EventsSince\", " +
                   "case when c.\"ClosedStatusId\" in (0, 1) then (select count(*) from \"CaseChecklistItem\" k " +
                   "where k.\"CaseId\" = c.\"Id\" and k.\"CaseWorkflowStatusGuid\" = c.\"CaseWorkflowStatusGuid\" " +
                   "and k.\"StateId\" = 1) else 0 end as \"ChecklistOpen\", " +
                   "case when c.\"ClosedStatusId\" in (0, 1) then (select count(*) from \"CaseChecklistItem\" k " +
                   "where k.\"CaseId\" = c.\"Id\" and k.\"CaseWorkflowStatusGuid\" = c.\"CaseWorkflowStatusGuid\" " +
                   "and k.\"StateId\" = 1 and (k.\"SlaBreachedDate\" is not null or " +
                   "k.\"DueDate\" < (now() at time zone 'utc'))) else 0 end as \"ChecklistBreached\" " +
                   "from \"Pre\" m join \"Case\" c on c.\"Id\" = m.\"CaseId\" " +
                   "join \"CaseWorkflowStatus\" s on s.\"Guid\" = c.\"CaseWorkflowStatusGuid\" and coalesce(s.\"Deleted\", 0) = 0 " +
                   "join \"CaseWorkflow\" w on w.\"Guid\" = c.\"CaseWorkflowGuid\" and coalesce(w.\"Deleted\", 0) = 0 " +
                   "left join lateral (select a.* from \"CaseRiskAssessment\" a where a.\"CaseId\" = c.\"Id\" " +
                   "order by a.\"Sequence\" desc limit 1) l on true " +
                   "left join \"CaseView\" v on v.\"CaseId\" = c.\"Id\" and v.\"User\" = " + me + ")";
        }

        public const string Ranked =
            "\"Ranked\" as (select r.*, (r.\"ClosedStatusId\" in (0, 1) and (r.\"Viewed\" or r.\"Mine\") " +
            "and (r.\"AttReopened\" or r.\"AttEscalated\" or r.\"AttMoved\" or r.\"AttDiaryDue\" " +
            "or r.\"PriorityRise\" >= 10 or r.\"EventsSince\" > 0)) as \"Attention\" from \"Rows\" r)";

        public static string Page(string whereSql, string extraCondition, int mePosition, CaseSearchSort sort,
            int take, int skip) =>
            "with " + Matched(whereSql, extraCondition) + ", " + Pre(mePosition, sort, take + skip) + ", " +
            Rows(mePosition) + ", " + Ranked +
            " select x.*, (select count(*) from \"Matched\") as \"Total\" from \"Ranked\" x order by " + OrderBy(sort) +
            " limit " + take + " offset " + skip;

        public static string Counts(string whereSql, IReadOnlyList<string> conditions)
        {
            var columns = new List<string>();

            for (var index = 0; index < conditions.Count; index++)
            {
                columns.Add("count(distinct case when (" + conditions[index] + ") then \"Case\".\"Id\" end) as \"C" +
                            index + "\"");
            }

            return "select " + string.Join(", ", columns) + " " + whereSql;
        }

        public static string Count(string whereSql, string extraCondition) =>
            "select count(*) as \"Total\" from (select distinct \"Case\".\"Id\" " + whereSql +
            (string.IsNullOrWhiteSpace(extraCondition) ? string.Empty : " and " + extraCondition) + ") x";

        public static string Matrix(string whereSql, string extraCondition) =>
            "with " + Matched(whereSql, extraCondition) + ", " + LatestOfMatched +
            " select coalesce(l.\"LikelihoodBandId\", 0) as \"LikelihoodBandId\", " +
            "coalesce(l.\"ImpactId\", 0) as \"ImpactId\", count(*) as \"Cases\" " +
            "from \"Matched\" m left join \"Latest\" l on l.\"CaseId\" = m.\"CaseId\" group by 1, 2";
    }
}
