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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Analytics.Datasets;
using Jube.Analytics.Datasets.Models;
using Xunit;

namespace Jube.Test.Exhaustive.Datasets
{
    public class DatasetCsvParserTests
    {
        private static readonly DatasetLimits limits = DatasetLimits.Default;

        private static Stream Bytes(string text, bool bom = false)
        {
            var body = Encoding.UTF8.GetBytes(text);

            return new MemoryStream(bom ? [.. Encoding.UTF8.GetPreamble(), .. body] : body);
        }

        private static async Task<(DatasetParseReport Report, List<DatasetCell[]> Rows)> ParseAsync(Stream stream,
            DatasetLimits? custom = null)
        {
            var rows = new List<DatasetCell[]>();

            var report = await DatasetCsvParser.ParseAsync(stream, custom ?? limits, (row, _) =>
            {
                rows.Add([.. row.Cells]);

                return ValueTask.CompletedTask;
            });

            return (report, rows);
        }

        private static Task<(DatasetParseReport Report, List<DatasetCell[]> Rows)> ParseAsync(string text,
            DatasetLimits? custom = null, bool bom = false)
        {
            return ParseAsync(Bytes(text, bom), custom);
        }

        [Fact]
        public async Task SimpleFile_IsParsedWithProfilesAsync()
        {
            var (report, rows) = await ParseAsync("A,B,Dependent\n1,2.5,0\n3,4.5,1\n5,6.5,0\n");

            report.Success.Should().BeTrue();
            report.RowCount.Should().Be(3);
            rows.Should().HaveCount(3);
            report.Columns.Select(c => c.Kind).Should().OnlyContain(k => k == DatasetColumnKinds.Numeric);
            report.Columns[1].Mean.Should().BeApproximately(4.5, 1e-9);
            report.Columns[2].IsBinary.Should().BeTrue();
            report.Columns[2].ZeroCount.Should().Be(2);
            report.Columns[2].OneCount.Should().Be(1);
            report.Columns[2].Include.Should().BeTrue();
        }

        [Fact]
        public async Task Bom_IsSkippedAndHeaderIsCleanAsync()
        {
            var (report, _) = await ParseAsync("Name,Value\n1,2\n", bom: true);

            report.Success.Should().BeTrue();
            report.Columns[0].Header.Should().Be("Name");
        }

        [Theory]
        [InlineData("A;B;C\n1;2;3\n4;5;6\n", ';')]
        [InlineData("A,B,C\n1,2,3\n4,5,6\n", ',')]
        public async Task Delimiter_IsDetectedAsync(string text, char expected)
        {
            var (report, rows) = await ParseAsync(text);

            report.Success.Should().BeTrue();
            report.Delimiter.Should().Be(expected);
            rows[1].Select(c => c.Number).Should().Equal(4, 5, 6);
        }

        [Fact]
        public async Task SemicolonHeaderWithQuotedCommas_UsesSemicolonAsync()
        {
            var (report, _) = await ParseAsync("\"A,x\";B\n1;2\n");

            report.Success.Should().BeTrue();
            report.Delimiter.Should().Be(';');
            report.Columns[0].Header.Should().Be("A,x");
        }

        [Fact]
        public async Task MixedLineEndings_AreAcceptedAsync()
        {
            var (report, rows) = await ParseAsync("A,B\r\n1,2\n3,4\r5,6\r\n");

            report.Success.Should().BeTrue();
            rows.Should().HaveCount(3);
        }

        [Fact]
        public async Task Quoting_EscapedQuotesAndEmbeddedDelimitersAndNewlinesAsync()
        {
            var (report, rows) = await ParseAsync("A,B\n\"1\",\"x,\"\"y\"\"\nz\"\n2,plain\n");

            report.Success.Should().BeTrue();
            rows[0][0].Number.Should().Be(1);
            rows[0][1].Text.Should().Be("x,\"y\"\nz");
            rows[1][1].Text.Should().Be("plain");
        }

        [Theory]
        [InlineData("A,B\n1,\"unterminated\n", "UnterminatedQuote")]
        [InlineData("A,B\n1,ab\"cd\n", "StrayQuote")]
        [InlineData("A,B\n1,\"ab\"cd\n", "StrayCharacter")]
        public async Task MalformedQuoting_IsRejectedAsync(string text, string code)
        {
            var (report, _) = await ParseAsync(text);

            report.Success.Should().BeFalse();
            report.Errors.Should().ContainSingle().Which.Code.Should().Be(code);
        }

        [Fact]
        public async Task InjectionCells_AreStoredAsDataNeverEvaluatedAsync()
        {
            var (report, rows) = await ParseAsync("A,B\n=cmd|' /C calc'!A0,1\n@SUM(1+1),2\n-2+3,3\n+1,4\n");

            report.Success.Should().BeTrue();
            rows[0][0].Kind.Should().Be(DatasetCellKind.Text);
            rows[0][0].Text.Should().StartWith("=cmd");
            rows[1][0].Kind.Should().Be(DatasetCellKind.Text);
            rows[2][0].Kind.Should().Be(DatasetCellKind.Text);
            rows[3][0].Kind.Should().Be(DatasetCellKind.Number);
            report.Columns[0].FormulaLikeCount.Should().Be(3);
            report.Warnings.Should().Contain(w => w.Contains("never evaluated"));
            DatasetRowJson.Serialise(rows[0]).Should().Contain("=cmd");
        }

        [Fact]
        public async Task MissingTokens_AreCountedAsync()
        {
            var (report, rows) = await ParseAsync("A,B\n1,\nNA,2\nnull,3\nNaN,4\nN/A,5\n6,6\n");

            report.Success.Should().BeTrue();
            report.Columns[0].MissingCount.Should().Be(4);
            report.Columns[0].NumericCount.Should().Be(2);
            report.Columns[1].MissingCount.Should().Be(1);
            rows[0][1].Kind.Should().Be(DatasetCellKind.Missing);
            report.Warnings.Should().Contain(w => w.Contains("missing"));
        }

        [Fact]
        public async Task Numbers_UseInvariantCultureAndRejectThousandsSeparatorsAsync()
        {
            var (report, rows) = await ParseAsync("A,B,C\n1.5,\"1,234\",1e3\n-2,\"5,5\",+7\n");

            report.Success.Should().BeTrue();
            rows[0][0].Number.Should().Be(1.5);
            rows[0][2].Number.Should().Be(1000);
            rows[0][1].Kind.Should().Be(DatasetCellKind.Text);
            rows[1][1].Kind.Should().Be(DatasetCellKind.Text);
            report.Warnings.Should().Contain(w => w.Contains("thousands"));
            report.Warnings.Should().Contain(w => w.Contains("decimal comma"));
        }

        [Fact]
        public async Task NonFiniteNumbers_AreNotAcceptedAsNumbersAsync()
        {
            var (report, rows) = await ParseAsync("A\n1e999\n-Infinity\n1\n");

            report.Success.Should().BeTrue();
            rows[0][0].Kind.Should().Be(DatasetCellKind.Text);
            rows[1][0].Kind.Should().Be(DatasetCellKind.Text);
            report.Columns[0].Kind.Should().Be(DatasetColumnKinds.Mixed);
        }

        [Fact]
        public async Task TextColumn_TooFewRowsToFitAsCategorical_IsExcludedByDefaultAsync()
        {
            var (report, _) = await ParseAsync("Country,Amount,Dependent\nSE,1,0\nNO,2,1\nSE,3,0\n");

            report.Success.Should().BeTrue();
            report.Columns[0].Kind.Should().Be(DatasetColumnKinds.Text);
            report.Columns[0].IsCategorical.Should().BeFalse();
            report.Columns[0].Include.Should().BeFalse();
            report.Columns[1].Include.Should().BeTrue();
            report.Warnings.Should().Contain(w => w.Contains("Country") && w.Contains("not numeric"));
        }

        [Fact]
        public async Task ConstantAndEmptyColumns_AreExcludedByDefaultAsync()
        {
            var (report, _) = await ParseAsync("A,Const,Empty,Dependent\n1,5,,0\n2,5,,1\n3,5,NA,0\n");

            report.Columns[1].IsConstant.Should().BeTrue();
            report.Columns[1].Include.Should().BeFalse();
            report.Columns[2].Kind.Should().Be(DatasetColumnKinds.Empty);
            report.Columns[2].Include.Should().BeFalse();
        }

        [Fact]
        public async Task RaggedRow_IsRejectedWithRowNumberAsync()
        {
            var (report, rows) = await ParseAsync("A,B,C\n1,2,3\n4,5\n6,7,8\n");

            report.Success.Should().BeFalse();
            var error = report.Errors.Should().ContainSingle().Subject;
            error.Code.Should().Be("RaggedRow");
            error.Row.Should().Be(2);
            error.Message.Should().Contain("Row 2");
            rows.Should().HaveCount(1);
        }

        [Fact]
        public async Task RaggedRow_TooManyValues_IsRejectedAsync()
        {
            var (report, _) = await ParseAsync("A,B\n1,2,3\n");

            report.Success.Should().BeFalse();
            report.Errors[0].Code.Should().Be("RaggedRow");
        }

        [Theory]
        [InlineData("A,B,A\n1,2,3\n")]
        [InlineData("A,B,a\n1,2,3\n")]
        [InlineData("A,B, A \n1,2,3\n")]
        public async Task DuplicateHeaders_AreRejectedAsync(string text)
        {
            var (report, _) = await ParseAsync(text);

            report.Success.Should().BeFalse();
            report.Errors.Should().Contain(e => e.Code == "DuplicateHeader");
        }

        [Theory]
        [InlineData("A,,C\n1,2,3\n", "EmptyHeader")]
        [InlineData("A,<b>x</b>\n1,2\n", "HeaderMarkup")]
        [InlineData("A,B>\n1,2\n", "HeaderMarkup")]
        public async Task BadHeaders_AreRejectedAsync(string text, string code)
        {
            var (report, _) = await ParseAsync(text);

            report.Success.Should().BeFalse();
            report.Errors.Should().Contain(e => e.Code == code);
        }

        [Fact]
        public async Task HugeCell_IsRejectedAsync()
        {
            var (report, _) = await ParseAsync("A,B\n1," + new string('x', 5000) + "\n");

            report.Success.Should().BeFalse();
            report.Errors[0].Code.Should().Be("CellTooLong");
        }

        [Fact]
        public async Task HugeHeader_IsRejectedAsync()
        {
            var (report, _) = await ParseAsync(new string('h', 300) + ",B\n1,2\n");

            report.Success.Should().BeFalse();
            report.Errors[0].Code.Should().BeOneOf("HeaderTooLong", "CellTooLong");
        }

        [Fact]
        public async Task NonUtf8Bytes_AreRejectedAsync()
        {
            var bytes = Encoding.ASCII.GetBytes("A,B\n1,").Concat(new byte[] { 0xC3, 0x28, 0xFF })
                .Concat(Encoding.ASCII.GetBytes("\n")).ToArray();

            var (report, _) = await ParseAsync(new MemoryStream(bytes));

            report.Success.Should().BeFalse();
            report.Errors[0].Code.Should().Be("NotUtf8");
        }

        [Fact]
        public async Task Utf16WithBom_IsRejectedAsync()
        {
            var bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("A,B\n1,2\n")).ToArray();

            var (report, _) = await ParseAsync(new MemoryStream(bytes));

            report.Success.Should().BeFalse();
            report.Errors[0].Code.Should().Be("NotCsv");
        }

        [Theory]
        [InlineData(new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x00, 0x00, 0x08 }, "ZIP")]
        [InlineData(new byte[] { 0x1F, 0x8B, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00 }, "gzip")]
        [InlineData(new byte[] { 0x42, 0x5A, 0x68, 0x39, 0x31, 0x41, 0x59, 0x26 }, "bzip2")]
        [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 }, "PDF")]
        [InlineData(new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 }, "executable")]
        public async Task CompressedAndBinaryContent_IsRejectedAsync(byte[] bytes, string description)
        {
            var (report, rows) = await ParseAsync(new MemoryStream(bytes));

            report.Success.Should().BeFalse();
            report.Errors[0].Code.Should().Be("NotCsv");
            report.Errors[0].Message.Should().Contain(description);
            rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ControlCharacters_AreRejectedAsync()
        {
            var (report, _) = await ParseAsync("A,B\n1,2\n3,\u0000\n");

            report.Success.Should().BeFalse();
            report.Errors[0].Code.Should().Be("BinaryContent");
        }

        [Fact]
        public async Task RowCap_IsEnforcedAsync()
        {
            var (report, rows) = await ParseAsync("A\n1\n2\n3\n4\n", limits with { MaxRows = 3 });

            report.Success.Should().BeFalse();
            report.Errors[0].Code.Should().Be("TooManyRows");
            rows.Should().HaveCount(3);
        }

        [Fact]
        public async Task RowCap_AtTheLimitIsAcceptedAsync()
        {
            var (report, _) = await ParseAsync("A\n1\n2\n3\n", limits with { MaxRows = 3 });

            report.Success.Should().BeTrue();
        }

        [Fact]
        public async Task ColumnCap_IsEnforcedBeforeAnyRowAsync()
        {
            var (report, rows) = await ParseAsync("A,B,C,D\n1,2,3,4\n", limits with { MaxColumns = 3 });

            report.Success.Should().BeFalse();
            report.Errors[0].Code.Should().Be("TooManyColumns");
            rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ByteCap_IsEnforcedWhileStreamingAsync()
        {
            var text = "A\n" + string.Join("\n", Enumerable.Range(0, 5000));

            var (report, _) = await ParseAsync(text, limits with { MaxBytes = 1000 });

            report.Success.Should().BeFalse();
            report.Errors[0].Code.Should().Be("TooLarge");
            report.BytesRead.Should().BeGreaterThan(1000).And.BeLessThan(1000 + 20000);
        }

        [Fact]
        public async Task EmptyFile_HeaderOnly_AndBlankLines_AreHandledAsync()
        {
            (await ParseAsync("")).Report.Errors[0].Code.Should().Be("Empty");
            (await ParseAsync("A,B\n")).Report.Errors[0].Code.Should().Be("NoRows");

            var (report, rows) = await ParseAsync("A,B\n\n1,2\n\n\n3,4\n\n");
            report.Success.Should().BeTrue();
            rows.Should().HaveCount(2);
            report.BlankLineCount.Should().Be(4);
        }

        [Fact]
        public async Task Cancellation_StopsParsingAsync()
        {
            using var cts = new CancellationTokenSource();
            var text = "A\n" + string.Join("\n", Enumerable.Range(0, 1000));
            var seen = 0;

            var act = async () => await DatasetCsvParser.ParseAsync(Bytes(text), limits, async (_, _) =>
            {
                if (++seen == 10)
                {
                    await cts.CancelAsync();
                }
            }, cts.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
            seen.Should().BeLessThan(20);
        }

        [Fact]
        public async Task LargeStreamedFile_DoesNotBufferRowsAsync()
        {
            var builder = new StringBuilder("A,B,Dependent\n");

            for (var i = 0; i < 20000; i++)
            {
                builder.Append(i).Append(',').Append(i * 0.5).Append(',').Append(i % 2).Append('\n');
            }

            long count = 0;

            var report = await DatasetCsvParser.ParseAsync(Bytes(builder.ToString()), limits, (_, _) =>
            {
                count++;

                return ValueTask.CompletedTask;
            });

            report.Success.Should().BeTrue();
            count.Should().Be(20000);
            report.Columns[2].ZeroCount.Should().Be(10000);
        }

        [Theory]
        [InlineData("Transaction Amount", "Transaction_Amount")]
        [InlineData("1st", "V1st")]
        [InlineData("<b>x</b>", "b_x_b")]
        [InlineData("%%%", "Column3")]
        [InlineData("a.b-c", "a_b_c")]
        public void SuggestName_ProducesSafeNames(string header, string expected)
        {
            var name = DatasetCsvParser.SuggestName(header, 3, new HashSet<string>());

            name.Should().Be(expected);
            DatasetMappingRules.IsSafeName(name).Should().BeTrue();
        }

        [Fact]
        public void SuggestName_DeduplicatesAndTruncates()
        {
            var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            DatasetCsvParser.SuggestName("A b", 0, taken).Should().Be("A_b");
            DatasetCsvParser.SuggestName("A-b", 1, taken).Should().Be("A_b_2");
            var long1 = DatasetCsvParser.SuggestName(new string('z', 200), 2, taken);
            long1.Length.Should().Be(64);
            DatasetCsvParser.SuggestName(new string('z', 200), 3, taken).Length.Should().BeLessOrEqualTo(64);
        }

        [Fact]
        public void RowJson_UsesIndexKeysAndNullsAndText()
        {
            var json = DatasetRowJson.Serialise(
            [
                new DatasetCell(DatasetCellKind.Number, 1.5, null),
                new DatasetCell(DatasetCellKind.Missing, 0, null),
                new DatasetCell(DatasetCellKind.Text, 0, "\"quoted\" <b>")
            ]);

            json.Should().Be("{\"values\":{\"c0\":1.5,\"c1\":null,\"c2\":\"\\u0022quoted\\u0022 \\u003Cb\\u003E\"}}");
            DatasetRowJson.ValueJsonPath(7).Should().Be("$.values.c7");
        }
    }
}