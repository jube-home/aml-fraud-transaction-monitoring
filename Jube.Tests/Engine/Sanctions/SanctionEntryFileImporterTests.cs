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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Repository;
using Jube.Engine.EntityAnalysisModelManager.Helpers;
using Jube.Engine.Sanctions;
using Jube.Engine.Sanctions.Models;
using Jube.Test.Infrastructure;
using log4net;
using Microsoft.VisualBasic.FileIO;
using Xunit;

namespace Jube.Test.Engine.Sanctions
{
    [Trait("Category", "Unit")]
    public sealed class SanctionEntryFileImporterTests
    {
        private static readonly ILog noOpLog = TestLog.NoOp;

        public static readonly TheoryData<int, string, string> KnownSdnRows = new()
        {
            { 36, "36", "AEROCARIBBEAN AIRLINES" },
            { 173, "173", "ANGLO-CARIBBEAN CO., LTD." },
            { 306, "306", "BANCO NACIONAL DE CUBA" }
        };

        private static TextFieldParser NewParser(string csv, string delimiter = ",")
        {
            return new TextFieldParser(new StringReader(csv))
            {
                TextFieldType = FieldType.Delimited,
                Delimiters = [delimiter]
            };
        }

        [Fact]
        public async Task ImportAsyncParsesASimpleRowIntoARecordAsync()
        {
            using var parser = NewParser("36,AEROCARIBBEAN AIRLINES,CUBA\n");
            var capture = new Capture();

            var result = await SanctionEntryFileImporter.ImportAsync(parser, 1,
                "1", 0, 0,
                capture.OnRecordAsync, capture.OnRejectedAsync, noOpLog);

            capture.Records.Should().ContainSingle();
            capture.Records[0].ElementValue.Should().Be("AEROCARIBBEAN AIRLINES");
            capture.Records[0].Reference.Should().Be("36");
            capture.Rejections.Should().BeEmpty();
            result.TotalRows.Should().Be(1);
            result.RejectedRows.Should().Be(0);
            result.Hashes.Should().ContainSingle();
        }

        [Fact]
        public async Task ImportAsyncSkipsTheConfiguredNumberOfHeaderRowsAsync()
        {
            using var parser = NewParser("Id,Name,Country\n36,AEROCARIBBEAN AIRLINES,CUBA\n");
            var capture = new Capture();

            var result = await SanctionEntryFileImporter.ImportAsync(parser, 1, "1", 0, 1,
                capture.OnRecordAsync, capture.OnRejectedAsync, noOpLog);

            capture.Records.Should().ContainSingle();
            capture.Records[0].Reference.Should().Be("36");
            result.TotalRows.Should().Be(1, "the skipped header row must not count towards total rows");
        }

        [Fact]
        public async Task ImportAsyncWithSkipGreaterThanRowCountProducesNoRecordsAndNoRejectionsAsync()
        {
            using var parser = NewParser("36,AEROCARIBBEAN AIRLINES,CUBA\n");
            var capture = new Capture();

            var result = await SanctionEntryFileImporter.ImportAsync(parser, 1, "1", 0, 5,
                capture.OnRecordAsync, capture.OnRejectedAsync, noOpLog);

            capture.Records.Should().BeEmpty();
            capture.Rejections.Should().BeEmpty();
            result.TotalRows.Should().Be(0);
        }

        [Fact]
        public async Task ImportAsyncHandlesFieldsWithEmbeddedCommasViaQuotingAsync()
        {
            using var parser = NewParser("173,\"ANGLO-CARIBBEAN CO., LTD.\",CUBA\n");
            var capture = new Capture();

            await SanctionEntryFileImporter.ImportAsync(parser, 1, "1", 0, 0,
                capture.OnRecordAsync, capture.OnRejectedAsync, noOpLog);

            capture.Records.Should().ContainSingle();
            capture.Records[0].ElementValue.Should().Be("ANGLO-CARIBBEAN CO., LTD.",
                "the comma is inside a quoted field and must not be treated as a delimiter");
        }

        [Fact]
        public async Task ImportAsyncCombinesMultipleColumnsWhenMultiPartStringIndexListsSeveralIndicesAsync()
        {
            using var parser = NewParser("1,John,Middle,Smith\n");
            var capture = new Capture();

            await SanctionEntryFileImporter.ImportAsync(parser, 1, "1,2,3", 0, 0,
                capture.OnRecordAsync, capture.OnRejectedAsync, noOpLog);

            capture.Records.Should().ContainSingle();
            capture.Records[0].ElementValue.Should().Be("John Middle Smith");
        }

        [Fact]
        public async Task ImportAsyncFallsBackToColumnZeroWhenAMultiPartIndexTokenIsNotANumberAsync()
        {
            using var parser = NewParser("1,John,Smith\n");
            var capture = new Capture();

            await SanctionEntryFileImporter.ImportAsync(parser, 1, "notanumber", 0, 0,
                capture.OnRecordAsync, capture.OnRejectedAsync, noOpLog);

            capture.Records.Should().ContainSingle();
            capture.Records[0].ElementValue.Should().Be("1", "an unparsable index falls back to data[0]");
        }

        [Fact]
        public async Task ImportAsyncCapturesTheFullRawRowAsThePayloadAsync()
        {
            using var parser = NewParser("36,AEROCARIBBEAN AIRLINES,CUBA\n");
            var capture = new Capture();

            await SanctionEntryFileImporter.ImportAsync(parser, 1, "1", 0, 0,
                capture.OnRecordAsync, capture.OnRejectedAsync, noOpLog);

            capture.Records[0].Payload.Should().Be("36,AEROCARIBBEAN AIRLINES,CUBA");
        }

        [Fact]
        public async Task ImportAsyncRejectsRowsWithOnlyOneFieldAsInsufficientAsync()
        {
            using var parser = NewParser("justonefield\n36,AEROCARIBBEAN AIRLINES,CUBA\n");
            var capture = new Capture();

            var result = await SanctionEntryFileImporter.ImportAsync(parser, 1, "1", 0, 0,
                capture.OnRecordAsync, capture.OnRejectedAsync, noOpLog);

            capture.Records.Should().ContainSingle();
            capture.Rejections.Should().ContainSingle();
            capture.Rejections[0].ReasonId.Should().Be(SanctionEntryRejectionReason.InsufficientFields);
            capture.Rejections[0].RowNumber.Should().Be(1);
            result.TotalRows.Should().Be(2);
            result.RejectedRows.Should().Be(1);
        }

        [Fact]
        public async Task ImportAsyncRejectsEveryDataRowWhenReferenceIndexIsNotConfiguredAsync()
        {
            using var parser = NewParser("36,AEROCARIBBEAN AIRLINES,CUBA\n37,BOUTIQUE LA MAISON,CUBA\n");
            var capture = new Capture();

            var result = await SanctionEntryFileImporter.ImportAsync(parser, 1, "1", null, 0,
                capture.OnRecordAsync, capture.OnRejectedAsync, noOpLog);

            capture.Records.Should().BeEmpty();
            capture.Rejections.Should().HaveCount(2);
            capture.Rejections.Should()
                .OnlyContain(r => r.ReasonId == SanctionEntryRejectionReason.NoReferenceIndexConfigured);
            result.RejectedRows.Should().Be(2);
        }

        [Fact]
        public async Task ImportAsyncRejectsARowThatThrowsDuringBuildAsParseErrorAndContinuesAsync()
        {
            using var parser = NewParser("36,AEROCARIBBEAN AIRLINES,CUBA\n37,BOUTIQUE LA MAISON,CUBA\n");
            var capture = new Capture();

            var result = await SanctionEntryFileImporter.ImportAsync(parser, 1, "1", 5, 0,
                capture.OnRecordAsync, capture.OnRejectedAsync, noOpLog);

            capture.Records.Should().BeEmpty();
            capture.Rejections.Should().HaveCount(2);
            capture.Rejections.Should().OnlyContain(r => r.ReasonId == SanctionEntryRejectionReason.ParseError);
            result.TotalRows.Should().Be(2);
            result.RejectedRows.Should().Be(2);
        }

        [Fact]
        public async Task ImportAsyncEmptyFileProducesNoRecordsNoRejectionsAndEmptyHashesAsync()
        {
            using var parser = NewParser("");
            var capture = new Capture();

            var result = await SanctionEntryFileImporter.ImportAsync(parser, 1, "1", 0, 0,
                capture.OnRecordAsync, capture.OnRejectedAsync, noOpLog);

            capture.Records.Should().BeEmpty();
            capture.Rejections.Should().BeEmpty();
            result.TotalRows.Should().Be(0);
            result.Hashes.Should().BeEmpty();
        }

        [Fact]
        public async Task ImportAsyncWithNullOnRejectedCallbackSwallowsRejectionsWithoutThrowingAsync()
        {
            using var parser = NewParser("justonefield\n");
            var capture = new Capture();

            var result = await SanctionEntryFileImporter.ImportAsync(parser, 1, "1", 0, 0,
                capture.OnRecordAsync, null, noOpLog);

            result.RejectedRows.Should().Be(1);
        }

        [Fact]
        public async Task ImportAsyncProducesTheSameHashForTheSameSourceElementAndReferenceAsync()
        {
            using var parserA = NewParser("36,AEROCARIBBEAN AIRLINES,CUBA\n");
            using var parserB = NewParser("36,AEROCARIBBEAN AIRLINES,CUBA\n");
            var captureA = new Capture();
            var captureB = new Capture();

            await SanctionEntryFileImporter.ImportAsync(parserA, 1, "1", 0, 0, captureA.OnRecordAsync,
                captureA.OnRejectedAsync, noOpLog);
            await SanctionEntryFileImporter.ImportAsync(parserB, 1, "1", 0, 0, captureB.OnRecordAsync,
                captureB.OnRejectedAsync, noOpLog);

            captureA.Records[0].Hash.Should().Be(captureB.Records[0].Hash);
        }

        [Fact]
        public async Task ImportAsyncProducesADifferentHashForADifferentSourceAsync()
        {
            using var parserA = NewParser("36,AEROCARIBBEAN AIRLINES,CUBA\n");
            using var parserB = NewParser("36,AEROCARIBBEAN AIRLINES,CUBA\n");
            var captureA = new Capture();
            var captureB = new Capture();

            await SanctionEntryFileImporter.ImportAsync(parserA, 1, "1", 0, 0, captureA.OnRecordAsync,
                captureA.OnRejectedAsync, noOpLog);
            await SanctionEntryFileImporter.ImportAsync(parserB, 2, "1", 0, 0, captureB.OnRecordAsync,
                captureB.OnRejectedAsync, noOpLog);

            captureA.Records[0].Hash.Should().NotBe(captureB.Records[0].Hash);
        }

        [Fact]
        public async Task ImportAsyncProducesADifferentHashForADifferentReferenceAsync()
        {
            using var parser = NewParser("36,AEROCARIBBEAN AIRLINES,CUBA\n37,AEROCARIBBEAN AIRLINES,CUBA\n");
            var capture = new Capture();

            await SanctionEntryFileImporter.ImportAsync(parser, 1, "1", 0, 0, capture.OnRecordAsync,
                capture.OnRejectedAsync, noOpLog);

            capture.Records[0].Hash.Should().NotBe(capture.Records[1].Hash);
        }

        [Fact]
        public async Task ImportAsyncHashMatchesTheDocumentedMd5OfSourceIdPlusElementPlusReferenceAsync()
        {
            using var parser = NewParser("36,AEROCARIBBEAN AIRLINES,CUBA\n");
            var capture = new Capture();

            await SanctionEntryFileImporter.ImportAsync(parser, 1, "1", 0, 0, capture.OnRecordAsync,
                capture.OnRejectedAsync, noOpLog);

            var expected = HashHelper.GetHash("1" + "AEROCARIBBEAN AIRLINES" + "36");
            capture.Records[0].Hash.Should().Be(expected);
        }

        [Fact]
        public async Task ImportAsyncResultHashesContainsOnlySuccessfullyParsedRowsAsync()
        {
            using var parser = NewParser("36,AEROCARIBBEAN AIRLINES,CUBA\njustonefield\n");
            var capture = new Capture();

            var result = await SanctionEntryFileImporter.ImportAsync(parser, 1, "1", 0, 0,
                capture.OnRecordAsync, capture.OnRejectedAsync, noOpLog);

            result.Hashes.Should().ContainSingle();
            result.Hashes.Should().Contain(capture.Records[0].Hash);
        }

        [Theory]
        [InlineData("Robert Mugabe", "robert mugabe")]
        [InlineData("O'Brien", "obrien")]
        [InlineData("Smith-Jones", "smithjones")]
        [InlineData("St. Petersburg", "st petersburg")]
        [InlineData("A, B", "a b")]
        [InlineData("FRANÇOIS", "francois")]
        [InlineData("MOHAMMED", "mohammed")]
        public void NormalizeElementValueLowercasesStripsDiacriticsAndRemovesPunctuationWithoutSpacing(
            string raw, string expected)
        {
            SanctionEntryFileImporter.NormalizeElementValue(raw).Should().Be(expected);
        }

        [Fact]
        public void NormalizeElementValueDropsHyphenWithNoSpaceUnlikeMatchTimeTokenization()
        {
            SanctionEntryFileImporter.NormalizeElementValue("Smith-Jones").Should().Be("smithjones");
        }

        [Fact]
        public async Task ImportAsyncParsesTheEntireRealSdnFileAndReportsConsistentCountsAsync()
        {
            await using var stream = File.OpenRead("sdn.csv");
            using var parser = new TextFieldParser(stream);
            parser.TextFieldType = FieldType.Delimited;
            parser.Delimiters = [","];
            var capture = new Capture();

            var result = await SanctionEntryFileImporter.ImportAsync(parser, 1,
                "1", 0, 0,
                capture.OnRecordAsync, capture.OnRejectedAsync, noOpLog);

            result.TotalRows.Should().Be(result.RejectedRows + capture.Records.Count);
            capture.Rejections.Should().ContainSingle();
            capture.Rejections[0].ReasonId.Should().Be(SanctionEntryRejectionReason.InsufficientFields);

            capture.Records.Should().HaveCountGreaterThan(19000);
            result.Hashes.Should().HaveCount(capture.Records.Count,
                "every real SDN row has a unique OFAC entity number, so every hash should be unique too");
        }

        [Theory]
        [MemberData(nameof(KnownSdnRows))]
        public async Task ImportAsyncParsesSpecificKnownSdnRowsCorrectlyAsync(int referenceNumber,
            string expectedReference, string expectedElementValue)
        {
            _ = referenceNumber;

            await using var stream = File.OpenRead("sdn.csv");
            using var parser = new TextFieldParser(stream);
            parser.TextFieldType = FieldType.Delimited;
            parser.Delimiters = [","];
            var capture = new Capture();

            await SanctionEntryFileImporter.ImportAsync(parser, 1, "1", 0, 0,
                capture.OnRecordAsync, capture.OnRejectedAsync, noOpLog);

            var record = capture.Records.Should()
                .ContainSingle(r => r.Reference == expectedReference).Subject;
            record.ElementValue.Should().Be(expectedElementValue);
        }

        [Fact]
        public async Task ImportAsyncOnTheRealSdnFileProducesNormalizedTokensThatStillMatchAtQueryTimeAsync()
        {
            await using var stream = File.OpenRead("sdn.csv");
            using var parser = new TextFieldParser(stream);
            parser.TextFieldType = FieldType.Delimited;
            parser.Delimiters = [","];

            var capture = new Capture();
            await SanctionEntryFileImporter.ImportAsync(parser, 1, "1", 0, 0,
                capture.OnRecordAsync, capture.OnRejectedAsync, noOpLog);

            var record = capture.Records.Single(r => r.Reference == "36");
            var sanctionEntry = new SanctionEntry
            {
                SanctionEntryId = 1,
                SanctionEntrySourceId = 1,
                SanctionEntryReference = record.Reference,
                SanctionElementValue =
                [
                    .. record.ElementValue
                        .Split([" "], StringSplitOptions.RemoveEmptyEntries)
                        .Select(SanctionEntryFileImporter.NormalizeElementValue)
                ]
            };
            var entries = new ConcurrentDictionary<int, SanctionEntry>();
            entries.TryAdd(sanctionEntry.SanctionEntryId, sanctionEntry);
            var noStopTokens = new ConcurrentDictionary<string, byte>();

            var matches = new LevenshteinDistance().CheckMultipartString("Aerocaribbean Airlines", 0, entries,
                noStopTokens);

            matches.Should().ContainSingle();
            matches[0].LevenshteinDistance.Should().Be(0);
        }

        private sealed class Capture
        {
            public readonly List<SanctionEntryFileRecord> Records = [];
            public readonly List<SanctionEntryFileRejection> Rejections = [];

            public Task OnRecordAsync(SanctionEntryFileRecord record, CancellationToken token)
            {
                Records.Add(record);
                return Task.CompletedTask;
            }

            public Task OnRejectedAsync(SanctionEntryFileRejection rejection, CancellationToken token)
            {
                Rejections.Add(rejection);
                return Task.CompletedTask;
            }
        }
    }
}