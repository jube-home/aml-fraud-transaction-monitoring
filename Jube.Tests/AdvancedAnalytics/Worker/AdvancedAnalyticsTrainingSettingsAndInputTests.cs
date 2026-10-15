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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Engine.AdvancedAnalytics.Interfaces;
using Jube.Engine.AdvancedAnalytics.Training;
using Jube.Engine.AdvancedAnalytics.Training.Exceptions;
using Xunit;
using Jube.Engine.AdvancedAnalytics.Training.Models;
using Jube.Analytics;

namespace Jube.Test.AdvancedAnalytics.Worker
{
    [Trait("Category", "Service")]
    public sealed class AdvancedAnalyticsTrainingSettingsAndInputTests
    {
        [Fact]
        public void SettingsDefaultToTheDocumentedValuesAndFallBackOnBadValues()
        {
            var defaults = AdvancedAnalyticsTrainingSettings.FromSettings(_ => null!);
            defaults.PollInterval.Should().Be(TimeSpan.FromSeconds(5));
            defaults.MaxConcurrentRuns.Should().Be(2);
            defaults.MaxConcurrentRunsPerTenant.Should().Be(1);
            defaults.MaxRunDuration.Should().Be(TimeSpan.FromMinutes(30));
            defaults.InlineMaxRows.Should().Be(20000);
            defaults.OnlineMaxRows.Should().Be(2000);
            defaults.StaleThreshold.Should().BeGreaterThan(defaults.HeartbeatInterval * 3);

            var custom = AdvancedAnalyticsTrainingSettings.FromSettings(k => k switch
            {
                AdvancedAnalyticsTrainingSettings.PollSecondsKey => "1",
                AdvancedAnalyticsTrainingSettings.MaxConcurrentRunsKey => "-3",
                AdvancedAnalyticsTrainingSettings.OnlineMaxRowsKey => "abc",
                AdvancedAnalyticsTrainingSettings.InlineMaxRowsKey => "5",
                _ => null!
            });

            custom.PollInterval.Should().Be(TimeSpan.FromSeconds(1));
            custom.MaxConcurrentRuns.Should().Be(2);
            custom.OnlineMaxRows.Should().Be(2000);
            custom.InlineMaxRows.Should().Be(5);
        }

        [Fact]
        public void TheResolverSupportsInlineAndCachePointerButNotDatasetUntilASourceIsRegistered()
        {
            var resolver = new AdvancedAnalyticsTrainingInputResolver();
            resolver.Supports(AdvancedAnalyticsTrainingInputKind.Inline).Should().BeTrue();
            resolver.Supports(AdvancedAnalyticsTrainingInputKind.CachePointer).Should().BeTrue();
            resolver.Supports(AdvancedAnalyticsTrainingInputKind.Dataset).Should().BeFalse();

            var extended = new AdvancedAnalyticsTrainingInputResolver([new FakeDatasetSource()]);
            extended.Supports(AdvancedAnalyticsTrainingInputKind.Dataset).Should().BeTrue();
        }

        [Fact]
        public async Task
            ADatasetSourceRegisteredWithTheResolverResolvesRowsAndAnUnregisteredOneThrowsNotSupportedAsync()
        {
            var context = new AdvancedAnalyticsTrainingInputContext
            {
                DbContext = null!,
                TenantRegistryId = 1,
                UserName = "u",
                Input = AdvancedAnalyticsTrainingInput.Dataset("d"),
                VariableCount = 1,
                Settings = new AdvancedAnalyticsTrainingSettings()
            };

            await FluentActions.Awaiting(() => new AdvancedAnalyticsTrainingInputResolver()
                    .ResolveAsync(context, CancellationToken.None))
                .Should().ThrowAsync<NotSupportedException>().WithMessage("*not supported*");

            var data = await new AdvancedAnalyticsTrainingInputResolver([new FakeDatasetSource()])
                .ResolveAsync(context, CancellationToken.None);
            data.Labels.Should().Equal(1, 0);
        }

        [Fact]
        public Task InlineInputWithoutRowsFailsClearlyAsync()
        {
            var context = new AdvancedAnalyticsTrainingInputContext
            {
                DbContext = null!,
                TenantRegistryId = 1,
                UserName = "u",
                Input = new AdvancedAnalyticsTrainingInput(),
                VariableCount = 1,
                Settings = new AdvancedAnalyticsTrainingSettings()
            };

            return FluentActions.Awaiting(() => new AdvancedAnalyticsTrainingInputResolver()
                    .ResolveAsync(context, CancellationToken.None))
                .Should().ThrowAsync<AdvancedAnalyticsTrainingInputException>();
        }

        [Fact]
        public void ArgumentsRoundTripThroughJsonWithMissingValuesAndStripInputData()
        {
            var arguments = new AdvancedAnalyticsTrainingArguments
            {
                ModelName = "m",
                VariableNames = ["a"],
                ProcessingTypeIds = [1],
                CostFalseNegative = 5,
                CostFalsePositive = 1,
                Input = AdvancedAnalyticsTrainingInput.Inline([[1.5], [double.NaN]], [0, 1])
            };

            var back = AdvancedAnalyticsTrainingArguments.FromJson(arguments.ToJson());

            back.Input!.Inputs![0][0].Should().Be(1.5);
            double.IsNaN(back.Input.Inputs[1][0]).Should().BeTrue();
            back.ToOptions().Costs.Should().NotBeNull();
            back.Input.WithoutData().Inputs.Should().BeNull();
            AdvancedAnalyticsTrainingArguments.InlineSizeBytes(back.Input).Should().BeGreaterThan(10);
        }

        private sealed class FakeDatasetSource : IAdvancedAnalyticsTrainingInputSource
        {
            public AdvancedAnalyticsTrainingInputKind Kind => AdvancedAnalyticsTrainingInputKind.Dataset;

            public Task<AdvancedAnalyticsResolvedTrainingData> ResolveAsync(
                AdvancedAnalyticsTrainingInputContext context, CancellationToken token)
            {
                return Task.FromResult(new AdvancedAnalyticsResolvedTrainingData
                {
                    Inputs = [[1.0], [2.0]],
                    Labels = [1, 0]
                });
            }
        }
    }
}