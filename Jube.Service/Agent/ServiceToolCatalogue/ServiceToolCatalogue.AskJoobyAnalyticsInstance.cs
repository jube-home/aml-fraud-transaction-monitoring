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
        static partial void AddAskJoobyAnalyticsInstance(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "AskJoobyAnalyticsInstanceGet", OperationKind.Read, true, false,
                    "Returns one AskJooby Analytics instance by id, scoped to the caller's tenant.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AskJoobyAnalyticsInstanceGetByGuid", OperationKind.Read, true, false,
                    "Returns one AskJooby Analytics instance by Guid, scoped to the caller's tenant.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AskJoobyAnalyticsInstanceList", OperationKind.Read, true, false,
                    "Lists AskJooby Analytics instances for the caller's tenant, keyset-paged and capped.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AskJoobyAnalyticsInstanceFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over AskJooby Analytics instances may use.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AskJoobyAnalyticsInstanceFilter", OperationKind.Read, true, false,
                    "Returns the AskJooby Analytics instances matching query builder JSON, keyset-paged and capped.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AskJoobyAnalyticsInstanceCount", OperationKind.Read, true, false,
                    "Counts the AskJooby Analytics instances matching query builder JSON, optionally grouped by a field.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AskJoobyAnalyticsInstanceValidate", OperationKind.Read, true, false,
                    "Validates an AskJooby Analytics instance without saving it and returns every failure.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AskJoobyAnalyticsInstanceCreate", OperationKind.Write, false, false,
                    "Creates an unallocated AskJooby Analytics instance in the caller's tenant.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AskJoobyAnalyticsInstanceUpdate", OperationKind.Write, true, false,
                    "Renames an instance or changes its output flags; never changes its allocation.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AskJoobyAnalyticsInstanceDelete", OperationKind.Delete, true, true,
                    "Soft-deletes an AskJooby Analytics instance, taking it out of the live pipeline.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AskJoobyAnalyticsInstanceAllocateToModel", OperationKind.Write, true, false,
                    "Allocates an instance to a Jube Model of the caller's tenant; needs the Promote Model permission.",
                    AdvancedAnalyticsPermissionSpecifications.promoteModelOnly),
                new ServiceToolDescriptor(
                    "AskJoobyAnalyticsInstanceDeallocate", OperationKind.Write, true, false,
                    "Removes an instance's allocation to a Jube Model; needs the Promote Model permission.",
                    AdvancedAnalyticsPermissionSpecifications.promoteModelOnly),
                new ServiceToolDescriptor(
                    "AskJoobyAnalyticsInstanceResubmit", OperationKind.Write, false, false,
                    "Resubmits a rejected change to an instance for approval again; needs the Promote Model permission.",
                    AdvancedAnalyticsPermissionSpecifications.promoteModelOnly),
                new ServiceToolDescriptor(
                    "AskJoobyAnalyticsInstanceRetract", OperationKind.Write, false, false,
                    "Retracts a rejected change to an instance, restoring its last approved state; needs the Promote Model permission.",
                    AdvancedAnalyticsPermissionSpecifications.promoteModelOnly)
            ]);
        }
    }
}