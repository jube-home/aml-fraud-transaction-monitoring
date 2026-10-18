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

using System.Threading.Tasks;
using FluentAssertions;
using Jube.Service.AskJoobyMemory;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class AskJoobySensitiveDataGuardTests
    {
        [Theory]
        [InlineData("token=ghp_16C7e42F292c6912E7710c838347Ae178B4a")]
        [InlineData("The caller's refund goes to 1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa.")]
        [InlineData("The applicant's PAN is ABCDE1234F for verification.")]
        public async Task FindAsync_refuses_a_memory_that_holds_a_secret_or_personal_dataAsync(string content)
        {
            var reason = await AskJoobySensitiveDataGuard.FindAsync(content);

            reason.Should().NotBeNullOrWhiteSpace();
            reason.Should().StartWith("Not remembered:");
        }

        [Theory]
        [InlineData("The velocity accumulation rule resets every 10 minutes.")]
        [InlineData("Counter 12345 was flagged for manual review in the last week.")]
        public async Task FindAsync_lets_an_ordinary_fact_throughAsync(string content)
        {
            var reason = await AskJoobySensitiveDataGuard.FindAsync(content);

            reason.Should().BeNull();
        }
    }
}