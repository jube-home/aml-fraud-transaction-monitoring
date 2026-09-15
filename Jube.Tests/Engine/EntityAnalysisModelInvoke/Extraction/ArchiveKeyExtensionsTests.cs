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
using Jube.Data.Poco;
using Jube.Engine.EntityAnalysisModelInvoke.Extraction.Extensions.YourNamespace.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Xunit;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke.Extraction
{
    [Trait("Category", "Unit")]
    public sealed class ArchiveKeyExtensionsTests
    {
        private static EntityAnalysisModelRequestXPath XPath(string name, bool reportTable)
        {
            return new EntityAnalysisModelRequestXPath { Name = name, ReportTable = reportTable };
        }

        private static EntityAnalysisModelInstanceEntryPayload NewPayload(Guid guid)
        {
            return new EntityAnalysisModelInstanceEntryPayload { EntityAnalysisModelInstanceEntryGuid = guid };
        }

        [Fact]
        public void WhenReportTableIsFalseNoArchiveKeyIsAdded()
        {
            var reportDatabaseValues = new List<ArchiveKey>();
            var xPath = XPath("AccountId", false);

            reportDatabaseValues.AddArchiveKey(xPath, NewPayload(Guid.NewGuid()), "Test1");

            reportDatabaseValues.Should().BeEmpty();
        }

        [Fact]
        public void WhenIsReprocessIsTrueNoArchiveKeyIsAddedEvenIfReportTableIsSet()
        {
            var reportDatabaseValues = new List<ArchiveKey>();
            var xPath = XPath("AccountId", true);

            reportDatabaseValues.AddArchiveKey(xPath, NewPayload(Guid.NewGuid()), "Test1",
                isReprocess: true);

            reportDatabaseValues.Should().BeEmpty();
        }

        [Fact]
        public void AStringValueProducesAnArchiveKeyWithTheKeyValueStringPopulated()
        {
            var reportDatabaseValues = new List<ArchiveKey>();
            var guid = Guid.NewGuid();
            var xPath = XPath("AccountId", true);

            reportDatabaseValues.AddArchiveKey(xPath, NewPayload(guid), "Test1");

            var key = reportDatabaseValues.Should().ContainSingle().Subject;
            key.ProcessingTypeId.Should().Be(1);
            key.Key.Should().Be("AccountId");
            key.EntityAnalysisModelInstanceEntryGuid.Should().Be(guid);
            key.KeyValueString.Should().Be("Test1");
            key.KeyValueInteger.Should().BeNull();
            key.KeyValueFloat.Should().BeNull();
            key.KeyValueDate.Should().BeNull();
            key.KeyValueBoolean.Should().BeNull();
        }

        [Fact]
        public void AnIntValueProducesAnArchiveKeyWithTheKeyValueIntegerPopulated()
        {
            var reportDatabaseValues = new List<ArchiveKey>();
            var xPath = XPath("TransactionTypeId", true);

            reportDatabaseValues.AddArchiveKey(xPath, NewPayload(Guid.NewGuid()), valueInt: 1000);

            reportDatabaseValues.Should().ContainSingle().Which.KeyValueInteger.Should().Be(1000);
        }

        [Fact]
        public void AFloatValueProducesAnArchiveKeyWithTheKeyValueFloatPopulated()
        {
            var reportDatabaseValues = new List<ArchiveKey>();
            var xPath = XPath("CurrencyAmount", true);

            reportDatabaseValues.AddArchiveKey(xPath, NewPayload(Guid.NewGuid()), valueFloat: 123.45);

            reportDatabaseValues.Should().ContainSingle().Which.KeyValueFloat.Should().Be(123.45);
        }

        [Fact]
        public void ADateValueProducesAnArchiveKeyWithTheKeyValueDatePopulated()
        {
            var reportDatabaseValues = new List<ArchiveKey>();
            var xPath = XPath("ActionDate", true);
            var date = new DateTime(2019, 04, 17, 01, 18, 15, DateTimeKind.Utc);

            reportDatabaseValues.AddArchiveKey(xPath, NewPayload(Guid.NewGuid()), valueDate: date);

            reportDatabaseValues.Should().ContainSingle().Which.KeyValueDate.Should().Be(date);
        }

        [Theory]
        [InlineData(true, (byte)1)]
        [InlineData(false, (byte)0)]
        public void ABoolValueProducesAnArchiveKeyWithTheKeyValueBooleanPopulatedAsAByte(bool value, byte expected)
        {
            var reportDatabaseValues = new List<ArchiveKey>();
            var xPath = XPath("DebuggerAttached", true);

            reportDatabaseValues.AddArchiveKey(xPath, NewPayload(Guid.NewGuid()), valueBool: value);

            reportDatabaseValues.Should().ContainSingle().Which.KeyValueBoolean.Should().Be(expected);
        }

        [Fact]
        public void PrecedenceFavoursStringOverAnyOtherValueWhenMultipleAreSuppliedTogether()
        {
            var reportDatabaseValues = new List<ArchiveKey>();
            var xPath = XPath("Ambiguous", true);

            reportDatabaseValues.AddArchiveKey(xPath, NewPayload(Guid.NewGuid()), "wins", 5);

            var key = reportDatabaseValues.Should().ContainSingle().Subject;
            key.KeyValueString.Should().Be("wins");
            key.KeyValueInteger.Should().BeNull();
        }

        [Fact]
        public void NoValueSuppliedProducesAnArchiveKeyWithNoValueFieldsPopulated()
        {
            var reportDatabaseValues = new List<ArchiveKey>();
            var xPath = XPath("Empty", true);

            reportDatabaseValues.AddArchiveKey(xPath, NewPayload(Guid.NewGuid()));

            var key = reportDatabaseValues.Should().ContainSingle().Subject;
            key.KeyValueString.Should().BeNull();
            key.KeyValueInteger.Should().BeNull();
            key.KeyValueFloat.Should().BeNull();
            key.KeyValueDate.Should().BeNull();
            key.KeyValueBoolean.Should().BeNull();
        }

        [Fact]
        public void MultipleCallsAppendIndependentArchiveKeysToTheSameList()
        {
            var reportDatabaseValues = new List<ArchiveKey>();
            var payload = NewPayload(Guid.NewGuid());

            reportDatabaseValues.AddArchiveKey(XPath("AccountId", true), payload, "Test1");
            reportDatabaseValues.AddArchiveKey(XPath("TransactionTypeId", true), payload, valueInt: 1000);

            reportDatabaseValues.Should().HaveCount(2);
        }
    }
}