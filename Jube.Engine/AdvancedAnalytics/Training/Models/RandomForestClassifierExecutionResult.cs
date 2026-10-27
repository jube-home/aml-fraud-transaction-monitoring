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

using Jube.Data.Repository.Models;
using System;
using Jube.Data.Poco;
using Jube.Engine.AdvancedAnalytics.Classification.Models;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Engine.AdvancedAnalytics.Training.Models
{
    public sealed class RandomForestClassifierExecutionResult
    {
        public int RunId { get; init; }
        public Guid RunGuid { get; init; }
        public AdvancedAnalyticsStatus Status { get; init; }
        public string Error { get; init; }
        public AskJoobyAnalyticsTrainedModel Model { get; init; }
        public bool CreatedModel { get; init; }
        public AskJoobyAnalyticsTrainedModelVersion Version { get; init; }
        public RandomForestClassifierResult Result { get; init; }
    }
}