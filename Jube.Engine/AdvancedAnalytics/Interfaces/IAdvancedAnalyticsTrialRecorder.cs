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
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Poco;

namespace Jube.Engine.AdvancedAnalytics.Interfaces
{
    public interface IAdvancedAnalyticsTrialRecorder
    {
        Task<bool> IsStopRequestedAsync(CancellationToken token = default);

        Task<AdvancedAnalyticsTrainingRunTrial> InsertTrialAsync(string hyperparametersJson,
            CancellationToken token = default);

        Task CompleteTrialAsync(int trialId, AdvancedAnalyticsStatus status, double? score, string error,
            CancellationToken token = default);

        Task MarkSelectedAsync(int trialId, CancellationToken token = default);
    }
}