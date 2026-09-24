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
using Jube.Dto.Validation;

namespace Jube.Dto.Query.EntityAnalysisModelDependency
{
    [Description("What would stop working if an entity were deleted, deactivated or renamed: every entity that " +
                 "uses it, directly or through another entity.")]
    public class DependencyImpactDto
    {
        [Description("The entity asked about.")]
        public ModelEntityDto? Entity { get; set; }

        [Description("Every use of the entity, direct uses first, then uses through them.")]
        public List<ModelDependencyDto> Dependents { get; set; } = [];

        [Description("How many entities use it directly.")]
        public int DirectDependents { get; set; }

        [Description("True when deleting the entity would be refused because other entities use it.")]
        public bool DeleteBlocked { get; set; }

        [Description("Why the entity could not be looked up.")]
        public List<ValidationErrorDto> Errors { get; set; } = [];
    }
}