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

namespace Jube.Dto.RuleExecution
{
    [Description("The outcome of turning query builder JSON into rule text: the rule text the browser's rule " +
                 "builder would produce, and whether it parses and compiles against the model.")]
    public class BuilderRuleResultDto
    {
        [Description("True when the JSON is valid builder JSON and the rule text it produces parses and compiles; " +
                     "the rule can then be saved with RuleScriptTypeId 1, BuilderRuleScript = RuleText and Json = " +
                     "the JSON sent.")]
        public bool Valid { get; set; }

        [Description("The rule text the builder JSON produces; null when the JSON itself is invalid.")]
        public string? RuleText { get; set; }

        [Description("Problems with the JSON (PropertyName is a JSON path such as $.rules[0].operator) or with the " +
                     "rule text it produces (PropertyName is BuilderRuleScript, with the line and position). Empty " +
                     "when Valid is true.")]
        public List<ValidationErrorDto> Errors { get; set; } = [];
    }
}