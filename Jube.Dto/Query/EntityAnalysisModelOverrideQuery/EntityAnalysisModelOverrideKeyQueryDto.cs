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

namespace Jube.Dto.Query.EntityAnalysisModelOverrideQuery
{
    [MessagePackObject]
    public class EntityAnalysisModelOverrideKeyQueryDto
    {
        [Description("Name of the request XPath that is enabled as an override key.")]
        [Key(0)]
        public string? OverrideKey { get; set; }

        [Description("Number of Models on which this Request XPath is enabled as an override key.")]
        [Key(1)]
        public int EnabledOnModels { get; set; }

        [Description("Number of distinct values currently overridden against this key.")]
        [Key(2)]
        public int Values { get; set; }

        [Description("Number of live overrides held against this key, counting each binding once.")]
        [Key(3)]
        public int Overrides { get; set; }

        [Description("Number of those overrides that force their Activation Rule rather than suppress it.")]
        [Key(4)]
        public int Forced { get; set; }

        [Description("Number of distinct Activation Rules bound to overrides on this key.")]
        [Key(5)]
        public int ActivationRules { get; set; }

        [Description("Number of overrides on this key bound to all Activation Rules rather than a named one.")]
        [Key(6)]
        public int AllActivationRules { get; set; }

        [Description("Earliest expiry date (UTC) among the overrides on this key, where any is set to expire.")]
        [Key(7)]
        public DateTimeOffset? NextExpiryDate { get; set; }

        [Description("Most recent creation date (UTC) among the overrides on this key.")]
        [Key(8)]
        public DateTimeOffset? LastCreatedDate { get; set; }
    }
}