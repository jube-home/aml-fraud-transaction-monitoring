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

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Jube.Dto.Query.EntityAnalysisModelSuppressionQuery
{
    public class EntityAnalysisModelSuppressionQueryDto
    {
        [Description("Name of the Entity Analysis Model that has a request XPath named after the queried " +
                     "suppression key with suppression enabled.")]
        public string? Name { get; set; }

        [Description("Globally unique identifier of the Entity Analysis Model.")]
        public Guid EntityAnalysisModelGuid { get; set; }

        [Description("True when an active, unexpired suppression exists for the queried key and key value on " +
                     "this model.")]
        public bool Suppression { get; set; }

        [Description("UTC instant at which the suppression expires; null when there is no suppression or it " +
                     "never expires.")]
        public DateTimeOffset? DeleteExpiryDate { get; set; }
    }
}