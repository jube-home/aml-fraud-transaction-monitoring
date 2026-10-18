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
using Jube.Service.Query.TreeChildren;

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddTreeChildren(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "TreeChildrenRequestXPathGet", OperationKind.Read, true, false,
                    "Lists the request XPath nodes of an Entity Analysis Model as tree children, ordered by Id.",
                    TreeChildrenNodes.RequestXPath.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenVisualisationRegistryDatasourceGet", OperationKind.Read, true,
                    false,
                    "Lists the datasource nodes of a Visualisation Registry as tree children, ordered by priority.",
                    TreeChildrenNodes.VisualisationRegistryDatasource.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenUserRegistryGet", OperationKind.Read, true, false,
                    "Lists the users of a Role Registry as tree children.",
                    TreeChildrenNodes.UserRegistry.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenRoleRegistryPermissionGet", OperationKind.Read, true, false,
                    "Lists the permission nodes granted to a Role Registry as tree children; the name is the permission specification name.",
                    TreeChildrenNodes.RoleRegistryPermission.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenVisualisationRegistryParameterGet", OperationKind.Read, true,
                    false,
                    "Lists the parameter nodes of a Visualisation Registry as tree children, ordered by Id.",
                    TreeChildrenNodes.VisualisationRegistryParameter.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenInlineFunctionGet", OperationKind.Read, true, false,
                    "Lists the inline function nodes of an Entity Analysis Model as tree children, ordered by Id.",
                    TreeChildrenNodes.InlineFunction.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenTagGet", OperationKind.Read, true, false,
                    "Lists the tag nodes of an Entity Analysis Model as tree children, ordered by Id.",
                    TreeChildrenNodes.Tag.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenGatewayRuleGet", OperationKind.Read, true, false,
                    "Lists the gateway rule nodes of an Entity Analysis Model as tree children, ordered by priority.",
                    TreeChildrenNodes.GatewayRule.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenExhaustiveGet", OperationKind.Read, true, false,
                    "Lists the exhaustive search instance nodes of an Entity Analysis Model as tree children, ordered by Id.",
                    TreeChildrenNodes.Exhaustive.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenReprocessingGet", OperationKind.Read, true, false,
                    "Lists the reprocessing rule nodes of an Entity Analysis Model as tree children.",
                    TreeChildrenNodes.Reprocessing.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenAdaptationGet", OperationKind.Read, true, false,
                    "Lists the HTTP adaptation nodes of an Entity Analysis Model as tree children, ordered by Id.",
                    TreeChildrenNodes.Adaptation.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenCaseWorkflowGet", OperationKind.Read, true, false,
                    "Lists the case workflow nodes of an Entity Analysis Model as tree children, ordered by Id.",
                    TreeChildrenNodes.CaseWorkflow.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenAbstractionCalculationGet", OperationKind.Read, true, false,
                    "Lists the abstraction calculation nodes of an Entity Analysis Model as tree children.",
                    TreeChildrenNodes.AbstractionCalculation.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenAbstractionRuleGet", OperationKind.Read, true, false,
                    "Lists the abstraction rule nodes of an Entity Analysis Model as tree children.",
                    TreeChildrenNodes.AbstractionRule.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenActivationRuleGet", OperationKind.Read, true, false,
                    "Lists the activation rule nodes of an Entity Analysis Model as tree children, ordered by priority.",
                    TreeChildrenNodes.ActivationRule.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenTTLCounterGet", OperationKind.Read, true, false,
                    "Lists the TTL counter nodes of an Entity Analysis Model as tree children, ordered by Id.",
                    TreeChildrenNodes.TtlCounter.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenInlineScriptGet", OperationKind.Read, true, false,
                    "Lists the inline script nodes of an Entity Analysis Model as tree children, ordered by Id.",
                    TreeChildrenNodes.InlineScript.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenSanctionsGet", OperationKind.Read, true, false,
                    "Lists the sanction nodes of an Entity Analysis Model as tree children, ordered by Id.",
                    TreeChildrenNodes.Sanctions.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenListGet", OperationKind.Read, true, false,
                    "Lists the list nodes of an Entity Analysis Model, addressed by model Guid, as tree children.",
                    TreeChildrenNodes.List.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenDictionaryGet", OperationKind.Read, true, false,
                    "Lists the dictionary nodes of an Entity Analysis Model, addressed by model Guid, as tree children.",
                    TreeChildrenNodes.Dictionary.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenCaseWorkflowXPathGet", OperationKind.Read, true, false,
                    "Lists the xpath nodes of a Case Workflow as tree children.",
                    TreeChildrenNodes.CaseWorkflowXPath.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenCaseWorkflowFormGet", OperationKind.Read, true, false,
                    "Lists the form nodes of a Case Workflow as tree children.",
                    TreeChildrenNodes.CaseWorkflowForm.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenCaseWorkflowActionGet", OperationKind.Read, true, false,
                    "Lists the action nodes of a Case Workflow as tree children.",
                    TreeChildrenNodes.CaseWorkflowAction.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenCaseWorkflowMacroGet", OperationKind.Read, true, false,
                    "Lists the macro nodes of a Case Workflow as tree children.",
                    TreeChildrenNodes.CaseWorkflowMacro.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenCaseWorkflowFilterGet", OperationKind.Read, true, false,
                    "Lists the filter nodes of a Case Workflow as tree children.",
                    TreeChildrenNodes.CaseWorkflowFilter.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenCaseWorkflowDisplayGet", OperationKind.Read, true, false,
                    "Lists the display nodes of a Case Workflow as tree children.",
                    TreeChildrenNodes.CaseWorkflowDisplay.Permissions),
                new ServiceToolDescriptor(
                    "TreeChildrenCaseWorkflowStatusGet", OperationKind.Read, true, false,
                    "Lists the status nodes of a Case Workflow as tree children.",
                    TreeChildrenNodes.CaseWorkflowStatus.Permissions)
            ]);
        }
    }
}