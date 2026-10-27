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

using Jube.Charting;
using Jube.Charting.Core.Models;
using Jube.Dto.Query.CaseSummary;

namespace Jube.Service.Charts
{
    public static class CaseWaterfallChartRenderer
    {
        private const double Edge = 1e-6;

        public static string Render(double baseRate, double likelihood, IReadOnlyList<CaseContributionDto> contributions,
            bool dark)
        {
            ArgumentNullException.ThrowIfNull(contributions);

            if (contributions.Count == 0)
            {
                throw new ArgumentException("There is no evidence to chart.");
            }

            var builder = Chart.Waterfall()
                .Title("Why this ranking")
                .Subtitle($"From the base rate of {baseRate:P0} to a likelihood of {likelihood:P0}, in log odds")
                .Size(520, 40 + 34 * (contributions.Count + 2))
                .Theme(dark ? ThemeMode.Dark : ThemeMode.Light)
                .ValueFormat(value => value.ToString("+0.00;-0.00;0.00"))
                .Step("Base rate", LogOdds(baseRate));

            foreach (var contribution in contributions)
            {
                builder = builder.Step(contribution.Label ?? contribution.Family ?? "Evidence", contribution.LogOdds);
            }

            return builder.Total("Likelihood").Render();
        }

        private static double LogOdds(double probability)
        {
            var clamped = Math.Clamp(probability, Edge, 1 - Edge);

            return Math.Log(clamped / (1 - clamped));
        }
    }
}
