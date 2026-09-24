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
        static partial void AddEntityAnalysisInlineScript(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisInlineScriptList", OperationKind.Read, true, false,
                    "Lists registered Inline Scripts from the system-wide catalogue, keyset-paged and capped. " +
                    "Not tenant-scoped."),
                new ServiceToolDescriptor(
                    "EntityAnalysisInlineScriptFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Entity Analysis Inline Scripts may use."),
                new ServiceToolDescriptor(
                    "EntityAnalysisInlineScriptFilter", OperationKind.Read, true, false,
                    "Returns the Entity Analysis Inline Scripts matching query builder JSON, keyset-paged and capped."),
                new ServiceToolDescriptor(
                    "EntityAnalysisInlineScriptCount", OperationKind.Read, true, false,
                    "Counts the Entity Analysis Inline Scripts matching query builder JSON, optionally grouped by a field.")
            ]);
        }
    }
}