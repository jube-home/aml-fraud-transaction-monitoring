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
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseSuppressionCandidates
{
    [MessagePackObject]
    public class CaseSuppressionKeyDto
    {
        [Description("The override key name: a request XPath of the case workflow model.")]
        [Key(0)]
        public string Name { get; set; } = string.Empty;

        [Description("True when this key is the case key of the case.")]
        [Key(1)]
        public bool IsCaseKey { get; set; }

        [Description("True when the request XPath is flagged Enable Override, so a suppression can be made on it.")]
        [Key(2)]
        public bool Enabled { get; set; }

        [Description("When not enabled, why: CaseKeyNotOverrideEnabled. Empty when enabled.")]
        [Key(3)]
        public string? Reason { get; set; }
    }
}
