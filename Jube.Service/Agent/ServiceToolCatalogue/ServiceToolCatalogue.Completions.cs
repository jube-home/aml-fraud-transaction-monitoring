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
using Jube.Service.Query.Completions;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddCompletions(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "CompletionsGetByCaseWorkflowId", OperationKind.Read, true, false,
                    "Gets the payload field completions for the model behind a case workflow, by workflow Id.",
                    CompletionsService.ByCaseWorkflowIdPermissions),
                new ServiceToolDescriptor(
                    "CompletionsGetByCaseWorkflowGuidIncludingDeleted", OperationKind.Read, true,
                    false,
                    "Gets the payload field completions for the model behind a case workflow, by workflow Guid, including deleted workflows.",
                    CompletionsService.IncludingDeletedPermissions),
                new ServiceToolDescriptor(
                    "CompletionsGetByCaseWorkflowIdIncludingDeleted", OperationKind.Read, true,
                    false,
                    "Gets the payload field completions for the model behind a case workflow, by workflow Id, including deleted workflows.",
                    CompletionsService.IncludingDeletedPermissions),
                new ServiceToolDescriptor(
                    "CompletionsGetByEntityAnalysisModelId", OperationKind.Read, true, false,
                    "Gets the payload field completions for an entity analysis model, for reporting.",
                    CompletionsService.ByEntityAnalysisModelIdPermissions),
                new ServiceToolDescriptor(
                    "CompletionsGetByEntityAnalysisModelIdParseTypeId", OperationKind.Read, true,
                    false,
                    "Gets the payload field completions for an entity analysis model and parse type.",
                    CompletionsService.ByParseTypePermissions),
                new ServiceToolDescriptor(
                    "CompletionsGetByExhaustiveInstanceDataset", OperationKind.Read, true,
                    false,
                    "Gets filter builder completions for every column of a confirmed uploaded Exhaustive dataset.",
                    CompletionsService.ByExhaustiveInstanceDatasetPermissions)
            ]);
        }
    }
}