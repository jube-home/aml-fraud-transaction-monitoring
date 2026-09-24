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

using System.Net;
using Jube.Engine.Integrity;

namespace Jube.Service.Query.EntityAnalysisModelIntegrity
{
    public sealed class InProcessEngineStateSource(global::Jube.Engine.Engine? engine) : IEngineStateSource
    {
        public string? Instance { get; } = engine == null ? null : Dns.GetHostName();

        public bool Available => engine?.Context?.Tasks?.EntityAnalysisModelManager?.Context?.EntityAnalysisModels
            is { EntityModelsHasLoadedForStartup: true };

        public EngineModelState? GetModelState(int entityAnalysisModelId)
        {
            if (!Available)
            {
                return null;
            }

            var models = engine!.Context.Tasks.EntityAnalysisModelManager.Context.EntityAnalysisModels
                .ActiveEntityAnalysisModels;
            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    return models.TryGetValue(entityAnalysisModelId, out var model)
                        ? EngineModelStateBuilder.FromModel(model, Instance!, DateTime.UtcNow)
                        : null;
                }
                catch (InvalidOperationException)
                {
                }
            }

            return null;
        }
    }
}