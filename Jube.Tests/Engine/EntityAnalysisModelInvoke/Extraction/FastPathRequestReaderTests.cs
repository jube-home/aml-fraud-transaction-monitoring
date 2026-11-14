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
using FluentAssertions;
using Jube.Engine.EntityAnalysisModelInvoke.Extraction;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke.Extraction
{
    [Trait("Category", "Unit")]
    public sealed class FastPathRequestReaderTests
    {
        private const string Document =
            "{\"TxnId\":\"T-1\",\"Amount\":1523.45,\"Whole\":1.0,\"Exp\":1e3,\"Trail\":100.50,\"Int\":42,\"Neg\":-7,\"Big\":12345678901234567890," +
            "\"NegZero\":-0,\"NegZeroReal\":-0.0,\"Tiny\":1e-7,\"Huge\":1e300,\"Flag\":true,\"Off\":false,\"Nothing\":null," +
            "\"When\":\"2026-11-07T10:11:12Z\",\"Text\":\"caf\\u00e9 \\n \\\"q\\\" \\ud83d\\ude00\",\"Empty\":\"\",\"Obj\":{\"a\":{\"b\":[10,{\"c\":\"deep\"},[1,2]]}}," +
            "\"Arr\":[\"p\",\"q\",\"r\"],\"Mixed\":[1,\"x\",null,true],\"payload\":{\"Channel\":\"WEB\",\"Lat\":51.5074},\"A\":9,\"a\":8,\"_u\":1,\"n1\":2}";

        private static readonly string[] Paths =
        [
            "$.TxnId", "$.Amount", "$.Whole", "$.Exp", "$.Trail", "$.Int", "$.Neg", "$.Big", "$.NegZero", "$.NegZeroReal",
            "$.Tiny", "$.Huge", "$.Flag", "$.Off", "$.Nothing", "$.When", "$.Text", "$.Empty", "$.Obj",
            "$.Obj.a", "$.Obj.a.b", "$.Obj.a.b[0]", "$.Obj.a.b[1].c", "$.Obj.a.b[2][1]", "$.Obj.a.b[2]", "$.Obj.a.b[3]",
            "$.Arr[0]", "$.Arr[2]", "$.Arr[3]", "$.Arr[99999]", "$.Arr[1].x", "$.Arr.x", "$.TxnId.x", "$.TxnId[0]", "$.Int.x",
            "$.Mixed[0]", "$.Mixed[1]", "$.Mixed[2]", "$.Mixed[3]", "$.payload.Channel", "$.payload.Lat", "$.payload.Nope",
            "$.A", "$.a", "$.txnid", "$.TXNID", "$._u", "$.n1", "$.Missing", "$.Missing.Deeper", "$.Obj.a.zz.yy"
        ];

        private static readonly string[] NotSimple =
        [
            "a.b", "$", "$.", "$..Int", "$.Arr[*]", "$.Arr[-1]", "$.Arr[0:1]", "$['TxnId']", "$.Arr[?(@ == 'p')]", "$.Arr[00]",
            "$.Arr[1234567]", "$.Arr[ 1]", "$.TxnId ", " $.TxnId", "$.user-id", "$.Obj.a.b[0,1]", "$.1abc"
        ];

        private static string Oracle(byte[] bytes, string path)
        {
            try
            {
                var json = RequestFieldExtraction.Parse(new StreamReader(new MemoryStream(bytes), Encoding.UTF8));

                return json.SelectToken(path)?.ToString();
            }
            catch (Exception ex)
            {
                return "THROWS " + ex.GetType().Name;
            }
        }

        private static bool NewtonsoftParses(byte[] bytes)
        {
            try
            {
                RequestFieldExtraction.Parse(new StreamReader(new MemoryStream(bytes), Encoding.UTF8));

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string Outcome(Func<string> action)
        {
            try
            {
                return action() ?? "<null>";
            }
            catch (Exception ex)
            {
                return "THROWS " + ex.GetType().Name;
            }
        }

        private static FastPathPlan PlanOf(IEnumerable<string> paths)
        {
            return FastPathPlan.TryBuild(paths.Where(FastPathPlan.IsSimplePath));
        }

        private static int AssertAgreement(byte[] bytes, IEnumerable<string> paths)
        {
            var simple = paths.Where(FastPathPlan.IsSimplePath).Distinct().ToList();
            var reader = FastPathRequestReader.TryRead(bytes, PlanOf(simple));

            if (reader == null)
            {
                return 0;
            }

            NewtonsoftParses(bytes).Should().BeTrue("the reader must never accept what the DOM parse rejects: " + Encoding.UTF8.GetString(bytes));

            var answered = 0;

            foreach (var path in simple)
            {
                if (!reader.TrySelect(path, out var value))
                {
                    continue;
                }

                answered++;
                value.Should().Be(Oracle(bytes, path), $"{path} in {Encoding.UTF8.GetString(bytes)}");
            }

            return answered;
        }

        [Theory]
        [InlineData("en-GB")]
        [InlineData("de-DE")]
        [InlineData("fr-FR")]
        [InlineData("tr-TR")]
        [InlineData("ar-SA")]
        [InlineData("ru-RU")]
        [InlineData("")]
        public void EveryCuratedPathAgreesWithNewtonsoftUnderEveryCulture(string culture)
        {
            var original = CultureInfo.CurrentCulture;

            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

                AssertAgreement(Encoding.UTF8.GetBytes(Document), Paths).Should().BeGreaterThan(40);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void TheReaderAnswersEveryLeafShapeIncludingBigNumbersAndNegativeZeroBecauseItUsesNewtonsoftsOwnLexer()
        {
            var reader = FastPathRequestReader.TryRead(Encoding.UTF8.GetBytes(Document), PlanOf(Paths));

            reader.Should().NotBeNull();

            foreach (var path in new[]
                     {
                         "$.TxnId", "$.Amount", "$.Whole", "$.Exp", "$.Int", "$.Neg", "$.Big", "$.NegZero", "$.NegZeroReal", "$.Flag",
                         "$.Nothing", "$.When", "$.Text", "$.Obj.a.b[1].c", "$.Arr[2]", "$.payload.Channel", "$.Missing", "$.Arr[99999]"
                     })
            {
                reader.TrySelect(path, out _).Should().BeTrue(path);
            }
        }

        [Fact]
        public void ContainerValuesAreDeferredToTheDomBecauseTheirTextIsFormattedJson()
        {
            var reader = FastPathRequestReader.TryRead(Encoding.UTF8.GetBytes(Document), PlanOf(Paths));

            foreach (var path in new[] { "$.Obj", "$.Obj.a", "$.Obj.a.b", "$.Obj.a.b[2]" })
            {
                reader.TrySelect(path, out _).Should().BeFalse(path);
            }
        }

        [Fact]
        public void PathsOutsideThePlanAreDeferredToTheDom()
        {
            var reader = FastPathRequestReader.TryRead(Encoding.UTF8.GetBytes(Document), PlanOf(["$.TxnId"]));

            reader.TrySelect("$.TxnId", out _).Should().BeTrue();
            reader.TrySelect("$.Amount", out _).Should().BeFalse();
            reader.TrySelect(null, out _).Should().BeFalse();
        }

        [Fact]
        public void APlanIsOnlyBuiltFromSimpleDottedAndIndexedPaths()
        {
            FastPathPlan.TryBuild(["$.a", "$.b[0].c", null, "", "$.a"]).Should().NotBeNull();
            FastPathPlan.TryBuild([null, ""]).Should().BeNull();
            FastPathPlan.TryBuild([]).Should().BeNull();

            foreach (var path in NotSimple)
            {
                FastPathPlan.TryBuild(["$.a", path]).Should().BeNull($"'{path}'");
                FastPathPlan.IsSimplePath(path).Should().BeFalse($"'{path}'");
            }
        }

        [Fact]
        public void APlanIsRefusedWhenAnObjectWouldNeedMoreThan63TrackedChildren()
        {
            FastPathPlan.TryBuild(Enumerable.Range(0, 63).Select(i => "$.f" + i)).Should().NotBeNull();
            FastPathPlan.TryBuild(Enumerable.Range(0, 64).Select(i => "$.f" + i)).Should().BeNull();
        }

        [Fact]
        public void AModelWithOnlySimplePathsGetsAPlan()
        {
            FastPathEligibility.BuildPlan(["$.a.b", "$.c[0]", null, ""]).Should().NotBeNull();
        }

        [Theory]
        [InlineData("$.a[?(@.b == 1)]")]
        [InlineData("$..a")]
        [InlineData("$.a[*]")]
        [InlineData("a.b")]
        [InlineData("$['a']")]
        public void AModelWithAnyNonSimplePathGetsNoPlan(string path)
        {
            FastPathEligibility.BuildPlan(["$.a", path]).Should().BeNull();
        }

        [Theory]
        [InlineData("{/*c*/\"a\":1}", "1")]
        [InlineData("{\"a\":1,}", "1")]
        [InlineData("{'a':1}", "1")]
        [InlineData("{a:1}", "1")]
        [InlineData("{\"a\":NaN}", "NaN")]
        [InlineData("{\"a\":007}", "7")]
        [InlineData("{\"a\":1} x", "1")]
        [InlineData("{\"a\":1}{\"b\":2}", "1")]
        [InlineData("{\"a\":\"line\nbreak\"}", "line\nbreak")]
        [InlineData("{\"a\":Infinity}", null)]
        [InlineData("{\"a\":undefined}", null)]
        [InlineData("{\"a\":/*c*/2}", "2")]
        [InlineData("{\"a\":[/*c*/1,/*d*/2]}", null)]
        public void LeniencyIsIdenticalBecauseItIsTheSameParser(string json, string expected)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            var reader = FastPathRequestReader.TryRead(bytes, PlanOf(["$.a"]));

            reader.Should().NotBeNull();

            if (reader.TrySelect("$.a", out var value))
            {
                value.Should().Be(Oracle(bytes, "$.a"));

                if (expected != null)
                {
                    value.Should().Be(expected);
                }
            }
        }

        [Theory]
        [InlineData("[1,2]")]
        [InlineData("\"s\"")]
        [InlineData("1")]
        [InlineData("null")]
        [InlineData("{\"a\":1")]
        [InlineData("{\"a\":")]
        [InlineData("")]
        [InlineData("{\"a\" 1}")]
        public void DocumentsThatAreNotAWellFormedObjectNeverProduceAReader(string json)
        {
            FastPathRequestReader.TryRead(Encoding.UTF8.GetBytes(json), PlanOf(["$.a"])).Should().BeNull();
        }

        [Theory]
        [InlineData("{\"a\":1,\"a\":2}")]
        [InlineData("{\"a\":{\"b\":1},\"a\":2}")]
        [InlineData("{\"a\":2,\"a\":{\"b\":1}}")]
        [InlineData("{\"o\":{\"a\":1,\"a\":2}}")]
        [InlineData("{\"o\":[{\"a\":1,\"a\":2}]}")]
        public void ADuplicatedKeyOnATrackedPathNeverProducesAReaderBecauseTheDomIsLastWins(string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);

            FastPathRequestReader.TryRead(bytes, PlanOf(["$.a", "$.a.b", "$.o.a", "$.o[0].a"])).Should().BeNull();
        }

        [Fact]
        public void ADuplicatedKeyThatNoPathTracksIsIrrelevantAndStillAgrees()
        {
            var bytes = Encoding.UTF8.GetBytes("{\"x\":1,\"x\":{\"y\":2},\"a\":\"kept\"}");
            var reader = FastPathRequestReader.TryRead(bytes, PlanOf(["$.a"]));

            reader.Should().NotBeNull();
            reader.TrySelect("$.a", out var value).Should().BeTrue();
            value.Should().Be(Oracle(bytes, "$.a")).And.Be("kept");
        }

        [Theory]
        [InlineData(30)]
        [InlineData(64)]
        [InlineData(65)]
        [InlineData(200)]
        public void NestingDepthIsPolicedByTheSameReaderAndNeverOverrunsTheStack(int depth)
        {
            var bytes = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("{\"a\":", depth)) + "1" + new string('}', depth));
            var reader = FastPathRequestReader.TryRead(bytes, PlanOf(["$.a.a", "$.a"]));

            if (NewtonsoftParses(bytes))
            {
                reader.Should().NotBeNull();
            }
            else
            {
                reader.Should().BeNull();
            }
        }

        [Fact]
        public void SeededFuzzOfRandomDocumentsAndPathsNeverDisagreesWithNewtonsoft()
        {
            var random = new Random(20261107);
            var cultures = new[] { "en-GB", "de-DE", "fr-FR", "tr-TR" };
            var original = CultureInfo.CurrentCulture;
            var answered = 0;

            try
            {
                for (var iteration = 0; iteration < 3000; iteration++)
                {
                    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultures[iteration % cultures.Length]);

                    var root = RandomObject(random, 0);
                    var text = root.ToString(random.Next(2) == 0 ? Newtonsoft.Json.Formatting.None : Newtonsoft.Json.Formatting.Indented);
                    var paths = new List<string>();

                    CollectPaths(root, "$", paths);

                    for (var i = 0; i < 6; i++)
                    {
                        paths.Add("$." + RandomName(random) + (random.Next(3) == 0 ? "[" + random.Next(4) + "]" : string.Empty));
                    }

                    answered += AssertAgreement(Encoding.UTF8.GetBytes(text), paths);
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }

            answered.Should().BeGreaterThan(10000);
        }

        [Fact]
        public void SeededFuzzOfRawTextWithDuplicateKeysCommentsAndStrayWhitespaceNeverDisagreesWithNewtonsoft()
        {
            var random = new Random(424242);
            var answered = 0;
            var dropped = 0;

            for (var iteration = 0; iteration < 6000; iteration++)
            {
                var text = RawObject(random, 0);
                var paths = new List<string>();

                for (var i = 0; i < 14; i++)
                {
                    var path = "$." + RandomName(random);

                    for (var d = random.Next(3); d > 0; d--)
                    {
                        path += random.Next(3) == 0 ? "[" + random.Next(3) + "]" : "." + RandomName(random);
                    }

                    paths.Add(path);
                }

                var bytes = Encoding.UTF8.GetBytes(text);
                var count = AssertAgreement(bytes, paths);

                answered += count;

                if (count == 0 && NewtonsoftParses(bytes))
                {
                    dropped++;
                }
            }

            answered.Should().BeGreaterThan(5000);
            dropped.Should().BeLessThan(6000 * 3 / 4);
        }

        [Fact]
        public void RandomDoubleBitPatternsRenderIdenticallyToNewtonsoft()
        {
            var random = new Random(7);
            var original = CultureInfo.CurrentCulture;

            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

                for (var i = 0; i < 20000; i++)
                {
                    var bits = BitConverter.Int64BitsToDouble(random.NextInt64());

                    if (!double.IsFinite(bits))
                    {
                        continue;
                    }

                    var literal = random.Next(3) switch
                    {
                        0 => bits.ToString("R", CultureInfo.InvariantCulture),
                        1 => bits.ToString("E16", CultureInfo.InvariantCulture),
                        _ => bits.ToString("F" + random.Next(0, 12), CultureInfo.InvariantCulture)
                    };

                    AssertAgreement(Encoding.UTF8.GetBytes("{\"v\":" + literal + "}"), ["$.v"]).Should().Be(1, literal);
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void RandomIntegersAcrossTheInt64BoundaryRenderIdenticallyToNewtonsoft()
        {
            var random = new Random(11);

            for (var i = 0; i < 5000; i++)
            {
                var literal = random.Next(5) switch
                {
                    0 => random.NextInt64().ToString(CultureInfo.InvariantCulture),
                    1 => (-random.NextInt64()).ToString(CultureInfo.InvariantCulture),
                    2 => (long.MaxValue - random.Next(3)).ToString(CultureInfo.InvariantCulture) + (random.Next(2) == 0 ? "0" : string.Empty),
                    3 => "-0",
                    _ => random.Next(-5, 5).ToString(CultureInfo.InvariantCulture)
                };

                AssertAgreement(Encoding.UTF8.GetBytes("{\"v\":" + literal + "}"), ["$.v"]).Should().Be(1, literal);
            }
        }

        [Fact]
        public void TheAccessorSelectsExactlyAsTheOldPathDidIncludingDefaultFallbackAndUnplannedPaths()
        {
            var bytes = Encoding.UTF8.GetBytes(Document);
            var accessor = RequestJsonAccessor.Create(new MemoryStream(bytes), PlanOf(Paths));
            var json = RequestFieldExtraction.Parse(new StreamReader(new MemoryStream(bytes), Encoding.UTF8));

            foreach (var path in Paths.Concat(NotSimple).Concat(["$.Arr[*]", "$..c", "$.Obj.a.b[?(@.c=='deep')].c", null, ""]))
            {
                foreach (var defaultValue in new[] { null, string.Empty, "fallback", "5" })
                {
                    var expected = Outcome(() =>
                    {
                        var e = RequestFieldExtraction.Select(json, path, defaultValue);

                        return $"{e.Value}|{e.DefaultFallback}|{e.Error != null}";
                    });
                    var actual = Outcome(() =>
                    {
                        var a = accessor.Select(path, defaultValue);

                        return $"{a.Value}|{a.DefaultFallback}|{a.Error != null}";
                    });

                    actual.Should().Be(expected, $"{path} default {defaultValue}");
                }
            }
        }

        [Fact]
        public void TheAccessorReferenceDateMatchesTheOldPathForEveryValueShape()
        {
            var bytes = Encoding.UTF8.GetBytes(Document);
            var accessor = RequestJsonAccessor.Create(new MemoryStream(bytes), PlanOf(Paths));
            var json = RequestFieldExtraction.Parse(new StreamReader(new MemoryStream(bytes), Encoding.UTF8));
            var utcNow = new DateTime(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);

            foreach (var path in Paths.Concat(["$.Arr[*]", "$..When"]))
            {
                foreach (var typeId in new[] { 1, 2, 3 })
                {
                    foreach (var assumeLocal in new[] { false, true })
                    {
                        Outcome(() => accessor.ReferenceDate(typeId, path, assumeLocal, utcNow).ToString("O")).Should()
                            .Be(Outcome(() => RequestFieldExtraction.ReferenceDate(json, typeId, path, assumeLocal, utcNow).ToString("O")),
                                $"{path} type {typeId} local {assumeLocal}");
                    }
                }
            }
        }

        [Fact]
        public void TheAccessorEntryValueMatchesTheOldPath()
        {
            var bytes = Encoding.UTF8.GetBytes(Document);
            var accessor = RequestJsonAccessor.Create(new MemoryStream(bytes), PlanOf(Paths));
            var json = RequestFieldExtraction.Parse(new StreamReader(new MemoryStream(bytes), Encoding.UTF8));

            foreach (var path in Paths.Concat(["$.Arr[*]", "$..When"]))
            {
                Outcome(() => accessor.SelectToString(path)).Should().Be(Outcome(() => json.SelectToken(path)?.ToString()), path);
            }
        }

        [Fact]
        public void TheLazyJObjectIsEquivalentToAnEagerParseAndIsOnlyBuiltOnDemand()
        {
            var bytes = Encoding.UTF8.GetBytes(Document);
            var accessor = RequestJsonAccessor.Create(new MemoryStream(bytes), PlanOf(Paths));

            accessor.Select("$.TxnId", null).Value.Should().Be("T-1");

            JToken.DeepEquals(accessor.JObject,
                RequestFieldExtraction.Parse(new StreamReader(new MemoryStream(bytes), Encoding.UTF8))).Should().BeTrue();
            accessor.JObject.Should().BeSameAs(accessor.JObject);
        }

        [Fact]
        public void WithoutAPlanTheAccessorParsesEagerlyExactlyAsBefore()
        {
            var bytes = Encoding.UTF8.GetBytes(Document);
            var eager = RequestJsonAccessor.Create(new MemoryStream(bytes));
            var planned = RequestJsonAccessor.Create(new MemoryStream(bytes), PlanOf(Paths));

            foreach (var path in Paths)
            {
                Outcome(() => eager.Select(path, null).Value).Should().Be(Outcome(() => planned.Select(path, null).Value), path);
            }
        }

        [Fact]
        public void DocumentsWithADuplicatedTrackedKeyFallBackToTheDomAndStillAnswerLastWins()
        {
            var accessor = RequestJsonAccessor.Create(new MemoryStream(Encoding.UTF8.GetBytes("{\"a\":{\"b\":1},\"a\":2}")),
                PlanOf(["$.a.b", "$.a"]));

            accessor.Select("$.a", null).Value.Should().Be("2");
            accessor.Select("$.a.b", null).Value.Should().BeNull();
        }

        [Fact]
        public void MalformedDocumentsStillThrowFromCreateAsBefore()
        {
            var act = () => RequestJsonAccessor.Create(new MemoryStream(Encoding.UTF8.GetBytes("{\"a\":")), PlanOf(["$.a"]));

            act.Should().Throw<Newtonsoft.Json.JsonReaderException>();
        }

        [Fact]
        public void AnEmptyStreamYieldsANullJObjectAndNullSelections()
        {
            var accessor = RequestJsonAccessor.Create(new MemoryStream(), PlanOf(["$.a"]));

            accessor.JObject.Should().BeNull();
            accessor.SelectToString("$.a").Should().BeNull();
            accessor.Select("$.a", "d").Value.Should().Be("d");
        }

        private static string RandomName(Random random)
        {
            return new[] { "a", "b", "id", "Amount", "x_1", "n2", "A", "name" }[random.Next(8)];
        }

        private static string RawValue(Random random, int depth)
        {
            var kind = depth > 3 ? random.Next(9) : random.Next(12);

            return kind switch
            {
                0 => random.Next(-100, 100).ToString(CultureInfo.InvariantCulture),
                1 => (random.NextDouble() * 1000).ToString("R", CultureInfo.InvariantCulture),
                2 => "true",
                3 => "false",
                4 => "null",
                5 => "\"" + new[] { "", "x", "caf\\u00e9", "q\\\"uote", "a\\nb", "2026-11-07T10:11:12Z" }[random.Next(6)] + "\"",
                6 => "1e" + random.Next(-5, 5),
                7 => "12345678901234567890",
                8 => "-0",
                9 or 10 => RawObject(random, depth + 1),
                _ => "[" + string.Join(",", Enumerable.Range(0, random.Next(0, 4)).Select(_ => Pad(random) + RawValue(random, depth + 1))) + "]"
            };
        }

        private static string Pad(Random random)
        {
            return random.Next(6) switch { 0 => " ", 1 => "/*c*/", 2 => "\n\t", _ => string.Empty };
        }

        private static string RawObject(Random random, int depth)
        {
            var members = Enumerable.Range(0, random.Next(0, 6))
                .Select(_ => Pad(random) + "\"" + RandomName(random) + "\"" + Pad(random) + ":" + Pad(random) + RawValue(random, depth + 1));

            return "{" + string.Join(",", members) + (random.Next(15) == 0 ? "," : string.Empty) + Pad(random) + "}";
        }

        private static JObject RandomObject(Random random, int depth)
        {
            var o = new JObject();
            var count = random.Next(0, 6);

            for (var i = 0; i < count; i++)
            {
                o[RandomName(random) + (random.Next(4) == 0 ? i.ToString(CultureInfo.InvariantCulture) : string.Empty)] =
                    RandomToken(random, depth + 1);
            }

            return o;
        }

        private static JToken RandomToken(Random random, int depth)
        {
            var kind = depth > 4 ? random.Next(8) : random.Next(11);

            return kind switch
            {
                0 => new JValue(random.Next(-1000, 1000)),
                1 => new JValue(random.NextInt64()),
                2 => new JValue(BitConverter.Int64BitsToDouble(random.NextInt64()) is var d && double.IsFinite(d) ? d : 1.5),
                3 => new JValue(Math.Round(random.NextDouble() * 1000, random.Next(0, 5))),
                4 => new JValue(random.Next(2) == 0),
                5 => JValue.CreateNull(),
                6 => new JValue(new[] { "", "x", "café", "q\"uote", "line\nbreak", "\U0001F600", "2026-11-07T10:11:12Z", "True", "1.0" }[random.Next(9)]),
                7 => new JValue((double)random.Next(0, 5)),
                8 => RandomObject(random, depth),
                _ => new JArray(Enumerable.Range(0, random.Next(0, 4)).Select(_ => RandomToken(random, depth + 1)))
            };
        }

        private static void CollectPaths(JToken token, string prefix, List<string> paths)
        {
            if (token is JObject o)
            {
                foreach (var p in o.Properties())
                {
                    var path = prefix + "." + p.Name;

                    paths.Add(path);
                    CollectPaths(p.Value, path, paths);
                }
            }
            else if (token is JArray a)
            {
                for (var i = 0; i < a.Count; i++)
                {
                    var path = prefix + "[" + i + "]";

                    paths.Add(path);
                    CollectPaths(a[i], path, paths);
                }

                paths.Add(prefix + "[" + a.Count + "]");
            }
        }
    }
}
