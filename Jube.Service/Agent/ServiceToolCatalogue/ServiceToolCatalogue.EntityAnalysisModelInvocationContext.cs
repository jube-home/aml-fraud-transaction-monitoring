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
using Jube.Service.Query.EntityAnalysisModelInvocationContext;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddEntityAnalysisModelInvocationContext(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelInvocationContextBlank", OperationKind.Read, true,
                    false,
                    "Returns a blank rule-evaluation context for a model, with every name a rule can use and its defaults.",
                    EntityAnalysisModelInvocationContextService.AuthoringPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelInvocationContextFromRequestJson", OperationKind.Read, true,
                    false,
                    "Builds a rule-evaluation context from request JSON with the engine's own extraction; stores nothing.",
                    EntityAnalysisModelInvocationContextService.AuthoringPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelInvocationContextFromArchive", OperationKind.Read, true,
                    false,
                    "Builds a rule-evaluation context from an archived transaction, with every value as it was.",
                    EntityAnalysisModelInvocationContextService.ArchivePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelInvocationContextOverlay", OperationKind.Read, true,
                    false,
                    "Sets values in a rule-evaluation context by completion name, checking each against its data type.",
                    EntityAnalysisModelInvocationContextService.AuthoringPermissions)
            ]);
        }
    }
}