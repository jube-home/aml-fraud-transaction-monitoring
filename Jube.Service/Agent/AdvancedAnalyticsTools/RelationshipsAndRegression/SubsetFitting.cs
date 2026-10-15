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

using Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression
{
    internal static class SubsetFitting
    {
        public static LinearFit? TryFit(PreparedRegressionData data, IReadOnlyList<int> subset)
        {
            try
            {
                return LinearFit.Fit(data.X.Select(r => subset.Select(j => r[j]).ToArray()).ToArray(), data.Y, null,
                    subset.Select(j => data.Names[j]).ToArray(), true);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static double Score(LinearFit fit, ModelSelectionCriterion criterion)
        {
            return criterion == ModelSelectionCriterion.Bic ? fit.Bic : fit.Aic;
        }

        public static double SlopePValue(LinearFit fit, int termIndex)
        {
            var t = fit.Beta[termIndex] / fit.StandardError(termIndex);

            return RegressionStats.TwoSidedTp(t, fit.DfResidual);
        }
    }
}