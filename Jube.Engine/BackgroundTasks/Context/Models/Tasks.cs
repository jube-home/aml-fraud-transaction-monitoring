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

using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Engine.BackgroundTasks.Context.Models
{
    public class Tasks
    {
        public Task SanctionsTask { get; set; }
        public EntityAnalysisModelManager.EntityAnalysisModelManager EntityAnalysisModelManager { get; set; }
        public Task EntityAnalysisModelManagerTask { get; set; }
        public Task AmqpTask { get; set; }
        public Task NotificationsViaAmqp { get; set; }
        public Task NotificationsViaConcurrentQueueTask { get; set; }
        public Task CaseAutomationTask { get; set; }

        // ReSharper disable once CollectionNeverQueried.Global
        public List<Task> AsyncHttpContextCorrelationTasks { get; } = [];
        public Task ManageCountersTask { get; set; }
        public Task ApplicationLogEntryTask { get; set; }
        public Task InfrastructureHealthMetricsTask { get; set; }
        public Task InfrastructureHealthMetricsPurgeTask { get; set; }
        public Task TaggingTask { get; set; }
        public Task ExhaustiveTrainingTask { get; set; }

        // ReSharper disable once CollectionNeverQueried.Global
        public List<Task> CaseCreationTasks { get; } = [];
    }
}