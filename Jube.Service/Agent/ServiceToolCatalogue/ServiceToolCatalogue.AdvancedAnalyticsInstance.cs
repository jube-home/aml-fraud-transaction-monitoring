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
        static partial void AddAdvancedAnalyticsInstance(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "AdvancedAnalyticsInstanceGet", OperationKind.Read, true, false,
                    "Returns one Advanced Analytics instance by id, scoped to the caller's tenant.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AdvancedAnalyticsInstanceGetByGuid", OperationKind.Read, true, false,
                    "Returns one Advanced Analytics instance by Guid, scoped to the caller's tenant.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AdvancedAnalyticsInstanceList", OperationKind.Read, true, false,
                    "Lists Advanced Analytics instances for the caller's tenant, keyset-paged and capped.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AdvancedAnalyticsInstanceFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Advanced Analytics instances may use.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AdvancedAnalyticsInstanceFilter", OperationKind.Read, true, false,
                    "Returns the Advanced Analytics instances matching query builder JSON, keyset-paged and capped.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AdvancedAnalyticsInstanceCount", OperationKind.Read, true, false,
                    "Counts the Advanced Analytics instances matching query builder JSON, optionally grouped by a field.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AdvancedAnalyticsInstanceValidate", OperationKind.Read, true, false,
                    "Validates an Advanced Analytics instance without saving it and returns every failure.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AdvancedAnalyticsInstanceCreate", OperationKind.Write, false, false,
                    "Creates an unallocated Advanced Analytics instance in the caller's tenant.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AdvancedAnalyticsInstanceUpdate", OperationKind.Write, true, false,
                    "Renames an instance or changes its output flags; never changes its allocation.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AdvancedAnalyticsInstanceDelete", OperationKind.Delete, true, true,
                    "Soft-deletes an Advanced Analytics instance, taking it out of the live pipeline.",
                    AdvancedAnalyticsPermissionSpecifications.analyticsOnly),
                new ServiceToolDescriptor(
                    "AdvancedAnalyticsInstanceAllocateToModel", OperationKind.Write, true, false,
                    "Allocates an instance to a Jube Model of the caller's tenant; needs the Promote Model permission.",
                    AdvancedAnalyticsPermissionSpecifications.promoteModelOnly),
                new ServiceToolDescriptor(
                    "AdvancedAnalyticsInstanceDeallocate", OperationKind.Write, true, false,
                    "Removes an instance's allocation to a Jube Model; needs the Promote Model permission.",
                    AdvancedAnalyticsPermissionSpecifications.promoteModelOnly),
                new ServiceToolDescriptor(
                    "AdvancedAnalyticsInstanceResubmit", OperationKind.Write, false, false,
                    "Resubmits a rejected change to an instance for approval again; needs the Promote Model permission.",
                    AdvancedAnalyticsPermissionSpecifications.promoteModelOnly),
                new ServiceToolDescriptor(
                    "AdvancedAnalyticsInstanceRetract", OperationKind.Write, false, false,
                    "Retracts a rejected change to an instance, restoring its last approved state; needs the Promote Model permission.",
                    AdvancedAnalyticsPermissionSpecifications.promoteModelOnly)
            ]);
        }
    }
}