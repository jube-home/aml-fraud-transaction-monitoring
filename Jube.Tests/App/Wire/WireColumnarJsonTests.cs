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
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.App.Wire;
using Jube.Dto.Query.CaseKeyJournal;
using Jube.Dto.Query.WireSchema;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Jube.Test.App.Wire
{
    [Trait("Category", "Unit")]
    public sealed class WireColumnarJsonTests
    {
        private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

        private static readonly List<WireRouteDto> Plain = [new() { Method = "GET", Template = "a", Shape = "b" }];

        private static string Write(object value, Type type)
        {
            return Encoding.UTF8.GetString(ColumnarJson.Serialize(value, type, Web));
        }

        private static WireTypeRegistry Registry(WireMode mode)
        {
            var registry = new WireTypeRegistry(new WireOptions(mode));
            registry.Attach([]);

            return registry;
        }

        private static async Task<(object Result, DefaultHttpContext Context)> Respond(WireMode mode, string accept,
            object endpointResult)
        {
            var context = new DefaultHttpContext();
            context.Request.Headers.Accept = accept;
            context.Response.Body = new MemoryStream();

            var filter = new MessagePackResponseFilter(Registry(mode));
            var result = await filter.InvokeAsync(EndpointFilterInvocationContext.Create(context),
                _ => ValueTask.FromResult(endpointResult));

            return (result, context);
        }

        [Fact]
        public void RowsAreWrittenOnceAsKeysAndColumnsWithMissingCellsAndNullRowsNamed()
        {
            var rows = new List<Dictionary<string, object>>
            {
                new() { ["id"] = 1, ["name"] = "a" },
                new() { ["id"] = 2 },
                null
            };

            Write(rows, rows.GetType()).Should().Be(
                "{\"count\":3,\"keys\":[\"id\",\"name\"],\"columns\":[[1,2,null],[\"a\",null,null]]," +
                "\"missing\":[[1,[1]]],\"nulls\":[2]}");
        }

        [Fact]
        public void NoRowsAreWrittenAsAnEmptyGridWithNothingToDeclare()
        {
            var rows = new List<Dictionary<string, string>>();

            Write(rows, rows.GetType()).Should().Be("{\"count\":0,\"keys\":[],\"columns\":[]}");
        }

        [Fact]
        public void CellsKeepTheTextAnOrdinaryJsonResponseWouldGiveThem()
        {
            var moment = new DateTimeOffset(2026, 11, 5, 13, 30, 15, TimeSpan.Zero);
            var rows = new List<Dictionary<string, object>>
            {
                new() { ["when"] = moment, ["nested"] = new Dictionary<string, object> { ["k"] = 1.5 }, ["flag"] = true }
            };

            var ordinary = JsonSerializer.Serialize(rows[0], Web);
            var columnar = JsonDocument.Parse(Write(rows, rows.GetType())).RootElement;

            foreach (var property in JsonDocument.Parse(ordinary).RootElement.EnumerateObject())
            {
                var keys = columnar.GetProperty("keys").EnumerateArray();
                var column = 0;

                foreach (var key in keys)
                {
                    if (key.GetString() == property.Name)
                    {
                        break;
                    }

                    column++;
                }

                columnar.GetProperty("columns")[column][0].GetRawText().Should().Be(property.Value.GetRawText());
            }
        }

        [Fact]
        public void ADtoMemberOfRowsIsWrittenColumnarAndTheRestOfTheDtoIsOrdinaryJson()
        {
            var dto = new CaseKeyJournalDto
            {
                Rows = [new() { ["Id"] = 7 }, new() { ["Id"] = 8 }]
            };

            var document = JsonDocument.Parse(Write(dto, typeof(CaseKeyJournalDto))).RootElement;

            document.GetProperty("rows").GetProperty("count").GetInt32().Should().Be(2);
            document.GetProperty("rows").GetProperty("keys")[0].GetString().Should().Be("Id");
            document.GetProperty("meta").ValueKind.Should().Be(JsonValueKind.Object);
        }

        [Fact]
        public async Task ARowGridIsAnsweredAsColumnarJsonWhenAskedForAndTheModeIsJsonAsync()
        {
            var (result, context) = await Respond(WireMode.Json, WireMediaType.ColumnarJson + ", application/json;q=0.9",
                TypedResults.Ok(new CaseKeyJournalDto()));

            await ((IResult)result).ExecuteAsync(context);

            context.Response.ContentType.Should().Be(WireMediaType.ColumnarJson);
            context.Response.Headers[WireMediaType.ShapeHeader].ToString()
                .Should().Be(typeof(CaseKeyJournalDto).FullName);
            context.Response.Headers[WireMediaType.SchemaHeader].ToString().Should().HaveLength(16);
            context.Response.Headers.Vary.ToString().Should().Contain("Accept");
        }

        [Fact]
        public async Task AResponseWithoutRowGridsStaysOrdinaryJsonEvenWhenColumnarJsonIsAskedForAsync()
        {
            var original = TypedResults.Ok(Plain);

            var (result, _) = await Respond(WireMode.Json, WireMediaType.ColumnarJson, original);

            result.Should().BeSameAs(original);
        }

        [Fact]
        public async Task AClientThatDoesNotAskForColumnarJsonGetsTheOrdinaryJsonRowsAsync()
        {
            var original = TypedResults.Ok(new CaseKeyJournalDto());

            var (result, _) = await Respond(WireMode.Json, "application/json", original);

            result.Should().BeSameAs(original);
        }

        [Fact]
        public async Task MessagePackIsPreferredWhenTheModeAllowsItAndBothAreAskedForAsync()
        {
            var accept = WireMediaType.MessagePack + ", " + WireMediaType.ColumnarJson + ";q=0.95";

            var (result, context) = await Respond(WireMode.MessagePack, accept,
                TypedResults.Ok(new CaseKeyJournalDto()));

            await ((IResult)result).ExecuteAsync(context);

            context.Response.ContentType.Should().Be(WireMediaType.MessagePack);
        }

        [Fact]
        public async Task JsonModeFallsBackToColumnarJsonWhereMessagePackIsAskedForButNotAllowedAsync()
        {
            var accept = WireMediaType.MessagePack + ", " + WireMediaType.ColumnarJson + ";q=0.95";

            var (result, context) = await Respond(WireMode.Json, accept, TypedResults.Ok(new CaseKeyJournalDto()));

            await ((IResult)result).ExecuteAsync(context);

            context.Response.ContentType.Should().Be(WireMediaType.ColumnarJson);
        }

        [Fact]
        public async Task MixedModeAnswersAPlainResponseAsJsonAndARowGridAsMessagePackAsync()
        {
            var accept = WireMediaType.MessagePack + ", " + WireMediaType.ColumnarJson + ";q=0.95";
            var plain = TypedResults.Ok(Plain);

            var (plainResult, _) = await Respond(WireMode.Mixed, accept, plain);
            var (gridResult, gridContext) = await Respond(WireMode.Mixed, accept,
                TypedResults.Ok(new CaseKeyJournalDto()));

            await ((IResult)gridResult).ExecuteAsync(gridContext);

            plainResult.Should().BeSameAs(plain);
            gridContext.Response.ContentType.Should().Be(WireMediaType.MessagePack);
        }
    }
}
