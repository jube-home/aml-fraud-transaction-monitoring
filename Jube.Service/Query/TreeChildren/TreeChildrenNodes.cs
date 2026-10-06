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

using Jube.Data.Repository;

namespace Jube.Service.Query.TreeChildren
{
    public static class TreeChildrenNodes
    {
        public const string ById = "id";
        public const string ByGuid = "guid";
        public const string ByKey = "key";

        public static readonly TreeChildrenNode RequestXPath =
            new("RequestXPath", ById, [7], EntityApprovalKind.EntityAnalysisModelRequestXPath);

        public static readonly TreeChildrenNode VisualisationRegistryDatasource =
            new("VisualisationRegistryDatasource", ById, [33]);

        public static readonly TreeChildrenNode UserRegistry = new("UserRegistry", ByGuid, [35]);
        public static readonly TreeChildrenNode RoleRegistryPermission = new("RoleRegistryPermission", ById, [36]);

        public static readonly TreeChildrenNode VisualisationRegistryParameter =
            new("VisualisationRegistryParameter", ById, [32]);

        public static readonly TreeChildrenNode InlineFunction = new("InlineFunction", ById, [8],
            EntityApprovalKind.EntityAnalysisModelInlineFunction);

        public static readonly TreeChildrenNode Tag = new("Tag", ById, [37], EntityApprovalKind.EntityAnalysisModelTag);

        public static readonly TreeChildrenNode GatewayRule = new("GatewayRule", ById, [10],
            EntityApprovalKind.EntityAnalysisModelGatewayRule);

        public static readonly TreeChildrenNode Exhaustive = new("Exhaustive", ById, [16],
            EntityApprovalKind.ExhaustiveSearchInstance);

        public static readonly TreeChildrenNode Reprocessing = new("Reprocessing", ById, [26]);

        public static readonly TreeChildrenNode Adaptation = new("Adaptation", ById, [15],
            EntityApprovalKind.EntityAnalysisModelHttpAdaptation);

        public static readonly TreeChildrenNode CaseWorkflow =
            new("CaseWorkflow", ById, [18, 19, 20, 21, 22, 23, 24, 25]);

        public static readonly TreeChildrenNode AbstractionCalculation = new("AbstractionCalculation", ById, [14],
            EntityApprovalKind.EntityAnalysisModelAbstractionCalculation);

        public static readonly TreeChildrenNode AbstractionRule = new("AbstractionRule", ById, [13, 14],
            EntityApprovalKind.EntityAnalysisModelAbstractionRule);

        public static readonly TreeChildrenNode ActivationRule = new("ActivationRule", ById, [17],
            EntityApprovalKind.EntityAnalysisModelActivationRule);

        public static readonly TreeChildrenNode TtlCounter = new("TTLCounter", ById, [12],
            EntityApprovalKind.EntityAnalysisModelTtlCounter);

        public static readonly TreeChildrenNode InlineScript =
            new("InlineScript", ById, [9], EntityApprovalKind.EntityAnalysisModelInlineScript);

        public static readonly TreeChildrenNode Sanctions = new("Sanctions", ById, [11],
            EntityApprovalKind.EntityAnalysisModelSanction);

        public static readonly TreeChildrenNode List = new("List", ByGuid, [3],
            EntityApprovalKind.EntityAnalysisModelList);

        public static readonly TreeChildrenNode Dictionary = new("Dictionary", ByGuid, [4],
            EntityApprovalKind.EntityAnalysisModelDictionary);

        public static readonly TreeChildrenNode CaseWorkflowXPath = new("CaseWorkflowXPath", ByKey, [20]);
        public static readonly TreeChildrenNode CaseWorkflowForm = new("CaseWorkflowForm", ByKey, [21]);
        public static readonly TreeChildrenNode CaseWorkflowAction = new("CaseWorkflowAction", ByKey, [22]);
        public static readonly TreeChildrenNode CaseWorkflowMacro = new("CaseWorkflowMacro", ByKey, [24]);
        public static readonly TreeChildrenNode CaseWorkflowFilter = new("CaseWorkflowFilter", ByKey, [25]);
        public static readonly TreeChildrenNode CaseWorkflowDisplay = new("CaseWorkflowDisplay", ByKey, [23]);
        public static readonly TreeChildrenNode CaseWorkflowStatus = new("CaseWorkflowStatus", ByKey, [19]);

        public static readonly IReadOnlyList<TreeChildrenNode> All =
        [
            RequestXPath, VisualisationRegistryDatasource, UserRegistry, RoleRegistryPermission,
            VisualisationRegistryParameter, InlineFunction, Tag, GatewayRule, Exhaustive, Reprocessing, Adaptation,
            CaseWorkflow, AbstractionCalculation, AbstractionRule, ActivationRule, TtlCounter, InlineScript,
            Sanctions, List, Dictionary, CaseWorkflowXPath, CaseWorkflowForm, CaseWorkflowAction,
            CaseWorkflowMacro, CaseWorkflowFilter, CaseWorkflowDisplay, CaseWorkflowStatus
        ];
    }
}