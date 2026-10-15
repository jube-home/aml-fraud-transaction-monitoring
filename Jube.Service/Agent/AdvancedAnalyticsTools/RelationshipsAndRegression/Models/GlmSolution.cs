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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.Models
{
    internal sealed class GlmSolution
    {
        public double[] Beta { get; init; } = Array.Empty<double>();
        public double[][] Covariance { get; init; } = Array.Empty<double[]>();
        public double[] Mean { get; init; } = Array.Empty<double>();
        public double[] Eta { get; init; } = Array.Empty<double>();
        public double Deviance { get; init; }
        public double NullDeviance { get; init; }
        public double Pearson { get; init; }
        public double Dispersion { get; init; }
        public bool DispersionEstimated { get; init; }
        public int Iterations { get; init; }
        public bool Converged { get; init; }
        public int N { get; init; }
        public int K { get; init; }
        public double LogLikelihood { get; init; }
        public double[] DevianceResiduals { get; init; } = Array.Empty<double>();
        public string[] Terms { get; init; } = Array.Empty<string>();
        public List<string> Warnings { get; init; } = new();
    }
}