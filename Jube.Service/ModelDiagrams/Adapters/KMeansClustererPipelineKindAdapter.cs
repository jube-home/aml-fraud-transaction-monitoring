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

using Jube.Charting.Interfaces;
using Jube.Service.ModelDiagrams.Interfaces;
using Newtonsoft.Json.Linq;

namespace Jube.Service.ModelDiagrams.Adapters
{
    public sealed class KMeansClustererPipelineKindAdapter : ISnapshotKindAdapter
    {
        public string Kind => "KMeansClustererPipeline";

        public ISvgChart Build(JObject envelope, JObject payload, ModelDiagramOptions options)
        {
            var clusters = ModelJson.Get(payload, "clusters") as JArray
                           ?? throw ModelJson.Inconsistent("the payload has no clusters");

            var storedNames = ModelJson.Strings(envelope, "variablesInOrder");

            return KMeansAdapter.BuildFromClusters(clusters, storedNames, options);
        }
    }
}