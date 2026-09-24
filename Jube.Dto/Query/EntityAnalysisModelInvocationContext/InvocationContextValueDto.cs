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

namespace Jube.Dto.Query.EntityAnalysisModelInvocationContext
{
    [Description("One name a rule can use, with its value in this context.")]
    public class InvocationContextValueDto
    {
        [Description("Completion name as used in rule text, e.g. Payload.Amount, TTLCounter.PerAccount.")]
        public string Name { get; set; } = string.Empty;

        [Description("Completion group, e.g. Payload, TTLCounter, Abstraction, Reference, Adaptation, Activation.")]
        public string Group { get; set; } = string.Empty;

        [Description("Data type: string, integer, double, datetime or boolean.")]
        public string DataType { get; set; } = string.Empty;

        [Description("The value as invariant text (dates in ISO 8601 UTC, booleans true/false); null when unset.")]
        public string? Value { get; set; }

        [Description("Where the value came from: Unset, Default, Extracted, Archive or Overlay.")]
        public string Origin { get; set; } = string.Empty;

        [Description("Why the value is as it is, when that is not obvious; otherwise null.")]
        public string? Note { get; set; }
    }
}