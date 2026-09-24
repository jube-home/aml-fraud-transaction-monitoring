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

namespace Jube.Dto.Query.EntityAnalysisModelDependency
{
    [Description("One use of an entity by another.")]
    public class ModelDependencyDto
    {
        [Description("The entity that uses the other.")]
        public ModelEntityDto Dependent { get; set; } = new();

        [Description("The entity that is used; null when the name does not resolve to any entity.")]
        public ModelEntityDto? Target { get; set; }

        [Description("How it is used: RuleText (named in rule text) or a setting such as SearchKey, " +
                     "TtlCounterDataName, CaseKey or CaseWorkflow.")]
        public string DependencyKind { get; set; } = string.Empty;

        [Description("The name as written, with its namespace, e.g. TTLCounter.PerAccount.")]
        public string Name { get; set; } = string.Empty;

        [Description(
            "The line of the rule text it appears on, for RuleText uses, counted from 0 as the parser counts lines.")]
        public int? Line { get; set; }

        [Description("1 for a direct use; 2 or more when it is used through another entity, which Via names.")]
        public int Depth { get; set; } = 1;

        [Description("For an indirect use, the entity it goes through, as Kind:Name.")]
        public string? Via { get; set; }
    }
}