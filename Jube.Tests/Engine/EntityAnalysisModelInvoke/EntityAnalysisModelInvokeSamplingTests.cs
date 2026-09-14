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
using System.Reflection;
using FluentAssertions;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke
{
    [Trait("Category", "Unit")]
    public sealed class EntityAnalysisModelInvokeSamplingTests
    {
        private static void InvokeDetermineSampled(Context context)
        {
            var method = typeof(Jube.Engine.EntityAnalysisModelInvoke.EntityAnalysisModelInvoke).GetMethod(
                "DetermineSampled", BindingFlags.NonPublic | BindingFlags.Static);
            method.Should()
                .NotBeNull("DetermineSampled must still exist with this exact name for these tests to mean anything");
            method!.Invoke(null, [context]);
        }

        private static Context NewContext(bool enableSampling, double samplePercentage, double randomDraw,
            bool enableLogsInfo = true)
        {
            var entityAnalysisModel = new EntityAnalysisModel
            {
                Flags =
                {
                    EnableLogsInfo = enableLogsInfo,
                    EnableSampling = enableSampling,
                    SamplePercentage = samplePercentage
                }
            };

            return new Context
            {
                EntityAnalysisModel = entityAnalysisModel,
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload(),
                Random = new FixedRandom(randomDraw)
            };
        }

        [Fact]
        public void WhenEnableSamplingIsOffEveryInvocationIsSampledRegardlessOfTheRandomDraw()
        {
            var context = NewContext(false, 1, 0.99);

            InvokeDetermineSampled(context);

            context.LogSampled.Should().BeTrue();
            context.EntityAnalysisModelInstanceEntryPayload.IsSampled.Should().BeTrue();
        }

        [Fact]
        public void WhenEnableSamplingIsOnAndTheDrawIsBelowThePercentageThresholdItIsSampled()
        {
            var context = NewContext(true, 50, 0.10);

            InvokeDetermineSampled(context);

            context.LogSampled.Should().BeTrue();
        }

        [Fact]
        public void WhenEnableSamplingIsOnAndTheDrawIsAboveThePercentageThresholdItIsNotSampled()
        {
            var context = NewContext(true, 50, 0.90);

            InvokeDetermineSampled(context);

            context.LogSampled.Should().BeFalse();
            context.EntityAnalysisModelInstanceEntryPayload.IsSampled.Should().BeFalse();
        }

        [Fact]
        public void WhenEnableSamplingIsOnAndSamplePercentageIsZeroNoDrawEverPasses()
        {
            var context = NewContext(true, 0, 0.0);

            InvokeDetermineSampled(context);

            context.LogSampled.Should().BeFalse();
        }

        [Fact]
        public void WhenEnableSamplingIsOnAndSamplePercentageIsOneHundredEveryDrawPasses()
        {
            var context = NewContext(true, 100, 0.999999);

            InvokeDetermineSampled(context);

            context.LogSampled.Should().BeTrue();
        }

        [Fact]
        public void IsSampledOnThePayloadAlwaysMirrorsContextLogSampledExactly()
        {
            var sampledContext = NewContext(true, 100, 0.5);
            var unsampledContext = NewContext(true, 1, 0.99);

            InvokeDetermineSampled(sampledContext);
            InvokeDetermineSampled(unsampledContext);

            sampledContext.EntityAnalysisModelInstanceEntryPayload.IsSampled.Should().Be(sampledContext.LogSampled);
            unsampledContext.EntityAnalysisModelInstanceEntryPayload.IsSampled.Should().Be(unsampledContext.LogSampled);
        }

        [Fact]
        public void WhenEnableLogsInfoIsOffEveryInvocationIsSampledEvenWithAStaleLowSamplePercentageConfigured()
        {
            var context = NewContext(true, 1, 0.99,
                false);

            InvokeDetermineSampled(context);

            context.LogSampled.Should().BeTrue();
            context.EntityAnalysisModelInstanceEntryPayload.IsSampled.Should().BeTrue();
        }

        [Fact]
        public void WhenEnableLogsInfoIsOnSamplingStillThrottlesAsBefore()
        {
            var context = NewContext(true, 1, 0.99);

            InvokeDetermineSampled(context);

            context.LogSampled.Should().BeFalse();
        }

        private sealed class FixedRandom(double fixedNextDouble) : Random
        {
            public override double NextDouble()
            {
                return fixedNextDouble;
            }
        }
    }
}