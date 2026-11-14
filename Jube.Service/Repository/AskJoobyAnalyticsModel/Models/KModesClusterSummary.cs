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

namespace Jube.Service.Repository.AskJoobyAnalyticsModel.Models
{
    public sealed class KModesClusterSummary
    {
        [Description("Zero based cluster index.")]
        public int Index { get; set; }

        [Description("Number of training rows assigned to this cluster.")]
        public int Size { get; set; }

        [Description("Share of the training rows assigned to this cluster.")]
        public double Proportion { get; set; }

        [Description("Cluster mode: the most frequent category recorded, per variable, among the training rows " +
                     "assigned to this cluster, expressed in the same quantised category codes as the inputs.")]
        public double[] Mode { get; set; } = [];

        [Description("Total mismatch count of the training rows in this cluster against this cluster's mode: the " +
                     "sum, over every assigned row and every variable, of 1 whenever that row's category differs " +
                     "from the mode's category on that variable and 0 otherwise.")]
        public double WithinClusterMismatchTotal { get; set; }
    }
}