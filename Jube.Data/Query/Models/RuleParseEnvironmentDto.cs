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

    public class RuleParseEnvironmentDto
    {
        public List<string> Tokens { get; set; } = [];
        public Dictionary<string, RuleParseEnvironmentRequestXPathDto> RequestXPaths { get; set; } = [];
        public Dictionary<string, int> InlineScriptProperties { get; set; } = [];
        public Dictionary<string, int> InlineFunctions { get; set; } = [];
        public List<string> Lists { get; set; } = [];
        public List<string> Dictionaries { get; set; } = [];
        public List<string> TtlCounters { get; set; }
        public List<string> AbstractionRules { get; set; }
        public List<string> Sanctions { get; set; }
        public List<string> AbstractionCalculations { get; set; }
        public List<string> HttpAdaptations { get; set; }
        public List<string> ExhaustiveAdaptations { get; set; }
        public List<string> ActivationRules { get; set; }
    }
}