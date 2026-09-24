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

namespace Jube.Data.Query.Models
{
    using System.Collections.Generic;

    public class ModelDependencyInputsDto
    {
        public List<ModelDependencyEntityDto> Entities { get; } = [];
        public List<ModelDependencyRuleTextDto> RuleTexts { get; } = [];
        public List<ModelDependencyReferenceDto> References { get; } = [];

        internal void Add(string kind, int id, string name, byte? active)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            Entities.Add(new ModelDependencyEntityDto { Kind = kind, Id = id, Name = name, Active = active == 1 });
        }

        internal void Text(string kind, int id, int ruleParseType, string text, bool? showOnlyCacheForPayload)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            RuleTexts.Add(new ModelDependencyRuleTextDto
            {
                Kind = kind, Id = id, RuleParseType = ruleParseType, Text = text,
                ShowOnlyCacheForPayload = showOnlyCacheForPayload
            });
        }

        internal void Reference(string dependentKind, int dependentId, string kind, string ns, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            References.Add(new ModelDependencyReferenceDto
            {
                DependentKind = dependentKind, DependentId = dependentId, Kind = kind, Namespace = ns,
                Name = name
            });
        }
    }
}