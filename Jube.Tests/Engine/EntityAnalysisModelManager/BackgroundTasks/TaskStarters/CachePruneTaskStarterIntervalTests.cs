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
using FluentAssertions;
using Xunit;
using CachePruneTaskStarter = Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.CachePruneTaskStarter;
using EntityAnalysisModelDomain = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters
{
    [Trait("Category", "Unit")]
    public sealed class CachePruneTaskStarterIntervalTests
    {
        private static readonly DateTime referenceDate = new(2024, 6, 15, 12, 0, 10, DateTimeKind.Utc);

        private static EntityAnalysisModelDomain NewModel(char interval, int value)
        {
            var model = new EntityAnalysisModelDomain
            {
                Cache =
                {
                    CacheTtlInterval = interval,
                    CacheTtlIntervalValue = value
                }
            };
            return model;
        }

        [Theory]
        [InlineData('d')]
        [InlineData('h')]
        [InlineData('n')]
        [InlineData('s')]
        [InlineData('m')]
        [InlineData('y')]
        public void EachRecognisedIntervalSubtractsFromReferenceDate(char interval)
        {
            var model = NewModel(interval, 1);

            var threshold = CachePruneTaskStarter.GetThresholdReferenceDateForDeletion(model, referenceDate);

            threshold.Should().NotBeNull();
            threshold!.Value.Should().BeBefore(referenceDate);
        }

        [Fact]
        public void SecondsIntervalSupportsVeryShortRetentionWindows()
        {
            var model = NewModel('s', 5);

            CachePruneTaskStarter.GetThresholdReferenceDateForDeletion(model, referenceDate)
                .Should().Be(new DateTime(2024, 6, 15, 12, 0, 5, DateTimeKind.Utc));
        }

        [Fact]
        public void UnrecognisedIntervalCharFallsBackToDays()
        {
            var model = NewModel('x', 2);

            CachePruneTaskStarter.GetThresholdReferenceDateForDeletion(model, referenceDate)
                .Should().Be(referenceDate.AddDays(-2));
        }

        [Fact]
        public void ZeroIntervalValueReturnsReferenceDateUnchanged()
        {
            var model = NewModel('d', 0);

            CachePruneTaskStarter.GetThresholdReferenceDateForDeletion(model, referenceDate).Should().Be(referenceDate);
        }

        [Fact]
        public void NegativeIntervalValuePushesTheThresholdIntoTheFutureRiskingDeletingEverything()
        {
            var model = NewModel('d', -3);

            CachePruneTaskStarter.GetThresholdReferenceDateForDeletion(model, referenceDate)
                .Should().Be(referenceDate.AddDays(3));
        }

        [Fact]
        public void NullReferenceDateReturnsNullThresholdSoTheCallerSkipsPruning()
        {
            var model = NewModel('d', 30);

            CachePruneTaskStarter.GetThresholdReferenceDateForDeletion(model, null).Should().BeNull();
        }
    }
}