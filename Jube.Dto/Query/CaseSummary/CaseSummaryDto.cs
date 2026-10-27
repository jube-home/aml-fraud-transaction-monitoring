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
using Jube.Dto.Query.CaseSearch;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseSummary
{
    [Description("Everything needed to decide whether to open a case, without opening it or taking its lock: " +
                 "the case as the triage list shows it, why it is ranked where it is, how its risk has moved, where " +
                 "it has been, what happened to it and what changed since the caller last looked.")]
    public class CaseSummaryDto
    {
        [Description("The case as the triage list shows it.")]
        public CaseSearchRowDto Case { get; set; } = new();

        [Description("Every piece of evidence behind the likelihood, strongest first; empty when none exists yet.")]
        public List<CaseContributionDto> Contributions { get; set; } = [];

        [Description("The base rate the evidence moved the likelihood away from.")]
        public double? BaseRate { get; set; }

        [Description("The components of the impact band.")]
        public CaseImpactComponentsDto? Impact { get; set; }

        [Description("The most recent assessments, newest first.")]
        public List<CaseRiskHistoryItemDto> RiskHistory { get; set; } = [];

        [Description("Every stay in a status, oldest first, with the time spent.")]
        public List<CaseStayDto> Stays { get; set; } = [];

        [Description("The most recent audit events, newest first.")]
        public List<CaseEventSummaryDto> RecentEvents { get; set; } = [];

        [Description("The most recent notes, newest first.")]
        public List<CaseNoteSummaryDto> Notes { get; set; } = [];

        [Description("What happened to earlier cases on the same key.")]
        public CaseKeyHistoryDto KeyHistory { get; set; } = new();

        [Description("What changed since the caller last looked.")]
        public CaseDeltaDto Since { get; set; } = new();
    }
}
