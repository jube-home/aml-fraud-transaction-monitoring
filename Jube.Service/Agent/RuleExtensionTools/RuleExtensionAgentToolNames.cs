/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

namespace Jube.Service.Agent.RuleExtensionTools
{
    public static class RuleExtensionAgentToolNames
    {
        public static readonly IReadOnlyList<string> Tools =
        [
            "RuleExtensionMethodSearch",
            "RuleExtensionMethodGet",
            "RuleExtensionMethodValidate"
        ];

        public static readonly IReadOnlyList<string> Agents =
        [
            "entity-analysis-model-activation-rule-author",
            "entity-analysis-model-activation-rule-test-bench",
            "entity-analysis-model-activation-rule-override-author",
            "entity-analysis-model-activation-rule-override-test-bench",
            "entity-analysis-model-abstraction-rule-author",
            "entity-analysis-model-abstraction-rule-test-bench",
            "entity-analysis-model-abstraction-calculation-author",
            "entity-analysis-model-abstraction-calculation-test-bench",
            "entity-analysis-model-gateway-rule-author",
            "entity-analysis-model-gateway-rule-test-bench",
            "entity-analysis-model-inline-function-author",
            "entity-analysis-model-inline-function-test-bench",
            "entity-analysis-model-inline-script-author",
            "entity-analysis-model-inline-script-test-bench",
            "entity-analysis-model-reprocessing-rule-author",
            "entity-analysis-model-reprocessing-rule-test-bench"
        ];

        public static IReadOnlyDictionary<string, IReadOnlyList<string>> All { get; } =
            Agents.ToDictionary(agent => agent, _ => Tools, StringComparer.Ordinal);
    }
}