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
using Jube.Engine.AdvancedAnalytics.Interfaces;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Repository.AdvancedAnalyticsModel.Models
{
    public sealed class PrincipalComponentEvidenceDetails : IAdvancedAnalyticsEvidenceDetails
    {
        [Description("Number of components the training rows can support, bounded by the row and variable counts.")]
        public int AvailableComponents { get; set; }

        [Description("Number of components actually retained in the stored model, the smallest count whose " +
                     "cumulative explained variance meets the variance threshold (and the maximum components cap, " +
                     "when one was given).")]
        public int RetainedComponents { get; set; }

        [Description("The cumulative explained variance threshold the retained count was chosen to reach.")]
        public double VarianceThreshold { get; set; }

        [Description("Eigenvalue of every available component, largest first, in the units of the standardised (or " +
                     "centred) design.")]
        public double[] Eigenvalues { get; set; } = [];

        [Description("Share of the total variance each available component explains on its own, summing to 1 across " +
                     "all available components.")]
        public double[] ExplainedVarianceRatio { get; set; } = [];

        [Description("Running total of ExplainedVarianceRatio up to and including each available component.")]
        public double[] CumulativeVariance { get; set; } = [];

        [Description("Mean per-row reconstruction error on the training split: each row is projected onto the " +
                     "retained components and reconstructed, then compared with its own standardised values.")]
        public double TrainReconstructionError { get; set; }

        [Description("Mean per-row reconstruction error on the validation split, or null when it has no rows.")]
        public double? ValidationReconstructionError { get; set; }

        [Description("Mean per-row reconstruction error on the held-out test split, or null when it has no rows.")]
        public double? TestReconstructionError { get; set; }
    }
}