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

namespace Jube.Dto.Query.EntityAnalysisModelDependency
{
    [Description("Dependency problems in a model.")]
    public class DependencyIssuesDto
    {
        [Description("Uses of a name that resolves to no entity.")]
        public List<ModelDependencyDto> Dangling { get; set; } = [];

        [Description("Active entities that use an inactive one.")]
        public List<ModelDependencyDto> OnInactiveTargets { get; set; } = [];

        [Description("Lists, dictionaries, TTL counters, abstraction rules, abstraction calculations, inline " +
                     "functions and adaptations nothing uses.")]
        public List<ModelEntityDto> Unreferenced { get; set; } = [];
    }
}