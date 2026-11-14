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
using System;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseSuppressionCandidates
{
    [MessagePackObject]
    public class CaseSuppressionCandidateTransactionDto
    {
        [Description("The archive id, sent back as evidence.")]
        [Key(0)]
        public long ArchiveId { get; set; }

        [Description("The reference date of the transaction (UTC).")]
        [Key(1)]
        public DateTimeOffset? ReferenceDate { get; set; }

        [Description("The response elevation the model gave it.")]
        [Key(2)]
        public double ResponseElevation { get; set; }

        [Description("The number of rules visible to the caller that fired.")]
        [Key(3)]
        public int ActivationCount { get; set; }

        [Description("The names of the visible rules that fired.")]
        [Key(4)]
        public string Activations { get; set; } = string.Empty;

        [Description("The merged journal key it came from, or null for the case key itself.")]
        [Key(5)]
        public string? LaneKey { get; set; }
    }
}
