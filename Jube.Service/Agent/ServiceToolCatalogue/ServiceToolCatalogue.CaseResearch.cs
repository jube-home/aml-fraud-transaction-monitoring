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
using Jube.Service.Repository.CaseResearch;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCaseResearch(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CaseResearchCreate", OperationKind.Write, false, false,
                    "Captures research on a case: topic, findings and up to 10 sources (title, http or https " +
                    "address, when retrieved). Attach only real addresses of sources actually retrieved; never " +
                    "invent one. Flagged as written by AskJooby and authored by the calling user.",
                    CaseResearchService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "CaseResearchListByCaseKeyValue", OperationKind.Read, true, false,
                    "Lists the research captured on a Case Key/Value pair, newest first, with its sources, scoped " +
                    "to the caller's tenant and Case Workflow roles.",
                    CaseResearchService.RequiredPermissions)
            ]);
        }
    }
}
