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

using Jube.Engine.AdvancedAnalytics.Interfaces;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Engine.AdvancedAnalytics.Clustering.Models
{
    public sealed class GaussianMixtureEvidenceDetails : IAdvancedAnalyticsEvidenceDetails
    {
        public int Components { get; set; }
        public bool DiagonalCovariance { get; set; }
        public bool Converged { get; set; }
        public int Iterations { get; set; }
        public double TrainLogLikelihood { get; set; }
        public double? ValidationLogLikelihood { get; set; }
        public double? TestLogLikelihood { get; set; }
        public double TrainMeanMaxPosterior { get; set; }
        public double? TestMeanMaxPosterior { get; set; }
        public GaussianMixtureClusterSummary[] ClusterSummaries { get; set; } = [];
    }
}