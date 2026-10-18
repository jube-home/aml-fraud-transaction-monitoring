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

using FluentAssertions;
using Jube.AskJooby.Observability;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class TokenBudgetCacheWeightingTests
    {
        [Fact]
        public void ACachedPrefixIsChargedAtItsWeightRatherThanInFull()
        {
            var budget = new TokenBudget(1000000, cachedInputWeight: 0.1);

            budget.Record(10000, 100, 9000);

            budget.Spent.Should().Be(2000,
                "a thousand uncached input and nine thousand cached at a tenth is nineteen hundred, "
                + "plus the hundred output that is never cached");
        }

        [Fact]
        public void NothingReportedAsCachedIsStillChargedInFull()
        {
            var budget = new TokenBudget(1000000, cachedInputWeight: 0.1);

            budget.Record(10000, 100);

            budget.Spent.Should().Be(10100,
                "a provider that reports no cache is billing for the whole prefix, so the budget must "
                + "charge for the whole prefix");
        }

        [Fact]
        public void AWeightOfOneChargesCachedAndUncachedAlike()
        {
            var budget = new TokenBudget(1000000);

            budget.Record(10000, 100, 9000);

            budget.Spent.Should().Be(10100,
                "the default weight makes no assumption about a provider's pricing");
        }

        [Fact]
        public void AReportedCacheLargerThanTheInputIsNotPriced()
        {
            var budget = new TokenBudget(1000000, cachedInputWeight: 0.1);

            budget.Record(1000, 50, 5000);

            budget.Spent.Should().Be(1050,
                "a cached count exceeding the input describes nothing that can be priced, so the "
                + "input is charged in full rather than discounted on a figure that cannot be right");
        }

        [Fact]
        public void TheDiscountDelaysExhaustionRatherThanRemovingIt()
        {
            var budget = new TokenBudget(10000, cachedInputWeight: 0.1);

            for (var call = 0; call < 9; call++)
            {
                budget.Record(10000, 0, 10000);
            }

            budget.IsExhausted.Should().BeFalse(
                "nine calls of an entirely cached ten thousand token prefix is nine thousand against "
                + "the limit, which is the point: re-reading what the provider already holds is not "
                + "what the budget is there to stop");

            budget.Record(10000, 0, 10000);

            budget.IsExhausted.Should().BeTrue("a discount is not an exemption");
        }
    }
}