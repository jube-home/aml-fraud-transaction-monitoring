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

using Jube.Data.Query.GetCaseTriagePageQuery;
using Jube.Dto.Options;

namespace Jube.Service.Query.CaseSearch
{
    internal static class CaseSearchViewSql
    {
        private const string Utc = "(now() at time zone 'utc')";
        private const string ChangeEvents = "5, 7, 8, 9, 10, 11, 12, 13, 14";

        private const string LatestAssessment =
            "a.\"Sequence\" = (select max(b.\"Sequence\") from \"CaseRiskAssessment\" b where b.\"CaseId\" = a.\"CaseId\")";

        private static string Overdue =>
            "(\"CaseWorkflow\".\"ResolutionTargetMinutes\" is not null and (" + Utc +
            " - \"Case\".\"CreatedDate\") > make_interval(mins => \"CaseWorkflow\".\"ResolutionTargetMinutes\")) " +
            "or (\"CaseWorkflow\".\"FirstViewTargetMinutes\" is not null and not exists (select 1 from \"CaseEvent\" e " +
            "where e.\"CaseId\" = \"Case\".\"Id\" and e.\"CaseEventTypeId\" in (2, 4, 16)) and (" + Utc +
            " - \"Case\".\"CreatedDate\") > make_interval(mins => \"CaseWorkflow\".\"FirstViewTargetMinutes\")) " +
            "or (\"CaseWorkflow\".\"OwnershipTargetMinutes\" is not null and not exists (select 1 from \"CaseEvent\" e " +
            "where e.\"CaseId\" = \"Case\".\"Id\" and e.\"CaseEventTypeId\" in (2, 3, 6, 14)) and (" + Utc +
            " - \"Case\".\"CreatedDate\") > make_interval(mins => \"CaseWorkflow\".\"OwnershipTargetMinutes\")) " +
            "or (\"CaseWorkflowStatus\".\"MaxDwellMinutes\" is not null and exists (select 1 from " +
            "\"CaseStatusTransition\" t where t.\"CaseId\" = \"Case\".\"Id\" and t.\"ExitedDate\" is null and (" + Utc +
            " - t.\"EnteredDate\") > make_interval(mins => \"CaseWorkflowStatus\".\"MaxDwellMinutes\")))";

        private static string Returned =>
            "(exists (select 1 from \"CaseView\" v where v.\"CaseId\" = \"Case\".\"Id\" and v.\"User\" = {me} and (" +
            "exists (select 1 from \"CaseStatusTransition\" t where t.\"CaseId\" = \"Case\".\"Id\" " +
            "and t.\"EnteredDate\" > v.\"LastViewedDate\" and (t.\"EnterReasonId\" in (3, 4) " +
            "or (t.\"EnterReasonId\" = 2 and t.\"EnteredUser\" is distinct from {me}))) " +
            "or exists (select 1 from \"CaseEvent\" e where e.\"CaseId\" = \"Case\".\"Id\" " +
            "and e.\"Id\" > coalesce(v.\"LastViewedCaseEventId\", 0) and e.\"CaseEventTypeId\" in (" + ChangeEvents +
            ") and e.\"CreatedUser\" is distinct from {me}) " +
            "or exists (select 1 from \"CaseRiskAssessment\" a where a.\"CaseId\" = \"Case\".\"Id\" and " +
            LatestAssessment + " and a.\"Sequence\" > coalesce(v.\"LastViewedRiskSequence\", 0) " +
            "and coalesce(a.\"Priority\", 0) - coalesce((select p.\"Priority\" from \"CaseRiskAssessment\" p " +
            "where p.\"CaseId\" = a.\"CaseId\" and p.\"Sequence\" = v.\"LastViewedRiskSequence\"), 0) >= 10))) " +
            "or (\"Case\".\"Diary\" = 1 and \"Case\".\"DiaryDate\" <= " + Utc + " and (exists (select 1 from " +
            "\"CaseView\" w where w.\"CaseId\" = \"Case\".\"Id\" and w.\"User\" = {me}) or (\"Case\".\"Locked\" = 1 " +
            "and \"Case\".\"LockedUser\" = {me}))))";

        public static string Condition(CaseSearchView view) => view switch
        {
            CaseSearchView.Mine => CaseTriageSql.OpenCondition + " and \"Case\".\"Locked\" = 1 and " +
                                   "\"Case\".\"LockedUser\" = {me}",
            CaseSearchView.Returned => CaseTriageSql.OpenCondition + " and " + Returned,
            CaseSearchView.Free => CaseTriageSql.OpenCondition +
                                   " and (\"Case\".\"Locked\" = 0 or \"Case\".\"Locked\" is null)",
            CaseSearchView.Urgent => CaseTriageSql.OpenCondition + " and exists (select 1 from " +
                                     "\"CaseRiskAssessment\" a where a.\"CaseId\" = \"Case\".\"Id\" and " +
                                     LatestAssessment + " and a.\"LaneId\" in (1, 2, 3))",
            CaseSearchView.Overdue => CaseTriageSql.OpenCondition + " and (" + Overdue + ")",
            CaseSearchView.QaSamples => CaseTriageSql.OpenCondition + " and \"Case\".\"QaSampleStatusId\" = 2",
            _ => CaseTriageSql.OpenCondition
        };

        public static string Skimmable =>
            CaseTriageSql.OpenCondition + " and (\"Case\".\"Locked\" = 0 or \"Case\".\"Locked\" is null " +
            "or \"Case\".\"LockedUser\" = {me})";
    }
}
