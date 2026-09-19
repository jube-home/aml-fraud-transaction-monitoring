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
namespace Jube.Dto.Query.Parser
{
    [Description("A request to parse and compile a user-authored rule against the fields, lists, dictionaries and " +
                 "other named entities of an Entity Analysis Model.")]
    public class ParseRuleRequestDto
    {
        [Description(
            "Kind of rule: 1 inline function, 2 gateway rule, 3 abstraction rule, 4 abstraction calculation, " +
            "5 activation rule.")]
        public int RuleParseType { get; set; }

        [Description("The rule source text to parse and compile.")]
        public string? RuleText { get; set; }

        [Description(
            "Id of the Entity Analysis Model the rule is authored against; must belong to the caller's tenant.")]
        public int EntityAnalysisModelId { get; set; }
    }
}