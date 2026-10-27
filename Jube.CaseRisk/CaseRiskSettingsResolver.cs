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

using System.Globalization;

namespace Jube.CaseRisk
{
    public static class CaseRiskSettingsResolver
    {
        public const int ImpactWeightCount = 5;
        public const int MoneyBandCount = 4;
        public const double MaximumQaSampleRate = 0.5;

        public static CaseRiskSettings Resolve(CaseRiskSettings defaults, CaseRiskOverrides? overrides)
        {
            ArgumentNullException.ThrowIfNull(defaults);

            if (overrides == null)
            {
                return defaults;
            }

            return defaults with
            {
                Kappa = overrides.Kappa ?? defaults.Kappa,
                Rho = overrides.Rho ?? defaults.Rho,
                PriorStrength = overrides.PriorStrength ?? defaults.PriorStrength,
                BaseRatePrior = overrides.BaseRatePrior ?? defaults.BaseRatePrior,
                DeadlineLaneDays = overrides.DeadlineLaneDays ?? defaults.DeadlineLaneDays,
                KeyHistoryHalfLifeDays = overrides.KeyHistoryHalfLifeDays ?? defaults.KeyHistoryHalfLifeDays,
                ImpactWeights = TryParseList(overrides.ImpactWeights, ImpactWeightCount) ?? defaults.ImpactWeights,
                MoneyBandThresholds = TryParseList(overrides.MoneyBands, MoneyBandCount) ?? defaults.MoneyBandThresholds,
                QaSampleRate = overrides.QaSampleRate ?? defaults.QaSampleRate
            };
        }

        public static IReadOnlyList<CaseRiskSettingsProblem> Check(CaseRiskOverrides? overrides)
        {
            var problems = new List<CaseRiskSettingsProblem>();

            if (overrides == null)
            {
                return problems;
            }

            if (overrides.Kappa is <= 0 or > 1 || IsNotFinite(overrides.Kappa))
            {
                problems.Add(CaseRiskSettingsProblem.KappaOutOfRange);
            }

            if (overrides.Rho is < 0 or > 1 || IsNotFinite(overrides.Rho))
            {
                problems.Add(CaseRiskSettingsProblem.RhoOutOfRange);
            }

            if (overrides.PriorStrength is <= 0 or > 10_000 || IsNotFinite(overrides.PriorStrength))
            {
                problems.Add(CaseRiskSettingsProblem.PriorStrengthOutOfRange);
            }

            if (overrides.BaseRatePrior is < 0.001 or > 0.999 || IsNotFinite(overrides.BaseRatePrior))
            {
                problems.Add(CaseRiskSettingsProblem.BaseRatePriorOutOfRange);
            }

            if (overrides.DeadlineLaneDays is < 0 or > 365)
            {
                problems.Add(CaseRiskSettingsProblem.DeadlineLaneDaysOutOfRange);
            }

            if (overrides.KeyHistoryHalfLifeDays is <= 0 or > 3650 || IsNotFinite(overrides.KeyHistoryHalfLifeDays))
            {
                problems.Add(CaseRiskSettingsProblem.KeyHistoryHalfLifeOutOfRange);
            }

            if (!string.IsNullOrWhiteSpace(overrides.ImpactWeights)
                && !IsIncreasingPositive(TryParseList(overrides.ImpactWeights, ImpactWeightCount)))
            {
                problems.Add(CaseRiskSettingsProblem.ImpactWeightsInvalid);
            }

            if (!string.IsNullOrWhiteSpace(overrides.MoneyBands)
                && !IsIncreasingPositive(TryParseList(overrides.MoneyBands, MoneyBandCount)))
            {
                problems.Add(CaseRiskSettingsProblem.MoneyBandsInvalid);
            }

            if (overrides.QaSampleRate is < 0 or > MaximumQaSampleRate || IsNotFinite(overrides.QaSampleRate))
            {
                problems.Add(CaseRiskSettingsProblem.QaSampleRateOutOfRange);
            }

            return problems;
        }

        public static double[]? TryParseList(string? text, int count)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length != count)
            {
                return null;
            }

            var values = new double[count];

            for (var i = 0; i < count; i++)
            {
                if (!double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out values[i])
                    || !double.IsFinite(values[i]))
                {
                    return null;
                }
            }

            return values;
        }

        private static bool IsNotFinite(double? value) => value.HasValue && !double.IsFinite(value.Value);

        private static bool IsIncreasingPositive(double[]? values)
        {
            if (values == null || values[0] <= 0)
            {
                return false;
            }

            for (var i = 1; i < values.Length; i++)
            {
                if (values[i] <= values[i - 1])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
