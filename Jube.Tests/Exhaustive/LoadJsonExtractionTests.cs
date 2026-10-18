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
using Jube.Engine.Exhaustive.Algorithms.Models;
using Jube.Engine.Exhaustive.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Exhaustive
{
    public class LoadJsonExtractionTests
    {
        private static Dictionary<int, Variable> Variables(params string[] paths)
        {
            return paths.Select((p, i) => (p, i)).ToDictionary(t => t.i,
                t => new Variable { Name = "V" + t.i, ProcessingTypeId = 5, ValueJsonPath = t.p });
        }

        [Theory]
        [InlineData("{\"abstraction\":{\"A\":12.5}}", "abstraction.A", 12.5)]
        [InlineData("{\"abstraction\":{\"A\":12.5}}", "$.abstraction.A", 12.5)]
        [InlineData("{\"abstraction\":{\"A\":7}}", "abstraction.A", 7)]
        [InlineData("{\"abstraction\":{\"A\":-3}}", "abstraction.A", -3)]
        [InlineData("{\"abstraction\":{\"A\":0}}", "abstraction.A", 0)]
        [InlineData("{\"abstraction\":{\"A\":1e3}}", "abstraction.A", 1000)]
        [InlineData("{\"abstraction\":{\"A\":\"4.25\"}}", "abstraction.A", 4.25)]
        [InlineData("{\"abstraction\":{\"A\":9999999999.5}}", "abstraction.A", 9999999999.5)]
        [InlineData("{\"a\":{\"b\":{\"c\":{\"d\":8}}}}", "a.b.c.d", 8)]
        [InlineData("{\"list\":[3,4,5]}", "list[1]", 4)]
        [InlineData("{\"list\":[{\"v\":6},{\"v\":7}]}", "list[1].v", 7)]
        [InlineData("{\"abstraction\":{\"A B\":11}}", "abstraction['A B']", 11)]
        public void ExtractValue_ReadsTheNumberAtThePath(string json, string path, double expected)
        {
            Extraction.ExtractValue(JObject.Parse(json), path).Should().Be(expected);
        }

        [Theory]
        [InlineData("{\"abstraction\":{}}", "abstraction.A")]
        [InlineData("{}", "abstraction.A")]
        [InlineData("{\"abstraction\":null}", "abstraction.A")]
        [InlineData("{\"abstraction\":{\"A\":null}}", "abstraction.A")]
        [InlineData("{\"abstraction\":{\"A\":\"not a number\"}}", "abstraction.A")]
        [InlineData("{\"abstraction\":{\"A\":\"\"}}", "abstraction.A")]
        [InlineData("{\"abstraction\":{\"A\":{\"nested\":1}}}", "abstraction.A")]
        [InlineData("{\"abstraction\":{\"A\":[1,2]}}", "abstraction.A")]
        [InlineData("{\"abstraction\":{\"A\":5}}", "Abstraction.A")]
        [InlineData("{\"list\":[3]}", "list[9]")]
        [InlineData("{\"abstraction\":{\"A\":5}}", "abstraction.A[")]
        [InlineData("{\"abstraction\":{\"A\":5}}", "")]
        [InlineData("{\"abstraction\":{\"A\":5}}", null)]
        public void ExtractValue_MissingNonNumericOrMalformedPathIsZero(string json, string? path)
        {
            Extraction.ExtractValue(JObject.Parse(json), path!).Should().Be(0);
        }

        [Fact]
        public void ExtractValue_ConsecutiveDotsInThePathAreTolerated()
        {
            Extraction.ExtractValue(JObject.Parse("{\"abstraction\":{\"A\":5}}"), "abstraction..A").Should().Be(5);
        }

        [Fact]
        public void ExtractValue_MissingAndGenuineZeroAreIndistinguishable()
        {
            var present = JObject.Parse("{\"abstraction\":{\"A\":0}}");
            var absent = JObject.Parse("{\"abstraction\":{}}");

            Extraction.ExtractValue(present, "abstraction.A")
                .Should().Be(Extraction.ExtractValue(absent, "abstraction.A"));
        }

        [Fact]
        public void ExtractValue_BooleanAndDateTokensConvertTheWayNewtonsoftConvertsThem()
        {
            var jObject = JObject.Parse("{\"b\":true,\"f\":false,\"d\":\"2026-03-01T10:00:00Z\"}");

            Extraction.ExtractValue(jObject, "b").Should().Be(1);
            Extraction.ExtractValue(jObject, "f").Should().Be(0);
            Extraction.ExtractValue(jObject, "d").Should().Be(0);
        }

        [Fact]
        public void BuildRows_OneRowPerJsonWithOneColumnPerVariableInVariableOrder()
        {
            var variables = Variables("abstraction.B", "abstraction.A", "ttlCounter.T");

            string[] json =
            [
                "{\"abstraction\":{\"A\":1,\"B\":2},\"ttlCounter\":{\"T\":3}}",
                "{\"abstraction\":{\"A\":4,\"B\":5},\"ttlCounter\":{\"T\":6}}"
            ];

            var rows = Extraction.BuildRows(variables, json);

            rows.Should().HaveCount(2);
            rows[0].Should().Equal(2, 1, 3);
            rows[1].Should().Equal(5, 4, 6);
        }

        [Fact]
        public void BuildRows_MissingValuesBecomeZeroWithoutDroppingTheRowOrShorteningIt()
        {
            var variables = Variables("abstraction.A", "abstraction.B", "abstraction.C");

            string[] json =
            [
                "{\"abstraction\":{\"A\":1}}",
                "{\"abstraction\":{\"B\":\"x\"}}",
                "{}",
                "{\"abstraction\":{\"A\":9,\"B\":8,\"C\":7}}"
            ];

            var rows = Extraction.BuildRows(variables, json);

            rows.Should().HaveCount(4);
            rows.Should().AllSatisfy(r => r.Should().HaveCount(3));
            rows[0].Should().Equal(1, 0, 0);
            rows[1].Should().Equal(0, 0, 0);
            rows[2].Should().Equal(0, 0, 0);
            rows[3].Should().Equal(9, 8, 7);
        }

        [Fact]
        public void BuildRows_ExtraJsonFieldsAreIgnored()
        {
            var rows = Extraction.BuildRows(Variables("abstraction.A"),
                ["{\"abstraction\":{\"A\":1,\"Z\":99},\"payload\":{\"Q\":5}}"]);

            rows.Should().ContainSingle().Which.Should().Equal(1);
        }

        [Fact]
        public void BuildRows_EmptyJsonListGivesNoRows()
        {
            Extraction.BuildRows(Variables("abstraction.A"), []).Should().BeEmpty();
        }

        [Fact]
        public void BuildRows_NoVariablesGivesEmptyRowsOnePerJson()
        {
            var rows = Extraction.BuildRows(new Dictionary<int, Variable>(), ["{}", "{}", "{}"]);

            rows.Should().HaveCount(3);
            rows.Should().AllSatisfy(r => r.Should().BeEmpty());
        }

        [Fact]
        public void BuildRows_UnparseableJsonThrowsRatherThanBeingSkipped()
        {
            var act = () => Extraction.BuildRows(Variables("a"), ["{\"a\":1}", "not json"]);

            act.Should().Throw<JsonReaderException>();
        }

        [Fact]
        public void BuildRows_VariableKeysAreReadAsAZeroBasedSequence()
        {
            var variables = new Dictionary<int, Variable>
            {
                [0] = new() { ValueJsonPath = "a" },
                [2] = new() { ValueJsonPath = "b" }
            };

            var act = () => Extraction.BuildRows(variables, ["{\"a\":1,\"b\":2}"]);

            act.Should().Throw<KeyNotFoundException>();
        }

        [Fact]
        public void BuildRows_ManyRowsKeepInputOrder()
        {
            var json = Enumerable.Range(0, 500).Select(i => "{\"a\":" + i + "}").ToArray();

            var rows = Extraction.BuildRows(Variables("a"), json);

            rows.Select(r => r[0]).Should().Equal(Enumerable.Range(0, 500).Select(i => (double)i));
        }
    }
}