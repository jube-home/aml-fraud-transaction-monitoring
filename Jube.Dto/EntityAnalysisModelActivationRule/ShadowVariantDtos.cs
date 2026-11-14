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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.EntityAnalysisModelActivationRule
{
    [Description("A request to save a chosen threshold variant of an Activation Rule as a new Shadow rule.")]
    [MessagePackObject]
    public class ShadowVariantRequestDto
    {
        [Description("The Activation Rule the variant is based on. Its settings (case workflow, response, priority) are copied.")]
        [Key(0)]
        public int BaseRuleId { get; set; }

        [Description("The rule text that was tuned, containing the Placeholder.")]
        [Key(1)]
        public string RuleText { get; set; } = string.Empty;

        [Description("The text in RuleText that stands for the threshold, for example {{threshold}}.")]
        [Key(2)]
        public string Placeholder { get; set; } = string.Empty;

        [Description("The chosen candidate, a plain number written with a full stop for the decimal point.")]
        [Key(3)]
        public string Value { get; set; } = string.Empty;
    }

    [Description("The Shadow rule that was created.")]
    [MessagePackObject]
    public class ShadowVariantResultDto
    {
        [Description("The new rule's identifier.")]
        [Key(0)]
        public int Id { get; set; }

        [Description("The new rule's Guid.")]
        [Key(1)]
        public System.Guid Guid { get; set; }

        [Description("The new rule's name.")]
        [Key(2)]
        public string Name { get; set; } = string.Empty;
    }
}
