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

// ReSharper disable CollectionNeverUpdated.Global

using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.ResponseTimePipelineCounters;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.StagePerformanceCounters;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.TaskPerformanceCounters;
using Jube.Engine.Helpers;

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel
{
    public class EntityAnalysisModel
    {
        public bool Started { get; set; }
        public JsonSerializationHelper JsonSerializationHelper { get; init; }
        public Instance Instance { get; } = new();
        public Services Services { get; } = new();
        public Flags Flags { get; } = new();
        public Collections Collections { get; } = new();
        public Dependencies Dependencies { get; } = new();
        public ConcurrentQueues ConcurrentQueues { get; } = new();
        public Counters Counters { get; } = new();
        public StagePerformanceCounters StagePerformanceCounters { get; } = new();
        public StagePerformanceCounters ArchiverStagePerformanceCounters { get; } = new();
        public ResponseTimePipelineCounters ResponseTimePipelineCounters { get; } = new();
        public TaskPerformanceCounters TaskPerformanceCounters { get; } = new();
        public Models.Cache Cache { get; } = new();
        public References References { get; } = new();
    }
}