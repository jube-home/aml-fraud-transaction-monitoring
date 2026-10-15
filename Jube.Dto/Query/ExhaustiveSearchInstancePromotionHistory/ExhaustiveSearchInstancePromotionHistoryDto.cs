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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.ExhaustiveSearchInstancePromotionHistory
{
    [Description("One promotion in the history of an Exhaustive Search Instance: the moment a trial topology was " +
                 "promoted as the best model so far, with its score, topology complexity and the number of trial " +
                 "instances that were explored since the previous promotion. The serialised network is not included.")]
    public class ExhaustiveSearchInstancePromotionHistoryDto
    {
        [Description("Server-assigned integer identifier of the promoted trial instance.")]
        public int Id { get; set; }

        [Description("Identifier of the Exhaustive Search Instance Trial Instance the promotion was made from.")]
        public int ExhaustiveSearchInstanceTrialInstanceId { get; set; }

        [Description("Score of the promoted trial instance. Zero if the stored value is not a finite number.")]
        public double Score { get; set; }

        [Description("Topology complexity of the promoted trial instance: the total number of neurons.")]
        public int TopologyComplexity { get; set; }

        [Description("UTC date and time of the promotion. The time axis of the promotion history.")]
        public DateTime CreatedDate { get; set; }

        [Description("True when this promotion is the currently active model of the search instance.")]
        public bool Active { get; set; }

        [Description("Number of trial instances explored since the previous promotion, including the promoted " +
                     "one. For the first promotion, every trial instance up to and including it.")]
        public int TrialInstances { get; set; }

        [Description("Cumulative number of trial instances explored up to and including the promoted one.")]
        public int TrialInstancesToDate { get; set; }
    }
}