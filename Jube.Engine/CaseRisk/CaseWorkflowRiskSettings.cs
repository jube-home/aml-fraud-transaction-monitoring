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
    using Data.Poco;
    using Jube.CaseRisk;

    public static class CaseWorkflowRiskSettings
    {
        public static CaseRiskSettings For(CaseRiskSettings defaults, CaseWorkflow workflow)
        {
            return CaseRiskSettingsResolver.Resolve(defaults, new CaseRiskOverrides
            {
                Kappa = workflow.RiskKappa,
                Rho = workflow.RiskRho,
                PriorStrength = workflow.RiskPriorStrength,
                BaseRatePrior = workflow.RiskBaseRatePrior,
                DeadlineLaneDays = workflow.RiskDeadlineLaneDays,
                KeyHistoryHalfLifeDays = workflow.RiskKeyHistoryHalfLifeDays,
                ImpactWeights = workflow.RiskImpactWeights,
                MoneyBands = workflow.RiskMoneyBands,
                QaSampleRate = workflow.RiskQaSampleRate
            });
        }
    }
}
