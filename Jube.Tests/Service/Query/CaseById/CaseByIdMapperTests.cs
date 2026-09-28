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

using System.Linq;
using FluentAssertions;
using Jube.Data.Query.CaseQuery.Dto;
using Jube.Service.Query.CaseById;
using Xunit;

namespace Jube.Test.Service.Query.CaseById;

[Trait("Category", "Unit")]
public sealed class CaseByIdMapperTests
{
    [Fact]
    public void AStoredXssPayloadInFormattedPayloadIsSanitisedOnMap()
    {
        var source = new CaseQueryDto
        {
            FormattedPayload =
            [
                new GetCaseByIdFieldEntryDto
                {
                    Name = "Narrative", Value = "<script>alert(1)</script>Legitimate narrative text"
                }
            ]
        };

        var dto = CaseByIdMapper.ToDto(source);

        var value = dto.FormattedPayload!.Single().Value;
        value.Should().NotContain("<script>");
        value.Should().Contain("Legitimate narrative text");
    }

    [Fact]
    public void FormattedPayloadNameIsNotAltered()
    {
        var source = new CaseQueryDto
        {
            FormattedPayload =
            [
                new GetCaseByIdFieldEntryDto { Name = "Payload.Narrative", Value = "safe" }
            ]
        };

        var dto = CaseByIdMapper.ToDto(source);

        dto.FormattedPayload!.Single().Name.Should().Be("Payload.Narrative");
    }
}