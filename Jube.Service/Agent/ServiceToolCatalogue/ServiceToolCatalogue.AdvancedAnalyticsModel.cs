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

using Jube.Service.Agent.Models;
using Jube.Service.Repository.AdvancedAnalyticsOperation;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddAdvancedAnalyticsModel(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "AdvancedAnalyticsModelList", OperationKind.Read, true, false,
                    "Lists trained models for the caller's tenant with their live version, keyset-paged and capped.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AdvancedAnalyticsModelGet", OperationKind.Read, true, false,
                    "Returns one trained model with all its versions and their headline metrics.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AdvancedAnalyticsModelListVersions", OperationKind.Read, true, false,
                    "Lists the versions of a trained model with headline metrics and warning codes.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AdvancedAnalyticsModelGetVersion", OperationKind.Read, true, false,
                    "Returns one model version with its complete stored evidence.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AdvancedAnalyticsModelCompareVersions", OperationKind.Read, true, false,
                    "Compares two model versions on held-out metrics and says whether the comparison is fair.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AdvancedAnalyticsModelGetPromotionEvidence", OperationKind.Read, true, false,
                    "Returns the evidence needed to decide whether a model version deserves promotion.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AdvancedAnalyticsModelPromoteVersion", OperationKind.Write, true, false,
                    "Makes a model version live, recording its evidence; needs the Promote Model permission.",
                    AdvancedAnalyticsPermissionSpecifications.promoteModelOnly),
                new ServiceToolDescriptor(
                    "AdvancedAnalyticsModelRollback", OperationKind.Write, true, false,
                    "Puts a model back on an earlier version; needs the Promote Model permission.",
                    AdvancedAnalyticsPermissionSpecifications.promoteModelOnly)
            ]);
        }
    }
}