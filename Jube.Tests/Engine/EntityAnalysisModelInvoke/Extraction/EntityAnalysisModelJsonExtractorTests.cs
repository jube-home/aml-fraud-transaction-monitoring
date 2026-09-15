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
using FluentAssertions;
using Jube.Cryptography;
using Jube.Engine.EntityAnalysisModelInvoke.Extraction;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Test.Infrastructure;
using Newtonsoft.Json;
using Xunit;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke.Extraction
{
    [Trait("Category", "Unit")]
    public sealed class EntityAnalysisModelJsonExtractorTests
    {
        private const string DemonstrationPayload = """
                                                    {
                                                      "TxnId": "0987654321",
                                                      "AccountId": "Test1",
                                                      "TxnDateTime": "2018-08-19T21:41:37.2470000+00:00",
                                                      "ActionDate": "2019-04-17T01:18:15.0000000+00:00",
                                                      "CurrencyAmount": "123.45",
                                                      "AccountLatitude": "5.3536",
                                                      "AccountLongitude": "36.1408",
                                                      "DebuggerAttached": "True",
                                                      "Is3D": "False",
                                                      "TransactionTypeId": "1000",
                                                      "Email": "please@hash.me",
                                                      "NotANumber": "abc"
                                                    }
                                                    """;

        private static EntityAnalysisModel NewModel(params EntityAnalysisModelRequestXPath[] xPaths)
        {
            var model = new EntityAnalysisModel
            {
                References =
                {
                    EntryXPath = "$.TxnId",
                    EntryName = "TxnId",
                    ReferenceDateXpath = "$.TxnDateTime",
                    ReferenceDateName = "TxnDateTime",
                    ReferenceDatePayloadLocationTypeId = 1,
                    PayloadInitialSize = 16
                }
            };
            model.Collections.EntityAnalysisModelRequestXPaths.AddRange(xPaths);
            return model;
        }

        private static EntityAnalysisModelRequestXPath XPath(string name, string path, int dataTypeId,
            bool reportTable = false, string? defaultValue = null, int encryptionId = 0)
        {
            return new EntityAnalysisModelRequestXPath
            {
                Name = name,
                XPath = path,
                DataTypeId = dataTypeId,
                ReportTable = reportTable,
                DefaultValue = defaultValue,
                EncryptionId = encryptionId
            };
        }

        private static EntityAnalysisModelJsonExtractor NewExtractor(EntityAnalysisModel model)
        {
            return new EntityAnalysisModelJsonExtractor(model, new Dictionary<int, EntityAnalysisModel>(),
                TestDynamicEnvironment.Create(), TestLog.NoOp);
        }

        private static MemoryStream Stream(string json)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }

        [Fact]
        public void TheEntryIsExtractedViaEntryXPathAndAddedToThePayloadUnderEntryName()
        {
            var model = NewModel();
            var context = NewExtractor(model).CreateContext(Stream(DemonstrationPayload));

            context.EntityAnalysisModelInstanceEntryPayload.EntityInstanceEntryId.Should().Be("0987654321");
            context.EntityAnalysisModelInstanceEntryPayload.Payload["TxnId"].AsString().Should().Be("0987654321");
        }

        [Fact]
        public void TheReferenceDateIsExtractedViaReferenceDateXPathAndAddedToThePayload()
        {
            var model = NewModel();
            var context = NewExtractor(model).CreateContext(Stream(DemonstrationPayload));

            context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate.Should()
                .Be(new DateTime(2018, 08, 19, 21, 41, 37, 247, DateTimeKind.Utc));
            context.EntityAnalysisModelInstanceEntryPayload.Payload["TxnDateTime"].AsDateTime().Should()
                .Be(new DateTime(2018, 08, 19, 21, 41, 37, 247, DateTimeKind.Utc));
        }

        [Fact]
        public void WhenReferenceDateLocationTypeIsNowTheCurrentUtcTimeIsUsedInstead()
        {
            var model = NewModel();
            model.References.ReferenceDatePayloadLocationTypeId = 3;
            var before = DateTime.UtcNow;

            var context = NewExtractor(model).CreateContext(Stream(DemonstrationPayload));

            context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate.Should()
                .BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
        }

        [Fact]
        public void AFieldAlreadyPresentInThePayloadFromEntryOrReferenceDateExtractionIsNotReprocessedAsAnXPath()
        {
            var model = NewModel(XPath("TxnId", "$.TxnId", 1, true));

            var context = NewExtractor(model).CreateContext(Stream(DemonstrationPayload));

            context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Should().BeEmpty();
        }

        [Fact]
        public void AStringFieldIsExtractedAndAddedToThePayload()
        {
            var model = NewModel(XPath("AccountId", "$.AccountId", 1));
            var context = NewExtractor(model).CreateContext(Stream(DemonstrationPayload));

            context.EntityAnalysisModelInstanceEntryPayload.Payload["AccountId"].AsString().Should().Be("Test1");
        }

        [Fact]
        public void AFloatFieldIsExtractedAsADoubleAndReportedToTheArchiveKeys()
        {
            var model = NewModel(XPath("CurrencyAmount", "$.CurrencyAmount", 3, true));
            var context = NewExtractor(model).CreateContext(Stream(DemonstrationPayload));

            context.EntityAnalysisModelInstanceEntryPayload.Payload["CurrencyAmount"].AsDouble().Should().Be(123.45);
            var archiveKey = context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Should().ContainSingle()
                .Subject;
            archiveKey.Key.Should().Be("CurrencyAmount");
            archiveKey.KeyValueFloat.Should().Be(123.45);
        }

        [Theory]
        [InlineData(6)]
        [InlineData(7)]
        public void LatitudeAndLongitudeDataTypesAreExtractedAsDoublesLikeAFloat(int dataTypeId)
        {
            var model = NewModel(XPath("AccountLatitude", "$.AccountLatitude", dataTypeId));
            var context = NewExtractor(model).CreateContext(Stream(DemonstrationPayload));

            context.EntityAnalysisModelInstanceEntryPayload.Payload["AccountLatitude"].AsDouble().Should().Be(5.3536);
        }

        [Fact]
        public void AnIntegerFieldIsExtractedAndReported()
        {
            var model = NewModel(XPath("TransactionTypeId", "$.TransactionTypeId", 2, true));
            var context = NewExtractor(model).CreateContext(Stream(DemonstrationPayload));

            context.EntityAnalysisModelInstanceEntryPayload.Payload["TransactionTypeId"].AsInt().Should().Be(1000);
            var archiveKey = context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Should().ContainSingle()
                .Subject;
            archiveKey.KeyValueInteger.Should().Be(1000);
        }

        [Fact]
        public void AnIntegerFieldThatFailsToParseIsSkippedWithoutThrowing()
        {
            var model = NewModel(XPath("NotANumber", "$.NotANumber", 2));

            var act = () => NewExtractor(model).CreateContext(Stream(DemonstrationPayload));

            act.Should().NotThrow();
            var context = NewExtractor(model).CreateContext(Stream(DemonstrationPayload));
            context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey("NotANumber").Should().BeFalse();
        }

        [Fact]
        public void ADoubleFieldThatFailsToParseIsSkippedWithoutThrowing()
        {
            var model = NewModel(XPath("NotANumber", "$.NotANumber", 3));
            var context = NewExtractor(model).CreateContext(Stream(DemonstrationPayload));

            context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey("NotANumber").Should().BeFalse();
        }

        [Theory]
        [InlineData("True", true)]
        [InlineData("False", false)]
        public void ABooleanFieldIsParsedCaseInsensitively(string raw, bool expected)
        {
            var json = $$"""{ "Flag": "{{raw}}" }""";
            var model = NewModel(XPath("Flag", "$.Flag", 5, true));

            var context = NewExtractor(model).CreateContext(Stream(json));

            context.EntityAnalysisModelInstanceEntryPayload.Payload["Flag"].AsBool().Should().Be(expected);
            context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Should().ContainSingle()
                .Which.KeyValueBoolean.Should().Be((byte)(expected ? 1 : 0));
        }

        [Fact]
        public void ADateFieldIsParsedAndReported()
        {
            var model = NewModel(XPath("ActionDate", "$.ActionDate", 4, true));
            var context = NewExtractor(model).CreateContext(Stream(DemonstrationPayload));

            var expected = new DateTime(2019, 04, 17, 01, 18, 15, DateTimeKind.Utc);
            context.EntityAnalysisModelInstanceEntryPayload.Payload["ActionDate"].AsDateTime().Should().Be(expected);
            context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Should().ContainSingle()
                .Which.KeyValueDate.Should().Be(expected);
        }

        [Fact]
        public void AnUnrecognisedDataTypeIdFallsBackToStringHandling()
        {
            var model = NewModel(XPath("AccountId", "$.AccountId", 99, true));
            var context = NewExtractor(model).CreateContext(Stream(DemonstrationPayload));

            context.EntityAnalysisModelInstanceEntryPayload.Payload["AccountId"].AsString().Should().Be("Test1");
            context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Should().ContainSingle()
                .Which.KeyValueString.Should().Be("Test1");
        }

        [Fact]
        public void AMissingFieldWithADefaultValueFallsBackToTheDefault()
        {
            var model = NewModel(XPath("Missing", "$.DoesNotExist", 1, defaultValue: "fallback-value"));
            var context = NewExtractor(model).CreateContext(Stream(DemonstrationPayload));

            context.EntityAnalysisModelInstanceEntryPayload.Payload["Missing"].AsString().Should()
                .Be("fallback-value");
        }

        [Fact]
        public void AMissingFieldWithNoDefaultValueIsNotAddedToThePayload()
        {
            var model = NewModel(XPath("Missing", "$.DoesNotExist", 1, defaultValue: null));
            var context = NewExtractor(model).CreateContext(Stream(DemonstrationPayload));

            context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey("Missing").Should().BeFalse();
        }

        [Fact]
        public void AMissingDateFieldWithADefaultUsesTheDefaultAsADaysOffsetFromNow()
        {
            var model = NewModel(XPath("Missing", "$.DoesNotExist", 4, defaultValue: "7"));
            var before = DateTime.UtcNow.AddDays(-7).AddSeconds(-5);

            var context = NewExtractor(model).CreateContext(Stream(DemonstrationPayload));

            var value = context.EntityAnalysisModelInstanceEntryPayload.Payload["Missing"].AsDateTime();
            value.Should().BeAfter(before).And.BeBefore(DateTime.UtcNow.AddDays(-7).AddSeconds(5));
        }

        [Fact]
        public void DeterministicEncryptionProducesTheSameCipherTextForTheSamePlainText()
        {
            var modelOne = NewModel(XPath("Email", "$.Email", 1, encryptionId: 1));
            modelOne.Services.AesEncryption = new AesEncryption("unit-test-key");
            var modelTwo = NewModel(XPath("Email", "$.Email", 1, encryptionId: 1));
            modelTwo.Services.AesEncryption = new AesEncryption("unit-test-key");

            var contextOne = NewExtractor(modelOne).CreateContext(Stream(DemonstrationPayload));
            var contextTwo = NewExtractor(modelTwo).CreateContext(Stream(DemonstrationPayload));

            var encryptedOne = contextOne.EntityAnalysisModelInstanceEntryPayload.Payload["Email"].AsString();
            var encryptedTwo = contextTwo.EntityAnalysisModelInstanceEntryPayload.Payload["Email"].AsString();

            encryptedOne.Should().NotBe("please@hash.me");
            encryptedOne.Should().Be(encryptedTwo);
        }

        [Fact]
        public void RandomEncryptionProducesDifferentCipherTextOnEachExtraction()
        {
            var model = NewModel(XPath("Email", "$.Email", 1, encryptionId: 2));
            model.Services.AesEncryption = new AesEncryption("unit-test-key");

            var contextOne = NewExtractor(model).CreateContext(Stream(DemonstrationPayload));
            var contextTwo = NewExtractor(model).CreateContext(Stream(DemonstrationPayload));

            var encryptedOne = contextOne.EntityAnalysisModelInstanceEntryPayload.Payload["Email"].AsString();
            var encryptedTwo = contextTwo.EntityAnalysisModelInstanceEntryPayload.Payload["Email"].AsString();

            encryptedOne.Should().NotBe(encryptedTwo);
        }

        [Fact]
        public void AStringFieldResolvesAConfiguredKvpDictionaryValueIntoTheDictionaryCache()
        {
            var model = NewModel(XPath("AccountId", "$.AccountId", 1));
            model.Dependencies.KvpDictionaries[1] = new EntityAnalysisModelDictionary
            {
                Name = "AccountRisk",
                DataName = "AccountId",
                KvPs =
                {
                    ["Test1"] = 42
                }
            };

            var context = NewExtractor(model).CreateContext(Stream(DemonstrationPayload));

            context.EntityAnalysisModelInstanceEntryPayload.Dictionary["AccountRisk"].Should().Be(42);
        }

        [Fact]
        public void AnEmptyInputStreamProducesANullJObjectAndDoesNotThrow()
        {
            var model = NewModel(XPath("AccountId", "$.AccountId", 1));

            var act = () => NewExtractor(model).CreateContext(new MemoryStream());

            act.Should().NotThrow();
            var context = NewExtractor(model).CreateContext(new MemoryStream());
            context.EntityAnalysisModelInstanceEntryPayload.JObject.Should().BeNull();
            context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey("AccountId").Should().BeFalse();
        }

        [Fact]
        public void MalformedJsonPropagatesAJsonReaderException()
        {
            var model = NewModel();

            var act = () => NewExtractor(model).CreateContext(Stream("{ not valid json"));

            act.Should().Throw<JsonReaderException>();
        }

        [Fact]
        public void TheReturnedContextCarriesTheModelAvailableModelsAndAsyncFlag()
        {
            var model = NewModel();
            var availableModels = new Dictionary<int, EntityAnalysisModel> { [1] = model };
            var extractor = new EntityAnalysisModelJsonExtractor(model, availableModels,
                TestDynamicEnvironment.Create(), TestLog.NoOp);

            var context = extractor.CreateContext(Stream(DemonstrationPayload));

            context.EntityAnalysisModel.Should().BeSameAs(model);
            context.AvailableEntityAnalysisModels.Should().BeSameAs(availableModels);
            context.Async.Should().BeFalse();
        }
    }
}