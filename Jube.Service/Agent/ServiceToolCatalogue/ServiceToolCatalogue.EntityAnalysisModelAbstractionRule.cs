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
using Jube.Service.EntityAnalysisModelAbstractionRule;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddEntityAnalysisModelAbstractionRule(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleList", OperationKind.Read, true, false,
                    "Lists Abstraction Rules for the caller's tenant, keyset-paged and capped.",
                    EntityAnalysisModelAbstractionRuleService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Abstraction Rules may use.",
                    EntityAnalysisModelAbstractionRuleService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleFilter", OperationKind.Read, true, false,
                    "Returns the Abstraction Rules matching query builder JSON, keyset-paged and capped.",
                    EntityAnalysisModelAbstractionRuleService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleCount", OperationKind.Read, true, false,
                    "Counts the Abstraction Rules matching query builder JSON, optionally grouped by a field.",
                    EntityAnalysisModelAbstractionRuleService.ListPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleGet", OperationKind.Read, true, false,
                    "Returns one Abstraction Rule by id, scoped to the caller's tenant.",
                    EntityAnalysisModelAbstractionRuleService.ReadPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleGetByGuid", OperationKind.Read, true, false,
                    "Returns one Abstraction Rule by Guid, scoped to the caller's tenant.",
                    EntityAnalysisModelAbstractionRuleService.ReadPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleGetByEntityAnalysisModelId", OperationKind.Read, true,
                    false,
                    "Lists Abstraction Rules belonging to a given Model, scoped to the caller's tenant.",
                    EntityAnalysisModelAbstractionRuleService.ReadPermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleCreate", OperationKind.Write, false, false,
                    "Registers an Abstraction Rule under a Model in the caller's tenant; calling twice creates two.",
                    EntityAnalysisModelAbstractionRuleService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleValidate", OperationKind.Read, true, false,
                    "Validates an Abstraction Rule without saving it and returns every failure.",
                    EntityAnalysisModelAbstractionRuleService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleParseRule", OperationKind.Read, true, false,
                    "Parses and compiles an Abstraction Rule's rule text as the engine would and returns each located error.",
                    EntityAnalysisModelAbstractionRuleService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleExecute", OperationKind.Read, true, false,
                    "Runs an Abstraction Rule against an invocation context as the engine would and returns the result.",
                    EntityAnalysisModelAbstractionRuleService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleBuildRuleFromBuilderJson", OperationKind.Read, true, false,
                    "Turns query builder JSON into an Abstraction Rule's rule text as the browser builder would, and parses it.",
                    EntityAnalysisModelAbstractionRuleService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleUpdate", OperationKind.Write, true, false,
                    "Updates an Abstraction Rule in the caller's tenant by id.",
                    EntityAnalysisModelAbstractionRuleService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleDelete", OperationKind.Delete, true, true,
                    "Soft-deletes an Abstraction Rule in the caller's tenant by id.",
                    EntityAnalysisModelAbstractionRuleService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleResubmit", OperationKind.Write, false, false,
                    "Resubmits a rejected Abstraction Rule for approval again, unchanged, opening a fresh approval cycle.",
                    EntityAnalysisModelAbstractionRuleService.WritePermissions),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleRetract", OperationKind.Write, false, false,
                    "Retracts a rejected Abstraction Rule, restoring it to its last approved version without a new approval cycle.",
                    EntityAnalysisModelAbstractionRuleService.WritePermissions)
            ]);
        }
    }
}