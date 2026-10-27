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

namespace Jube.Resources
{
    public sealed class CaseWorkflowResources
    {
        public const string PermissionDenied = nameof(PermissionDenied);
        public const string NotAuthenticated = nameof(NotAuthenticated);
        public const string NotFound = nameof(NotFound);
        public const string EntityAnalysisModelIdInvalid = nameof(EntityAnalysisModelIdInvalid);
        public const string EntityAnalysisModelNotFound = nameof(EntityAnalysisModelNotFound);
        public const string NameRequired = nameof(NameRequired);
        public const string NameMaxLength = nameof(NameMaxLength);
        public const string NameAlreadyExists = nameof(NameAlreadyExists);
        public const string DescriptionMaxLength = nameof(DescriptionMaxLength);
        public const string VisualisationRegistryGuidRequired = nameof(VisualisationRegistryGuidRequired);
        public const string VisualisationRegistryNotFound = nameof(VisualisationRegistryNotFound);
        public const string RiskKappaOutOfRange = nameof(RiskKappaOutOfRange);
        public const string RiskRhoOutOfRange = nameof(RiskRhoOutOfRange);
        public const string RiskPriorStrengthOutOfRange = nameof(RiskPriorStrengthOutOfRange);
        public const string RiskBaseRatePriorOutOfRange = nameof(RiskBaseRatePriorOutOfRange);
        public const string DailyGoalOutOfRange = nameof(DailyGoalOutOfRange);
        public const string RiskDeadlineLaneDaysOutOfRange = nameof(RiskDeadlineLaneDaysOutOfRange);
        public const string RiskKeyHistoryHalfLifeOutOfRange = nameof(RiskKeyHistoryHalfLifeOutOfRange);
        public const string RiskImpactWeightsInvalid = nameof(RiskImpactWeightsInvalid);
        public const string RiskMoneyBandsInvalid = nameof(RiskMoneyBandsInvalid);
        public const string RiskQaSampleRateOutOfRange = nameof(RiskQaSampleRateOutOfRange);
    }
}