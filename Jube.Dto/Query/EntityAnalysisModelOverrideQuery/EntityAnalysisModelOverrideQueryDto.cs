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
using Jube.Dto.Overrides;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Jube.Dto.Query.EntityAnalysisModelOverrideQuery
{
    public class EntityAnalysisModelOverrideQueryDto
    {
        [Description("Name of the Entity Analysis Model that has a request XPath named after the queried " +
                     "override key with override enabled.")]
        public string? Name { get; set; }

        [Description("Globally unique identifier of the Entity Analysis Model.")]
        public Guid EntityAnalysisModelGuid { get; set; }

        [Description("True when an active, unexpired override exists for the queried key and key value on " +
                     "this model.")]
        public bool HasOverride { get; set; }

        [Description("Whether the override mutes the Activation Rules' consequences or forces them. " +
                     "Meaningless when Override is false.")]
        public EntityAnalysisModelOverrideKind OverrideKind { get; set; }

        [Description("UTC instant at which the override expires; null when there is no override or it " +
                     "never expires.")]
        public DateTimeOffset? DeleteExpiryDate { get; set; }
    }
}