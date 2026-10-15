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
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Analytics.Datasets;
using Jube.Analytics.Datasets.Models;
using Xunit;

namespace Jube.Test.Exhaustive.Datasets
{
    public class DatasetJsonParserTests
    {
        private static readonly DatasetLimits limits = DatasetLimits.Default;

        private static async Task<(DatasetParseReport Report, List<DatasetCell[]> Rows)> ParseAsync(Stream stream,
            DatasetLimits? custom = null)
        {
            var rows = new List<DatasetCell[]>();

            var report = await DatasetJsonParser.ParseAsync(stream, custom ?? limits, (row, _) =>
            {
                rows.Add([.. row.Cells]);

                return ValueTask.CompletedTask;
            });

            return (report, rows);
        }

        private static Task<(DatasetParseReport Report, List<DatasetCell[]> Rows)> ParseAsync(string text,
            DatasetLimits? custom = null)
        {
            return ParseAsync(new MemoryStream(Encoding.UTF8.GetBytes(text)), custom);
        }

        private static Task<(DatasetParseReport Report, List<DatasetCell[]> Rows)> ParseAsync(byte[] bytes,
            DatasetLimits? custom = null)
        {
            return ParseAsync(new MemoryStream(bytes), custom);
        }

        private static void Rejected(DatasetParseReport report, string code)
        {
            report.Success.Should().BeFalse();

            report.Errors.Should().Contain(e => e.Code == code,
                $"errors were: {string.Join("; ", report.Errors.Select(e => e.Code + ":" + e.Message))}");
        }

        [Fact]
        public async Task ArrayOfObjects_IsParsedWithProfilesAsync()
        {
            var (report, rows) =
                await ParseAsync(
                    "[{\"a\":1,\"b\":2.5,\"dependent\":0},{\"a\":3,\"b\":4.5,\"dependent\":1},{\"a\":5,\"b\":6.5,\"dependent\":0}]");

            report.Success.Should().BeTrue();
            report.RowCount.Should().Be(3);
            rows.Should().HaveCount(3);
            report.Columns.Select(c => c.Header).Should().Equal("a", "b", "dependent");
            report.Columns.Select(c => c.Kind).Should().OnlyContain(k => k == DatasetColumnKinds.Numeric);
            report.Columns[1].Mean.Should().BeApproximately(4.5, 1e-9);
            report.Columns[2].IsBinary.Should().BeTrue();
            report.Columns[2].Include.Should().BeTrue();
        }

        [Fact]
        public async Task ColumnarForm_IsParsedWithProfilesAsync()
        {
            var (report, rows) =
                await ParseAsync("{\"columns\":[\"a\",\"b\",\"dependent\"],\"rows\":[[1,2.5,0],[3,4.5,1],[5,6.5,0]]}");

            report.Success.Should().BeTrue();
            report.RowCount.Should().Be(3);
            rows[1].Select(c => c.Number).Should().Equal(3, 4.5, 1);
        }

        [Fact]
        public async Task BothShapes_GiveIdenticalReportsAndRowsAsync()
        {
            var objects = await ParseAsync("[{\"a\":1,\"b\":0},{\"a\":2,\"b\":1},{\"a\":3,\"b\":0}]");
            var columnar = await ParseAsync("{\"columns\":[\"a\",\"b\"],\"rows\":[[1,0],[2,1],[3,0]]}");

            JsonSerializer.Serialize(objects.Report.Columns).Should()
                .Be(JsonSerializer.Serialize(columnar.Report.Columns));

            objects.Rows.Select(r => DatasetRowJson.Serialise(r)).Should()
                .Equal(columnar.Rows.Select(r => DatasetRowJson.Serialise(r)));
        }

        [Fact]
        public async Task ObjectForm_UnionOfKeysDefinesColumns_AndMissingKeyIsMissingAsync()
        {
            var (report, rows) = await ParseAsync("[{\"a\":1},{\"a\":2,\"b\":5},{\"b\":7,\"c\":1}]");

            report.Success.Should().BeTrue();
            report.Columns.Select(c => c.Header).Should().Equal("a", "b", "c");
            report.Columns[0].MissingCount.Should().Be(1);
            report.Columns[1].MissingCount.Should().Be(1);
            report.Columns[2].MissingCount.Should().Be(2);
            rows[0].Should().HaveCount(1);
            DatasetRowJson.Serialise(rows[2]).Should().Be("{\"values\":{\"c0\":null,\"c1\":7,\"c2\":1}}");
            report.Warnings.Should().Contain(w => w.Contains("missing values"));
        }

        [Fact]
        public async Task Booleans_BecomeOneAndZero_WithAWarningAsync()
        {
            var (report, rows) = await ParseAsync("[{\"a\":true,\"y\":0},{\"a\":false,\"y\":1}]");

            report.Success.Should().BeTrue();
            rows.Select(r => r[0].Number).Should().Equal(1, 0);
            report.Columns[0].IsBinary.Should().BeTrue();
            report.Warnings.Should().Contain(w => w.Contains("boolean"));
        }

        [Fact]
        public async Task NullIsMissing_AndCountedAsync()
        {
            var (report, rows) = await ParseAsync("{\"columns\":[\"a\",\"b\"],\"rows\":[[1,null],[null,2],[3,4]]}");

            report.Success.Should().BeTrue();
            rows[0][1].Kind.Should().Be(DatasetCellKind.Missing);
            report.Columns[0].MissingCount.Should().Be(1);
            report.Columns[1].MissingCount.Should().Be(1);
        }

        [Fact]
        public async Task Strings_FlagNonNumericColumns_ExcludedByDefault_AndKeepTheTextAsync()
        {
            var (report, rows) = await ParseAsync("[{\"a\":1,\"t\":\"hello\"},{\"a\":2,\"t\":\"world\"}]");

            report.Success.Should().BeTrue();
            report.Columns[1].Kind.Should().Be(DatasetColumnKinds.Text);
            report.Columns[1].Include.Should().BeFalse();
            report.Warnings.Should().Contain(w => w.Contains("'t' is not numeric"));
            rows[0][1].Text.Should().Be("hello");
        }

        [Fact]
        public async Task MixedTypesInAColumn_AreMixedAndExcludedAsync()
        {
            var (report, _) = await ParseAsync("[{\"a\":1},{\"a\":\"x\"},{\"a\":3}]");

            report.Success.Should().BeTrue();
            report.Columns[0].Kind.Should().Be(DatasetColumnKinds.Mixed);
            report.Columns[0].Include.Should().BeFalse();
        }

        [Fact]
        public async Task NumbersAsStrings_AreTextAndWarnedAsync()
        {
            var (report, rows) = await ParseAsync("[{\"a\":\"12\"},{\"a\":\"13.5\"}]");

            report.Success.Should().BeTrue();
            report.Columns[0].Kind.Should().Be(DatasetColumnKinds.Text);
            rows[0][0].Kind.Should().Be(DatasetCellKind.Text);
            report.Warnings.Should().Contain(w => w.Contains("numbers supplied as strings"));
        }

        [Fact]
        public async Task NegativeZero_IsStoredAsZeroAsync()
        {
            var (report, rows) = await ParseAsync("[{\"a\":-0,\"b\":1},{\"a\":0,\"b\":2},{\"a\":1,\"b\":3}]");

            report.Success.Should().BeTrue();
            rows[0][0].Number.Should().Be(0);
            double.IsNegative(rows[0][0].Number).Should().BeFalse();
            report.Columns[0].ZeroCount.Should().Be(2);
            DatasetRowJson.Serialise(rows[0]).Should().Be("{\"values\":{\"c0\":0,\"c1\":1}}");
        }

        [Fact]
        public async Task IntegersBeyondTwoToThe53_AreWarned_AndExactOnesAreNotAsync()
        {
            var (report, _) = await ParseAsync("[{\"a\":9007199254740993,\"b\":9007199254740992},{\"a\":1,\"b\":2}]");

            report.Success.Should().BeTrue();
            report.Warnings.Should().Contain(w => w.Contains("'a'") && w.Contains("2^53"));
            report.Warnings.Should().NotContain(w => w.Contains("'b'") && w.Contains("2^53"));
        }

        [Theory]
        [InlineData("1e400")]
        [InlineData("-1e400")]
        [InlineData("1E999")]
        public async Task OverflowingNumbers_AreRejectedAsync(string literal)
        {
            var (report, _) = await ParseAsync($"[{{\"a\":{literal}}}]");

            Rejected(report, "NumberInvalid");
        }

        [Fact]
        public async Task LargeButFiniteNumber_IsAcceptedAsync()
        {
            var (report, rows) = await ParseAsync("[{\"a\":1e300},{\"a\":-1e300}]");

            report.Success.Should().BeTrue();
            rows[0][0].Number.Should().Be(1e300);
        }

        [Fact]
        public async Task UnicodeColumnNames_AreAcceptedAndGetSafeSuggestedNamesAsync()
        {
            var (report, _) = await ParseAsync("[{\"\u9280\u884c\":1,\"caf\u00e9\":2,\"a b\":3}]");

            report.Success.Should().BeTrue();
            report.Columns.Select(c => c.Header).Should().Equal("\u9280\u884c", "caf\u00e9", "a b");
            report.Columns.Select(c => c.Name).Should().Equal("Column0", "caf", "a_b");
            report.Columns.All(c => DatasetMappingRules.IsSafeName(c.Name)).Should().BeTrue();
        }

        [Fact]
        public async Task NestedObject_IsRejectedWithAPrecisePathAsync()
        {
            var (report, _) = await ParseAsync("[{\"a\":1},{\"a\":2,\"b\":{\"c\":3}}]");

            Rejected(report, "NestedValue");
            report.Errors[0].Message.Should().Contain("$[1].b").And.Contain("no flattening");
        }

        [Fact]
        public async Task NestedArray_InColumnarRows_IsRejectedWithAPrecisePathAsync()
        {
            var (report, _) = await ParseAsync("{\"columns\":[\"a\",\"b\"],\"rows\":[[1,2],[3,[4]]]}");

            Rejected(report, "NestedValue");
            report.Errors[0].Message.Should().Contain("$.rows[1][1]");
        }

        [Fact]
        public async Task DuplicateKeys_InOneObject_AreRejectedAsync()
        {
            var (report, _) = await ParseAsync("[{\"a\":1,\"a\":2}]");

            Rejected(report, "DuplicateKey");
        }

        [Fact]
        public async Task KeysDifferingOnlyByCase_AreRejectedLikeCsvHeadersAsync()
        {
            var (report, _) = await ParseAsync("[{\"a\":1,\"A\":2}]");

            Rejected(report, "DuplicateHeader");
        }

        [Fact]
        public async Task DuplicateColumnNames_InColumnarForm_AreRejectedAsync()
        {
            var (report, _) = await ParseAsync("{\"columns\":[\"a\",\"A\"],\"rows\":[[1,2]]}");

            Rejected(report, "DuplicateHeader");
        }

        [Theory]
        [InlineData("{\"columns\":[\"a\",\"\"],\"rows\":[[1,2]]}", "EmptyHeader")]
        [InlineData("{\"columns\":[\"a\",\"<b>\"],\"rows\":[[1,2]]}", "HeaderMarkup")]
        [InlineData("{\"columns\":[\"a\",\"b\\u0001\"],\"rows\":[[1,2]]}", "ControlCharacter")]
        [InlineData("[{\"\":1}]", "EmptyHeader")]
        [InlineData("[{\"<script>\":1}]", "HeaderMarkup")]
        public async Task UnsafeNames_AreRejectedWithTheCsvHeaderRulesAsync(string json, string code)
        {
            var (report, _) = await ParseAsync(json);

            Rejected(report, code);
        }

        [Fact]
        public async Task NameLongerThanTheHeaderCap_IsRejectedAsync()
        {
            var (report, _) = await ParseAsync($"[{{\"{new string('a', 200)}\":1}}]");

            Rejected(report, "CellTooLong");
        }

        [Theory]
        [InlineData("5")]
        [InlineData("\"text\"")]
        [InlineData("true")]
        [InlineData("null")]
        public async Task TopLevelScalars_AreRejectedAsync(string json)
        {
            var (report, _) = await ParseAsync(json);

            report.Success.Should().BeFalse();
            report.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public async Task EmptyArray_IsRejectedAsNoRowsAsync()
        {
            var (report, _) = await ParseAsync("[]");

            Rejected(report, "NoRows");
        }

        [Fact]
        public async Task ColumnarWithNoRows_IsRejectedAsync()
        {
            var (report, _) = await ParseAsync("{\"columns\":[\"a\"],\"rows\":[]}");

            Rejected(report, "NoRows");
        }

        [Fact]
        public async Task EmptyObject_AndEmptyInput_AreRejectedAsync()
        {
            Rejected((await ParseAsync("{}")).Report, "BadShape");
            Rejected((await ParseAsync("")).Report, "Empty");
            Rejected((await ParseAsync("   \n ")).Report, "InvalidJson");
        }

        [Theory]
        [InlineData("[{\"a\":1},]")]
        [InlineData("[{\"a\":1,}]")]
        [InlineData("[{\"a\":1} // c\n]")]
        [InlineData("[{\"a\":1} /* c */]")]
        [InlineData("[{\"a\":1}")]
        [InlineData("[{\"a\":")]
        [InlineData("[{'a':1}]")]
        [InlineData("[{\"a\":NaN}]")]
        [InlineData("[{\"a\":Infinity}]")]
        [InlineData("[{\"a\":01}]")]
        [InlineData("[{\"a\":1.}]")]
        [InlineData("[{\"a\":.5}]")]
        [InlineData("{\"columns\":[\"a\"],\"rows\":[[1]]")]
        public async Task MalformedJson_IsRejectedAndNothingSucceedsAsync(string json)
        {
            var (report, _) = await ParseAsync(json);

            report.Success.Should().BeFalse();
            report.Errors.Should().NotBeEmpty();
        }

        [Theory]
        [InlineData("[{\"a\":1}] x")]
        [InlineData("[{\"a\":1}][{\"a\":2}]")]
        [InlineData("{\"columns\":[\"a\"],\"rows\":[[1]]} 5")]
        public async Task TrailingContent_IsRejectedAsync(string json)
        {
            var (report, _) = await ParseAsync(json);

            Rejected(report, "TrailingContent");
        }

        [Fact]
        public async Task TrailingWhitespace_IsAcceptedAsync()
        {
            var (report, _) = await ParseAsync("[{\"a\":1}]  \n\t\r\n");

            report.Success.Should().BeTrue();
        }

        [Fact]
        public async Task CsvContent_WithAJsonName_IsRejectedAsync()
        {
            var (report, _) = await ParseAsync("a,b,c\n1,2,3\n");

            Rejected(report, "NotJson");
        }

        [Fact]
        public async Task ByteOrderMark_IsRejectedAsync()
        {
            var body = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("[{\"a\":1}]")).ToArray();

            Rejected((await ParseAsync(body)).Report, "ByteOrderMark");
        }

        [Theory]
        [InlineData(new byte[] { 0xFF, 0xFE, 0x5B, 0x00, 0x7B, 0x00, 0x7D, 0x00, 0x5D, 0x00 })]
        [InlineData(new byte[] { 0x1F, 0x8B, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00 })]
        [InlineData(new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x00, 0x00 })]
        public async Task BinaryAndUtf16_AreRejectedAsync(byte[] body)
        {
            Rejected((await ParseAsync(body)).Report, "NotJson");
        }

        [Fact]
        public async Task InvalidUtf8_InAString_IsRejectedAsync()
        {
            var body = Encoding.UTF8.GetBytes("[{\"a\":\"x").Concat(new byte[] { 0xC3, 0x28 })
                .Concat(Encoding.UTF8.GetBytes("\"}]")).ToArray();

            var (report, _) = await ParseAsync(body);

            report.Success.Should().BeFalse();
            report.Errors.Should().Contain(e => e.Code == "InvalidText" || e.Code == "InvalidJson");
        }

        [Fact]
        public async Task LoneSurrogateEscape_IsRejectedAsync()
        {
            var (report, _) = await ParseAsync("[{\"a\":\"\\ud800\"}]");

            report.Success.Should().BeFalse();
            report.Errors.Should().Contain(e => e.Code == "InvalidText" || e.Code == "InvalidJson");
        }

        [Fact]
        public async Task LoneSurrogateInAName_IsRejectedAsync()
        {
            var (report, _) = await ParseAsync("[{\"\\udc00\":1}]");

            report.Success.Should().BeFalse();
        }

        [Fact]
        public async Task ValidSurrogatePair_IsAcceptedAsync()
        {
            var (report, rows) = await ParseAsync("[{\"a\":1,\"t\":\"\\ud83d\\ude00\"},{\"a\":2,\"t\":\"x\"}]");

            report.Success.Should().BeTrue();
            rows[0][1].Text.Should().Be("\ud83d\ude00");
        }

        [Fact]
        public async Task NulInAStringValue_IsRejectedAsync()
        {
            var (report, _) = await ParseAsync("[{\"a\":\"x\\u0000y\"}]");

            Rejected(report, "ControlCharacter");
        }

        [Fact]
        public async Task DeepNesting_IsRejectedAsync()
        {
            var deep = new string('[', 100000) + new string(']', 100000);

            var (report, _) = await ParseAsync(deep);

            report.Success.Should().BeFalse();
            report.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public async Task NestingBeyondTheDepthLimit_InAValue_IsRejectedAsync()
        {
            var nested = string.Concat(Enumerable.Repeat("[", 20)) + string.Concat(Enumerable.Repeat("]", 20));

            var (report, _) = await ParseAsync($"[{{\"a\":{nested}}}]");

            report.Success.Should().BeFalse();
            report.Errors.Should().Contain(e => e.Code == "NestedValue" || e.Code == "TooDeep");
        }

        [Fact]
        public async Task HugeString_IsRejected_WithoutBufferingItAsync()
        {
            var stream = new RepeatStream("[{\"a\":\"", (byte)'x', 100_000_000);

            var (report, _) = await ParseAsync(stream, limits with { MaxBytes = 500_000_000 });

            report.Success.Should().BeFalse();
            report.Errors.Should().Contain(e => e.Code == "TokenTooLong");
        }

        [Fact]
        public async Task HugeNumber_IsRejectedAsync()
        {
            var stream = new RepeatStream("[{\"a\":1", (byte)'0', 100_000_000);

            var (report, _) = await ParseAsync(stream, limits with { MaxBytes = 500_000_000 });

            report.Success.Should().BeFalse();
            report.Errors.Should().Contain(e => e.Code == "TokenTooLong");
        }

        [Fact]
        public async Task StringLongerThanTheCellCap_IsRejectedAsync()
        {
            var (report, _) = await ParseAsync($"[{{\"a\":\"{new string('x', 1025)}\"}}]");

            Rejected(report, "CellTooLong");
        }

        [Fact]
        public async Task LongStoredText_IsTruncatedTo64CharactersAsync()
        {
            var (report, rows) = await ParseAsync($"[{{\"a\":\"{new string('x', 500)}\"}},{{\"a\":\"y\"}}]");

            report.Success.Should().BeTrue();
            rows[0][0].Text!.Length.Should().Be(64);
        }

        [Fact]
        public async Task NonObjectRow_InArrayForm_IsRejectedWithAPathAsync()
        {
            var (report, _) = await ParseAsync("[{\"a\":1},[1]]");

            Rejected(report, "RowNotObject");
            report.Errors[0].Message.Should().Contain("$[1]");
        }

        [Fact]
        public async Task RaggedRow_InColumnarForm_IsRejectedAsync()
        {
            Rejected((await ParseAsync("{\"columns\":[\"a\",\"b\"],\"rows\":[[1,2],[3]]}")).Report, "RaggedRow");
            Rejected((await ParseAsync("{\"columns\":[\"a\",\"b\"],\"rows\":[[1,2,3]]}")).Report, "RaggedRow");
        }

        [Fact]
        public async Task ColumnarShapeErrors_AreRejectedAsync()
        {
            Rejected((await ParseAsync("{\"rows\":[[1]],\"columns\":[\"a\"]}")).Report, "ColumnsFirst");
            Rejected((await ParseAsync("{\"columns\":[\"a\"]}")).Report, "BadShape");
            Rejected((await ParseAsync("{\"columns\":[\"a\"],\"rows\":[[1]],\"extra\":1}")).Report, "UnexpectedKey");
            Rejected((await ParseAsync("{\"columns\":[\"a\"],\"columns\":[\"b\"],\"rows\":[[1]]}")).Report,
                "DuplicateKey");
            Rejected((await ParseAsync("{\"columns\":[1],\"rows\":[[1]]}")).Report, "ColumnNameNotString");
            Rejected((await ParseAsync("{\"columns\":[],\"rows\":[[]]}")).Report, "NoColumns");
            Rejected((await ParseAsync("{\"columns\":\"a\",\"rows\":[[1]]}")).Report, "BadShape");
            Rejected((await ParseAsync("{\"columns\":[\"a\"],\"rows\":[1]}")).Report, "RowNotArray");
        }

        [Fact]
        public async Task ColumnCap_IsEnforced_ForBothShapesAsync()
        {
            var small = limits with { MaxColumns = 3 };

            Rejected((await ParseAsync("[{\"a\":1,\"b\":2,\"c\":3},{\"d\":4}]", small)).Report, "TooManyColumns");
            Rejected((await ParseAsync("{\"columns\":[\"a\",\"b\",\"c\",\"d\"],\"rows\":[[1,2,3,4]]}", small)).Report,
                "TooManyColumns");

            (await ParseAsync("[{\"a\":1,\"b\":2,\"c\":3}]", small)).Report.Success.Should().BeTrue();
        }

        [Fact]
        public async Task RowCap_IsEnforced_ForBothShapesAsync()
        {
            var small = limits with { MaxRows = 2 };

            Rejected((await ParseAsync("[{\"a\":1},{\"a\":2},{\"a\":3}]", small)).Report, "TooManyRows");
            Rejected((await ParseAsync("{\"columns\":[\"a\"],\"rows\":[[1],[2],[3]]}", small)).Report, "TooManyRows");
            (await ParseAsync("[{\"a\":1},{\"a\":2}]", small)).Report.Success.Should().BeTrue();
        }

        [Fact]
        public async Task SizeCap_IsEnforcedAsync()
        {
            var text = "[" + string.Join(",", Enumerable.Range(0, 100).Select(i => $"{{\"a\":{i}}}")) + "]";

            var (report, _) = await ParseAsync(text, limits with { MaxBytes = 200 });

            Rejected(report, "TooLarge");
        }

        [Fact]
        public async Task LargeFile_WithinCaps_IsStreamedAndCountedAsync()
        {
            var builder = new StringBuilder("{\"columns\":[\"a\",\"b\",\"dependent\"],\"rows\":[");

            for (var i = 0; i < 50000; i++)
            {
                builder.Append(i == 0 ? "" : ",").Append('[').Append(i).Append(',').Append(i % 7)
                    .Append(',').Append(i % 2).Append(']');
            }

            builder.Append("]}");

            var count = 0;

            var report = await DatasetJsonParser.ParseAsync(
                new MemoryStream(Encoding.UTF8.GetBytes(builder.ToString())),
                limits, (_, _) =>
                {
                    count++;

                    return ValueTask.CompletedTask;
                });

            report.Success.Should().BeTrue();
            count.Should().Be(50000);
            report.RowCount.Should().Be(50000);
        }

        [Fact]
        public async Task TokensSplitAcrossReads_AreParsedIdenticallyAsync()
        {
            var text = "[" + string.Join(",", Enumerable.Range(0, 3000)
                .Select(i =>
                    $"{{\"alpha\":{i}.25,\"beta\":\"value {i}\",\"gamma\":{(i % 2 == 0 ? "true" : "false")}}}")) + "]";

            var whole = await ParseAsync(text);
            var drip = await ParseAsync(new SlowStream(Encoding.UTF8.GetBytes(text), 7));

            drip.Report.Success.Should().BeTrue();
            drip.Report.RowCount.Should().Be(whole.Report.RowCount);

            drip.Rows.Select(r => DatasetRowJson.Serialise(r)).Should()
                .Equal(whole.Rows.Select(r => DatasetRowJson.Serialise(r)));
        }

        [Fact]
        public async Task ManyRowsOfEmptyObjects_AreBatchedAndDoNotExhaustMemoryAsync()
        {
            var text = "[{\"a\":1}," + string.Join(",", Enumerable.Repeat("{}", 20000)) + "]";
            var count = 0;

            var report = await DatasetJsonParser.ParseAsync(new MemoryStream(Encoding.UTF8.GetBytes(text)), limits,
                (_, _) =>
                {
                    count++;

                    return ValueTask.CompletedTask;
                });

            report.Success.Should().BeTrue();
            count.Should().Be(20001);
            report.Columns[0].MissingCount.Should().Be(20000);
        }

        [Fact]
        public Task SinkFailure_PropagatesAsync()
        {
            var act = async () => await DatasetJsonParser.ParseAsync(
                new MemoryStream(Encoding.UTF8.GetBytes("[{\"a\":1},{\"a\":2}]")), limits,
                (_, _) => throw new InvalidOperationException("sink"));

            return act.Should().ThrowAsync<InvalidOperationException>().WithMessage("sink");
        }

        [Fact]
        public async Task Cancellation_IsHonouredAsync()
        {
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var token = cts.Token;

            var act = async () => await DatasetJsonParser.ParseAsync(
                new MemoryStream(Encoding.UTF8.GetBytes("[{\"a\":1}]")), limits, null, token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task ClassBalance_AndConstantColumns_MatchTheCsvInspectionAsync()
        {
            var (report, _) =
                await ParseAsync("[{\"a\":1,\"c\":5,\"y\":0},{\"a\":2,\"c\":5,\"y\":1},{\"a\":3,\"c\":5,\"y\":1}]");

            report.Columns[1].IsConstant.Should().BeTrue();
            report.Columns[1].Include.Should().BeFalse();
            report.Columns[2].ZeroCount.Should().Be(1);
            report.Columns[2].OneCount.Should().Be(2);
        }

        [Fact]
        public async Task SameDataAsCsv_GivesTheSameColumnsRowsAndReportAsync()
        {
            const string csv = "a,b,dependent\n1,2.5,0\n3,4.5,1\n5,6.5,0\n7,8.5,1\n";
            const string json =
                "[{\"a\":1,\"b\":2.5,\"dependent\":0},{\"a\":3,\"b\":4.5,\"dependent\":1},{\"a\":5,\"b\":6.5,\"dependent\":0},{\"a\":7,\"b\":8.5,\"dependent\":1}]";

            var csvRows = new List<string>();

            var csvReport = await DatasetCsvParser.ParseAsync(new MemoryStream(Encoding.UTF8.GetBytes(csv)), limits,
                (row, _) =>
                {
                    csvRows.Add(DatasetRowJson.Serialise(row.Cells));

                    return ValueTask.CompletedTask;
                });

            var (jsonReport, jsonRows) = await ParseAsync(json);

            jsonRows.Select(r => DatasetRowJson.Serialise(r)).Should().Equal(csvRows);

            JsonSerializer.Serialize(jsonReport.Columns).Should()
                .Be(JsonSerializer.Serialize(csvReport.Columns));
        }

        [Fact]
        public async Task CsvParser_NamesJsonContentAsync()
        {
            var report = await DatasetCsvParser.ParseAsync(new MemoryStream(Encoding.UTF8.GetBytes("[{\"a\":1}]")),
                limits, null);

            report.Success.Should().BeFalse();
            report.Errors[0].Message.Should().Contain("JSON");
        }

        [Fact]
        public void LargeIntegerLiteral_Detection()
        {
            DatasetJsonParser.IsLargeIntegerLiteral(Encoding.ASCII.GetBytes("9007199254740992")).Should().BeFalse();
            DatasetJsonParser.IsLargeIntegerLiteral(Encoding.ASCII.GetBytes("9007199254740993")).Should().BeTrue();
            DatasetJsonParser.IsLargeIntegerLiteral(Encoding.ASCII.GetBytes("-9007199254740993")).Should().BeTrue();
            DatasetJsonParser.IsLargeIntegerLiteral(Encoding.ASCII.GetBytes("12345678901234567890")).Should().BeTrue();
            DatasetJsonParser.IsLargeIntegerLiteral(Encoding.ASCII.GetBytes("1e20")).Should().BeFalse();
            DatasetJsonParser.IsLargeIntegerLiteral(Encoding.ASCII.GetBytes("123456789.12345678")).Should().BeFalse();
        }

        private sealed class RepeatStream(string prefix, byte filler, long count) : Stream
        {
            private readonly byte[] head = Encoding.UTF8.GetBytes(prefix);
            private long position;
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => position;
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int length)
            {
                var written = 0;

                while (written < length && position < head.Length + count)
                {
                    buffer[offset + written++] = position < head.Length ? head[position] : filler;
                    position++;
                }

                return written;
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int length)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class SlowStream(byte[] data, int chunk) : Stream
        {
            private int position;
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => data.Length;

            public override long Position
            {
                get => position;
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int length)
            {
                var take = Math.Min(Math.Min(chunk, length), data.Length - position);
                Array.Copy(data, position, buffer, offset, take);
                position += take;

                return take;
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int length)
            {
                throw new NotSupportedException();
            }
        }
    }
}