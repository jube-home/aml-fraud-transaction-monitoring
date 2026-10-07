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
using Jube.Data.Query.Models;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Utilities;
using Xunit;

namespace Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Utilities
{
    [Trait("Category", "Unit")]
    public class SyncWatermarkDecisionTests
    {
        private static readonly DateTime now = new(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

        private static EntityAnalysisModelSyncWatermark Watermark(int overrideCount = 2, int overrideMaxId = 20,
            DateTime? overrideMutation = null, int exhaustiveMaxId = 5, DateTime? nextExpiry = null,
            int approvalMaxId = 100, int approvalCount = 40)
        {
            return new EntityAnalysisModelSyncWatermark
            {
                ApprovalCount = approvalCount,
                ApprovalMaxId = approvalMaxId,
                OverrideCount = overrideCount,
                OverrideMaxId = overrideMaxId,
                OverrideMaxMutationDate = overrideMutation ?? now.AddHours(-1),
                ActivationRuleOverrideCount = 1,
                ActivationRuleOverrideMaxId = 7,
                ActivationRuleOverrideMaxMutationDate = now.AddHours(-2),
                ExhaustivePromotedMaxId = exhaustiveMaxId,
                ExhaustivePromotedMaxCreatedDate = now.AddDays(-1),
                NextExpiryDate = nextExpiry
            };
        }

        [Fact]
        public void TheFirstCycleForATenantAlwaysSynchronises()
        {
            SyncWatermarkDecision.RequiresSynchronisation(null, Watermark(), now).Should().BeTrue();
        }

        [Fact]
        public void AnUnreadableWatermarkFallsBackToSynchronising()
        {
            SyncWatermarkDecision.RequiresSynchronisation(Watermark(), null, now).Should().BeTrue();
        }

        [Fact]
        public void AnUnchangedWatermarkDoesNotSynchronise()
        {
            SyncWatermarkDecision.RequiresSynchronisation(Watermark(), Watermark(), now).Should().BeFalse();
        }

        [Fact]
        public void AnApprovalSynchronisesBecauseApprovalIsWhatMakesAChangeEffective()
        {
            var last = Watermark();
            var current = Watermark(approvalMaxId: 101, approvalCount: 41);

            SyncWatermarkDecision.RequiresSynchronisation(last, current, now).Should().BeTrue();
        }

        [Fact]
        public void ANewOverrideSynchronises()
        {
            var last = Watermark();
            var current = Watermark(overrideCount: 3, overrideMaxId: 21);

            SyncWatermarkDecision.RequiresSynchronisation(last, current, now).Should().BeTrue();
        }

        [Fact]
        public void AnOverrideDeletedWithoutChangingTheHighestIdStillSynchronises()
        {
            var last = Watermark();
            var current = Watermark(overrideCount: 1, overrideMutation: now.AddMinutes(-1));

            SyncWatermarkDecision.RequiresSynchronisation(last, current, now).Should().BeTrue();
        }

        [Fact]
        public void APromotedExhaustiveModelSynchronises()
        {
            var last = Watermark();
            var current = Watermark(exhaustiveMaxId: 6);

            SyncWatermarkDecision.RequiresSynchronisation(last, current, now).Should().BeTrue();
        }

        [Fact]
        public void AnExpiryStillInTheFutureDoesNotSynchronise()
        {
            var watermark = Watermark(nextExpiry: now.AddMinutes(5));

            SyncWatermarkDecision.RequiresSynchronisation(watermark, watermark, now).Should().BeFalse();
        }

        [Fact]
        public void AnExpiryThatHasPassedSynchronisesEvenThoughNothingWasWritten()
        {
            var last = Watermark(nextExpiry: now.AddMinutes(-1));

            SyncWatermarkDecision.RequiresSynchronisation(last, last, now).Should().BeTrue();
        }

        [Fact]
        public void AnExpiryFallingDueExactlyNowSynchronises()
        {
            var last = Watermark(nextExpiry: now);

            SyncWatermarkDecision.RequiresSynchronisation(last, last, now).Should().BeTrue();
        }

        [Fact]
        public void NoExpiryAtAllDoesNotSynchronise()
        {
            var watermark = Watermark();

            SyncWatermarkDecision.RequiresSynchronisation(watermark, watermark, now.AddYears(1)).Should().BeFalse();
        }

        [Fact]
        public void TheExpiryThatMattersIsTheOneTheLastSynchronisationSawNotTheFreshOne()
        {
            var last = Watermark(nextExpiry: now.AddMinutes(-1));
            var current = Watermark(nextExpiry: now.AddMinutes(-1));

            SyncWatermarkDecision.RequiresSynchronisation(last, current, now).Should().BeTrue();
        }
    }
}