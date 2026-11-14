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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Extraction;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Test.Infrastructure;
using Newtonsoft.Json;
using Xunit;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke.Extraction
{
    [Trait("Category", "Unit")]
    public sealed class EntityAnalysisModelJsonExtractorFastPathParityTests
    {
        private static readonly string[] Literals =
        [
            "42", "-7", "0", "-0", "-0.0", "1", "1.0", "1.5", "1e3", "1E-7", "0.1", "3000000000", "-3000000000",
            "2147483647", "2147483648", "-2147483648", "-2147483649", "9007199254740993", "12345678901234567890",
            "1e300", "123.45", "true", "false", "null", "\"42\"", "\"1.5\"", "\"abc\"", "\"True\"", "\"1\"", "\"\"",
            "\"2026-11-07T10:11:12Z\"", "\"07/11/2026\"", "\" 42 \"", "\"1,5\"", "[1]", "{\"x\":1}", "NaN", "Infinity"
        ];

        private static readonly (string Name, int Type, string DefaultValue)[] Fields =
        [
            ("I", 2, null), ("I2", 2, "5"), ("D", 3, null), ("L", 6, null), ("G", 7, "2.5"), ("B", 5, null), ("B2", 5, "true"),
            ("S", 1, null), ("S2", 1, "dflt"), ("T", 4, null), ("T2", 4, "3"), ("P", 8, null), ("U", 99, null)
        ];

        private static EntityAnalysisModel NewModel(bool eager)
        {
            var model = new EntityAnalysisModel
            {
                Flags = { EnablePayloadJObject = eager },
                References =
                {
                    EntryXPath = "$.TxnId",
                    EntryName = "TxnId",
                    ReferenceDateXpath = "$.TxnDateTime",
                    ReferenceDateName = "TxnDateTime",
                    ReferenceDatePayloadLocationTypeId = 1
                }
            };

            model.PublishSnapshot(new ModelSnapshotBuilder(model.Snapshot)
            {
                EntityAnalysisModelRequestXPaths =
                [
                    .. Fields.Select(f => new EntityAnalysisModelRequestXPath
                    {
                        Name = f.Name,
                        XPath = "$." + f.Name,
                        DataTypeId = f.Type,
                        ReportTable = true,
                        DefaultValue = f.DefaultValue
                    })
                ]
            }.Build());

            return model;
        }

        private static string Extract(EntityAnalysisModel model, string json)
        {
            var extractor = new EntityAnalysisModelJsonExtractor(model, new Dictionary<int, EntityAnalysisModel>(),
                TestDynamicEnvironment.Create(), TestLog.NoOp);

            var context = extractor.CreateContext(new MemoryStream(Encoding.UTF8.GetBytes(json)));
            var payload = context.EntityAnalysisModelInstanceEntryPayload;
            var entries = new List<string>();

            foreach (var kv in payload.Payload)
            {
                var rendered = kv.Value.Type switch
                {
                    InternalValue.ValueType.String => "S:" + kv.Value.AsString(),
                    InternalValue.ValueType.DateTime => "T:" + kv.Value.AsDateTime().ToString("O", CultureInfo.InvariantCulture),
                    InternalValue.ValueType.Bool => "B:" + kv.Value.AsBool(),
                    InternalValue.ValueType.Double => "D:" + BitConverter.DoubleToInt64Bits(kv.Value.AsDouble()),
                    InternalValue.ValueType.Int => "I:" + kv.Value.AsInt(),
                    InternalValue.ValueType.Guid => "G:" + kv.Value.AsGuid(),
                    _ => "N"
                };

                entries.Add(kv.Key + "=" + rendered);
            }

            entries.Sort(StringComparer.Ordinal);

            var keys = payload.ArchiveKeys.Select(k => JsonConvert.SerializeObject(k)).OrderBy(k => k, StringComparer.Ordinal);

            return Normalise(string.Join("|", entries) + "##" + string.Join("|", keys));
        }

        private static string Normalise(string text)
        {
            var now = DateTime.UtcNow;

            text = Regex.Replace(text, "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", "GUID");

            return Regex.Replace(text, @"\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d(\.\d+)?Z", match =>
            {
                var value = DateTime.Parse(match.Value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

                for (var days = 0; days <= 30; days++)
                {
                    if (Math.Abs((value - now.AddDays(-days)).TotalSeconds) < 120)
                    {
                        return "NOW-" + days;
                    }
                }

                return match.Value;
            });
        }

        private static string RandomPayload(Random random)
        {
            var members = new List<string>
            {
                "\"TxnId\":\"T-" + random.Next(1000) + "\"",
                "\"TxnDateTime\":" + (random.Next(4) == 0 ? "\"2026-11-07T10:11:12Z\"" : "\"2026-01-02T03:04:05+01:00\"")
            };

            foreach (var field in Fields)
            {
                if (random.Next(6) == 0)
                {
                    continue;
                }

                members.Add("\"" + field.Name + "\":" + Literals[random.Next(Literals.Length)]);
            }

            return "{" + string.Join(",", members) + "}";
        }

        [Theory]
        [InlineData("en-GB")]
        [InlineData("de-DE")]
        [InlineData("tr-TR")]
        [InlineData("fr-FR")]
        [InlineData("")]
        public void FastPathWithDirectCastsProducesExactlyTheSamePayloadAndArchiveKeysAsTheEagerPath(string culture)
        {
            var original = CultureInfo.CurrentCulture;
            var random = new Random(culture.GetHashCode() ^ 99);
            var fast = NewModel(false);
            var eager = NewModel(true);
            var compared = 0;

            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

                for (var i = 0; i < 1500; i++)
                {
                    var json = RandomPayload(random);

                    Extract(fast, json).Should().Be(Extract(eager, json), json);
                    compared++;
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }

            compared.Should().Be(1500);
        }

        [Fact]
        public void EveryLiteralInEveryFieldTypeMatchesTheEagerPathExhaustively()
        {
            var fast = NewModel(false);
            var eager = NewModel(true);

            foreach (var field in Fields)
            {
                foreach (var literal in Literals)
                {
                    var json = "{\"TxnId\":\"T-1\",\"TxnDateTime\":\"2026-01-02T03:04:05Z\",\"" + field.Name + "\":" + literal + "}";

                    Extract(fast, json).Should().Be(Extract(eager, json), json);
                }
            }
        }

        [Fact]
        public void TheSwitchOnMeansEagerParsingAndNoPlanWhateverThePaths()
        {
            var model = NewModel(true);

            FastPathEligibility.PlanFor(model.Snapshot, model).Should().BeNull();
        }

        [Fact]
        public void TheSwitchOffWithOnlySimplePathsGivesAPlanCoveringEveryRequestEntryAndReferenceDatePath()
        {
            var model = NewModel(false);

            var plan = FastPathEligibility.PlanFor(model.Snapshot, model);

            plan.Should().NotBeNull();
            plan.Should().BeSameAs(FastPathEligibility.PlanFor(model.Snapshot, model));
        }

        [Fact]
        public void TheSwitchOffWithAComplexPathStillParsesEagerly()
        {
            var model = NewModel(false);

            model.PublishSnapshot(new ModelSnapshotBuilder(model.Snapshot)
            {
                EntityAnalysisModelRequestXPaths =
                [
                    new EntityAnalysisModelRequestXPath { Name = "C", XPath = "$.items[?(@.k == 1)].v", DataTypeId = 1 }
                ]
            }.Build());

            FastPathEligibility.PlanFor(model.Snapshot, model).Should().BeNull();
        }

        [Fact]
        public void FlippingTheSwitchAtRuntimeIsHonouredWithoutANewSnapshot()
        {
            var model = NewModel(false);

            FastPathEligibility.PlanFor(model.Snapshot, model).Should().NotBeNull();

            model.Flags.EnablePayloadJObject = true;

            FastPathEligibility.PlanFor(model.Snapshot, model).Should().BeNull();
        }

        [Fact]
        public void TheDirectCastsAreActuallyTakenForTheCommonTypedTokens()
        {
            var plan = FastPathPlan.TryBuild(["$.I", "$.D", "$.B", "$.S"]);
            var reader = FastPathRequestReader.TryRead(
                Encoding.UTF8.GetBytes("{\"I\":42,\"D\":1523.45,\"B\":true,\"S\":\"x\"}"), plan);

            reader.TryGetInt32("$.I", out var i).Should().BeTrue();
            i.Should().Be(42);
            reader.TryGetDouble("$.D", out var d).Should().BeTrue();
            d.Should().Be(1523.45);
            reader.TryGetDouble("$.I", out var di).Should().BeTrue();
            di.Should().Be(42d);
            reader.TryGetBoolean("$.B", out var b).Should().BeTrue();
            b.Should().BeTrue();
            reader.TryGetInt32("$.S", out _).Should().BeFalse();
            reader.TryGetBoolean("$.S", out _).Should().BeFalse();
        }

        [Theory]
        [InlineData("{\"I\":3000000000}", "$.I")]
        [InlineData("{\"I\":-2147483649}", "$.I")]
        [InlineData("{\"I\":1.0}", "$.I")]
        [InlineData("{\"I\":12345678901234567890}", "$.I")]
        [InlineData("{\"I\":\"42\"}", "$.I")]
        [InlineData("{\"I\":null}", "$.I")]
        [InlineData("{\"I\":true}", "$.I")]
        public void ShapesThatAreNotAnExactInt32FallBackToTheStringRoute(string json, string path)
        {
            var reader = FastPathRequestReader.TryRead(Encoding.UTF8.GetBytes(json), FastPathPlan.TryBuild([path]));

            reader.TryGetInt32(path, out _).Should().BeFalse();
        }

        [Theory]
        [InlineData("{\"D\":12345678901234567890}")]
        [InlineData("{\"D\":\"1.5\"}")]
        [InlineData("{\"D\":null}")]
        [InlineData("{\"D\":true}")]
        [InlineData("{\"D\":NaN}")]
        [InlineData("{\"D\":Infinity}")]
        [InlineData("{\"D\":-Infinity}")]
        public void ShapesThatAreNotAnExactFiniteDoubleFallBackToTheStringRoute(string json)
        {
            var reader = FastPathRequestReader.TryRead(Encoding.UTF8.GetBytes(json), FastPathPlan.TryBuild(["$.D"]));

            reader.TryGetDouble("$.D", out _).Should().BeFalse();
        }

        [Theory]
        [InlineData("{\"B\":1}")]
        [InlineData("{\"B\":1.0}")]
        [InlineData("{\"B\":\"true\"}")]
        [InlineData("{\"B\":null}")]
        public void OnlyABooleanTokenIsCastDirectlyToABoolean(string json)
        {
            var reader = FastPathRequestReader.TryRead(Encoding.UTF8.GetBytes(json), FastPathPlan.TryBuild(["$.B"]));

            reader.TryGetBoolean("$.B", out _).Should().BeFalse();
        }

        [Fact]
        public void DoubleCastsEqualTheStringRouteBitForBitAcrossEveryInstalledCulture()
        {
            var random = new Random(31337);
            var original = CultureInfo.CurrentCulture;
            var bytesList = Enumerable.Range(0, 40).Select(_ =>
                    BitConverter.Int64BitsToDouble(random.NextInt64()))
                .Where(double.IsFinite)
                .Select(d => d.ToString("R", CultureInfo.InvariantCulture))
                .Concat(["0", "-0.0", "1.0", "0.1", "1e-320", "1.7976931348623157e308", "9007199254740993", "123456789012345678"])
                .ToList();

            try
            {
                foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
                {
                    CultureInfo.CurrentCulture = culture;

                    foreach (var literal in bytesList)
                    {
                        var bytes = Encoding.UTF8.GetBytes("{\"v\":" + literal + "}");
                        var reader = FastPathRequestReader.TryRead(bytes, FastPathPlan.TryBuild(["$.v"]));

                        if (!reader.TryGetDouble("$.v", out var direct))
                        {
                            continue;
                        }

                        reader.TrySelect("$.v", out var text).Should().BeTrue();

                        RequestFieldExtraction.TryConvert(3, text, false, null, false, DateTime.UtcNow, out var viaString)
                            .Should().BeTrue($"{literal} in {culture.Name}");
                        BitConverter.DoubleToInt64Bits(direct).Should()
                            .Be(BitConverter.DoubleToInt64Bits((double)viaString), $"{literal} in {culture.Name}");
                    }
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void Int32CastsEqualTheStringRouteAcrossEveryInstalledCulture()
        {
            var original = CultureInfo.CurrentCulture;
            var literals = new[] { "0", "-0", "1", "-1", "42", "-42", "2147483647", "-2147483648", "2147483648", "-2147483649", "1000000" };

            try
            {
                foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
                {
                    CultureInfo.CurrentCulture = culture;

                    foreach (var literal in literals)
                    {
                        var reader = FastPathRequestReader.TryRead(Encoding.UTF8.GetBytes("{\"v\":" + literal + "}"),
                            FastPathPlan.TryBuild(["$.v"]));

                        if (!reader.TryGetInt32("$.v", out var direct))
                        {
                            continue;
                        }

                        reader.TrySelect("$.v", out var text).Should().BeTrue();

                        RequestFieldExtraction.TryConvert(2, text, false, null, false, DateTime.UtcNow, out var viaString)
                            .Should().BeTrue($"{literal} in {culture.Name}");
                        direct.Should().Be((int)viaString, $"{literal} in {culture.Name}");
                    }
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }
    }
}
