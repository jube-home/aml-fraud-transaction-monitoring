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

using Jube.Dto.Options;

namespace Jube.CaseRisk
{
    public static class ImpactCalculator
    {
        public static int MoneyBandOf(double value, CaseRiskSettings settings)
        {
            var band = 1;

            foreach (var threshold in settings.MoneyBandThresholds)
            {
                if (value >= threshold)
                {
                    band++;
                }
            }

            return Math.Min(5, band);
        }

        public static ImpactAssessment Compute(ImpactInputs inputs, CaseRiskSettings settings)
        {
            var typology = Math.Clamp(inputs.TypologySeverity, 1, 5);
            int? money = inputs.Value.HasValue && inputs.Value.Value > 0
                ? MoneyBandOf(inputs.Value.Value, settings)
                : null;
            var exposure = Math.Clamp(inputs.ExposureUplift, 0, 1);
            var breadth = Math.Clamp(inputs.BreadthUplift, 0, 1);
            var floor = Math.Clamp(inputs.MinimumImpact, 1, 5);

            var score = Math.Min(5, Math.Max(typology, money.GetValueOrDefault()) + exposure + breadth);

            return new ImpactAssessment((CaseImpactBand)Math.Max(score, floor), typology, money, inputs.Value,
                exposure, breadth, floor);
        }
    }
}
