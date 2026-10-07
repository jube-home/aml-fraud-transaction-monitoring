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


using Jube.Data.Poco;
using Jube.Dto.EntityAnalysisModel;
using Jube.Dto.EntityAnalysisModelAbstractionCalculation;
using Jube.Dto.EntityAnalysisModelAbstractionRule;
using Jube.Dto.EntityAnalysisModelActivationRule;
using Jube.Dto.EntityAnalysisModelActivationRuleOverride;
using Jube.Dto.EntityAnalysisModelDictionary;
using Jube.Dto.EntityAnalysisModelDictionaryKvp;
using Jube.Dto.EntityAnalysisModelGatewayRule;
using Jube.Dto.EntityAnalysisModelHttpAdaptation;
using Jube.Dto.EntityAnalysisModelInlineFunction;
using Jube.Dto.EntityAnalysisModelInlineScript;
using Jube.Dto.EntityAnalysisModelList;
using Jube.Dto.EntityAnalysisModelListValue;
using Jube.Dto.EntityAnalysisModelOverride;
using Jube.Dto.EntityAnalysisModelRequestXPath;
using Jube.Dto.EntityAnalysisModelSanction;
using Jube.Dto.EntityAnalysisModelTag;
using Jube.Dto.EntityAnalysisModelTtlCounter;
using Jube.Dto.ExhaustiveSearchInstance;
using Jube.Dto.Repository.CaseWorkflow;
using Jube.Dto.Repository.CaseWorkflowAction;
using Jube.Dto.Repository.CaseWorkflowDisplay;
using Jube.Dto.Repository.CaseWorkflowFilter;
using Jube.Dto.Repository.CaseWorkflowForm;
using Jube.Dto.Repository.CaseWorkflowMacro;
using Jube.Dto.Repository.CaseWorkflowStatus;
using Jube.Dto.Repository.CaseWorkflowXPath;
using Jube.Dto.Repository.RoleRegistry;
using Jube.Dto.Repository.RoleRegistryPermission;
using Jube.Dto.Repository.TenantRegistry;
using Jube.Dto.Repository.UserRegistry;
using Jube.Dto.Repository.VisualisationRegistry;
using Jube.Dto.Repository.VisualisationRegistryDatasource;
using Jube.Dto.Repository.VisualisationRegistryParameter;
using Jube.Service.Query.CaseWorkflowActionVersionHistory;
using Jube.Service.Query.CaseWorkflowDisplayVersionHistory;
using Jube.Service.Query.CaseWorkflowFilterVersionHistory;
using Jube.Service.Query.CaseWorkflowFormVersionHistory;
using Jube.Service.Query.CaseWorkflowMacroVersionHistory;
using Jube.Service.Query.CaseWorkflowStatusVersionHistory;
using Jube.Service.Query.CaseWorkflowVersionHistory;
using Jube.Service.Query.CaseWorkflowXPathVersionHistory;
using Jube.Service.Query.EntityAnalysisModelAbstractionCalculationVersionHistory;
using Jube.Service.Query.EntityAnalysisModelAbstractionRuleVersionHistory;
using Jube.Service.Query.EntityAnalysisModelActivationRuleOverrideVersionHistory;
using Jube.Service.Query.EntityAnalysisModelActivationRuleVersionHistory;
using Jube.Service.Query.EntityAnalysisModelDictionaryKvpVersionHistory;
using Jube.Service.Query.EntityAnalysisModelDictionaryVersionHistory;
using Jube.Service.Query.EntityAnalysisModelGatewayRuleVersionHistory;
using Jube.Service.Query.EntityAnalysisModelHttpAdaptationVersionHistory;
using Jube.Service.Query.EntityAnalysisModelInlineFunctionVersionHistory;
using Jube.Service.Query.EntityAnalysisModelInlineScriptVersionHistory;
using Jube.Service.Query.EntityAnalysisModelListValueVersionHistory;
using Jube.Service.Query.EntityAnalysisModelListVersionHistory;
using Jube.Service.Query.EntityAnalysisModelOverrideVersionHistory;
using Jube.Service.Query.EntityAnalysisModelRequestXpathVersionHistory;
using Jube.Service.Query.EntityAnalysisModelSanctionVersionHistory;
using Jube.Service.Query.EntityAnalysisModelTagVersionHistory;
using Jube.Service.Query.EntityAnalysisModelTtlCounterVersionHistory;
using Jube.Service.Query.EntityAnalysisModelVersionHistory;
using Jube.Service.Query.ExhaustiveSearchInstanceVersionHistory;
using Jube.Service.Query.RoleRegistryPermissionVersionHistory;
using Jube.Service.Query.RoleRegistryVersionHistory;
using Jube.Service.Query.TenantRegistryVersionHistory;
using Jube.Service.Query.UserRegistryVersionHistory;
using Microsoft.AspNetCore.Routing;
using Jube.Service.Query.VisualisationRegistryDatasourceVersionHistory;
using Jube.Service.Query.VisualisationRegistryParameterVersionHistory;
using Jube.Service.Query.VisualisationRegistryVersionHistory;

namespace Jube.App.Endpoints.VersionHistory
{
    public static class VersionHistoryEndpoints
    {
        public static void MapVersionHistoryEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints
                .MapVersionHistoryGroup<CaseWorkflowActionVersionHistoryService, CaseWorkflowActionVersion,
                    CaseWorkflowActionDto>("CaseWorkflowActionVersionHistory");
            endpoints
                .MapVersionHistoryGroup<CaseWorkflowDisplayVersionHistoryService, CaseWorkflowDisplayVersion,
                    CaseWorkflowDisplayDto>("CaseWorkflowDisplayVersionHistory");
            endpoints
                .MapVersionHistoryGroup<CaseWorkflowFilterVersionHistoryService, CaseWorkflowFilterVersion,
                    CaseWorkflowFilterDto>("CaseWorkflowFilterVersionHistory");
            endpoints
                .MapVersionHistoryGroup<CaseWorkflowFormVersionHistoryService, CaseWorkflowFormVersion,
                    CaseWorkflowFormDto>("CaseWorkflowFormVersionHistory");
            endpoints
                .MapVersionHistoryGroup<CaseWorkflowMacroVersionHistoryService, CaseWorkflowMacroVersion,
                    CaseWorkflowMacroDto>("CaseWorkflowMacroVersionHistory");
            endpoints
                .MapVersionHistoryGroup<CaseWorkflowStatusVersionHistoryService, CaseWorkflowStatusVersion,
                    CaseWorkflowStatusDto>("CaseWorkflowStatusVersionHistory");
            endpoints.MapVersionHistoryGroup<CaseWorkflowVersionHistoryService, CaseWorkflowVersion, CaseWorkflowDto>(
                "CaseWorkflowVersionHistory");
            endpoints
                .MapVersionHistoryGroup<CaseWorkflowXPathVersionHistoryService, CaseWorkflowXPathVersion,
                    CaseWorkflowXPathDto>("CaseWorkflowXPathVersionHistory");
            endpoints
                .MapVersionHistoryGroup<EntityAnalysisModelAbstractionCalculationVersionHistoryService,
                    EntityAnalysisModelAbstractionCalculationVersion, EntityAnalysisModelAbstractionCalculationDto>(
                    "EntityAnalysisModelAbstractionCalculationVersionHistory");
            endpoints
                .MapVersionHistoryGroup<EntityAnalysisModelAbstractionRuleVersionHistoryService,
                    EntityAnalysisModelAbstractionRuleVersion, EntityAnalysisModelAbstractionRuleDto>(
                    "EntityAnalysisModelAbstractionRuleVersionHistory");
            endpoints
                .MapVersionHistoryGroup<EntityAnalysisModelActivationRuleOverrideVersionHistoryService,
                    EntityAnalysisModelActivationRuleOverrideVersion, EntityAnalysisModelActivationRuleOverrideDto>(
                    "EntityAnalysisModelActivationRuleOverrideVersionHistory");
            endpoints
                .MapVersionHistoryGroup<EntityAnalysisModelActivationRuleVersionHistoryService,
                    EntityAnalysisModelActivationRuleVersion, EntityAnalysisModelActivationRuleDto>(
                    "EntityAnalysisModelActivationRuleVersionHistory");
            endpoints
                .MapVersionHistoryGroup<EntityAnalysisModelDictionaryKvpVersionHistoryService,
                    EntityAnalysisModelDictionaryKvpVersion, EntityAnalysisModelDictionaryKvpDto>(
                    "EntityAnalysisModelDictionaryKvpVersionHistory");
            endpoints
                .MapVersionHistoryGroup<EntityAnalysisModelDictionaryVersionHistoryService,
                    EntityAnalysisModelDictionaryVersion, EntityAnalysisModelDictionaryDto>(
                    "EntityAnalysisModelDictionaryVersionHistory");
            endpoints
                .MapVersionHistoryGroup<EntityAnalysisModelGatewayRuleVersionHistoryService,
                    EntityAnalysisModelGatewayRuleVersion, EntityAnalysisModelGatewayRuleDto>(
                    "EntityAnalysisModelGatewayRuleVersionHistory");
            endpoints
                .MapVersionHistoryGroup<EntityAnalysisModelHttpAdaptationVersionHistoryService,
                    EntityAnalysisModelHttpAdaptationVersion, EntityAnalysisModelHttpAdaptationDto>(
                    "EntityAnalysisModelHttpAdaptationVersionHistory");
            endpoints
                .MapVersionHistoryGroup<EntityAnalysisModelInlineFunctionVersionHistoryService,
                    EntityAnalysisModelInlineFunctionVersion, EntityAnalysisModelInlineFunctionDto>(
                    "EntityAnalysisModelInlineFunctionVersionHistory");
            endpoints
                .MapVersionHistoryGroup<EntityAnalysisModelInlineScriptVersionHistoryService,
                    EntityAnalysisModelInlineScriptVersion, EntityAnalysisModelInlineScriptDto>(
                    "EntityAnalysisModelInlineScriptVersionHistory");
            endpoints
                .MapVersionHistoryGroup<EntityAnalysisModelListValueVersionHistoryService,
                    EntityAnalysisModelListValueVersion, EntityAnalysisModelListValueDto>(
                    "EntityAnalysisModelListValueVersionHistory");
            endpoints
                .MapVersionHistoryGroup<EntityAnalysisModelListVersionHistoryService, EntityAnalysisModelListVersion,
                    EntityAnalysisModelListDto>("EntityAnalysisModelListVersionHistory");
            endpoints
                .MapVersionHistoryGroup<EntityAnalysisModelOverrideVersionHistoryService,
                    EntityAnalysisModelOverrideVersion, EntityAnalysisModelOverrideDto>(
                    "EntityAnalysisModelOverrideVersionHistory");
            endpoints
                .MapVersionHistoryGroup<EntityAnalysisModelRequestXpathVersionHistoryService,
                    EntityAnalysisModelRequestXpathVersion, EntityAnalysisModelRequestXPathDto>(
                    "EntityAnalysisModelRequestXpathVersionHistory");
            endpoints
                .MapVersionHistoryGroup<EntityAnalysisModelSanctionVersionHistoryService,
                    EntityAnalysisModelSanctionVersion, EntityAnalysisModelSanctionDto>(
                    "EntityAnalysisModelSanctionVersionHistory");
            endpoints
                .MapVersionHistoryGroup<EntityAnalysisModelTagVersionHistoryService, EntityAnalysisModelTagVersion,
                    EntityAnalysisModelTagDto>("EntityAnalysisModelTagVersionHistory");
            endpoints
                .MapVersionHistoryGroup<EntityAnalysisModelTtlCounterVersionHistoryService,
                    EntityAnalysisModelTtlCounterVersion, EntityAnalysisModelTtlCounterDto>(
                    "EntityAnalysisModelTtlCounterVersionHistory");
            endpoints
                .MapVersionHistoryGroup<EntityAnalysisModelVersionHistoryService, EntityAnalysisModelVersion,
                    EntityAnalysisModelDto>("EntityAnalysisModelVersionHistory");
            endpoints
                .MapVersionHistoryGroup<ExhaustiveSearchInstanceVersionHistoryService, ExhaustiveSearchInstanceVersion,
                    ExhaustiveSearchInstanceDto>("ExhaustiveSearchInstanceVersionHistory");
            endpoints
                .MapVersionHistoryGroup<RoleRegistryPermissionVersionHistoryService, RoleRegistryPermissionVersion,
                    RoleRegistryPermissionDto>("RoleRegistryPermissionVersionHistory");
            endpoints.MapVersionHistoryGroup<RoleRegistryVersionHistoryService, RoleRegistryVersion, RoleRegistryDto>(
                "RoleRegistryVersionHistory");
            endpoints
                .MapVersionHistoryGroup<TenantRegistryVersionHistoryService, TenantRegistryVersion, TenantRegistryDto>(
                    "TenantRegistryVersionHistory");
            endpoints.MapVersionHistoryGroup<UserRegistryVersionHistoryService, UserRegistryVersion, UserRegistryDto>(
                "UserRegistryVersionHistory");
            endpoints
                .MapVersionHistoryGroup<VisualisationRegistryDatasourceVersionHistoryService,
                    VisualisationRegistryDatasourceVersion, VisualisationRegistryDatasourceDto>(
                    "VisualisationRegistryDatasourceVersionHistory");
            endpoints
                .MapVersionHistoryGroup<VisualisationRegistryParameterVersionHistoryService,
                    VisualisationRegistryParameterVersion, VisualisationRegistryParameterDto>(
                    "VisualisationRegistryParameterVersionHistory");
            endpoints
                .MapVersionHistoryGroup<VisualisationRegistryVersionHistoryService, VisualisationRegistryVersion,
                    VisualisationRegistryDto>("VisualisationRegistryVersionHistory");
        }
    }
}