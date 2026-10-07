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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Models
{
    using System.Collections.Generic;
    using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models;
    using EntityAnalysisModel = EntityAnalysisModel;

    public class Snapshots
    {
        private readonly Dictionary<int, ModelSnapshotBuilder> builders = new();
        private readonly Dictionary<int, EntityAnalysisModel> models = new();

        public ModelSnapshotBuilder Builder(int entityAnalysisModelId, EntityAnalysisModel entityAnalysisModel)
        {
            if (builders.TryGetValue(entityAnalysisModelId, out var existing))
            {
                return existing;
            }

            var builder = new ModelSnapshotBuilder(entityAnalysisModel.Snapshot);
            builders.Add(entityAnalysisModelId, builder);
            models.Add(entityAnalysisModelId, entityAnalysisModel);
            return builder;
        }

        public IReadOnlyCollection<int> PendingEntityAnalysisModelIds => builders.Keys;

        public void Publish()
        {
            foreach (var (entityAnalysisModelId, builder) in builders)
            {
                models[entityAnalysisModelId].PublishSnapshot(builder.Build());
            }

            builders.Clear();
            models.Clear();
        }
    }
}