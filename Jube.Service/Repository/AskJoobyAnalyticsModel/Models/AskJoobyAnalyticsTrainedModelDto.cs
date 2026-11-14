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
using Jube.Data.Repository.Models;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AskJoobyAnalyticsModel.Models
{
    [MessagePackObject]
    public class AskJoobyAnalyticsTrainedModelDto
    {
        [Description("Numeric identifier of the trained model.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("Globally unique identifier of the trained model.")]
        [Key(1)]
        public Guid Guid { get; set; }

        [Description("Identifier of the AskJooby Analytics instance that owns the model.")]
        [Key(2)]
        public int AskJoobyAnalyticsInstanceId { get; set; }

        [Description("Name of the model, unique within its instance for the same kind.")]
        [Key(3)]
        public string? Name { get; set; }

        [Description("What sort of model it is, for example a classifier.")]
        [Key(4)]
        public AdvancedAnalyticsKind Kind { get; set; }

        [Description("How the model came to exist: made by the agent or uploaded.")]
        [Key(5)]
        public AdvancedAnalyticsSource Source { get; set; }

        [Description("True when the model has a live version. Only promotion makes a model live.")]
        [Key(6)]
        public bool Active { get; set; }

        [Description("Identifier of the live version, or null when none has been promoted.")]
        [Key(7)]
        public int? ActiveVersionId { get; set; }

        [Description("Sequence number of the live version, or null when none has been promoted.")]
        [Key(8)]
        public int? ActiveVersionNumber { get; set; }

        [Description("Opaque label supplied when the model was created, or null.")]
        [Key(9)]
        public string? CorrelationKey { get; set; }

        [Description("User who created the model.")]
        [Key(10)]
        public string? CreatedUser { get; set; }

        [Description("When the model was created, in UTC.")]
        [Key(11)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("When the model was last changed, in UTC, or null if it never was.")]
        [Key(12)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Change counter of the model row, raised by every promotion or rollback.")]
        [Key(13)]
        public int Version { get; set; }

        [Description("The versions of the model, newest first. Only filled in when a single model is fetched.")]
        [Key(14)]
        public List<AskJoobyAnalyticsModelVersionDto>? Versions { get; set; }
    }
}