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
using System.Linq;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models
{
    [Trait("Category", "Unit")]
    public sealed class HttpAdaptationSettingsTests
    {
        private static HttpAdaptationSettings Resolve(TenantRegistry? tenant,
            params (string Key, string Value)[] overrides)
        {
            var environment = TestDynamicEnvironment.Create(
                overrides.ToDictionary(k => k.Key, k => k.Value));

            return HttpAdaptationSettings.Resolve(environment, tenant!);
        }

        [Fact]
        public void WithNoTenantUrlTheGlobalSettingsApplyAndAreTrusted()
        {
            var settings = Resolve(new TenantRegistry { Id = 1 });

            settings.IsTenantUrl.Should().BeFalse();
            settings.BaseUrl.Should().Be("http://localhost:5001");
            settings.Refusal.Should().BeNull();
            settings.AllowInsecureTargets.Should().BeTrue();
            settings.ValidateSsl.Should().BeTrue();
            settings.TimeoutMilliseconds.Should().Be(1000);
        }

        [Fact]
        public void GlobalValuesAreReadFromTheEnvironment()
        {
            var settings = Resolve(null, ("HttpAdaptationUrl", "https://scoring.internal:9000"),
                ("HttpAdaptationTimeout", "2500"), ("HttpAdaptationValidateSsl", "False"));

            settings.BaseUrl.Should().Be("https://scoring.internal:9000");
            settings.TimeoutMilliseconds.Should().Be(2500);
            settings.ValidateSsl.Should().BeFalse();
        }

        [Fact]
        public void UnparseableGlobalValuesFallBackToTheSecureDefaults()
        {
            var settings = Resolve(null, ("HttpAdaptationTimeout", "abc"), ("HttpAdaptationValidateSsl", "maybe"));

            settings.TimeoutMilliseconds.Should().Be(1000);
            settings.ValidateSsl.Should().BeTrue();
        }

        [Fact]
        public void ATenantUrlOverridesTheGlobalUrlAndIsHeldToTheOutboundPolicy()
        {
            var settings = Resolve(new TenantRegistry
            {
                Id = 7,
                HttpAdaptationUrl = " https://scoring.example.com ",
                HttpAdaptationTimeoutMilliseconds = 400,
                HttpAdaptationValidateSsl = 1
            });

            settings.IsTenantUrl.Should().BeTrue();
            settings.BaseUrl.Should().Be("https://scoring.example.com");
            settings.Refusal.Should().BeNull();
            settings.AllowInsecureTargets.Should().BeFalse();
            settings.TimeoutMilliseconds.Should().Be(400);
            settings.ValidateSsl.Should().BeTrue();
        }

        [Fact]
        public void ATenantWithNoTimeoutInheritsTheGlobalTimeoutButKeepsItsOwnSslSetting()
        {
            var settings = Resolve(new TenantRegistry
            {
                Id = 7, HttpAdaptationUrl = "https://scoring.example.com", HttpAdaptationValidateSsl = 0
            }, ("HttpAdaptationTimeout", "1800"), ("HttpAdaptationValidateSsl", "True"));

            settings.TimeoutMilliseconds.Should().Be(1800);
            settings.ValidateSsl.Should().BeFalse();
        }

        [Fact]
        public void ANullSslColumnMeansValidate()
        {
            var settings = Resolve(new TenantRegistry { Id = 7, HttpAdaptationUrl = "https://scoring.example.com" });

            settings.ValidateSsl.Should().BeTrue();
        }

        [Theory]
        [InlineData("http://scoring.example.com")]
        [InlineData("https://169.254.169.254")]
        [InlineData("https://localhost")]
        [InlineData("https://u:p@scoring.example.com")]
        public void AnUnsafeTenantUrlIsRefusedAtSyncEvenIfItWasStoredBypassingTheValidator(string url)
        {
            var settings = Resolve(new TenantRegistry { Id = 7, HttpAdaptationUrl = url });

            settings.Refusal.Should().NotBeNull();
        }

        [Fact]
        public void TheDevelopmentFlagRelaxesATenantUrlOnly()
        {
            var settings = Resolve(new TenantRegistry { Id = 7, HttpAdaptationUrl = "http://localhost:5001" },
                ("HttpAdaptationAllowInsecureTargets", "True"));

            settings.Refusal.Should().BeNull();
            settings.AllowInsecureTargets.Should().BeTrue();
        }

        [Fact]
        public void ResilienceDefaultsMatchTheDocumentedSettingsAndNeverRetry()
        {
            var settings = Resolve(null);

            settings.Resilience.FailureRatio.Should().Be(0.5);
            settings.Resilience.MinimumThroughput.Should().Be(5);
            settings.Resilience.SamplingDuration.TotalSeconds.Should().Be(30);
            settings.Resilience.BreakDuration.TotalSeconds.Should().Be(30);
            settings.Resilience.MaxRetries.Should().Be(0);
            settings.Resilience.Timeout.TotalMilliseconds.Should().Be(1000);
        }

        [Fact]
        public void ResilienceValuesOutsideWhatThePipelineAcceptsAreClamped()
        {
            var settings = Resolve(null, ("ResilientHttpFailureRatio", "5"), ("ResilientHttpMinimumThroughput", "0"),
                ("ResilientHttpSamplingDurationSeconds", "0"), ("ResilientHttpBreakDurationSeconds", "-3"),
                ("ResilientHttpMaxRetries", "-1"), ("ResilientHttpRetryDelayMilliseconds", "0"));

            settings.Resilience.FailureRatio.Should().Be(0.5);
            settings.Resilience.MinimumThroughput.Should().Be(2);
            settings.Resilience.SamplingDuration.TotalSeconds.Should().Be(1);
            settings.Resilience.BreakDuration.TotalSeconds.Should().Be(1);
            settings.Resilience.MaxRetries.Should().Be(0);
            settings.Resilience.RetryDelay.TotalMilliseconds.Should().Be(1);
        }
    }
}
