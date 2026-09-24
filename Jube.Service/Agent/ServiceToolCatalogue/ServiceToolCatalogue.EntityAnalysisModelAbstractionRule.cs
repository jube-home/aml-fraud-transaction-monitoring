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
        static partial void AddEntityAnalysisModelAbstractionRule(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleList", OperationKind.Read, true, false,
                    "Lists Abstraction Rules for the caller's tenant, keyset-paged and capped."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Abstraction Rules may use."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleFilter", OperationKind.Read, true, false,
                    "Returns the Abstraction Rules matching query builder JSON, keyset-paged and capped."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleCount", OperationKind.Read, true, false,
                    "Counts the Abstraction Rules matching query builder JSON, optionally grouped by a field."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleGet", OperationKind.Read, true, false,
                    "Returns one Abstraction Rule by id, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleGetByEntityAnalysisModelId", OperationKind.Read, true,
                    false,
                    "Lists Abstraction Rules belonging to a given Model, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleCreate", OperationKind.Write, false, false,
                    "Registers an Abstraction Rule under a Model in the caller's tenant; calling twice creates two."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleValidate", OperationKind.Read, true, false,
                    "Validates an Abstraction Rule without saving it and returns every failure."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleParseRule", OperationKind.Read, true, false,
                    "Parses and compiles an Abstraction Rule's rule text as the engine would and returns each located error."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleExecute", OperationKind.Read, true, false,
                    "Runs an Abstraction Rule against an invocation context as the engine would and returns the result."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleBuildRuleFromBuilderJson", OperationKind.Read, true, false,
                    "Turns query builder JSON into an Abstraction Rule's rule text as the browser builder would, and parses it."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleUpdate", OperationKind.Write, true, false,
                    "Updates an Abstraction Rule in the caller's tenant by id."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionRuleDelete", OperationKind.Delete, true, true,
                    "Soft-deletes an Abstraction Rule in the caller's tenant by id.")
            ]);
        }
    }
}