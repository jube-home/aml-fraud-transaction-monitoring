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
using Jube.Service.Extensions.StructuredData;
using Xunit;

namespace Jube.Test.Service.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class StructuredDataToolsTests
    {
        private const string Json = "{\"payer\":{\"name\":\"Ann\",\"amounts\":[10.5,20]}}";

        [Fact]
        [Trait("Covers", "IsValidJson")]
        [Trait("Covers", "JsonValueAtPath")]
        [Trait("Covers", "JsonNumberAtPath")]
        [Trait("Covers", "JsonArrayLengthAtPath")]
        [Trait("Covers", "JsonHasPath")]
        [Trait("Covers", "MinifyJson")]
        public void JsonPaths()
        {
            StructuredDataTools.IsValidJson(Json).Should().BeTrue();
            StructuredDataTools.JsonValueAtPath(Json, "$.payer.name").Should().Be("Ann");
            StructuredDataTools.JsonNumberAtPath(Json, "$.payer.amounts[1]").Should().Be(20);
            StructuredDataTools.JsonArrayLengthAtPath(Json, "$.payer.amounts").Should().Be(2);
            StructuredDataTools.JsonHasPath(Json, "$.payee").Should().BeFalse();
            StructuredDataTools.MinifyJson("{ \"a\" : 1 }").Should().Be("{\"a\":1}");
            var notNumber = () => StructuredDataTools.JsonNumberAtPath(Json, "$.payer.name");
            notNumber.Should().Throw<ArgumentException>();
            var notJson = () => StructuredDataTools.MinifyJson("{");
            notJson.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "IsValidXml")]
        [Trait("Covers", "XmlValueAtXPath")]
        public void XmlPaths()
        {
            const string pacs =
                "<Document xmlns=\"urn:iso:std:iso:20022:tech:xsd:pacs.008.001.08\"><GrpHdr><MsgId>M-1</MsgId></GrpHdr></Document>";

            StructuredDataTools.IsValidXml(pacs).Should().BeTrue();
            StructuredDataTools.XmlValueAtXPath(pacs, "//*[local-name()='MsgId']").Should().Be("M-1");
            StructuredDataTools.IsValidXml("<!DOCTYPE x SYSTEM \"file:///etc/passwd\"><x/>").Should().BeFalse();
        }
    }
}