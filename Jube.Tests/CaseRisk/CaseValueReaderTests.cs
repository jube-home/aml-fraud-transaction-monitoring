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

using FluentAssertions;
using Jube.Engine.CaseRisk;
using Xunit;

namespace Jube.Test.CaseRisk
{
    [Trait("Category", "Unit")]
    public sealed class CaseValueReaderTests
    {
        [Theory]
        [InlineData("{\"Payload\":{\"Amount\":12.5}}", "Amount", 12.5)]
        [InlineData("{\"payload\":{\"AMOUNT\":\"1200\"}}", "amount", 1200.0)]
        [InlineData("{\"Payload\":{\"Amount\":7}}", "Amount", 7.0)]
        public void AValueIsFoundWhateverTheCaseAndWhetherANumberOrNumericText(string json, string field,
            double expected)
        {
            CaseValueReader.Read(json, field).Should().Be(expected);
        }

        [Theory]
        [InlineData("{\"Payload\":{\"Amount\":\"lots\"}}", "Amount")]
        [InlineData("{\"Payload\":{\"Other\":1}}", "Amount")]
        [InlineData("{\"Payload\":{\"Amount\":null}}", "Amount")]
        [InlineData("{\"Payload\":{\"Amount\":{\"x\":1}}}", "Amount")]
        [InlineData("{}", "Amount")]
        [InlineData("not json", "Amount")]
        [InlineData("", "Amount")]
        [InlineData(null, "Amount")]
        [InlineData("{\"Payload\":{\"Amount\":1}}", "")]
        [InlineData("{\"Payload\":{\"Amount\":1}}", null)]
        public void NothingUsableGivesNoValueAndNeverThrows(string? json, string? field)
        {
            CaseValueReader.Read(json!, field!).Should().BeNull();
        }
    }
}
