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
using System.Collections.Generic;
using System.ComponentModel;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.ModelGovernance
{
    [Description("A model's governance statement, one version of it.")]
    [MessagePackObject]
    public class ModelGovernanceRecordDto
    {
        [Description("1 Entity analysis model, 2 Exhaustive adaptation. Required.")]
        [Key(0)]
        public int KindId { get; set; }

        [Description("The Guid of the model. Required.")]
        [Key(1)]
        public Guid ModelGuid { get; set; }

        [Description("Version number, starting at 1; the highest is current. Ignored on input.")]
        [Key(2)]
        public int Version { get; set; }

        [Description("The person accountable for the model. Required, at most 200 characters.")]
        [Key(3)]
        public string OwnerUser { get; set; } = string.Empty;

        [Description("What the model is for and the decisions it informs. Required, at most 2000 characters.")]
        [Key(4)]
        public string IntendedPurpose { get; set; } = string.Empty;

        [Description("1 Low, 2 Medium, 3 High. Required.")]
        [Key(5)]
        public int RiskTierId { get; set; }

        [Description("How a person reviews or can override what the model does. Required, at most 2000 characters.")]
        [Key(6)]
        public string HumanOversight { get; set; } = string.Empty;

        [Description("When the model is next due a review, in UTC. Optional.")]
        [Key(7)]
        public DateTime? NextReviewDate { get; set; }

        [Description("Who saved this version. Set by the service.")]
        [Key(8)]
        public string CreatedUser { get; set; } = string.Empty;

        [Description("When this version was saved. Set by the service.")]
        [Key(9)]
        public DateTime CreatedDate { get; set; }
    }

    [Description("A model and where its governance stands.")]
    [MessagePackObject]
    public class ModelGovernanceInventoryRowDto
    {
        [Description("1 Entity analysis model, 2 Exhaustive adaptation.")]
        [Key(0)]
        public int KindId { get; set; }

        [Description("The Guid of the model.")]
        [Key(1)]
        public Guid ModelGuid { get; set; }

        [Description("The model's name.")]
        [Key(2)]
        public string Name { get; set; } = string.Empty;

        [Description("True when a governance statement has been recorded.")]
        [Key(3)]
        public bool Recorded { get; set; }

        [Description("The accountable person, when recorded.")]
        [Key(4)]
        public string? OwnerUser { get; set; }

        [Description("1 Low, 2 Medium, 3 High, when recorded.")]
        [Key(5)]
        public int? RiskTierId { get; set; }

        [Description("When the next review is due, when set.")]
        [Key(6)]
        public DateTime? NextReviewDate { get; set; }

        [Description("1 Not recorded, 2 Current, 3 Due within 30 days, 4 Overdue.")]
        [Key(7)]
        public int ReviewStatusId { get; set; }
    }

    [Description("A model's current governance statement and its earlier versions.")]
    [MessagePackObject]
    public class ModelGovernanceHistoryDto
    {
        [Description("The current statement, or empty when none has been recorded.")]
        [Key(0)]
        public ModelGovernanceRecordDto? Current { get; set; }

        [Description("Every version, newest first, at most 50.")]
        [Key(1)]
        public List<ModelGovernanceRecordDto> History { get; set; } = [];
    }
}
