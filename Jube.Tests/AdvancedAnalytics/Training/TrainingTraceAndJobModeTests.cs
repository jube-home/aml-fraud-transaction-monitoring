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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Engine.AdvancedAnalytics.Jobs;
using Jube.Engine.AdvancedAnalytics.Interfaces;
using Jube.Engine.AdvancedAnalytics.Training;
using Xunit;
using Jube.Engine.AdvancedAnalytics.Training.Models;

namespace Jube.Test.AdvancedAnalytics.Training
{
    public class TrainingTraceAndJobModeTests
    {
        [Fact]
        public async Task NullTraceAcceptsEntriesAndCompletesAsync()
        {
            IAdvancedAnalyticsTrainingTrace trace = NullAdvancedAnalyticsTrainingTrace.Instance;

            var task = trace.AppendAsync(new AdvancedAnalyticsTrainingTraceEntry(
                Guid.NewGuid(), 0, "epoch", new Dictionary<string, double> { ["loss"] = 0.5 }));

            await task;

            task.IsCompletedSuccessfully.Should().BeTrue();
        }

        [Fact]
        public void NullTraceIsASingleton()
        {
            NullAdvancedAnalyticsTrainingTrace.Instance.Should().BeSameAs(NullAdvancedAnalyticsTrainingTrace.Instance);
        }

        [Fact]
        public void JobModeHasOnlineAndBatch()
        {
            Enum.GetNames<JobMode>().Should().Equal("Online", "Batch");
        }
    }
}