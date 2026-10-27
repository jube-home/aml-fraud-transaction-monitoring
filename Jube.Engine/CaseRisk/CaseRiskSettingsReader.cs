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

namespace Jube.Engine.CaseRisk
{
    using System;
    using System.Globalization;
    using System.Linq;
    using Jube.CaseRisk;

    public static class CaseRiskSettingsReader
    {
        public static CaseRiskSettings Read(DynamicEnvironment.DynamicEnvironment environment)
        {
            double Number(string key) => double.Parse(environment.AppSettings(key), CultureInfo.InvariantCulture);

            double[] List(string key) => environment.AppSettings(key)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray();

            return new CaseRiskSettings
            {
                Kappa = Number("CaseRiskKappa"),
                Rho = Number("CaseRiskRho"),
                PriorStrength = Number("CaseRiskPriorStrength"),
                BaseRatePrior = Number("CaseRiskBaseRatePrior"),
                DeadlineLaneDays = (int)Number("CaseRiskDeadlineLaneDays"),
                KeyHistoryHalfLifeDays = Number("CaseRiskKeyHistoryHalfLifeDays"),
                ImpactWeights = List("CaseRiskImpactWeights"),
                MoneyBandThresholds = List("CaseRiskMoneyBands"),
                QaSampleRate = Number("CaseRiskQaSampleRate"),
                QaMaxPriority = (int)Number("CaseRiskQaMaxPriority")
            };
        }
    }
}
