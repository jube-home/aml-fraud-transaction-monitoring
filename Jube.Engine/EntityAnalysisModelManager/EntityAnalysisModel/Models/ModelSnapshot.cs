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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models
{
    using System;
    using System.Collections.Generic;
    using Exhaustive.Models;
    using Models;
    using Models.EntityAnalysisModelInlineScript;
    using EntityAnalysisModelDictionary = Models.EntityAnalysisModelDictionary;
    using EntityAnalysisModelOverride = Models.EntityAnalysisModelOverride;

    public sealed class ModelSnapshot
    {
        public long Generation { get; init; }
        public int PayloadInitialSize { get; init; }
        public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
        public List<string> Users { get; init; } = [];
        public List<EntityAnalysisModelAbstractionRule> ModelAbstractionRules { get; init; } = [];
        public List<EntityAnalysisModelTtlCounter> ModelTtlCounters { get; init; } = [];
        public List<EntityAnalysisModelSanction> EntityAnalysisModelSanctions { get; init; } = [];
        public List<EntityAnalysisModelActivationRule> ModelActivationRules { get; init; } = [];
        public List<EntityModelGatewayRule> ModelGatewayRules { get; init; } = [];
        public List<EntityAnalysisModelHttpAdaptation> EntityAnalysisModelAdaptations { get; init; } = [];
        public List<ExhaustiveSearchInstance> ExhaustiveModels { get; init; } = [];
        public List<EntityAnalysisModelRequestXPath> EntityAnalysisModelRequestXPaths { get; init; } = [];

        public List<EntityAnalysisModelAbstractionCalculation> EntityAnalysisModelAbstractionCalculations
        {
            get;
            init;
        } = [];

        public List<EntityAnalysisModelInlineFunction> EntityAnalysisModelInlineFunctions { get; init; } = [];
        public List<EntityAnalysisModelInlineScript> EntityAnalysisModelInlineScripts { get; init; } = [];
        public List<EntityAnalysisModelTag> EntityAnalysisModelTags { get; init; } = [];
        public Dictionary<string, DistinctSearchKey> DistinctSearchKeys { get; init; } = new();
        public Dictionary<int, string> ParseIndexCache { get; init; } = new();
        public Dictionary<string, List<string>> EntityAnalysisModelLists { get; init; } = new();
        public Dictionary<int, EntityAnalysisModelDictionary> KvpDictionaries { get; init; } = new();

        public Dictionary<string, Dictionary<string, EntityAnalysisModelOverride>> EntityAnalysisModelOverrides
        {
            get;
            init;
        } = new();
    }
}