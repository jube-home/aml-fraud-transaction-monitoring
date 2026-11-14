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

using System;
using System.ComponentModel;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.CaseAgentRun
{
    [Description("An analyst's decision on a triage recommendation: accept it, or override it with a reason.")]
    [MessagePackObject]
    public class CaseAgentRunDecisionDto
    {
        [Description("The recommendation's identifier. Required.")]
        [Key(0)]
        public Guid Guid { get; set; }

        [Description("True to accept the recommendation, false to override it.")]
        [Key(1)]
        public bool Accept { get; set; }

        [Description("Why the recommendation is overridden; required when overriding, at most 500 characters.")]
        [Key(2)]
        public string? Reason { get; set; }
    }
}
