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

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Dto.Query.EntityAnalysisModelDependency
{
    [Description("What an entity uses.")]
    public class DependencyListDto
    {
        [Description("The entity asked about.")]
        public ModelEntityDto? Entity { get; set; }

        [Description("Everything the entity uses; a null Target means the name does not resolve.")]
        public List<ModelDependencyDto> Dependencies { get; set; } = [];

        [Description("Why the entity could not be looked up.")]
        public List<ValidationErrorDto> Errors { get; set; } = [];
    }
}