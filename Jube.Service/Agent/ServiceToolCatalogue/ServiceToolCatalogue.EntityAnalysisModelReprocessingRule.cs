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
using Jube.Service.EntityAnalysisModelReprocessingRule;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddEntityAnalysisModelReprocessingRule(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleList", OperationKind.Read, true, false,
                    "Lists Reprocessing Rules for the caller's tenant, keyset-paged and capped.",
                    EntityAnalysisModelReprocessingRuleService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Reprocessing Rules may use.",
                    EntityAnalysisModelReprocessingRuleService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleFilter", OperationKind.Read, true, false,
                    "Returns the Reprocessing Rules matching query builder JSON, keyset-paged and capped.",
                    EntityAnalysisModelReprocessingRuleService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleCount", OperationKind.Read, true, false,
                    "Counts the Reprocessing Rules matching query builder JSON, optionally grouped by a field.",
                    EntityAnalysisModelReprocessingRuleService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleGet", OperationKind.Read, true, false,
                    "Returns one Reprocessing Rule by id, scoped to the caller's tenant.",
                    EntityAnalysisModelReprocessingRuleService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleGetByEntityAnalysisModelId", OperationKind.Read, true,
                    false,
                    "Lists Reprocessing Rules belonging to a given Model, scoped to the caller's tenant.",
                    EntityAnalysisModelReprocessingRuleService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleCreate", OperationKind.Write, false, false,
                    "Registers a Reprocessing Rule under a Model in the caller's tenant; calling twice creates two.",
                    EntityAnalysisModelReprocessingRuleService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleValidate", OperationKind.Read, true, false,
                    "Validates a Reprocessing Rule without saving it and returns every failure.",
                    EntityAnalysisModelReprocessingRuleService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleParseRule", OperationKind.Read, true, false,
                    "Parses and compiles a Reprocessing Rule's rule text as the engine would and returns each located error.",
                    EntityAnalysisModelReprocessingRuleService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleExecute", OperationKind.Read, true, false,
                    "Runs a Reprocessing Rule against an invocation context as the engine would and returns the result.",
                    EntityAnalysisModelReprocessingRuleService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleBuildRuleFromBuilderJson", OperationKind.Read, true, false,
                    "Turns query builder JSON into a Reprocessing Rule's rule text as the browser builder would, and parses it.",
                    EntityAnalysisModelReprocessingRuleService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleUpdate", OperationKind.Write, false, false,
                    "Supersedes a Reprocessing Rule in the caller's tenant by id, assigning it a new id.",
                    EntityAnalysisModelReprocessingRuleService.RequiredPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelReprocessingRuleDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Reprocessing Rule in the caller's tenant by id.",
                    EntityAnalysisModelReprocessingRuleService.RequiredPermissions)
            ]);
        }
    }
}