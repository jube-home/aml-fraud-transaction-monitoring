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
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Xunit;
using Jube.Analytics.Datasets;

namespace Jube.Test.AdvancedAnalytics.Repository
{
    public sealed class AdvancedAnalyticsCacheSettingsTests
    {
        [Fact]
        public void DefaultsApplyWhenNothingIsConfigured()
        {
            var limits = AdvancedAnalyticsCacheLimits.FromSettings(_ => null);

            limits.Ttl.Should().Be(TimeSpan.FromSeconds(3600));
            limits.MaxEntryBytes.Should().Be(64L * 1024 * 1024);
            limits.MaxTenantBytes.Should().Be(1024L * 1024 * 1024);
        }

        [Fact]
        public void ConfiguredValuesAreUsedAndInvalidOnesFallBack()
        {
            var limits = AdvancedAnalyticsCacheLimits.FromSettings(key => key switch
            {
                AdvancedAnalyticsCacheLimits.TtlSecondsKey => "90",
                AdvancedAnalyticsCacheLimits.MaxEntryBytesKey => "-5",
                AdvancedAnalyticsCacheLimits.MaxTenantBytesKey => "lots",
                _ => null
            });

            limits.Ttl.Should().Be(TimeSpan.FromSeconds(90));
            limits.MaxEntryBytes.Should().Be(AdvancedAnalyticsCacheLimits.DefaultMaxEntryBytes);
            limits.MaxTenantBytes.Should().Be(AdvancedAnalyticsCacheLimits.DefaultMaxTenantBytes);
        }

        [Fact]
        public void StageTimeBudgetDefaultsToOneSecondAndAcceptsZeroToDisable()
        {
            AdvancedAnalyticsSettings.ResolveStageTimeBudgetMilliseconds(_ => null).Should().Be(1000);
            AdvancedAnalyticsSettings.ResolveStageTimeBudgetMilliseconds(_ => "250").Should().Be(250);
            AdvancedAnalyticsSettings.ResolveStageTimeBudgetMilliseconds(_ => "0").Should().Be(0);
            AdvancedAnalyticsSettings.ResolveStageTimeBudgetMilliseconds(_ => "x").Should().Be(1000);
        }

        [Fact]
        public void SettingsKeysAreRegisteredInTheDynamicEnvironmentWithMatchingDefaults()
        {
            var environment = new Jube.DynamicEnvironment.DynamicEnvironment();

            AdvancedAnalyticsSettings.ResolveStageTimeBudgetMilliseconds(environment.AppSettings).Should().Be(1000);
            var limits = AdvancedAnalyticsCacheLimits.FromSettings(environment.AppSettings);
            limits.Ttl.Should().Be(TimeSpan.FromSeconds(AdvancedAnalyticsCacheLimits.DefaultTtlSeconds));

            environment.AppSettings(AdvancedAnalyticsCacheLimits.MaxEntryBytesKey)
                .Should().Be(AdvancedAnalyticsCacheLimits.DefaultMaxEntryBytes.ToString());
            environment.AppSettings(AdvancedAnalyticsCacheLimits.MaxTenantBytesKey)
                .Should().Be(AdvancedAnalyticsCacheLimits.DefaultMaxTenantBytes.ToString());

            environment.AppSettings(AdvancedAnalyticsSettings.StageTimeBudgetKey).Should().Be("1000");
        }
    }
}