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

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddSanctionEntrySource(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "SanctionEntrySourceList", OperationKind.Read, true, false,
                    "Lists the configured Sanction Entry Sources (e.g. SDN, BOE, EU) the Sanctions Loader can poll " +
                    "or manually import from, each identified by Id and Name. Landlord-only. Capped at 'take' " +
                    "rows (max 200); use 'afterId' to page."),
                new ServiceToolDescriptor(
                    "SanctionEntrySourceFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Sanction Entry Sources may use."),
                new ServiceToolDescriptor(
                    "SanctionEntrySourceFilter", OperationKind.Read, true, false,
                    "Returns the Sanction Entry Sources matching query builder JSON, keyset-paged and capped."),
                new ServiceToolDescriptor(
                    "SanctionEntrySourceCount", OperationKind.Read, true, false,
                    "Counts the Sanction Entry Sources matching query builder JSON, optionally grouped by a field.")
            ]);
        }
    }
}