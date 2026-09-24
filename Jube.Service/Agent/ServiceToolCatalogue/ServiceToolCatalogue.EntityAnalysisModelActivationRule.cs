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
        static partial void AddEntityAnalysisModelActivationRule(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleList", OperationKind.Read, true, false,
                    "Lists Activation Rules for the caller's tenant, keyset-paged and capped."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleFilterFields", OperationKind.Read, true, false,
                    "Lists the fields a query builder JSON filter over Activation Rules may use."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleFilter", OperationKind.Read, true, false,
                    "Returns the Activation Rules matching query builder JSON, keyset-paged and capped."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleCount", OperationKind.Read, true, false,
                    "Counts the Activation Rules matching query builder JSON, optionally grouped by a field."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleGet", OperationKind.Read, true, false,
                    "Returns one Activation Rule by id, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleGetByEntityAnalysisModelId", OperationKind.Read, true,
                    false,
                    "Lists Activation Rules belonging to a given Model, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleCreate", OperationKind.Write, false, false,
                    "Registers an Activation Rule under a Model in the caller's tenant; calling twice creates two."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleValidate", OperationKind.Read, true, false,
                    "Validates an Activation Rule without saving it and returns every failure."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleParseRule", OperationKind.Read, true, false,
                    "Parses and compiles an Activation Rule's rule text as the engine would and returns each located error."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleExecute", OperationKind.Read, true, false,
                    "Runs an Activation Rule against an invocation context as the engine would and returns the result."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleBuildRuleFromBuilderJson", OperationKind.Read, true, false,
                    "Turns query builder JSON into an Activation Rule's rule text as the browser builder would, and parses it."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleUpdate", OperationKind.Write, true, false,
                    "Updates an Activation Rule in the caller's tenant by id."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleDelete", OperationKind.Delete, true, true,
                    "Soft-deletes an Activation Rule in the caller's tenant by id."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelActivationRuleReset", OperationKind.Write, true, true,
                    "Resets the ActivationCounter and EvaluationCounter of an Activation Rule to zero.")
            ]);
        }
    }
}