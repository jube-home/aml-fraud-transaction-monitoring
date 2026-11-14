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

namespace Jube.Dto.Repository.CaseAnalystAssessment
{
    [Description("An analyst's own view of how likely a case is to be confirmed, kept beside the computed likelihood. It never replaces the computed value and never changes the ranking.")]
    [MessagePackObject]
    public class CaseAnalystAssessmentDto
    {
        [Description("Identifies the assessment. Set by the service.")]
        [Key(0)]
        public Guid Guid { get; set; }

        [Description("The case assessed. Required.")]
        [Key(1)]
        public int CaseId { get; set; }

        [Description("The analyst's view of the likelihood: 1 Low, 2 Medium, 3 High. Required.")]
        [Key(2)]
        public int LikelihoodId { get; set; }

        [Description("Why the analyst holds this view. Required, at most 1000 characters.")]
        [Key(3)]
        public string? Reason { get; set; }

        [Description("The computed likelihood in force when the view was recorded, 0 to 1; empty when the case had not been assessed. Set by the service.")]
        [Key(4)]
        public double? ComputedLikelihoodProbability { get; set; }

        [Description("The analyst who recorded the view. Set by the service.")]
        [Key(5)]
        public string CreatedUser { get; set; } = string.Empty;

        [Description("When the view was recorded. Set by the service.")]
        [Key(6)]
        public DateTimeOffset CreatedDate { get; set; }

        [Description("When the view was withdrawn; empty while it stands. Set by the service.")]
        [Key(7)]
        public DateTimeOffset? WithdrawnDate { get; set; }

        [Description("True when the viewer recorded this view and may withdraw it. Set by the service.")]
        [Key(8)]
        public bool Mine { get; set; }
    }
}
