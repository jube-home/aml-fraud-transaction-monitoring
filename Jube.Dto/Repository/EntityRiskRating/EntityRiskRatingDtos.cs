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
using Jube.Dto.Validation;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.EntityRiskRating
{
    [Description("The risk rating of an entity key, one version of it.")]
    [MessagePackObject]
    public class EntityRiskRatingDto
    {
        [Description("The key field name, for example Account. Required, at most 128 characters.")]
        [Key(0)]
        public string KeyName { get; set; } = string.Empty;

        [Description("The key value exactly as stored against transactions and cases. Required, at most 256 characters.")]
        [Key(1)]
        public string KeyValue { get; set; } = string.Empty;

        [Description("Version number, starting at 1; the highest is current. Ignored on input.")]
        [Key(2)]
        public int Version { get; set; }

        [Description("The tier: 1 Low, 2 Medium, 3 High. Required.")]
        [Key(3)]
        public int TierId { get; set; }

        [Description("Optional numeric score alongside the tier.")]
        [Key(4)]
        public double? Score { get; set; }

        [Description("Where the rating came from: 1 Imported, 2 Set by hand. Set by the service.")]
        [Key(5)]
        public int SourceId { get; set; }

        [Description("Why the rating was set; required for a rating set by hand, at most 500 characters.")]
        [Key(6)]
        public string? Reason { get; set; }

        [Description("When the rating is next due a review, in UTC. Optional.")]
        [Key(7)]
        public DateTime? ReviewByDate { get; set; }

        [Description("The user who made this version. Set by the service.")]
        [Key(8)]
        public string CreatedUser { get; set; } = string.Empty;

        [Description("When this version was made. Set by the service.")]
        [Key(9)]
        public DateTime CreatedDate { get; set; }
    }

    [Description("A rating and its history for one entity key.")]
    [MessagePackObject]
    public class EntityRiskRatingHistoryDto
    {
        [Description("The current rating, or empty when the key has never been rated.")]
        [Key(0)]
        public EntityRiskRatingDto? Current { get; set; }

        [Description("Every version, newest first, at most 50.")]
        [Key(1)]
        public List<EntityRiskRatingDto> History { get; set; } = [];
    }

    [Description("A batch of institution-supplied ratings to import, at most 500.")]
    [MessagePackObject]
    public class EntityRiskRatingImportDto
    {
        [Description("The ratings. Each is checked on its own; the source is recorded as Imported.")]
        [Key(0)]
        public List<EntityRiskRatingDto> Ratings { get; set; } = [];
    }

    [Description("What happened to each rating in an import, in the order given.")]
    [MessagePackObject]
    public class EntityRiskRatingImportResultDto
    {
        [Description("How many were written as a new version.")]
        [Key(0)]
        public int Written { get; set; }

        [Description("How many were already the current rating and so unchanged.")]
        [Key(1)]
        public int Unchanged { get; set; }

        [Description("Why each refused rating was refused: its position in the batch (from 0) and a plain message.")]
        [Key(2)]
        public List<ValidationErrorDto> Errors { get; set; } = [];
    }

    [Description("A proposed manual change to a rating, waiting for a second person.")]
    [MessagePackObject]
    public class EntityRiskRatingRequestDto
    {
        [Description("The request's identifier.")]
        [Key(0)]
        public Guid Guid { get; set; }

        [Description("The key field name.")]
        [Key(1)]
        public string KeyName { get; set; } = string.Empty;

        [Description("The key value.")]
        [Key(2)]
        public string KeyValue { get; set; } = string.Empty;

        [Description("The tier proposed: 1 Low, 2 Medium, 3 High.")]
        [Key(3)]
        public int TierId { get; set; }

        [Description("The score proposed.")]
        [Key(4)]
        public double? Score { get; set; }

        [Description("The review date proposed, in UTC.")]
        [Key(5)]
        public DateTime? ReviewByDate { get; set; }

        [Description("Why the change is proposed.")]
        [Key(6)]
        public string Reason { get; set; } = string.Empty;

        [Description("The rating version current when it was proposed.")]
        [Key(7)]
        public int BasedOnVersion { get; set; }

        [Description("1 Pending, 2 Approved, 3 Rejected, 4 Retracted.")]
        [Key(8)]
        public int StateId { get; set; }

        [Description("The rating version written on approval.")]
        [Key(9)]
        public int? ResultingVersion { get; set; }

        [Description("Who proposed it.")]
        [Key(10)]
        public string RequestedUser { get; set; } = string.Empty;

        [Description("UTC. When it was proposed.")]
        [Key(11)]
        public DateTime RequestedDate { get; set; }

        [Description("Who decided it.")]
        [Key(12)]
        public string? DecidedUser { get; set; }

        [Description("UTC. When it was decided.")]
        [Key(13)]
        public DateTime? DecidedDate { get; set; }

        [Description("The reason given with the decision.")]
        [Key(14)]
        public string? DecisionNote { get; set; }
    }

    [Description("A decision on a proposed rating change.")]
    [MessagePackObject]
    public class EntityRiskRatingDecisionDto
    {
        [Description("The request decided.")]
        [Key(0)]
        public Guid Guid { get; set; }

        [Description("The reason; required to reject, at most 500 characters.")]
        [Key(1)]
        public string? Note { get; set; }
    }
}
