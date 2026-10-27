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
    public sealed class GaussianMixtureClusterSummary
    {
        [Description("Zero based component index.")]
        public int Index { get; set; }

        [Description("Number of training rows whose highest posterior probability falls on this component.")]
        public int Size { get; set; }

        [Description("Fitted mixture weight of this component: the model's own prior share of rows it expects to " +
                     "explain, between 0 and 1 and summing to 1 across every component.")]
        public double MixtureWeight { get; set; }

        [Description("Mean of this component in the original units of every variable, averaged over the training " +
                     "rows whose highest posterior probability falls on this component.")]
        public double[] Mean { get; set; } = [];

        [Description("Per-variable sample variance, in the original units, of the training rows whose highest " +
                     "posterior probability falls on this component.")]
        public double[] Variance { get; set; } = [];
    }
}