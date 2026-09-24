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
using System.Linq;
using System.Text;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation;
using Jube.Engine.Helpers;
using Jube.HttpAdaptationProtocol;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke.Simulation
{
    [Trait("Category", "Unit")]
    public sealed class InvocationContextBuilderTests
    {
        private static readonly DateTime now = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);

        private static readonly InvocationContextField[] fields =
        [
            new("Payload.Amount", "Payload", "double"),
            new("Payload.Country", "Payload", "string"),
            new("Payload.Count", "Payload", "integer"),
            new("Payload.Opened", "Payload", "datetime"),
            new("Payload.Card", "Payload", "string"),
            new("TTLCounter.PerAccount", "TTLCounter", "double"),
            new("Abstraction.CountLastHour", "Abstraction", "double"),
            new("Dictionary.Limit", "Reference", "double"),
            new("HTTPAdaptation.Score", "Adaptation", "double"),
            new("Activation.HighValue", "Activation", "boolean"),
            new("Activation.Unmatched", "Activation", "boolean")
        ];

        private static readonly RequestField[] requestFields =
        [
            new("Amount", "$.amount", 3, "", false),
            new("Country", "$.country", 1, "GB", false),
            new("Count", "$.count", 2, "", false),
            new("Opened", "$.opened", 4, "30", false),
            new("Card", "$.card", 1, "", true)
        ];

        private static readonly RequestReferences references = new("$.id", "$.at", 1);

        private static InvocationContext FromJson(string json)
        {
            return InvocationContextBuilder.FromRequestJson(7, JObject.Parse(json), fields, requestFields, references,
                false, now);
        }

        [Fact]
        public void RequestJsonFieldsAreExtractedTypedWithTheEnginesDefaults()
        {
            var context =
                FromJson(
                    "{\"id\":\"TX1\",\"at\":\"2026-09-01T10:00:00Z\",\"amount\":\"12.5\",\"count\":\"x\",\"card\":\"4111\"}");

            context.EntryId.Should().Be("TX1");
            context.ReferenceDate.Should().Be(new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc));
            context.Values["Payload.Amount"].Should().Match<InvocationValue>(v =>
                Math.Abs((double)v.Value - 12.5) < 0.0001 && v.Origin == InvocationValueOrigin.Extracted);
            context.Values["Payload.Country"].Should().Match<InvocationValue>(v =>
                (string)v.Value == "GB" && v.Origin == InvocationValueOrigin.Default);
            context.Values["Payload.Opened"].Should().Match<InvocationValue>(v =>
                (DateTime)v.Value == now.AddDays(-30) && v.Origin == InvocationValueOrigin.Default);
            context.Values["Payload.Count"].Should().Match<InvocationValue>(v =>
                v.Value == null && v.Origin == InvocationValueOrigin.Unset && v.Note.Contains("not a valid integer"));
            context.Values["Payload.Card"].Note.Should().Contain("encrypts");
            context.Values["TTLCounter.PerAccount"].Origin.Should().Be(InvocationValueOrigin.Unset);
        }

        [Fact]
        public void OnlyExtractionIsComputedFromRequestJsonAndStorageIsExcluded()
        {
            var context = FromJson("{}");

            context.Stages.Single(s => s.Stage == InvocationContextBuilder.Extraction).Status.Should()
                .Be(InvocationStageStatus.Computed);
            context.Stages.Single(s => s.Stage == InvocationContextBuilder.AbstractionRulesWithSearchKeys).Status
                .Should().Be(InvocationStageStatus.NotComputed);
            context.Stages.Single(s => s.Stage == InvocationContextBuilder.HttpAdaptations).Status.Should()
                .Be(InvocationStageStatus.Excluded);
            context.Stages.Where(s => s.Stage is "CaseCreation" or "Notifications" or "ArchiveStorage"
                    or "TtlCounterIncrements" or "ActivationWatcher")
                .Should().HaveCount(5).And.OnlyContain(s => s.Status == InvocationStageStatus.Excluded);
        }

        [Fact]
        public void TheReferenceDateComesFromTheClockWhenTheModelSaysSo()
        {
            var context = InvocationContextBuilder.FromRequestJson(7, JObject.Parse("{\"at\":\"2020-01-01\"}"),
                fields, requestFields, references with { ReferenceDatePayloadLocationTypeId = 3 }, false, now);

            context.ReferenceDate.Should().Be(now);
        }

        [Fact]
        public void ABlankContextCarriesOnlyTheDefaults()
        {
            var context = InvocationContextBuilder.Blank(7, fields, requestFields, false, now);

            context.Values.Values.Where(v => v.Origin == InvocationValueOrigin.Default).Select(v => v.Field.Name)
                .Should().BeEquivalentTo("Payload.Country", "Payload.Opened");
            context.Stages.Should().Contain(s => s.Status == InvocationStageStatus.NotComputed);
        }

        [Fact]
        public void AnArchivedTransactionReadsBackThroughTheEnginesOwnSerializer()
        {
            var payload = new EntityAnalysisModelInstanceEntryPayload
            {
                Payload = new DictionaryNoBoxing<string>(),
                EntityInstanceEntryId = "TX9",
                ReferenceDate = new DateTime(2026, 9, 2, 8, 30, 0, DateTimeKind.Utc),
                TtlCounter = new PooledDictionary<string, double> { ["PerAccount"] = 3 },
                Abstraction = new PooledDictionary<string, double> { ["CountLastHour"] = 4 },
                Dictionary = new PooledDictionary<string, double> { ["Limit"] = 500 },
                HttpAdaptation = new PooledDictionary<string, Adaptation> { ["Score"] = new() { Value = 0.75 } },
                Activation = new PooledDictionary<string, EntityModelActivationRulePayload>
                    { ["HighValue"] = new() { Visible = true } }
            };
            payload.Payload.TryAdd("Amount", 99.5);
            payload.Payload.TryAdd("Country", "FR");

            var json = BuildJsonResponses.BuildFullJson(payload, new JsonSerializationHelper().ArchiveJsonSerializer);
            var context = InvocationContextBuilder.FromArchive(7, Guid.NewGuid(),
                JObject.Parse(Encoding.UTF8.GetString(json)), fields);

            context.EntryId.Should().Be("TX9");
            context.ReferenceDate.Should().Be(payload.ReferenceDate);
            context.Values.ToDictionary(k => k.Key, v => v.Value.Value).Should().Contain(new Dictionary<string, object>
            {
                ["Payload.Amount"] = 99.5, ["Payload.Country"] = "FR", ["TTLCounter.PerAccount"] = 3d,
                ["Abstraction.CountLastHour"] = 4d, ["Dictionary.Limit"] = 500d, ["HTTPAdaptation.Score"] = 0.75,
                ["Activation.HighValue"] = true, ["Activation.Unmatched"] = false
            });
            context.Stages.Where(s => s.Status != InvocationStageStatus.Excluded).Should()
                .OnlyContain(s => s.Status == InvocationStageStatus.FromArchive);
        }

        [Fact]
        public void TheArchiveStoresDictionaryValuesUnderDictionaryNotKvp()
        {
            var payload = new EntityAnalysisModelInstanceEntryPayload
            {
                Payload = new DictionaryNoBoxing<string>(),
                Dictionary = new PooledDictionary<string, double> { ["Limit"] = 500 }
            };

            var json = JObject.Parse(Encoding.UTF8.GetString(
                BuildJsonResponses.BuildFullJson(payload, new JsonSerializationHelper().ArchiveJsonSerializer)));

            json["dictionary"]?["Limit"]?.Value<double>().Should().Be(500);
            json["kvp"].Should().BeNull();
        }

        [Fact]
        public void AnOverlaySetsTypedValuesAndReportsUnknownNamesAndBadValues()
        {
            var context = FromJson("{}");

            var errors = InvocationContextBuilder.Overlay(context, new Dictionary<string, string?>
            {
                ["Abstraction.CountLastHour"] = "6",
                ["Payload.Opened"] = "2026-01-02T03:04:05Z",
                ["Payload.Count"] = "not a number",
                ["Payload.Nope"] = "1"
            });

            errors.Should().HaveCount(2).And.Contain(e => e.Contains("Payload.Nope"))
                .And.Contain(e => e.Contains("valid integer"));
            context.Values["Abstraction.CountLastHour"].Should().Match<InvocationValue>(v =>
                Math.Abs((double)v.Value - 6) < 0.0001 && v.Origin == InvocationValueOrigin.Overlay);
            context.Values["Payload.Opened"].Value.Should().Be(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        }

        [Theory]
        [InlineData("boolean", "1", true)]
        [InlineData("boolean", "false", false)]
        [InlineData("integer", "42", 42)]
        [InlineData("double", "1.5", 1.5)]
        [InlineData("string", "x", "x")]
        public void TextConvertsByCompletionDataType(string dataType, string text, object expected)
        {
            InvocationContextBuilder.TryConvertText(dataType, text, out var value).Should().BeTrue();
            value.Should().Be(expected);
        }

        [Fact]
        public void ValuesFormatInvariantly()
        {
            InvocationContextBuilder.FormatValue(1.5).Should().Be("1.5");
            InvocationContextBuilder.FormatValue(true).Should().Be("true");
            InvocationContextBuilder.FormatValue(new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)).Should()
                .Be("2026-01-02T00:00:00.0000000Z");
        }
    }
}