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
    using System.Linq;
    using Exhaustive.Models;
    using Models;
    using Models.EntityAnalysisModelInlineScript;
    using EntityAnalysisModelDictionary = Models.EntityAnalysisModelDictionary;
    using EntityAnalysisModelOverride = Models.EntityAnalysisModelOverride;

    public sealed class ModelSnapshotBuilder
    {
        private const int MinimumPayloadInitialSize = 8;
        private const int PayloadFieldsOutsideTheFieldCollections = 2;

        private readonly ModelSnapshot previous;

        public ModelSnapshotBuilder(ModelSnapshot previous)
        {
            this.previous = previous ?? new ModelSnapshot();

            Users = this.previous.Users;
            ModelAbstractionRules = this.previous.ModelAbstractionRules;
            ModelTtlCounters = this.previous.ModelTtlCounters;
            EntityAnalysisModelSanctions = this.previous.EntityAnalysisModelSanctions;
            ModelActivationRules = this.previous.ModelActivationRules;
            ModelGatewayRules = this.previous.ModelGatewayRules;
            EntityAnalysisModelAdaptations = this.previous.EntityAnalysisModelAdaptations;
            ExhaustiveModels = this.previous.ExhaustiveModels;
            EntityAnalysisModelRequestXPaths = this.previous.EntityAnalysisModelRequestXPaths;
            EntityAnalysisModelAbstractionCalculations = this.previous.EntityAnalysisModelAbstractionCalculations;
            EntityAnalysisModelInlineFunctions = this.previous.EntityAnalysisModelInlineFunctions;
            EntityAnalysisModelInlineScripts = this.previous.EntityAnalysisModelInlineScripts;
            EntityAnalysisModelTags = this.previous.EntityAnalysisModelTags;
            DistinctSearchKeys = new Dictionary<string, DistinctSearchKey>(this.previous.DistinctSearchKeys);
            ParseIndexCache = this.previous.ParseIndexCache;
            EntityAnalysisModelLists = this.previous.EntityAnalysisModelLists;
            KvpDictionaries = this.previous.KvpDictionaries;
            EntityAnalysisModelOverrides = this.previous.EntityAnalysisModelOverrides;
        }

        public List<string> Users { get; set; }
        public List<EntityAnalysisModelAbstractionRule> ModelAbstractionRules { get; set; }
        public List<EntityAnalysisModelTtlCounter> ModelTtlCounters { get; set; }
        public List<EntityAnalysisModelSanction> EntityAnalysisModelSanctions { get; set; }
        public List<EntityAnalysisModelActivationRule> ModelActivationRules { get; set; }
        public List<EntityModelGatewayRule> ModelGatewayRules { get; set; }
        public List<EntityAnalysisModelHttpAdaptation> EntityAnalysisModelAdaptations { get; set; }
        public List<ExhaustiveSearchInstance> ExhaustiveModels { get; set; }
        public List<EntityAnalysisModelRequestXPath> EntityAnalysisModelRequestXPaths { get; set; }

        public List<EntityAnalysisModelAbstractionCalculation> EntityAnalysisModelAbstractionCalculations { get; set; }

        public List<EntityAnalysisModelInlineFunction> EntityAnalysisModelInlineFunctions { get; set; }
        public List<EntityAnalysisModelInlineScript> EntityAnalysisModelInlineScripts { get; set; }
        public List<EntityAnalysisModelTag> EntityAnalysisModelTags { get; set; }
        public Dictionary<string, DistinctSearchKey> DistinctSearchKeys { get; set; }
        public Dictionary<int, string> ParseIndexCache { get; set; }
        public Dictionary<string, List<string>> EntityAnalysisModelLists { get; set; }
        public Dictionary<int, EntityAnalysisModelDictionary> KvpDictionaries { get; set; }

        public Dictionary<string, Dictionary<string, EntityAnalysisModelOverride>>
            EntityAnalysisModelOverrides { get; set; }

        public void CarryForwardActivationRuleCounters()
        {
            if (ReferenceEquals(ModelActivationRules, previous.ModelActivationRules))
            {
                return;
            }

            var previousById = previous.ModelActivationRules.GroupBy(r => r.Id)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var current in ModelActivationRules)
            {
                if (!previousById.TryGetValue(current.Id, out var carried))
                {
                    continue;
                }

                current.Counters = carried.Counters;
            }
        }

        public void CarryForwardGatewayRuleCounters()
        {
            if (ReferenceEquals(ModelGatewayRules, previous.ModelGatewayRules))
            {
                return;
            }

            var previousById = previous.ModelGatewayRules.GroupBy(r => r.EntityAnalysisModelGatewayRuleId)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var current in ModelGatewayRules)
            {
                if (!previousById.TryGetValue(current.EntityAnalysisModelGatewayRuleId, out var carried))
                {
                    continue;
                }

                current.Counters = carried.Counters;
            }
        }

        public ModelSnapshot Build()
        {
            CarryForwardActivationRuleCounters();
            CarryForwardGatewayRuleCounters();

            return new ModelSnapshot
            {
                Generation = previous.Generation + 1,
                PayloadInitialSize = Math.Max(MinimumPayloadInitialSize,
                    EntityAnalysisModelRequestXPaths.Count
                    + EntityAnalysisModelAbstractionCalculations.Count
                    + EntityAnalysisModelInlineFunctions.Count
                    + PayloadFieldsOutsideTheFieldCollections),
                Users = Users,
                ModelAbstractionRules = ModelAbstractionRules,
                ModelTtlCounters = ModelTtlCounters,
                EntityAnalysisModelSanctions = EntityAnalysisModelSanctions,
                ModelActivationRules = ModelActivationRules,
                ModelGatewayRules = ModelGatewayRules,
                EntityAnalysisModelAdaptations = EntityAnalysisModelAdaptations,
                ExhaustiveModels = ExhaustiveModels,
                EntityAnalysisModelRequestXPaths = EntityAnalysisModelRequestXPaths,
                EntityAnalysisModelAbstractionCalculations = EntityAnalysisModelAbstractionCalculations,
                EntityAnalysisModelInlineFunctions = EntityAnalysisModelInlineFunctions,
                EntityAnalysisModelInlineScripts = EntityAnalysisModelInlineScripts,
                EntityAnalysisModelTags = EntityAnalysisModelTags,
                DistinctSearchKeys = DistinctSearchKeys,
                ParseIndexCache = ParseIndexCache,
                EntityAnalysisModelLists = EntityAnalysisModelLists,
                KvpDictionaries = KvpDictionaries,
                EntityAnalysisModelOverrides = EntityAnalysisModelOverrides
            };
        }
    }
}