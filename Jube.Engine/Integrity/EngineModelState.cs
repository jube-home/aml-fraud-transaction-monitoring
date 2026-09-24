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

namespace Jube.Engine.Integrity
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EntityAnalysisModelManager.EntityAnalysisModel;

    public sealed record EngineLoadedEntity(string Kind, int Id, string Name);

    public sealed record EngineModelState(
        string Instance,
        int EntityAnalysisModelId,
        int TenantRegistryId,
        bool Started,
        DateTime CapturedDate,
        IReadOnlyList<EngineLoadedEntity> Loaded);

    public static class EngineModelStateBuilder
    {
        public static EngineModelState FromModel(EntityAnalysisModel model, string instance, DateTime capturedDate)
        {
            ArgumentNullException.ThrowIfNull(model);

            var collections = model.Collections;
            var loaded = new List<EngineLoadedEntity>();

            loaded.AddRange(collections.EntityAnalysisModelRequestXPaths.Select(x =>
                new EngineLoadedEntity("RequestXPath", x.Id, x.Name)));
            loaded.AddRange(collections.EntityAnalysisModelInlineFunctions.Select(x =>
                new EngineLoadedEntity("InlineFunction", x.Id, x.Name)));
            loaded.AddRange(collections.EntityAnalysisModelInlineScripts.Select(x =>
                new EngineLoadedEntity("InlineScript", x.Id, x.Name)));
            loaded.AddRange(collections.ModelGatewayRules.Select(x =>
                new EngineLoadedEntity("GatewayRule", x.EntityAnalysisModelGatewayRuleId, x.Name)));
            loaded.AddRange(collections.ModelAbstractionRules.Select(x =>
                new EngineLoadedEntity("AbstractionRule", x.Id, x.Name)));
            loaded.AddRange(collections.EntityAnalysisModelAbstractionCalculations.Select(x =>
                new EngineLoadedEntity("AbstractionCalculation", x.Id, x.Name)));
            loaded.AddRange(collections.ModelTtlCounters.Select(x =>
                new EngineLoadedEntity("TtlCounter", x.Id, x.Name)));
            loaded.AddRange(collections.EntityAnalysisModelSanctions.Select(x =>
                new EngineLoadedEntity("Sanction", x.EntityAnalysisModelSanctionsId, x.Name)));
            loaded.AddRange(collections.EntityAnalysisModelAdaptations.Select(x =>
                new EngineLoadedEntity("HttpAdaptation", x.Id, x.Name)));
            loaded.AddRange(collections.ModelActivationRules.Select(x =>
                new EngineLoadedEntity("ActivationRule", x.Id, x.Name)));

            return new EngineModelState(instance, model.Instance.Id, model.Instance.TenantRegistryId, model.Started,
                capturedDate, loaded);
        }
    }
}