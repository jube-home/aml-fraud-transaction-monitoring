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
using FluentAssertions;
using Jube.Dictionary;
using Jube.Dictionary.Models;
using Jube.Engine.EntityAnalysisModelInvoke.Extraction;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Test.Infrastructure;
using Xunit;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke.Extraction
{
    [Trait("Category", "Unit")]
    public sealed class EntityAnalysisModelDictionaryNoBoxingExtractorTests
    {
        private static EntityAnalysisModel NewModel(params EntityAnalysisModelRequestXPath[] xPaths)
        {
            var model = new EntityAnalysisModel
            {
                References =
                {
                    EntryName = "TxnId",
                    ReferenceDateName = "TxnDateTime"
                }
            };
            model.Collections.EntityAnalysisModelRequestXPaths.AddRange(xPaths);
            return model;
        }

        private static EntityAnalysisModelRequestXPath XPath(string name, int dataTypeId, string? defaultValue = null)
        {
            return new EntityAnalysisModelRequestXPath
                { Name = name, DataTypeId = dataTypeId, DefaultValue = defaultValue };
        }

        private static EntityAnalysisModelDictionaryNoBoxingExtractor NewExtractor(EntityAnalysisModel model)
        {
            return new EntityAnalysisModelDictionaryNoBoxingExtractor(model, new Dictionary<int, EntityAnalysisModel>(),
                TestDynamicEnvironment.Create(), TestLog.NoOp);
        }

        private static DictionaryNoBoxing<string> NewEntry()
        {
            var entry = new DictionaryNoBoxing<string>();
            entry["EntityAnalysisModelInstanceEntryGuid"] = new InternalValue(Guid.NewGuid());
            entry.Add("TxnId", "0987654321");
            entry.Add("TxnDateTime", new DateTime(2018, 08, 19, 21, 41, 37, DateTimeKind.Utc));
            return entry;
        }

        [Fact]
        public void TheEntryAndReferenceDateAreCopiedFromTheirConfiguredFieldNames()
        {
            var model = NewModel();
            var entry = NewEntry();

            var context = NewExtractor(model).CreateContext(entry, 0);

            context.EntityAnalysisModelInstanceEntryPayload.EntityInstanceEntryId.Should().Be("0987654321");
            context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate.Should()
                .Be(new DateTime(2018, 08, 19, 21, 41, 37, DateTimeKind.Utc));
        }

        [Fact]
        public void TheReprocessingRuleInstanceIdIsCarriedOntoThePayload()
        {
            var model = NewModel();
            var entry = NewEntry();

            var context = NewExtractor(model).CreateContext(entry, 42);

            context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId.Should()
                .Be(42);
        }

        [Fact]
        public void TheOriginalEntryBecomesThePayloadInstanceDirectly()
        {
            var model = NewModel();
            var entry = NewEntry();

            var context = NewExtractor(model).CreateContext(entry, 0);

            context.EntityAnalysisModelInstanceEntryPayload.Payload.Should().BeSameAs(entry);
        }

        [Fact]
        public void AFieldAlreadyPresentInTheArchivedEntryIsLeftUntouchedAndNotOverwrittenByAnyDefault()
        {
            var model = NewModel(XPath("TxnId", 1, "should-never-be-used"));
            var entry = NewEntry();

            var context = NewExtractor(model).CreateContext(entry, 0);

            context.EntityAnalysisModelInstanceEntryPayload.Payload["TxnId"].AsString().Should().Be("0987654321");
        }

        [Fact]
        public void AStringFieldMissingFromTheArchivedEntryIsBackfilledFromTheConfiguredDefault()
        {
            var model = NewModel(XPath("Channel", 1, "Unknown"));
            var entry = NewEntry();

            var context = NewExtractor(model).CreateContext(entry, 0);

            context.EntityAnalysisModelInstanceEntryPayload.Payload["Channel"].AsString().Should().Be("Unknown");
        }

        [Fact]
        public void AnIntegerFieldMissingFromTheArchivedEntryIsBackfilledFromTheConfiguredDefault()
        {
            var model = NewModel(XPath("RetryCount", 2, "3"));
            var entry = NewEntry();

            var context = NewExtractor(model).CreateContext(entry, 0);

            context.EntityAnalysisModelInstanceEntryPayload.Payload["RetryCount"].AsInt().Should().Be(3);
        }

        [Fact]
        public void AnIntegerFieldWithANonNumericDefaultIsNotAddedRatherThanCorruptingThePayloadWithAZero()
        {
            var model = NewModel(XPath("RetryCount", 2, "not-a-number"));
            var entry = NewEntry();

            var context = NewExtractor(model).CreateContext(entry, 0);

            context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey("RetryCount").Should().BeFalse();
        }

        [Theory]
        [InlineData(3)]
        [InlineData(6)]
        [InlineData(7)]
        public void AFloatLatitudeOrLongitudeFieldMissingFromTheArchivedEntryIsBackfilledFromTheConfiguredDefault(
            int dataTypeId)
        {
            var model = NewModel(XPath("Amount", dataTypeId, "123.45"));
            var entry = NewEntry();

            var context = NewExtractor(model).CreateContext(entry, 0);

            context.EntityAnalysisModelInstanceEntryPayload.Payload["Amount"].AsDouble().Should().Be(123.45);
        }

        [Theory]
        [InlineData("True", true)]
        [InlineData("1", true)]
        [InlineData("False", false)]
        [InlineData("0", false)]
        public void ABooleanFieldMissingFromTheArchivedEntryIsBackfilledFromTheConfiguredDefault(string defaultValue,
            bool expected)
        {
            var model = NewModel(XPath("Flag", 5, defaultValue));
            var entry = NewEntry();

            var context = NewExtractor(model).CreateContext(entry, 0);

            context.EntityAnalysisModelInstanceEntryPayload.Payload["Flag"].AsBool().Should().Be(expected);
        }

        [Fact]
        public void ADateFieldMissingFromTheArchivedEntryUsesTheConfiguredDefaultAsADaysOffsetFromNow()
        {
            var model = NewModel(XPath("ActionDate", 4, "7"));
            var before = DateTime.UtcNow.AddDays(-7).AddSeconds(-5);
            var entry = NewEntry();

            var context = NewExtractor(model).CreateContext(entry, 0);

            var value = context.EntityAnalysisModelInstanceEntryPayload.Payload["ActionDate"].AsDateTime();
            value.Should().BeAfter(before).And.BeBefore(DateTime.UtcNow.AddDays(-7).AddSeconds(5));
        }

        [Fact]
        public void AFieldMissingFromTheArchivedEntryWithNoConfiguredDefaultIsNotAddedToThePayload()
        {
            var model = NewModel(XPath("NeverSupplied", 1));
            var entry = NewEntry();

            var act = () => NewExtractor(model).CreateContext(entry, 0);

            act.Should().NotThrow();
            var context = NewExtractor(model).CreateContext(NewEntry(), 0);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey("NeverSupplied").Should().BeFalse();
        }

        [Fact]
        public void AnUnrecognisedDataTypeIdFallsBackToStringHandlingForTheDefault()
        {
            var model = NewModel(XPath("Channel", 99, "Unknown"));
            var entry = NewEntry();

            var context = NewExtractor(model).CreateContext(entry, 0);

            context.EntityAnalysisModelInstanceEntryPayload.Payload["Channel"].AsString().Should().Be("Unknown");
        }

        [Fact]
        public void TheReturnedContextCarriesTheModelAndAvailableModels()
        {
            var model = NewModel();
            var availableModels = new Dictionary<int, EntityAnalysisModel> { [1] = model };
            var extractor = new EntityAnalysisModelDictionaryNoBoxingExtractor(model, availableModels,
                TestDynamicEnvironment.Create(), TestLog.NoOp);

            var context = extractor.CreateContext(NewEntry(), 0);

            context.EntityAnalysisModel.Should().BeSameAs(model);
            context.AvailableEntityAnalysisModels.Should().BeSameAs(availableModels);
            context.Async.Should().BeFalse();
        }
    }
}