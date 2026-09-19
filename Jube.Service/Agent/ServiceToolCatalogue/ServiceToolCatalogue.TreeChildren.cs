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
        static partial void AddTreeChildren(List<ServiceToolDescriptor> tools) => tools.AddRange(
        [
            new ServiceToolDescriptor(
                "TreeChildrenRequestXPathGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the request XPath nodes of an Entity Analysis Model as tree children, ordered by Id."),
            new ServiceToolDescriptor(
                "TreeChildrenVisualisationRegistryDatasourceGet", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Lists the datasource nodes of a Visualisation Registry as tree children, ordered by priority."),
            new ServiceToolDescriptor(
                "TreeChildrenUserRegistryGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the users of a Role Registry as tree children."),
            new ServiceToolDescriptor(
                "TreeChildrenRoleRegistryPermissionGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the permission nodes granted to a Role Registry as tree children; the name is the permission specification name."),
            new ServiceToolDescriptor(
                "TreeChildrenVisualisationRegistryParameterGet", OperationKind.Read, Idempotent: true,
                Destructive: false,
                "Lists the parameter nodes of a Visualisation Registry as tree children, ordered by Id."),
            new ServiceToolDescriptor(
                "TreeChildrenInlineFunctionGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the inline function nodes of an Entity Analysis Model as tree children, ordered by Id."),
            new ServiceToolDescriptor(
                "TreeChildrenTagGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the tag nodes of an Entity Analysis Model as tree children, ordered by Id."),
            new ServiceToolDescriptor(
                "TreeChildrenGatewayRuleGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the gateway rule nodes of an Entity Analysis Model as tree children, ordered by priority."),
            new ServiceToolDescriptor(
                "TreeChildrenExhaustiveGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the exhaustive search instance nodes of an Entity Analysis Model as tree children, ordered by Id."),
            new ServiceToolDescriptor(
                "TreeChildrenReprocessingGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the reprocessing rule nodes of an Entity Analysis Model as tree children."),
            new ServiceToolDescriptor(
                "TreeChildrenAdaptationGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the HTTP adaptation nodes of an Entity Analysis Model as tree children, ordered by Id."),
            new ServiceToolDescriptor(
                "TreeChildrenCaseWorkflowGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the case workflow nodes of an Entity Analysis Model as tree children, ordered by Id."),
            new ServiceToolDescriptor(
                "TreeChildrenAbstractionCalculationGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the abstraction calculation nodes of an Entity Analysis Model as tree children."),
            new ServiceToolDescriptor(
                "TreeChildrenAbstractionRuleGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the abstraction rule nodes of an Entity Analysis Model as tree children."),
            new ServiceToolDescriptor(
                "TreeChildrenActivationRuleGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the activation rule nodes of an Entity Analysis Model as tree children, ordered by priority."),
            new ServiceToolDescriptor(
                "TreeChildrenTTLCounterGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the TTL counter nodes of an Entity Analysis Model as tree children, ordered by Id."),
            new ServiceToolDescriptor(
                "TreeChildrenInlineScriptGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the inline script nodes of an Entity Analysis Model as tree children, ordered by Id."),
            new ServiceToolDescriptor(
                "TreeChildrenSanctionsGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the sanction nodes of an Entity Analysis Model as tree children, ordered by Id."),
            new ServiceToolDescriptor(
                "TreeChildrenListGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the list nodes of an Entity Analysis Model, addressed by model Guid, as tree children."),
            new ServiceToolDescriptor(
                "TreeChildrenDictionaryGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the dictionary nodes of an Entity Analysis Model, addressed by model Guid, as tree children."),
            new ServiceToolDescriptor(
                "TreeChildrenCaseWorkflowXPathGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the xpath nodes of a Case Workflow as tree children."),
            new ServiceToolDescriptor(
                "TreeChildrenCaseWorkflowFormGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the form nodes of a Case Workflow as tree children."),
            new ServiceToolDescriptor(
                "TreeChildrenCaseWorkflowActionGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the action nodes of a Case Workflow as tree children."),
            new ServiceToolDescriptor(
                "TreeChildrenCaseWorkflowMacroGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the macro nodes of a Case Workflow as tree children."),
            new ServiceToolDescriptor(
                "TreeChildrenCaseWorkflowFilterGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the filter nodes of a Case Workflow as tree children."),
            new ServiceToolDescriptor(
                "TreeChildrenCaseWorkflowDisplayGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the display nodes of a Case Workflow as tree children."),
            new ServiceToolDescriptor(
                "TreeChildrenCaseWorkflowStatusGet", OperationKind.Read, Idempotent: true, Destructive: false,
                "Lists the status nodes of a Case Workflow as tree children.")
        ]);
    }
}