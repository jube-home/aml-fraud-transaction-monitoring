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

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Engine.Exhaustive.Utilities;
using Xunit;

namespace Jube.Test.Exhaustive
{
    public class MockVariableListTests
    {
        [Fact]
        public async Task MockVariables_MatchTheCsvFeatureColumnsOnceEach_Async()
        {
            var header = (await File.ReadAllLinesAsync("Exhaustive/Mock.csv")).First().Split(',')
                .Where(c => c != "Dependent").ToArray();

            var json = "{\"abstraction\":{" + string.Join(",", header.Select((h, i) => $"\"{h}\":{i + 1}")) + "}}";

            var result = await Extraction.ProcessJsonAsync(null!, 0, 0, true, [json]);
            var variables = result.Item1;
            var rows = result.Item2;

            variables.Values.Select(v => v.Name).Should()
                .BeEquivalentTo(header.Select(h => "Abstraction." + h), "each CSV column is exactly one variable");

            variables.Keys.Should().Equal(Enumerable.Range(0, header.Length));

            rows.Should().HaveCount(1);
            rows[0].Should().Equal(header.Select((_, i) => (double)(i + 1)));
        }
    }
}