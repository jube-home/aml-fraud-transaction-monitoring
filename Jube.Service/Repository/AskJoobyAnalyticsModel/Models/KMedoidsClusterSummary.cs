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
    public sealed class KMedoidsClusterSummary
    {
        [Description("Zero based cluster index.")]
        public int Index { get; set; }

        [Description("Number of training rows assigned to this cluster.")]
        public int Size { get; set; }

        [Description("Share of the training rows assigned to this cluster.")]
        public double Proportion { get; set; }

        [Description("The actual training row chosen as this cluster's medoid, in the original units of every " +
                     "variable. Unlike a k-means centroid this is never an average; it is one of the rows that was " +
                     "trained on.")]
        public double[] Medoid { get; set; } = [];

        [Description("Total distance of every row assigned to this cluster to its medoid.")]
        public double TotalDistanceToMedoid { get; set; }
    }
}