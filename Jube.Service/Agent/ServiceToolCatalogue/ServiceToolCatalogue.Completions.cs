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
        static partial void AddCompletions(List<ServiceToolDescriptor> tools) => tools.AddRange(
        [
            new ServiceToolDescriptor(
                "CompletionsGetByCaseWorkflowId", OperationKind.Read, Idempotent: true, Destructive: false,
                "Gets the payload field completions for the model behind a case workflow, by workflow Id."),
            new ServiceToolDescriptor(
                "CompletionsGetByCaseWorkflowGuidIncludingDeleted", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Gets the payload field completions for the model behind a case workflow, by workflow Guid, including deleted workflows."),
            new ServiceToolDescriptor(
                "CompletionsGetByCaseWorkflowIdIncludingDeleted", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Gets the payload field completions for the model behind a case workflow, by workflow Id, including deleted workflows."),
            new ServiceToolDescriptor(
                "CompletionsGetByEntityAnalysisModelId", OperationKind.Read, Idempotent: true, Destructive: false,
                "Gets the payload field completions for an entity analysis model, for reporting."),
            new ServiceToolDescriptor(
                "CompletionsGetByEntityAnalysisModelIdParseTypeId", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Gets the payload field completions for an entity analysis model and parse type.")
        ]);
    }
}