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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Preservation.Models
{
    [MessagePackObject]
    public class AdvancedAnalyticsPayload
    {
        [Key(0)] public IEnumerable<AdvancedAnalyticsInstance>? Instances { get; set; }
        [Key(1)] public IEnumerable<AdvancedAnalyticsTrainedModel>? TrainedModels { get; set; }
        [Key(2)] public IEnumerable<AdvancedAnalyticsTrainedModelVersion>? TrainedModelVersions { get; set; }
        [Key(3)] public IEnumerable<AdvancedAnalyticsTrainingRun>? TrainingRuns { get; set; }
        [Key(4)] public IEnumerable<AdvancedAnalyticsTrainingRunStep>? TrainingRunSteps { get; set; }
        [Key(5)] public IEnumerable<AdvancedAnalyticsTrainingRunTrial>? TrainingRunTrials { get; set; }
        [Key(6)] public IEnumerable<AdvancedAnalyticsTrainingRunIteration>? TrainingRunIterations { get; set; }
        [Key(7)] public IEnumerable<AdvancedAnalyticsTrainingRunVariable>? TrainingRunVariables { get; set; }
        [Key(8)] public IEnumerable<AdvancedAnalyticsTrainingRunMetric>? TrainingRunMetrics { get; set; }
        [Key(9)] public IEnumerable<AdvancedAnalyticsTrainingRunCurvePoint>? TrainingRunCurvePoints { get; set; }
    }
}