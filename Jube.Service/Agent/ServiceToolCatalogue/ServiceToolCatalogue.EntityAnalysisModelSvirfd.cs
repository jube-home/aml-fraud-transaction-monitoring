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
using Jube.Service.Query.Svirfd;
using Jube.Service.Repository.EntityAnalysisModelSvirfd;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddEntityAnalysisModelSvirfd(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSvirfdList", OperationKind.Read, true, false,
                    "Lists Svirfd entities visible to the caller's tenant, keyset-paged and capped.",
                    EntityAnalysisModelSvirfdService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSvirfdFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Svirfd entities may use.",
                    EntityAnalysisModelSvirfdService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSvirfdFilter", OperationKind.Read, true, false,
                    "Returns the Svirfd entities matching query builder JSON, keyset-paged and capped.",
                    EntityAnalysisModelSvirfdService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSvirfdCount", OperationKind.Read, true, false,
                    "Counts the Svirfd entities matching query builder JSON, optionally grouped by a field.",
                    EntityAnalysisModelSvirfdService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSvirfdListByEntityAnalysisModelId", OperationKind.Read, true, false,
                    "Lists the Svirfd entities of one model.",
                    EntityAnalysisModelSvirfdService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSvirfdGet", OperationKind.Read, true, false,
                    "Returns a single Svirfd entry by Id.",
                    EntityAnalysisModelSvirfdService.ReadPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSvirfdFields", OperationKind.Read, true, false,
                    "Lists the request fields of one model a Svirfd entry may name, with whether each has data type IP.",
                    EntityAnalysisModelSvirfdService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSvirfdCreate", OperationKind.Write, false, false,
                    "Creates a Svirfd entry (a model IP field looked up and read as Svirfd.{Name}.{Member} in rules); calling twice creates two.",
                    EntityAnalysisModelSvirfdService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSvirfdValidate", OperationKind.Read, true, false,
                    "Validates a Svirfd entry without saving it and returns every failure.",
                    EntityAnalysisModelSvirfdService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSvirfdUpdate", OperationKind.Write, true, false,
                    "Updates a Svirfd entry; overwrites, writes a version-audit row.",
                    EntityAnalysisModelSvirfdService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSvirfdDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Svirfd entry; reversible only by direct database action.",
                    EntityAnalysisModelSvirfdService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSvirfdResubmit", OperationKind.Write, false, false,
                    "Resubmits a rejected Svirfd entry for approval again, unchanged, opening a fresh approval cycle.",
                    EntityAnalysisModelSvirfdService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelSvirfdRetract", OperationKind.Write, false, false,
                    "Retracts a rejected Svirfd entry, restoring it to its last approved version without a new approval cycle.",
                    EntityAnalysisModelSvirfdService.WritePermissions),
                new ServiceToolDescriptor(
                    "SvirfdLookup", OperationKind.Read, true, false,
                    "Looks up one IP address for IP intelligence from svirfd.com: location with evidence and confidence, ASN and routing, VPN, Tor and relay lists, cloud and a narrative.",
                    SvirfdService.ReadPermissions),
                new ServiceToolDescriptor(
                    "SvirfdStatus", OperationKind.Read, true, false,
                    "Returns whether Svirfd IP intelligence is enabled and ready, the mode in use and the credit to show with its data.",
                    SvirfdService.ReadPermissions)
            ]);
        }
    }
}
