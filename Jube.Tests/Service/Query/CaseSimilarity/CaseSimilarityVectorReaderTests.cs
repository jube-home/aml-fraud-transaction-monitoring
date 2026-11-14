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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.CaseRisk.Similarity;
using Jube.Data.Query.ArchiveKeyLookup;
using Jube.Dto.Options;
using Jube.Service.Query.CaseSimilarity;
using Jube.Test.Infrastructure;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Service.Query.CaseSimilarity
{
    [Trait("Category", "Unit")]
    public sealed class CaseSimilarityVectorReaderTests
    {
        private static readonly SimilaritySettings settings = new();

        private static readonly Dictionary<string, ArchiveKeyField> fields = new()
        {
            ["Device"] = Field("Device", 1, true),
            ["Channel"] = Field("Channel", 1, false),
            ["Reference"] = Field("Reference", 2, true),
            ["When"] = Field("When", 4, false),
            ["Latitude"] = Field("Latitude", 6, false)
        };

        private static ArchiveKeyField Field(string name, int dataTypeId, bool archiveKey) =>
            new(ArchiveKeySourceKind.EntityAnalysisModelRequestXPath, System.Guid.NewGuid(), name, dataTypeId,
                ArchiveKeyPredicate.ColumnOf(dataTypeId), 1, "payload", archiveKey, null);

        private static string Plain(string field, string value) => field + "|" + value;

        private static List<string> Read(CaseSimilarityKind kind, params JObject[] rows) =>
        [
            .. CaseSimilarityVectorReader.Read(rows, fields, Plain, settings)
                .Where(component => component.Kind == kind).Select(component => component.Value)
        ];

        private static JObject Row(JObject payload, params string[] rules)
        {
            var activation = new JObject();

            foreach (var rule in rules)
            {
                activation[rule] = new JObject { ["visible"] = true };
            }

            return new JObject { ["payload"] = payload, ["activation"] = activation };
        }

        [Fact]
        public void EveryRuleThatFiredOnAnyTransactionIsAComponentAndAHiddenOneIsNot()
        {
            var second = Row(new JObject(), "Velocity");
            ((JObject)second["activation"]!)["Silent"] = new JObject { ["visible"] = false };

            Read(CaseSimilarityKind.Rule, Row(new JObject(), "Structuring"), second)
                .Should().Equal("Structuring", "Velocity");
        }

        [Fact]
        public void AnArchiveKeyTextFieldIsAnEntityAndAnyOtherTextFieldIsACategory()
        {
            var row = Row(new JObject { ["Device"] = "D1", ["Channel"] = "Card", ["Unknown"] = "X", ["Flag"] = true });

            Read(CaseSimilarityKind.Entity, row).Should().Equal("Device=Device|D1");
            Read(CaseSimilarityKind.Category, row)
                .Should().Equal("Channel=Channel|Card", "Flag=Flag|True", "Unknown=Unknown|X");
        }

        [Fact]
        public void AWholeNumberThatIsAnArchiveKeyIsAnEntityNotAMeasure()
        {
            var row = Row(new JObject { ["Reference"] = 778 });

            Read(CaseSimilarityKind.Entity, row).Should().Equal("Reference=Reference|778");
            Read(CaseSimilarityKind.Measure, row).Should().BeEmpty();
        }

        [Fact]
        public void ANumberIsAggregatedAcrossEveryTransaction()
        {
            Read(CaseSimilarityKind.Measure, Row(new JObject { ["Amount"] = 600 }),
                    Row(new JObject { ["Amount"] = 30 }), Row(new JObject { ["Amount"] = 3000.0 }))
                .Should().Equal("Amount:max=+e3.0", "Amount:mean=+e3.0", "Amount:sum=+e3.5");
        }

        [Fact]
        public void DatesAndCoordinatesAndEmptyValuesContributeNothing()
        {
            var row = Row(new JObject
            {
                ["When"] = "2026-10-31T13:49:00Z", ["Latitude"] = 30.04, ["Channel"] = "", ["Missing"] = null
            });

            CaseSimilarityVectorReader.Read([row], fields, Plain, settings).Should().BeEmpty();
        }

        [Fact]
        public void AFieldThatDiffersOnEveryTransactionIsLeftOut()
        {
            var rows = Enumerable.Range(0, settings.MaximumValuesPerField + 1)
                .Select(index => Row(new JObject { ["Device"] = "D" + index, ["Channel"] = "Card" })).ToArray();

            Read(CaseSimilarityKind.Entity, rows).Should().BeEmpty();
            Read(CaseSimilarityKind.Category, rows).Should().Equal("Channel=Channel|Card");
        }

        [Fact]
        public void AModelScoreIsItsHighestAcrossTransactionsAndFeaturesComeFromTheLatestOnly()
        {
            var latest = Row(new JObject());
            latest["exhaustiveAdaptation"] = new JObject { ["Fraud"] = 0.2 };
            latest["abstraction"] = new JObject { ["Spend30d"] = 4000 };
            latest["ttlCounter"] = new JObject { ["Declines"] = 3 };

            var earlier = Row(new JObject());
            earlier["exhaustiveAdaptation"] = new JObject { ["Fraud"] = 0.9 };
            earlier["abstraction"] = new JObject { ["Spend30d"] = 10 };

            Read(CaseSimilarityKind.Model, latest, earlier).Should().Equal("Fraud:max=q3");
            Read(CaseSimilarityKind.Feature, latest, earlier)
                .Should().Equal("abstraction:Spend30d=+e3.5", "ttlCounter:Declines=+e0.0");
        }

        [Fact]
        public void TheVectorIsCappedAndOverlongComponentsAreDropped()
        {
            var many = Enumerable.Range(0, settings.MaximumComponents + 50)
                .Select(index => new SimilarityComponent(CaseSimilarityKind.Rule, "R" + index.ToString("0000")))
                .Append(new SimilarityComponent(CaseSimilarityKind.Tag, new string('x', 301)));

            var bounded = CaseSimilarityVectorReader.Bound(many, settings);

            bounded.Should().HaveCount(settings.MaximumComponents);
            bounded.Should().OnlyContain(component => component.Kind == CaseSimilarityKind.Rule);
        }

        [Theory]
        [InlineData("a-long-random-value", "a-long-random-value")]
        [InlineData("", null)]
        [InlineData("   ", null)]
        [InlineData("[@CASE_SIMILARITY_HASHING_KEY@]", null)]
        public void TheHashingKeyIsItsOwnSettingAndAnEmptyOrUnresolvedOneCountsAsNotSet(string configured,
            string? expected)
        {
            var environment = TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                [CaseSimilarityHash.Setting] = configured,
                ["PasswordHashingKey"] = "a-different-key"
            });

            CaseSimilarityHash.KeyOf(environment).Should().Be(expected);
        }

        [Fact]
        public void TheHashIsStableDiffersByFieldAndBySecretAndNeverContainsTheValue()
        {
            var hash = CaseSimilarityHash.For("secret");

            hash("Device", "DEV-8831A").Should().Be(hash("Device", "DEV-8831A")).And.HaveLength(32);
            hash("Device", "DEV-8831A").Should().NotBe(hash("Email", "DEV-8831A"));
            hash("Device", "DEV-8831A").Should().NotBe(CaseSimilarityHash.For("other")("Device", "DEV-8831A"));
            hash("Device", "DEV-8831A").Should().NotContain("8831");
        }

        [Theory]
        [InlineData(CaseSimilarityKind.Rule, "Structuring 9d", "Structuring 9d")]
        [InlineData(CaseSimilarityKind.Tag, "Mule", "Mule")]
        [InlineData(CaseSimilarityKind.Model, "Fraud:max=q3", "Fraud")]
        [InlineData(CaseSimilarityKind.Measure, "Amount:sum=+e3.5", "Amount")]
        [InlineData(CaseSimilarityKind.Feature, "abstraction:Spend30d=+e3.5", "Spend30d")]
        [InlineData(CaseSimilarityKind.Entity, "Device=0A1B", "Device")]
        [InlineData(CaseSimilarityKind.Category, "Channel=0A1B", "Channel")]
        [InlineData(CaseSimilarityKind.Band, "Value=+e4.5", "Value")]
        public void AReasonNamesTheRuleOrFieldAndNeverItsValue(CaseSimilarityKind kind, string component,
            string name)
        {
            CaseSimilarityReasons.NameOf(new SimilarityComponent(kind, component)).Should().Be(name);
        }
    }
}
