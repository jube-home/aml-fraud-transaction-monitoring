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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AdvancedAnalyticsModel.Models
{
    public class AdvancedAnalyticsTrainedModelDto
    {
        [Description("Numeric identifier of the trained model.")]
        public int Id { get; set; }

        [Description("Globally unique identifier of the trained model.")]
        public Guid Guid { get; set; }

        [Description("Identifier of the Advanced Analytics instance that owns the model.")]
        public int AdvancedAnalyticsInstanceId { get; set; }

        [Description("Name of the model, unique within its instance for the same kind.")]
        public string? Name { get; set; }

        [Description("What sort of model it is, for example a classifier.")]
        public AdvancedAnalyticsKind Kind { get; set; }

        [Description("How the model came to exist: made by the agent or uploaded.")]
        public AdvancedAnalyticsSource Source { get; set; }

        [Description("True when the model has a live version. Only promotion makes a model live.")]
        public bool Active { get; set; }

        [Description("Identifier of the live version, or null when none has been promoted.")]
        public int? ActiveVersionId { get; set; }

        [Description("Sequence number of the live version, or null when none has been promoted.")]
        public int? ActiveVersionNumber { get; set; }

        [Description("Opaque label supplied when the model was created, or null.")]
        public string? CorrelationKey { get; set; }

        [Description("User who created the model.")]
        public string? CreatedUser { get; set; }

        [Description("When the model was created, in UTC.")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("When the model was last changed, in UTC, or null if it never was.")]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Change counter of the model row, raised by every promotion or rollback.")]
        public int Version { get; set; }

        [Description("The versions of the model, newest first. Only filled in when a single model is fetched.")]
        public List<AdvancedAnalyticsModelVersionDto>? Versions { get; set; }
    }
}