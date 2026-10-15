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

using Jube.Engine.AdvancedAnalytics.Clustering.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Agent.AdvancedAnalyticsTools.Models
{
    public sealed class ClustererTrainingRunSubmitRequest
    {
        public int InstanceId { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public string? CorrelationKey { get; set; }
        public string[] VariableNames { get; set; } = [];
        public int[] ProcessingTypeIds { get; set; } = [];
        public double[][] Inputs { get; set; } = [];
        public int Clusters { get; set; } = 3;
        public KMeansSeeding Seeding { get; set; } = KMeansSeeding.KMeansPlusPlus;
        public int MaximumIterations { get; set; } = 100;
        public double Tolerance { get; set; } = 1e-5;
        public bool Standardise { get; set; } = true;
        public double ValidationFraction { get; set; } = 0.2;
        public double TestFraction { get; set; } = 0.2;
        public int Seed { get; set; } = 1;
    }
}