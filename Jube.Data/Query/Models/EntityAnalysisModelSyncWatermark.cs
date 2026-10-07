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
    using System;

    public sealed record EntityAnalysisModelSyncWatermark
    {
        public int ApprovalCount { get; init; }
        public int ApprovalMaxId { get; init; }
        public int OverrideCount { get; init; }
        public int OverrideMaxId { get; init; }
        public DateTime? OverrideMaxMutationDate { get; init; }
        public int ActivationRuleOverrideCount { get; init; }
        public int ActivationRuleOverrideMaxId { get; init; }
        public DateTime? ActivationRuleOverrideMaxMutationDate { get; init; }
        public int ExhaustivePromotedMaxId { get; init; }
        public DateTime? ExhaustivePromotedMaxCreatedDate { get; init; }
        public int ApiKeyCount { get; init; }
        public int ApiKeyMaxId { get; init; }
        public int ModelRoleCount { get; init; }
        public int ModelRoleMaxId { get; init; }
        public int ListValueCount { get; init; }
        public int ListValueMaxId { get; init; }
        public DateTime? ListValueMaxMutationDate { get; init; }
        public int DictionaryKvpCount { get; init; }
        public int DictionaryKvpMaxId { get; init; }
        public DateTime? DictionaryKvpMaxMutationDate { get; init; }
        public DateTime? NextExpiryDate { get; init; }
    }
}