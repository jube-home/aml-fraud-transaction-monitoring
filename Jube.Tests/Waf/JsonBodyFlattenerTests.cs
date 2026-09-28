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

using System.Linq;
using FluentAssertions;
using Jube.App.Code.Waf;
using Xunit;

namespace Jube.Test.Waf
{
    [Trait("Category", "Unit")]
    public sealed class JsonBodyFlattenerTests
    {
        [Fact]
        public void StringValuesAreFlattenedWithTheirPath()
        {
            var values = JsonBodyFlattener.Flatten("{\"name\":\"acme\",\"amount\":100}");

            values.Should().Contain(v => v.Value == "acme");
            values.Should().Contain(v => v.Value == "100");
        }

        [Fact]
        public void NestedObjectsAndArraysAreTraversed()
        {
            const string json = "{\"a\":{\"b\":\"deep\"},\"c\":[\"x\",\"y\"]}";

            var values = JsonBodyFlattener.Flatten(json);

            values.Should().Contain(v => v.Value == "deep");
            values.Should().Contain(v => v.Value == "x");
            values.Should().Contain(v => v.Value == "y");
        }

        [Fact]
        public void ObjectKeysAreThemselvesInspectableFields()
        {
            var values = JsonBodyFlattener.Flatten("{\"<script>\":\"v\"}");

            values.Should().Contain(v => v.Value == "<script>");
        }

        [Fact]
        public void NullValuesAreSkipped()
        {
            var values = JsonBodyFlattener.Flatten("{\"a\":null}");

            values.Should().NotContain(v => v.Field == "$.a" && v.Value == null);
        }

        [Fact]
        public void MalformedJsonFallsBackToInspectingTheRawBody()
        {
            var values = JsonBodyFlattener.Flatten("{not valid json <script>");

            values.Should().ContainSingle();
            values[0].Value.Should().Contain("<script>");
        }

        [Fact]
        public void EmptyOrWhitespaceInputYieldsNothing()
        {
            JsonBodyFlattener.Flatten("").Should().BeEmpty();
            JsonBodyFlattener.Flatten("   ").Should().BeEmpty();
        }

        [Fact]
        public void TheNumberOfValuesIsCapped()
        {
            var items = string.Join(",", Enumerable.Range(0, 5000).Select(i => $"\"v{i}\""));
            var json = "[" + items + "]";

            var values = JsonBodyFlattener.Flatten(json, maxValues: 100);

            values.Count.Should().BeLessThanOrEqualTo(100);
        }

        [Fact]
        public void ExcessivelyDeepJsonDoesNotThrowAndIsBounded()
        {
            var json = string.Concat(Enumerable.Repeat("{\"a\":", 200)) + "\"x\"" + new string('}', 200);

            var act = () => JsonBodyFlattener.Flatten(json, maxDepth: 32);

            act.Should().NotThrow();
        }
    }
}