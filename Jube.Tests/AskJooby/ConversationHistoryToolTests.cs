/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.History;
using Jube.AskJooby.Tools.History;
using Jube.AskJooby.Tools.History.Interfaces;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class ConversationHistoryToolTests
    {
        [Fact]
        public async Task SearchHistoryTruncatesAHoofprintLongerThanFourThousandCharactersAsync()
        {
            var longContent = new string('x', 5000);

            var tool = new ConversationHistoryTool(new FakeHistoryClient([
                new HistoryEntry(1, 100, "Long turn", longContent, false, 0.9)
            ]));

            var result = await tool.SearchHistory("anything");

            result.Should().Contain(new string('x', 4000));
            result.Should().NotContain(new string('x', 4001));
            result.Should().Contain("[...truncated]");
        }

        private sealed class FakeHistoryClient(IReadOnlyList<HistoryEntry> hits) : IConversationHistoryClient
        {
            public Task<IReadOnlyList<HistoryEntry>> SearchAsync(string query, string scope, int topK = 5,
                CancellationToken token = default)
            {
                return Task.FromResult(hits);
            }
        }
    }
}