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
using Jube.Data.Repository.Models;
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
        [InlineData("Smith-Jones", "smith jones")]
        [InlineData("St. Petersburg", "st petersburg")]
        [InlineData("A, B", "a b")]
        [InlineData("FRANÇOIS", "francois")]
        [InlineData("MOHAMMED", "mohammed")]
        public void NormalizeElementValueLowercasesStripsDiacriticsAndSpacesPunctuation(
            string raw, string expected)
        {
            SanctionEntryFileImporter.NormalizeElementValue(raw).Should().Be(expected);
        }

        [Fact]
        public void NormalizeElementValueSplitsHyphenExactlyAsMatchTimeTokenizationDoes()
        {
            SanctionEntryFileImporter.NormalizeElementValue("Smith-Jones").Should().Be("smith jones");

            SanctionTextFolding.Tokens(SanctionEntryFileImporter.NormalizeElementValue("Smith-Jones"))
                .Should().Equal(SanctionTextFolding.Tokens("Smith-Jones"));
        }

        [Theory]
        [InlineData("AL-QAIDA", "al-Qaida")]
        [InlineData("AL-QAIDA", "Al Qaida")]
        [InlineData("J.R. SMITH", "J R Smith")]
        [InlineData("SMITH, JOHN", "john smith")]
        [InlineData("O'BRIEN-JONES", "OBrien Jones")]
        public void ListEntryAndScreenedNameMatchAtDistanceZeroWhateverPunctuationEachSideUses(string listName,
            string screened)
        {
            var entry = new SanctionEntry
            {
                SanctionEntryId = 1,
                SanctionEntrySourceId = 1,
                SanctionEntryReference = "1",
                SanctionElementValue =
                [
                    .. listName
                        .Split([" "], StringSplitOptions.RemoveEmptyEntries)
                        .Select(SanctionEntryFileImporter.NormalizeElementValue)
                ]
            };

            var entries = new ConcurrentDictionary<int, SanctionEntry>();
            entries.TryAdd(entry.SanctionEntryId, entry);

            var matches = new LevenshteinDistance().CheckMultipartString(screened, 0, entries,
                new ConcurrentDictionary<string, byte>());

            matches.Should().ContainSingle().Which.LevenshteinDistance.Should().Be(0);
        }

        [Fact]
        public async Task ImportAsyncParsesTheUkSanctionsListLayoutUsingTheCorrectedSourceMappingAsync()
        {
            const string csv = "Report Date: 10-Nov-2026\n" +
                               "Last Updated,Unique ID,OFSI Group ID,UN Reference Number,Name 6,Name 1,Name 2,Name 3,Name 4,Name 5,Name type\n" +
                               "01/01/2024,AFG0001,12703,,HAJI KHAIRULLAH MONEY EXCHANGE,,,,,,Primary Name\n" +
                               "02/02/2024,RUS0042,,,PETROV,Ivan,Ivanovich,,,,Primary Name\n";
            using var parser = NewParser(csv);
            var capture = new Capture();

            var result = await SanctionEntryFileImporter.ImportAsync(parser, 1, "4,5,6,7,8,9", 1, 2,
                capture.OnRecordAsync, capture.OnRejectedAsync, noOpLog);

            capture.Rejections.Should().BeEmpty();
            result.TotalRows.Should().Be(2);
            capture.Records.Select(r => r.Reference).Should().Equal("AFG0001", "RUS0042");
            SanctionTextFolding.Tokens(capture.Records[0].ElementValue).Should()
                .Equal("haji", "khairullah", "money", "exchange");
            SanctionTextFolding.Tokens(capture.Records[1].ElementValue).Should()
                .Equal("petrov", "ivan", "ivanovich");
        }

        [Fact]
        public async Task ImportAsyncParsesTheOfacAlternateNamesLayoutUsingTheCorrectedSourceMappingAsync()
        {
            const string csv = "36,12,\"aka\",\"AERO-CARIBBEAN\",-0- \n" +
                               "906,46100,\"aka\",\"HAVANA INTERNATIONAL BANK, LTD\",-0- \n";
            using var parser = NewParser(csv);
            var capture = new Capture();

            var result = await SanctionEntryFileImporter.ImportAsync(parser, 1, "3", 0, 0,
                capture.OnRecordAsync, capture.OnRejectedAsync, noOpLog);

            capture.Rejections.Should().BeEmpty();
            result.TotalRows.Should().Be(2);
            capture.Records.Select(r => r.Reference).Should().Equal("36", "906");
            capture.Records.Select(r => r.ElementValue).Should()
                .Equal("AERO-CARIBBEAN", "HAVANA INTERNATIONAL BANK, LTD");
        }

        [Fact]
        public async Task ImportAsyncRejectsTheOfacAlternateNamesLayoutUnderTheOriginalSemicolonMappingAsync()
        {
            using var parser = NewParser("36,12,\"aka\",\"AERO-CARIBBEAN\",-0- \n", ";");
            var capture = new Capture();

            await SanctionEntryFileImporter.ImportAsync(parser, 1, "3", 8, 0,
                capture.OnRecordAsync, capture.OnRejectedAsync, noOpLog);

            capture.Records.Should().BeEmpty();
            capture.Rejections.Should().ContainSingle();
        }

        private const string OpenSanctionsSample =
            "id,schema,name,aliases,birth_date,countries,dataset\n" +
            "Q1,Person,Ivan Petrov,\"Vanya Petrov; I. Petrov;Ivan Petrov\",1970-01-01,ru,pep\n" +
            "Q2,Organization,Acme Holdings,,,cy,sanctions\n" +
            "Q3,Vessel,MV Lucky Star,,,pa,sanctions\n" +
            "Q4,Person,Maria Gomez,,,es,pep\n";

        private static readonly SanctionEntryFileOptions openSanctions =
            new(3, ";", 1, "Person,Organization");

        [Fact]
        public async Task ImportAsyncLoadsEachAliasAsItsOwnEntryWithTheSameReferenceAsync()
        {
            using var parser = NewParser(OpenSanctionsSample);
            var capture = new Capture();

            await SanctionEntryFileImporter.ImportAsync(parser, 1, "2", 0, 1, openSanctions,
                capture.OnRecordAsync, capture.OnRejectedAsync, noOpLog);

            var ivan = capture.Records.Where(r => r.Reference == "Q1").Select(r => r.ElementValue).ToList();
            ivan.Should().BeEquivalentTo("Ivan Petrov", "Vanya Petrov", "I. Petrov");
            capture.Rejections.Should().BeEmpty();
        }

        [Fact]
        public async Task ImportAsyncSkipsRowsWhoseFilterColumnIsNotAnAcceptedValueAsync()
        {
            using var parser = NewParser(OpenSanctionsSample);
            var capture = new Capture();

            var result = await SanctionEntryFileImporter.ImportAsync(parser, 1, "2", 0, 1, openSanctions,
                capture.OnRecordAsync, capture.OnRejectedAsync, noOpLog);

            capture.Records.Select(r => r.Reference).Distinct().Should().Equal("Q1", "Q2", "Q4");
            capture.Records.Should().NotContain(r => r.ElementValue == "MV Lucky Star");
            capture.Rejections.Should().BeEmpty("a filtered row is not a rejected row");
            result.RejectedRows.Should().Be(0);
            result.TotalRows.Should().Be(4);
        }

        [Fact]
        public async Task ImportAsyncComparesTheFilterIgnoringCaseAndSpacesAsync()
        {
            using var parser = NewParser("1,PERSON,A One\n2,Vessel,B Two\n");
            var capture = new Capture();

            await SanctionEntryFileImporter.ImportAsync(parser, 1, "2", 0, 0,
                new SanctionEntryFileOptions(null, null, 1, " person , company "),
                capture.OnRecordAsync, capture.OnRejectedAsync, noOpLog);

            capture.Records.Should().ContainSingle().Which.Reference.Should().Be("1");
        }

        [Fact]
        public async Task ImportAsyncAliasHashesAreStableAndDifferFromThePrimaryAsync()
        {
            using var first = NewParser(OpenSanctionsSample);
            using var second = NewParser(OpenSanctionsSample);
            var one = new Capture();
            var two = new Capture();

            await SanctionEntryFileImporter.ImportAsync(first, 1, "2", 0, 1, openSanctions, one.OnRecordAsync,
                one.OnRejectedAsync, noOpLog);
            await SanctionEntryFileImporter.ImportAsync(second, 1, "2", 0, 1, openSanctions, two.OnRecordAsync,
                two.OnRejectedAsync, noOpLog);

            one.Records.Select(r => r.Hash).Should().Equal(two.Records.Select(r => r.Hash));
            one.Records.Select(r => r.Hash).Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public async Task ImportAsyncWithAnAliasColumnButNoSeparatorSplitsOnSemicolonsAsync()
        {
            using var parser = NewParser("Q9,Person,Ana Silva,\"Ana S.;A. Silva\"\n");
            var capture = new Capture();

            await SanctionEntryFileImporter.ImportAsync(parser, 1, "2", 0, 0, new SanctionEntryFileOptions(3),
                capture.OnRecordAsync, capture.OnRejectedAsync, noOpLog);

            capture.Records.Select(r => r.ElementValue).Should().BeEquivalentTo("Ana Silva", "Ana S.", "A. Silva");
        }

        [Fact]
        public async Task ImportAsyncAliasedEntriesStillMatchAtQueryTimeAsync()
        {
            using var parser = NewParser(OpenSanctionsSample);
            var capture = new Capture();
            await SanctionEntryFileImporter.ImportAsync(parser, 1, "2", 0, 1, openSanctions, capture.OnRecordAsync,
                capture.OnRejectedAsync, noOpLog);

            var entries = new ConcurrentDictionary<int, SanctionEntry>();
            var id = 1;

            foreach (var record in capture.Records)
            {
                entries.TryAdd(id, new SanctionEntry
                {
                    SanctionEntryId = id++,
                    SanctionEntrySourceId = 1,
                    SanctionEntryReference = record.Reference,
                    SanctionElementValue =
                    [
                        .. record.ElementValue.Split([" "], StringSplitOptions.RemoveEmptyEntries)
                            .Select(SanctionEntryFileImporter.NormalizeElementValue)
                    ]
                });
            }

            var matches = new LevenshteinDistance().CheckMultipartString("Vanya Petrov", 0, entries,
                new ConcurrentDictionary<string, byte>());

            matches.Should().NotBeEmpty();
            matches.Should().OnlyContain(m => m.SanctionEntry.SanctionEntryReference == "Q1",
                "every entry that matches an alias of Ivan Petrov is Ivan Petrov");
            matches.Should().Contain(m => m.SanctionEntry.SanctionElementValue.SequenceEqual(new[] { "vanya", "petrov" }));
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