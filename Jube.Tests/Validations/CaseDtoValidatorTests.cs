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
using Jube.Dto.Repository.Case;
using Jube.Resources;
using Jube.Validations.Repository.Case;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Validations
{
    [Trait("Category", "Unit")]
    public sealed class CaseDtoValidatorTests
    {
        private static readonly CaseDtoValidator validator = new(
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance).Create(typeof(CaseResources)));

        private static CaseDto EngineCreatedCase()
        {
            return new CaseDto
            {
                Id = 7,
                CaseWorkflowStatusGuid = Guid.NewGuid(),
                ClosedStatusId = 0,
                Rating = 0,
                Payload = null
            };
        }

        [Fact]
        public void TakingAnEngineCreatedCaseThatIsUnratedAndHasNoPayloadIsValid()
        {
            var model = EngineCreatedCase();
            model.Locked = true;
            model.LockedUser = "analyst";

            validator.Validate(model).IsValid.Should().BeTrue();
        }

        [Fact]
        public void ReleasingAndClosingAnUnratedCaseWithNoPayloadIsValid()
        {
            var released = EngineCreatedCase();
            var closed = EngineCreatedCase();
            closed.ClosedStatusId = 3;

            validator.Validate(released).IsValid.Should().BeTrue();
            validator.Validate(closed).IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        public void RatingsFromZeroToFiveAreValid(byte rating)
        {
            var model = EngineCreatedCase();
            model.Rating = rating;

            validator.Validate(model).IsValid.Should().BeTrue();
        }

        [Fact]
        public void ARatingAboveFiveIsRefused()
        {
            var model = EngineCreatedCase();
            model.Rating = 6;

            validator.Validate(model).Errors.Should().ContainSingle(c => c.ErrorCode == "RatingInclusiveBetween");
        }

        [Fact]
        public void ALockWithoutAUserIsStillRefused()
        {
            var model = EngineCreatedCase();
            model.Locked = true;

            validator.Validate(model).Errors.Should().ContainSingle(c => c.ErrorCode == "LockedUserNotEmpty");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(8)]
        public void ClosingWithAKnownCloseReasonIsValid(byte reason)
        {
            var model = EngineCreatedCase();
            model.ClosedStatusId = 3;
            model.ClosedReasonId = reason;
            model.ClosedReasonNote = "Customer confirmed the transfer by phone.";

            validator.Validate(model).IsValid.Should().BeTrue();
        }

        [Fact]
        public void ACloseReasonOutsideTheEnumerationIsInvalid()
        {
            var model = EngineCreatedCase();
            model.ClosedStatusId = 3;
            model.ClosedReasonId = 9;

            validator.Validate(model).Errors.Should().Contain(e => e.ErrorCode == "ClosedReasonIdInclusiveBetween");
        }

        [Fact]
        public void ACloseReasonNoteOverOneThousandCharactersIsInvalid()
        {
            var model = EngineCreatedCase();
            model.ClosedStatusId = 3;
            model.ClosedReasonNote = new string('x', 1001);

            validator.Validate(model).Errors.Should().Contain(e => e.ErrorCode == "ClosedReasonNoteMaximumLength");
        }

        [Fact]
        public void AStaleCloseReasonCarriedOnAReopenIsNotRejectedBecauseTheServiceClearsIt()
        {
            var model = EngineCreatedCase();
            model.ClosedStatusId = 2;
            model.ClosedReasonId = 4;

            validator.Validate(model).IsValid.Should().BeTrue();
        }
    }
}
