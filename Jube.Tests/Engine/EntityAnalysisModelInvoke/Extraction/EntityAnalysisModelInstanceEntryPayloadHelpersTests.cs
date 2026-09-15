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
using Jube.Engine.EntityAnalysisModelInvoke.Extraction.Helpers;
using Xunit;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke.Extraction
{
    [Trait("Category", "Unit")]
    public sealed class EntityAnalysisModelInstanceEntryPayloadHelpersTests
    {
        private static EntityAnalysisModel NewModel()
        {
            var model = new EntityAnalysisModel
            {
                Instance =
                {
                    Name = "DetailedAccountFinancialTransaction",
                    Id = 1,
                    Guid = Guid.NewGuid(),
                    TenantRegistryId = 7
                },
                Flags =
                {
                    EnableRdbmsArchive = true
                }
            };
            return model;
        }

        [Fact]
        public void IdentifyingModelMetadataIsCopiedOntoThePayload()
        {
            var model = NewModel();

            var payload = EntityAnalysisModelInstanceEntryPayloadHelpers.Create(model);

            payload.EntityAnalysisModelName.Should().Be("DetailedAccountFinancialTransaction");
            payload.EntityAnalysisModelId.Should().Be(1);
            payload.EntityAnalysisModelGuid.Should().Be(model.Instance.Guid);
            payload.TenantRegistryId.Should().Be(7);
            payload.EnableRdbmsArchive.Should().BeTrue();
        }

        [Fact]
        public void WithNoGuidSuppliedANewGuidIsGenerated()
        {
            var model = NewModel();

            var payload = EntityAnalysisModelInstanceEntryPayloadHelpers.Create(model);

            payload.EntityAnalysisModelInstanceEntryGuid.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void ASuppliedGuidIsUsedInsteadOfGeneratingANewOne()
        {
            var model = NewModel();
            var guid = Guid.NewGuid();

            var payload = EntityAnalysisModelInstanceEntryPayloadHelpers.Create(model, guid);

            payload.EntityAnalysisModelInstanceEntryGuid.Should().Be(guid);
        }

        [Fact]
        public void EveryPooledCollectionIsInitialisedAndEmpty()
        {
            var model = NewModel();

            var payload = EntityAnalysisModelInstanceEntryPayloadHelpers.Create(model);

            payload.Abstraction.Should().NotBeNull().And.BeEmpty();
            payload.Activation.Should().NotBeNull().And.BeEmpty();
            payload.Dictionary.Should().NotBeNull().And.BeEmpty();
            payload.TtlCounter.Should().NotBeNull().And.BeEmpty();
            payload.Sanction.Should().NotBeNull().And.BeEmpty();
            payload.AbstractionCalculation.Should().NotBeNull().And.BeEmpty();
            payload.HttpAdaptation.Should().NotBeNull().And.BeEmpty();
            payload.ExhaustiveAdaptation.Should().NotBeNull().And.BeEmpty();
            payload.Tag.Should().NotBeNull().And.BeEmpty();
            payload.ArchiveKeys.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void InvokeTaskPerformanceAndCreatedDateAreAlwaysPopulated()
        {
            var before = DateTime.UtcNow;
            var model = NewModel();

            var payload = EntityAnalysisModelInstanceEntryPayloadHelpers.Create(model);

            payload.InvokeTaskPerformance.Should().NotBeNull();
            payload.CreatedDate.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
        }

        [Fact]
        public void TwoCallsForTheSameModelProduceIndependentPayloadInstances()
        {
            var model = NewModel();

            var first = EntityAnalysisModelInstanceEntryPayloadHelpers.Create(model);
            var second = EntityAnalysisModelInstanceEntryPayloadHelpers.Create(model);

            first.Should().NotBeSameAs(second);
            first.EntityAnalysisModelInstanceEntryGuid.Should().NotBe(second.EntityAnalysisModelInstanceEntryGuid);
        }
    }
}