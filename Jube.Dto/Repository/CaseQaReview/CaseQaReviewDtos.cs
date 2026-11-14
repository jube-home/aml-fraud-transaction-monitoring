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

using System;
using System.ComponentModel;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.CaseQaReview
{
    [Description("A quality review of a closed case that was picked for sampling.")]
    [MessagePackObject]
    public class CaseQaReviewDto
    {
        [Description("The case reviewed. Required.")]
        [Key(0)]
        public int CaseId { get; set; }

        [Description("How the analyst handled the case: 1 Pass, 2 Fail, 3 Not applicable. Required.")]
        [Key(1)]
        public int AnalystVerdictId { get; set; }

        [Description("How good the triage agent's recommendation was: 1 Pass, 2 Fail, 3 Not applicable; leave empty when there was none.")]
        [Key(2)]
        public int? AgentVerdictId { get; set; }

        [Description("Optional free text, at most 1000 characters.")]
        [Key(3)]
        public string? Comment { get; set; }

        [Description("The reviewer. Set by the service.")]
        [Key(4)]
        public string ReviewerUser { get; set; } = string.Empty;

        [Description("When the review was made. Set by the service.")]
        [Key(5)]
        public DateTimeOffset CreatedDate { get; set; }
    }

    [Description("Whether the viewer may review a case, and the reviews already made of it.")]
    [MessagePackObject]
    public class CaseQaReviewStateDto
    {
        [Description("True when the case was picked for quality review, is closed, and was not closed by the viewer.")]
        [Key(0)]
        public bool Eligible { get; set; }

        [Description("True when the case has a triage recommendation that can be judged.")]
        [Key(1)]
        public bool HasRecommendation { get; set; }

        [Description("The reviews made so far, newest first.")]
        [Key(2)]
        public List<CaseQaReviewDto> Reviews { get; set; } = [];
    }
}
