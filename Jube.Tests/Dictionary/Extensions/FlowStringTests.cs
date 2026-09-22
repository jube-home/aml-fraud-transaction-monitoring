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
using Jube.Dictionary.Extensions;
using Xunit;

namespace Jube.Test.Dictionary.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class FlowStringTests
    {
        [Theory]
        [InlineData(null, true)]
        [InlineData("", false)]
        [InlineData("a", false)]
        public void IsNullAppliesTheOutcomeOfEveryStepKind(string? value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsNull(),
                start.RejectIsNull(),
                start.BreakIsNull(),
                start.RequireIsNull(),
                start.EnsureIsNull());

            FlowAssert.Kinds(expected,
                value.MatchIsNull(),
                value.RejectIsNull(),
                value.BreakIsNull(),
                value.RequireIsNull(),
                value.EnsureIsNull());
        }

        [Theory]
        [InlineData("", true)]
        [InlineData(null, false)]
        [InlineData("a", false)]
        public void IsEmptyAppliesTheOutcomeOfEveryStepKind(string? value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsEmpty(),
                start.RejectIsEmpty(),
                start.BreakIsEmpty(),
                start.RequireIsEmpty(),
                start.EnsureIsEmpty());

            FlowAssert.Kinds(expected,
                value.MatchIsEmpty(),
                value.RejectIsEmpty(),
                value.BreakIsEmpty(),
                value.RequireIsEmpty(),
                value.EnsureIsEmpty());
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData(" ", false)]
        [InlineData("a", false)]
        public void IsNullOrEmptyAppliesTheOutcomeOfEveryStepKind(string? value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsNullOrEmpty(),
                start.RejectIsNullOrEmpty(),
                start.BreakIsNullOrEmpty(),
                start.RequireIsNullOrEmpty(),
                start.EnsureIsNullOrEmpty());

            FlowAssert.Kinds(expected,
                value.MatchIsNullOrEmpty(),
                value.RejectIsNullOrEmpty(),
                value.BreakIsNullOrEmpty(),
                value.RequireIsNullOrEmpty(),
                value.EnsureIsNullOrEmpty());
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("  ", true)]
        [InlineData("a", false)]
        public void IsNullOrWhiteSpaceAppliesTheOutcomeOfEveryStepKind(string? value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsNullOrWhiteSpace(),
                start.RejectIsNullOrWhiteSpace(),
                start.BreakIsNullOrWhiteSpace(),
                start.RequireIsNullOrWhiteSpace(),
                start.EnsureIsNullOrWhiteSpace());

            FlowAssert.Kinds(expected,
                value.MatchIsNullOrWhiteSpace(),
                value.RejectIsNullOrWhiteSpace(),
                value.BreakIsNullOrWhiteSpace(),
                value.RequireIsNullOrWhiteSpace(),
                value.EnsureIsNullOrWhiteSpace());
        }

        [Theory]
        [InlineData("a", true)]
        [InlineData(" ", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsNotNullOrEmptyAppliesTheOutcomeOfEveryStepKind(string? value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsNotNullOrEmpty(),
                start.RejectIsNotNullOrEmpty(),
                start.BreakIsNotNullOrEmpty(),
                start.RequireIsNotNullOrEmpty(),
                start.EnsureIsNotNullOrEmpty());

            FlowAssert.Kinds(expected,
                value.MatchIsNotNullOrEmpty(),
                value.RejectIsNotNullOrEmpty(),
                value.BreakIsNotNullOrEmpty(),
                value.RequireIsNotNullOrEmpty(),
                value.EnsureIsNotNullOrEmpty());
        }

        [Theory]
        [InlineData("a", true)]
        [InlineData(" ", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsNotNullOrWhiteSpaceAppliesTheOutcomeOfEveryStepKind(string? value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsNotNullOrWhiteSpace(),
                start.RejectIsNotNullOrWhiteSpace(),
                start.BreakIsNotNullOrWhiteSpace(),
                start.RequireIsNotNullOrWhiteSpace(),
                start.EnsureIsNotNullOrWhiteSpace());

            FlowAssert.Kinds(expected,
                value.MatchIsNotNullOrWhiteSpace(),
                value.RejectIsNotNullOrWhiteSpace(),
                value.BreakIsNotNullOrWhiteSpace(),
                value.RequireIsNotNullOrWhiteSpace(),
                value.EnsureIsNotNullOrWhiteSpace());
        }

        [Theory]
        [InlineData("abc", "abc", true)]
        [InlineData("abc", "ABC", false)]
        [InlineData(null, null, false)]
        [InlineData("abc", null, false)]
        public void EqualAppliesTheOutcomeOfEveryStepKind(string? value, string? other, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchEqual(other),
                start.RejectEqual(other),
                start.BreakEqual(other),
                start.RequireEqual(other),
                start.EnsureEqual(other));

            FlowAssert.Kinds(expected,
                value.MatchEqual(other),
                value.RejectEqual(other),
                value.BreakEqual(other),
                value.RequireEqual(other),
                value.EnsureEqual(other));
        }

        [Theory]
        [InlineData("abc", "ABC", true)]
        [InlineData("abc", "abd", false)]
        [InlineData(null, null, false)]
        public void EqualIgnoreCaseAppliesTheOutcomeOfEveryStepKind(string? value, string? other, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchEqualIgnoreCase(other),
                start.RejectEqualIgnoreCase(other),
                start.BreakEqualIgnoreCase(other),
                start.RequireEqualIgnoreCase(other),
                start.EnsureEqualIgnoreCase(other));

            FlowAssert.Kinds(expected,
                value.MatchEqualIgnoreCase(other),
                value.RejectEqualIgnoreCase(other),
                value.BreakEqualIgnoreCase(other),
                value.RequireEqualIgnoreCase(other),
                value.EnsureEqualIgnoreCase(other));
        }

        [Theory]
        [InlineData("abc", "abd", true)]
        [InlineData("abc", "abc", false)]
        [InlineData(null, "abc", false)]
        public void NotEqualAppliesTheOutcomeOfEveryStepKind(string? value, string? other, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchNotEqual(other),
                start.RejectNotEqual(other),
                start.BreakNotEqual(other),
                start.RequireNotEqual(other),
                start.EnsureNotEqual(other));

            FlowAssert.Kinds(expected,
                value.MatchNotEqual(other),
                value.RejectNotEqual(other),
                value.BreakNotEqual(other),
                value.RequireNotEqual(other),
                value.EnsureNotEqual(other));
        }

        [Theory]
        [InlineData("GB", new[] { "GB", "IE" }, true)]
        [InlineData("gb", new[] { "GB", "IE" }, false)]
        [InlineData("FR", new[] { "GB", "IE" }, false)]
        [InlineData(null, new[] { "GB" }, false)]
        public void InAppliesTheOutcomeOfEveryStepKind(string? value, string[] values, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIn(values),
                start.RejectIn(values),
                start.BreakIn(values),
                start.RequireIn(values),
                start.EnsureIn(values));

            FlowAssert.Kinds(expected,
                value.MatchIn(values),
                value.RejectIn(values),
                value.BreakIn(values),
                value.RequireIn(values),
                value.EnsureIn(values));
        }

        [Theory]
        [InlineData("gb", new[] { "GB", "IE" }, true)]
        [InlineData("FR", new[] { "GB", "IE" }, false)]
        [InlineData(null, new[] { "GB" }, false)]
        public void InIgnoreCaseAppliesTheOutcomeOfEveryStepKind(string? value, string[] values, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchInIgnoreCase(values),
                start.RejectInIgnoreCase(values),
                start.BreakInIgnoreCase(values),
                start.RequireInIgnoreCase(values),
                start.EnsureInIgnoreCase(values));

            FlowAssert.Kinds(expected,
                value.MatchInIgnoreCase(values),
                value.RejectInIgnoreCase(values),
                value.BreakInIgnoreCase(values),
                value.RequireInIgnoreCase(values),
                value.EnsureInIgnoreCase(values));
        }

        [Theory]
        [InlineData("FR", new[] { "GB", "IE" }, true)]
        [InlineData("GB", new[] { "GB", "IE" }, false)]
        [InlineData(null, new[] { "GB" }, false)]
        public void NotInAppliesTheOutcomeOfEveryStepKind(string? value, string[] values, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchNotIn(values),
                start.RejectNotIn(values),
                start.BreakNotIn(values),
                start.RequireNotIn(values),
                start.EnsureNotIn(values));

            FlowAssert.Kinds(expected,
                value.MatchNotIn(values),
                value.RejectNotIn(values),
                value.BreakNotIn(values),
                value.RequireNotIn(values),
                value.EnsureNotIn(values));
        }

        [Theory]
        [InlineData("GB", new[] { "GB", "IE" }, true)]
        [InlineData("FR", new[] { "GB", "IE" }, false)]
        [InlineData(null, new[] { "GB" }, false)]
        [InlineData("John", new string[0], false)]
        public void InListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchInList(list),
                start.RejectInList(list),
                start.BreakInList(list),
                start.RequireInList(list),
                start.EnsureInList(list));

            FlowAssert.Kinds(expected,
                value.MatchInList(list),
                value.RejectInList(list),
                value.BreakInList(list),
                value.RequireInList(list),
                value.EnsureInList(list));
        }

        [Theory]
        [InlineData("gb", new[] { "GB", "IE" }, true)]
        [InlineData("FR", new[] { "GB", "IE" }, false)]
        [InlineData(null, new[] { "GB" }, false)]
        public void InListIgnoreCaseAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchInListIgnoreCase(list),
                start.RejectInListIgnoreCase(list),
                start.BreakInListIgnoreCase(list),
                start.RequireInListIgnoreCase(list),
                start.EnsureInListIgnoreCase(list));

            FlowAssert.Kinds(expected,
                value.MatchInListIgnoreCase(list),
                value.RejectInListIgnoreCase(list),
                value.BreakInListIgnoreCase(list),
                value.RequireInListIgnoreCase(list),
                value.EnsureInListIgnoreCase(list));
        }

        [Theory]
        [InlineData("FR", new[] { "GB", "IE" }, true)]
        [InlineData("GB", new[] { "GB", "IE" }, false)]
        [InlineData(null, new[] { "GB" }, false)]
        public void NotInListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchNotInList(list),
                start.RejectNotInList(list),
                start.BreakNotInList(list),
                start.RequireNotInList(list),
                start.EnsureNotInList(list));

            FlowAssert.Kinds(expected,
                value.MatchNotInList(list),
                value.RejectNotInList(list),
                value.BreakNotInList(list),
                value.RequireNotInList(list),
                value.EnsureNotInList(list));
        }

        [Theory]
        [InlineData("hello world", "lo w", true)]
        [InlineData("hello", "HELLO", false)]
        [InlineData(null, "a", false)]
        [InlineData("a", null, false)]
        public void ContainsAppliesTheOutcomeOfEveryStepKind(string? value, string? substring, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchContains(substring),
                start.RejectContains(substring),
                start.BreakContains(substring),
                start.RequireContains(substring),
                start.EnsureContains(substring));

            FlowAssert.Kinds(expected,
                value.MatchContains(substring),
                value.RejectContains(substring),
                value.BreakContains(substring),
                value.RequireContains(substring),
                value.EnsureContains(substring));
        }

        [Theory]
        [InlineData("Hello", "ELL", true)]
        [InlineData("Hello", "xyz", false)]
        [InlineData(null, "a", false)]
        public void ContainsIgnoreCaseAppliesTheOutcomeOfEveryStepKind(string? value, string? substring, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchContainsIgnoreCase(substring),
                start.RejectContainsIgnoreCase(substring),
                start.BreakContainsIgnoreCase(substring),
                start.RequireContainsIgnoreCase(substring),
                start.EnsureContainsIgnoreCase(substring));

            FlowAssert.Kinds(expected,
                value.MatchContainsIgnoreCase(substring),
                value.RejectContainsIgnoreCase(substring),
                value.BreakContainsIgnoreCase(substring),
                value.RequireContainsIgnoreCase(substring),
                value.EnsureContainsIgnoreCase(substring));
        }

        [Theory]
        [InlineData("pay in crypto", new[] { "gift", "crypto" }, true)]
        [InlineData("pay in cash", new[] { "gift", "crypto" }, false)]
        [InlineData(null, new[] { "a" }, false)]
        [InlineData("abc", new string[0], false)]
        [InlineData("abc", new[] { "" }, true)]
        public void ContainsAnyAppliesTheOutcomeOfEveryStepKind(string? value, string[] values, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchContainsAny(values),
                start.RejectContainsAny(values),
                start.BreakContainsAny(values),
                start.RequireContainsAny(values),
                start.EnsureContainsAny(values));

            FlowAssert.Kinds(expected,
                value.MatchContainsAny(values),
                value.RejectContainsAny(values),
                value.BreakContainsAny(values),
                value.RequireContainsAny(values),
                value.EnsureContainsAny(values));
        }

        [Theory]
        [InlineData("Pay in CRYPTO", new[] { "gift", "crypto" }, true)]
        [InlineData("pay in cash", new[] { "gift", "crypto" }, false)]
        [InlineData(null, new[] { "a" }, false)]
        public void ContainsAnyIgnoreCaseAppliesTheOutcomeOfEveryStepKind(string? value, string[] values, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchContainsAnyIgnoreCase(values),
                start.RejectContainsAnyIgnoreCase(values),
                start.BreakContainsAnyIgnoreCase(values),
                start.RequireContainsAnyIgnoreCase(values),
                start.EnsureContainsAnyIgnoreCase(values));

            FlowAssert.Kinds(expected,
                value.MatchContainsAnyIgnoreCase(values),
                value.RejectContainsAnyIgnoreCase(values),
                value.BreakContainsAnyIgnoreCase(values),
                value.RequireContainsAnyIgnoreCase(values),
                value.EnsureContainsAnyIgnoreCase(values));
        }

        [Theory]
        [InlineData("gift card crypto", new[] { "gift", "crypto" }, true)]
        [InlineData("gift card", new[] { "gift", "crypto" }, false)]
        [InlineData(null, new[] { "a" }, false)]
        public void ContainsAllAppliesTheOutcomeOfEveryStepKind(string? value, string[] values, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchContainsAll(values),
                start.RejectContainsAll(values),
                start.BreakContainsAll(values),
                start.RequireContainsAll(values),
                start.EnsureContainsAll(values));

            FlowAssert.Kinds(expected,
                value.MatchContainsAll(values),
                value.RejectContainsAll(values),
                value.BreakContainsAll(values),
                value.RequireContainsAll(values),
                value.EnsureContainsAll(values));
        }

        [Theory]
        [InlineData("pay in cash", new[] { "gift", "crypto" }, true)]
        [InlineData("pay in crypto", new[] { "gift", "crypto" }, false)]
        [InlineData(null, new[] { "a" }, false)]
        public void ContainsNoneAppliesTheOutcomeOfEveryStepKind(string? value, string[] values, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchContainsNone(values),
                start.RejectContainsNone(values),
                start.BreakContainsNone(values),
                start.RequireContainsNone(values),
                start.EnsureContainsNone(values));

            FlowAssert.Kinds(expected,
                value.MatchContainsNone(values),
                value.RejectContainsNone(values),
                value.BreakContainsNone(values),
                value.RequireContainsNone(values),
                value.EnsureContainsNone(values));
        }

        [Theory]
        [InlineData("pay in crypto", new[] { "gift", "crypto" }, true)]
        [InlineData("pay in cash", new[] { "gift", "crypto" }, false)]
        [InlineData(null, new[] { "a" }, false)]
        public void ContainsAnyInListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchContainsAnyInList(list),
                start.RejectContainsAnyInList(list),
                start.BreakContainsAnyInList(list),
                start.RequireContainsAnyInList(list),
                start.EnsureContainsAnyInList(list));

            FlowAssert.Kinds(expected,
                value.MatchContainsAnyInList(list),
                value.RejectContainsAnyInList(list),
                value.BreakContainsAnyInList(list),
                value.RequireContainsAnyInList(list),
                value.EnsureContainsAnyInList(list));
        }

        [Theory]
        [InlineData("Pay in CRYPTO", new[] { "gift", "crypto" }, true)]
        [InlineData("pay in cash", new[] { "gift", "crypto" }, false)]
        [InlineData(null, new[] { "a" }, false)]
        public void ContainsAnyInListIgnoreCaseAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchContainsAnyInListIgnoreCase(list),
                start.RejectContainsAnyInListIgnoreCase(list),
                start.BreakContainsAnyInListIgnoreCase(list),
                start.RequireContainsAnyInListIgnoreCase(list),
                start.EnsureContainsAnyInListIgnoreCase(list));

            FlowAssert.Kinds(expected,
                value.MatchContainsAnyInListIgnoreCase(list),
                value.RejectContainsAnyInListIgnoreCase(list),
                value.BreakContainsAnyInListIgnoreCase(list),
                value.RequireContainsAnyInListIgnoreCase(list),
                value.EnsureContainsAnyInListIgnoreCase(list));
        }

        [Theory]
        [InlineData("gift card crypto", new[] { "gift", "crypto" }, true)]
        [InlineData("gift card", new[] { "gift", "crypto" }, false)]
        [InlineData(null, new[] { "a" }, false)]
        public void ContainsAllInListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchContainsAllInList(list),
                start.RejectContainsAllInList(list),
                start.BreakContainsAllInList(list),
                start.RequireContainsAllInList(list),
                start.EnsureContainsAllInList(list));

            FlowAssert.Kinds(expected,
                value.MatchContainsAllInList(list),
                value.RejectContainsAllInList(list),
                value.BreakContainsAllInList(list),
                value.RequireContainsAllInList(list),
                value.EnsureContainsAllInList(list));
        }

        [Theory]
        [InlineData("pay in cash", new[] { "gift", "crypto" }, true)]
        [InlineData("pay in crypto", new[] { "gift", "crypto" }, false)]
        [InlineData(null, new[] { "a" }, false)]
        public void ContainsNoneInListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchContainsNoneInList(list),
                start.RejectContainsNoneInList(list),
                start.BreakContainsNoneInList(list),
                start.RequireContainsNoneInList(list),
                start.EnsureContainsNoneInList(list));

            FlowAssert.Kinds(expected,
                value.MatchContainsNoneInList(list),
                value.RejectContainsNoneInList(list),
                value.BreakContainsNoneInList(list),
                value.RequireContainsNoneInList(list),
                value.EnsureContainsNoneInList(list));
        }

        [Theory]
        [InlineData("GB29NWBK", "GB", true)]
        [InlineData("gb29", "GB", false)]
        [InlineData(null, "GB", false)]
        public void StartsWithAppliesTheOutcomeOfEveryStepKind(string? value, string? prefix, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchStartsWith(prefix),
                start.RejectStartsWith(prefix),
                start.BreakStartsWith(prefix),
                start.RequireStartsWith(prefix),
                start.EnsureStartsWith(prefix));

            FlowAssert.Kinds(expected,
                value.MatchStartsWith(prefix),
                value.RejectStartsWith(prefix),
                value.BreakStartsWith(prefix),
                value.RequireStartsWith(prefix),
                value.EnsureStartsWith(prefix));
        }

        [Theory]
        [InlineData("a@example.com", "example.com", true)]
        [InlineData("a@example.org", "example.com", false)]
        [InlineData(null, "x", false)]
        public void EndsWithAppliesTheOutcomeOfEveryStepKind(string? value, string? suffix, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchEndsWith(suffix),
                start.RejectEndsWith(suffix),
                start.BreakEndsWith(suffix),
                start.RequireEndsWith(suffix),
                start.EnsureEndsWith(suffix));

            FlowAssert.Kinds(expected,
                value.MatchEndsWith(suffix),
                value.RejectEndsWith(suffix),
                value.BreakEndsWith(suffix),
                value.RequireEndsWith(suffix),
                value.EnsureEndsWith(suffix));
        }

        [Theory]
        [InlineData("GB29NWBK", new[] { "FR", "GB" }, true)]
        [InlineData("DE89", new[] { "FR", "GB" }, false)]
        [InlineData(null, new[] { "GB" }, false)]
        public void StartsWithAnyInListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchStartsWithAnyInList(list),
                start.RejectStartsWithAnyInList(list),
                start.BreakStartsWithAnyInList(list),
                start.RequireStartsWithAnyInList(list),
                start.EnsureStartsWithAnyInList(list));

            FlowAssert.Kinds(expected,
                value.MatchStartsWithAnyInList(list),
                value.RejectStartsWithAnyInList(list),
                value.BreakStartsWithAnyInList(list),
                value.RequireStartsWithAnyInList(list),
                value.EnsureStartsWithAnyInList(list));
        }

        [Theory]
        [InlineData("AB123", "^[A-Z]{2}[0-9]{3}$", true)]
        [InlineData("ab123", "^[A-Z]{2}[0-9]{3}$", false)]
        [InlineData(null, "a", false)]
        [InlineData("a", null, false)]
        [InlineData("a", "(", false)]
        public void MatchesAppliesTheOutcomeOfEveryStepKind(string? value, string? pattern, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchMatches(pattern),
                start.RejectMatches(pattern),
                start.BreakMatches(pattern),
                start.RequireMatches(pattern),
                start.EnsureMatches(pattern));

            FlowAssert.Kinds(expected,
                value.MatchMatches(pattern),
                value.RejectMatches(pattern),
                value.BreakMatches(pattern),
                value.RequireMatches(pattern),
                value.EnsureMatches(pattern));
        }

        [Theory]
        [InlineData("abc", 3, true)]
        [InlineData("abc", 4, false)]
        [InlineData(null, 0, false)]
        public void LengthEqualAppliesTheOutcomeOfEveryStepKind(string? value, int length, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchLengthEqual(length),
                start.RejectLengthEqual(length),
                start.BreakLengthEqual(length),
                start.RequireLengthEqual(length),
                start.EnsureLengthEqual(length));

            FlowAssert.Kinds(expected,
                value.MatchLengthEqual(length),
                value.RejectLengthEqual(length),
                value.BreakLengthEqual(length),
                value.RequireLengthEqual(length),
                value.EnsureLengthEqual(length));
        }

        [Theory]
        [InlineData("abc", 2, true)]
        [InlineData("abc", 3, false)]
        [InlineData(null, 0, false)]
        public void LengthGreaterAppliesTheOutcomeOfEveryStepKind(string? value, int length, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchLengthGreater(length),
                start.RejectLengthGreater(length),
                start.BreakLengthGreater(length),
                start.RequireLengthGreater(length),
                start.EnsureLengthGreater(length));

            FlowAssert.Kinds(expected,
                value.MatchLengthGreater(length),
                value.RejectLengthGreater(length),
                value.BreakLengthGreater(length),
                value.RequireLengthGreater(length),
                value.EnsureLengthGreater(length));
        }

        [Theory]
        [InlineData("abc", 4, true)]
        [InlineData("abc", 3, false)]
        [InlineData(null, 5, false)]
        public void LengthLessAppliesTheOutcomeOfEveryStepKind(string? value, int length, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchLengthLess(length),
                start.RejectLengthLess(length),
                start.BreakLengthLess(length),
                start.RequireLengthLess(length),
                start.EnsureLengthLess(length));

            FlowAssert.Kinds(expected,
                value.MatchLengthLess(length),
                value.RejectLengthLess(length),
                value.BreakLengthLess(length),
                value.RequireLengthLess(length),
                value.EnsureLengthLess(length));
        }

        [Theory]
        [InlineData("abc", 3, 5, true)]
        [InlineData("abcde", 3, 5, true)]
        [InlineData("ab", 3, 5, false)]
        [InlineData("abcdef", 3, 5, false)]
        [InlineData(null, 0, 5, false)]
        public void LengthInRangeAppliesTheOutcomeOfEveryStepKind(string? value, int minimum, int maximum, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchLengthInRange(minimum, maximum),
                start.RejectLengthInRange(minimum, maximum),
                start.BreakLengthInRange(minimum, maximum),
                start.RequireLengthInRange(minimum, maximum),
                start.EnsureLengthInRange(minimum, maximum));

            FlowAssert.Kinds(expected,
                value.MatchLengthInRange(minimum, maximum),
                value.RejectLengthInRange(minimum, maximum),
                value.BreakLengthInRange(minimum, maximum),
                value.RequireLengthInRange(minimum, maximum),
                value.EnsureLengthInRange(minimum, maximum));
        }

        [Theory]
        [InlineData("12345", true)]
        [InlineData("12a45", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsNumericAppliesTheOutcomeOfEveryStepKind(string? value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsNumeric(),
                start.RejectIsNumeric(),
                start.BreakIsNumeric(),
                start.RequireIsNumeric(),
                start.EnsureIsNumeric());

            FlowAssert.Kinds(expected,
                value.MatchIsNumeric(),
                value.RejectIsNumeric(),
                value.BreakIsNumeric(),
                value.RequireIsNumeric(),
                value.EnsureIsNumeric());
        }

        [Theory]
        [InlineData("abcXYZ", true)]
        [InlineData("abc1", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsAlphaAppliesTheOutcomeOfEveryStepKind(string? value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsAlpha(),
                start.RejectIsAlpha(),
                start.BreakIsAlpha(),
                start.RequireIsAlpha(),
                start.EnsureIsAlpha());

            FlowAssert.Kinds(expected,
                value.MatchIsAlpha(),
                value.RejectIsAlpha(),
                value.BreakIsAlpha(),
                value.RequireIsAlpha(),
                value.EnsureIsAlpha());
        }

        [Theory]
        [InlineData("abc123", true)]
        [InlineData("abc 123", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsAlphaNumericAppliesTheOutcomeOfEveryStepKind(string? value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsAlphaNumeric(),
                start.RejectIsAlphaNumeric(),
                start.BreakIsAlphaNumeric(),
                start.RequireIsAlphaNumeric(),
                start.EnsureIsAlphaNumeric());

            FlowAssert.Kinds(expected,
                value.MatchIsAlphaNumeric(),
                value.RejectIsAlphaNumeric(),
                value.BreakIsAlphaNumeric(),
                value.RequireIsAlphaNumeric(),
                value.EnsureIsAlphaNumeric());
        }

        [Theory]
        [InlineData("aaaa", true)]
        [InlineData("aaab", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsAllSameCharacterAppliesTheOutcomeOfEveryStepKind(string? value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsAllSameCharacter(),
                start.RejectIsAllSameCharacter(),
                start.BreakIsAllSameCharacter(),
                start.RequireIsAllSameCharacter(),
                start.EnsureIsAllSameCharacter());

            FlowAssert.Kinds(expected,
                value.MatchIsAllSameCharacter(),
                value.RejectIsAllSameCharacter(),
                value.BreakIsAllSameCharacter(),
                value.RequireIsAllSameCharacter(),
                value.EnsureIsAllSameCharacter());
        }

        [Theory]
        [InlineData("ab123456cd", 6, true)]
        [InlineData("ab12345cd", 6, false)]
        [InlineData("987654", 6, true)]
        [InlineData(null, 3, false)]
        public void HasSequentialDigitsAppliesTheOutcomeOfEveryStepKind(string? value, int minimumRunLength, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchHasSequentialDigits(minimumRunLength),
                start.RejectHasSequentialDigits(minimumRunLength),
                start.BreakHasSequentialDigits(minimumRunLength),
                start.RequireHasSequentialDigits(minimumRunLength),
                start.EnsureHasSequentialDigits(minimumRunLength));

            FlowAssert.Kinds(expected,
                value.MatchHasSequentialDigits(minimumRunLength),
                value.RejectHasSequentialDigits(minimumRunLength),
                value.BreakHasSequentialDigits(minimumRunLength),
                value.RequireHasSequentialDigits(minimumRunLength),
                value.EnsureHasSequentialDigits(minimumRunLength));
        }

        [Theory]
        [InlineData("abcd", 1.5, true)]
        [InlineData("aaaa", 1.5, false)]
        [InlineData(null, 0, false)]
        public void ShannonEntropyAboveAppliesTheOutcomeOfEveryStepKind(string? value, double threshold, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchShannonEntropyAbove(threshold),
                start.RejectShannonEntropyAbove(threshold),
                start.BreakShannonEntropyAbove(threshold),
                start.RequireShannonEntropyAbove(threshold),
                start.EnsureShannonEntropyAbove(threshold));

            FlowAssert.Kinds(expected,
                value.MatchShannonEntropyAbove(threshold),
                value.RejectShannonEntropyAbove(threshold),
                value.BreakShannonEntropyAbove(threshold),
                value.RequireShannonEntropyAbove(threshold),
                value.EnsureShannonEntropyAbove(threshold));
        }

        [Theory]
        [InlineData("aaaa", 1.5, true)]
        [InlineData("abcd", 1.5, false)]
        [InlineData(null, 1.5, false)]
        public void ShannonEntropyBelowAppliesTheOutcomeOfEveryStepKind(string? value, double threshold, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchShannonEntropyBelow(threshold),
                start.RejectShannonEntropyBelow(threshold),
                start.BreakShannonEntropyBelow(threshold),
                start.RequireShannonEntropyBelow(threshold),
                start.EnsureShannonEntropyBelow(threshold));

            FlowAssert.Kinds(expected,
                value.MatchShannonEntropyBelow(threshold),
                value.RejectShannonEntropyBelow(threshold),
                value.BreakShannonEntropyBelow(threshold),
                value.RequireShannonEntropyBelow(threshold),
                value.EnsureShannonEntropyBelow(threshold));
        }

        [Theory]
        [InlineData("a@example.com", true)]
        [InlineData("not-an-email", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsValidEmailAppliesTheOutcomeOfEveryStepKind(string? value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsValidEmail(),
                start.RejectIsValidEmail(),
                start.BreakIsValidEmail(),
                start.RequireIsValidEmail(),
                start.EnsureIsValidEmail());

            FlowAssert.Kinds(expected,
                value.MatchIsValidEmail(),
                value.RejectIsValidEmail(),
                value.BreakIsValidEmail(),
                value.RequireIsValidEmail(),
                value.EnsureIsValidEmail());
        }

        [Theory]
        [InlineData("not-an-email", true)]
        [InlineData(null, true)]
        [InlineData("a@example.com", false)]
        public void IsNotValidEmailAppliesTheOutcomeOfEveryStepKind(string? value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsNotValidEmail(),
                start.RejectIsNotValidEmail(),
                start.BreakIsNotValidEmail(),
                start.RequireIsNotValidEmail(),
                start.EnsureIsNotValidEmail());

            FlowAssert.Kinds(expected,
                value.MatchIsNotValidEmail(),
                value.RejectIsNotValidEmail(),
                value.BreakIsNotValidEmail(),
                value.RequireIsNotValidEmail(),
                value.EnsureIsNotValidEmail());
        }

        [Theory]
        [InlineData("192.168.1.1", true)]
        [InlineData("999.1.1.1", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsValidIpAppliesTheOutcomeOfEveryStepKind(string? value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsValidIp(),
                start.RejectIsValidIp(),
                start.BreakIsValidIp(),
                start.RequireIsValidIp(),
                start.EnsureIsValidIp());

            FlowAssert.Kinds(expected,
                value.MatchIsValidIp(),
                value.RejectIsValidIp(),
                value.BreakIsValidIp(),
                value.RequireIsValidIp(),
                value.EnsureIsValidIp());
        }

        [Theory]
        [InlineData("999.1.1.1", true)]
        [InlineData(null, true)]
        [InlineData("192.168.1.1", false)]
        public void IsNotValidIpAppliesTheOutcomeOfEveryStepKind(string? value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsNotValidIp(),
                start.RejectIsNotValidIp(),
                start.BreakIsNotValidIp(),
                start.RequireIsNotValidIp(),
                start.EnsureIsNotValidIp());

            FlowAssert.Kinds(expected,
                value.MatchIsNotValidIp(),
                value.RejectIsNotValidIp(),
                value.BreakIsNotValidIp(),
                value.RequireIsNotValidIp(),
                value.EnsureIsNotValidIp());
        }

        [Theory]
        [InlineData("GB82WEST12345698765432", true)]
        [InlineData("GB82WEST12345698765433", false)]
        [InlineData(null, false)]
        public void IsValidIbanAppliesTheOutcomeOfEveryStepKind(string? value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsValidIban(),
                start.RejectIsValidIban(),
                start.BreakIsValidIban(),
                start.RequireIsValidIban(),
                start.EnsureIsValidIban());

            FlowAssert.Kinds(expected,
                value.MatchIsValidIban(),
                value.RejectIsValidIban(),
                value.BreakIsValidIban(),
                value.RequireIsValidIban(),
                value.EnsureIsValidIban());
        }

        [Theory]
        [InlineData("GB82WEST12345698765433", true)]
        [InlineData(null, true)]
        [InlineData("GB82WEST12345698765432", false)]
        public void IsNotValidIbanAppliesTheOutcomeOfEveryStepKind(string? value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsNotValidIban(),
                start.RejectIsNotValidIban(),
                start.BreakIsNotValidIban(),
                start.RequireIsNotValidIban(),
                start.EnsureIsNotValidIban());

            FlowAssert.Kinds(expected,
                value.MatchIsNotValidIban(),
                value.RejectIsNotValidIban(),
                value.BreakIsNotValidIban(),
                value.RequireIsNotValidIban(),
                value.EnsureIsNotValidIban());
        }

        [Theory]
        [InlineData("DEUTDEFF", true)]
        [InlineData("DEUTDEFF500", true)]
        [InlineData("DEUT", false)]
        [InlineData(null, false)]
        public void IsValidBicAppliesTheOutcomeOfEveryStepKind(string? value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsValidBic(),
                start.RejectIsValidBic(),
                start.BreakIsValidBic(),
                start.RequireIsValidBic(),
                start.EnsureIsValidBic());

            FlowAssert.Kinds(expected,
                value.MatchIsValidBic(),
                value.RejectIsValidBic(),
                value.BreakIsValidBic(),
                value.RequireIsValidBic(),
                value.EnsureIsValidBic());
        }

        [Theory]
        [InlineData("DEUT", true)]
        [InlineData(null, true)]
        [InlineData("DEUTDEFF", false)]
        public void IsNotValidBicAppliesTheOutcomeOfEveryStepKind(string? value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsNotValidBic(),
                start.RejectIsNotValidBic(),
                start.BreakIsNotValidBic(),
                start.RequireIsNotValidBic(),
                start.EnsureIsNotValidBic());

            FlowAssert.Kinds(expected,
                value.MatchIsNotValidBic(),
                value.RejectIsNotValidBic(),
                value.BreakIsNotValidBic(),
                value.RequireIsNotValidBic(),
                value.EnsureIsNotValidBic());
        }

        [Theory]
        [InlineData("4111111111111111", true)]
        [InlineData("4111111111111112", false)]
        [InlineData(null, false)]
        public void IsValidLuhnAppliesTheOutcomeOfEveryStepKind(string? value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsValidLuhn(),
                start.RejectIsValidLuhn(),
                start.BreakIsValidLuhn(),
                start.RequireIsValidLuhn(),
                start.EnsureIsValidLuhn());

            FlowAssert.Kinds(expected,
                value.MatchIsValidLuhn(),
                value.RejectIsValidLuhn(),
                value.BreakIsValidLuhn(),
                value.RequireIsValidLuhn(),
                value.EnsureIsValidLuhn());
        }

        [Theory]
        [InlineData("4111111111111112", true)]
        [InlineData(null, true)]
        [InlineData("4111111111111111", false)]
        public void IsNotValidLuhnAppliesTheOutcomeOfEveryStepKind(string? value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsNotValidLuhn(),
                start.RejectIsNotValidLuhn(),
                start.BreakIsNotValidLuhn(),
                start.RequireIsNotValidLuhn(),
                start.EnsureIsNotValidLuhn());

            FlowAssert.Kinds(expected,
                value.MatchIsNotValidLuhn(),
                value.RejectIsNotValidLuhn(),
                value.BreakIsNotValidLuhn(),
                value.RequireIsNotValidLuhn(),
                value.EnsureIsNotValidLuhn());
        }

        [Theory]
        [InlineData("12/99", true)]
        [InlineData("01/20", false)]
        [InlineData(null, false)]
        public void IsValidCardExpiryAppliesTheOutcomeOfEveryStepKind(string? value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsValidCardExpiry(),
                start.RejectIsValidCardExpiry(),
                start.BreakIsValidCardExpiry(),
                start.RequireIsValidCardExpiry(),
                start.EnsureIsValidCardExpiry());

            FlowAssert.Kinds(expected,
                value.MatchIsValidCardExpiry(),
                value.RejectIsValidCardExpiry(),
                value.BreakIsValidCardExpiry(),
                value.RequireIsValidCardExpiry(),
                value.EnsureIsValidCardExpiry());
        }

        [Theory]
        [InlineData("01/20", true)]
        [InlineData(null, true)]
        [InlineData("12/99", false)]
        public void IsNotValidCardExpiryAppliesTheOutcomeOfEveryStepKind(string? value, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchIsNotValidCardExpiry(),
                start.RejectIsNotValidCardExpiry(),
                start.BreakIsNotValidCardExpiry(),
                start.RequireIsNotValidCardExpiry(),
                start.EnsureIsNotValidCardExpiry());

            FlowAssert.Kinds(expected,
                value.MatchIsNotValidCardExpiry(),
                value.RejectIsNotValidCardExpiry(),
                value.BreakIsNotValidCardExpiry(),
                value.RequireIsNotValidCardExpiry(),
                value.EnsureIsNotValidCardExpiry());
        }

        [Theory]
        [InlineData("a@Example.com", "example.com", true)]
        [InlineData("a@example.org", "example.com", false)]
        [InlineData("no-at", "", true)]
        [InlineData(null, "a", false)]
        public void EmailDomainEqualAppliesTheOutcomeOfEveryStepKind(string? value, string? domain, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchEmailDomainEqual(domain),
                start.RejectEmailDomainEqual(domain),
                start.BreakEmailDomainEqual(domain),
                start.RequireEmailDomainEqual(domain),
                start.EnsureEmailDomainEqual(domain));

            FlowAssert.Kinds(expected,
                value.MatchEmailDomainEqual(domain),
                value.RejectEmailDomainEqual(domain),
                value.BreakEmailDomainEqual(domain),
                value.RequireEmailDomainEqual(domain),
                value.EnsureEmailDomainEqual(domain));
        }

        [Theory]
        [InlineData("a@gmail.com", new[] { "gmail.com", "yahoo.com" }, true)]
        [InlineData("a@corp.com", new[] { "gmail.com", "yahoo.com" }, false)]
        [InlineData(null, new[] { "a" }, false)]
        public void EmailDomainInAppliesTheOutcomeOfEveryStepKind(string? value, string[] domains, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchEmailDomainIn(domains),
                start.RejectEmailDomainIn(domains),
                start.BreakEmailDomainIn(domains),
                start.RequireEmailDomainIn(domains),
                start.EnsureEmailDomainIn(domains));

            FlowAssert.Kinds(expected,
                value.MatchEmailDomainIn(domains),
                value.RejectEmailDomainIn(domains),
                value.BreakEmailDomainIn(domains),
                value.RequireEmailDomainIn(domains),
                value.EnsureEmailDomainIn(domains));
        }

        [Theory]
        [InlineData("a@gmail.com", new[] { "gmail.com", "yahoo.com" }, true)]
        [InlineData("a@corp.com", new[] { "gmail.com", "yahoo.com" }, false)]
        [InlineData(null, new[] { "a" }, false)]
        public void EmailDomainInListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchEmailDomainInList(list),
                start.RejectEmailDomainInList(list),
                start.BreakEmailDomainInList(list),
                start.RequireEmailDomainInList(list),
                start.EnsureEmailDomainInList(list));

            FlowAssert.Kinds(expected,
                value.MatchEmailDomainInList(list),
                value.RejectEmailDomainInList(list),
                value.BreakEmailDomainInList(list),
                value.RequireEmailDomainInList(list),
                value.EnsureEmailDomainInList(list));
        }

        [Theory]
        [InlineData("john+spam@gmail.com", "JOHN@gmail.com", true)]
        [InlineData("john.doe@gmail.com", "johndoe@gmail.com", false)]
        [InlineData("john@gmail.com", "jane@gmail.com", false)]
        [InlineData(null, "a@b.com", false)]
        public void EmailAliasNormalisedEqualAppliesTheOutcomeOfEveryStepKind(string? value, string? other, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchEmailAliasNormalisedEqual(other),
                start.RejectEmailAliasNormalisedEqual(other),
                start.BreakEmailAliasNormalisedEqual(other),
                start.RequireEmailAliasNormalisedEqual(other),
                start.EnsureEmailAliasNormalisedEqual(other));

            FlowAssert.Kinds(expected,
                value.MatchEmailAliasNormalisedEqual(other),
                value.RejectEmailAliasNormalisedEqual(other),
                value.BreakEmailAliasNormalisedEqual(other),
                value.RequireEmailAliasNormalisedEqual(other),
                value.EnsureEmailAliasNormalisedEqual(other));
        }

        [Theory]
        [InlineData("Robert", "Rupert", true)]
        [InlineData("Robert", "Smith", false)]
        [InlineData(null, "a", false)]
        public void SoundexEqualAppliesTheOutcomeOfEveryStepKind(string? value, string? other, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchSoundexEqual(other),
                start.RejectSoundexEqual(other),
                start.BreakSoundexEqual(other),
                start.RequireSoundexEqual(other),
                start.EnsureSoundexEqual(other));

            FlowAssert.Kinds(expected,
                value.MatchSoundexEqual(other),
                value.RejectSoundexEqual(other),
                value.BreakSoundexEqual(other),
                value.RequireSoundexEqual(other),
                value.EnsureSoundexEqual(other));
        }

        [Theory]
        [InlineData("MARTHA", "MARHTA", 0.9, true)]
        [InlineData("MARTHA", "ZZZZZZ", 0.9, false)]
        [InlineData(null, "a", 0.5, false)]
        public void JaroWinklerAtLeastAppliesTheOutcomeOfEveryStepKind(string? value, string? other, double threshold, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchJaroWinklerAtLeast(other, threshold),
                start.RejectJaroWinklerAtLeast(other, threshold),
                start.BreakJaroWinklerAtLeast(other, threshold),
                start.RequireJaroWinklerAtLeast(other, threshold),
                start.EnsureJaroWinklerAtLeast(other, threshold));

            FlowAssert.Kinds(expected,
                value.MatchJaroWinklerAtLeast(other, threshold),
                value.RejectJaroWinklerAtLeast(other, threshold),
                value.BreakJaroWinklerAtLeast(other, threshold),
                value.RequireJaroWinklerAtLeast(other, threshold),
                value.EnsureJaroWinklerAtLeast(other, threshold));
        }

        [Theory]
        [InlineData("kitten", "kitten", 1, true)]
        [InlineData("kitten", "sitting", 0.9, false)]
        [InlineData("kitten", "sitting", 0.5, true)]
        [InlineData(null, "a", 0, false)]
        public void SimilarityAtLeastAppliesTheOutcomeOfEveryStepKind(string? value, string? other, double threshold, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchSimilarityAtLeast(other, threshold),
                start.RejectSimilarityAtLeast(other, threshold),
                start.BreakSimilarityAtLeast(other, threshold),
                start.RequireSimilarityAtLeast(other, threshold),
                start.EnsureSimilarityAtLeast(other, threshold));

            FlowAssert.Kinds(expected,
                value.MatchSimilarityAtLeast(other, threshold),
                value.RejectSimilarityAtLeast(other, threshold),
                value.BreakSimilarityAtLeast(other, threshold),
                value.RequireSimilarityAtLeast(other, threshold),
                value.EnsureSimilarityAtLeast(other, threshold));
        }

        [Theory]
        [InlineData("kitten", "sitting", 3, true)]
        [InlineData("kitten", "sitting", 2, false)]
        [InlineData(null, "a", 5, false)]
        public void LevenshteinAtMostAppliesTheOutcomeOfEveryStepKind(string? value, string? other, int distance, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchLevenshteinAtMost(other, distance),
                start.RejectLevenshteinAtMost(other, distance),
                start.BreakLevenshteinAtMost(other, distance),
                start.RequireLevenshteinAtMost(other, distance),
                start.EnsureLevenshteinAtMost(other, distance));

            FlowAssert.Kinds(expected,
                value.MatchLevenshteinAtMost(other, distance),
                value.RejectLevenshteinAtMost(other, distance),
                value.BreakLevenshteinAtMost(other, distance),
                value.RequireLevenshteinAtMost(other, distance),
                value.EnsureLevenshteinAtMost(other, distance));
        }

        [Theory]
        [InlineData("a@example.com", new[] { ".com", ".org" }, true)]
        [InlineData("a@example.net", new[] { ".com", ".org" }, false)]
        [InlineData(null, new[] { ".com" }, false)]
        public void EndsWithAnyInListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchEndsWithAnyInList(list),
                start.RejectEndsWithAnyInList(list),
                start.BreakEndsWithAnyInList(list),
                start.RequireEndsWithAnyInList(list),
                start.EnsureEndsWithAnyInList(list));

            FlowAssert.Kinds(expected,
                value.MatchEndsWithAnyInList(list),
                value.RejectEndsWithAnyInList(list),
                value.BreakEndsWithAnyInList(list),
                value.RequireEndsWithAnyInList(list),
                value.EnsureEndsWithAnyInList(list));
        }

        [Theory]
        [InlineData("Gift CARD Crypto", new[] { "gift", "crypto" }, true)]
        [InlineData("gift card", new[] { "gift", "crypto" }, false)]
        [InlineData(null, new[] { "a" }, false)]
        public void ContainsAllIgnoreCaseAppliesTheOutcomeOfEveryStepKind(string? value, string[] values, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchContainsAllIgnoreCase(values),
                start.RejectContainsAllIgnoreCase(values),
                start.BreakContainsAllIgnoreCase(values),
                start.RequireContainsAllIgnoreCase(values),
                start.EnsureContainsAllIgnoreCase(values));

            FlowAssert.Kinds(expected,
                value.MatchContainsAllIgnoreCase(values),
                value.RejectContainsAllIgnoreCase(values),
                value.BreakContainsAllIgnoreCase(values),
                value.RequireContainsAllIgnoreCase(values),
                value.EnsureContainsAllIgnoreCase(values));
        }

        [Theory]
        [InlineData("pay in cash", new[] { "GIFT", "crypto" }, true)]
        [InlineData("Pay in CRYPTO", new[] { "gift", "crypto" }, false)]
        [InlineData(null, new[] { "a" }, false)]
        public void ContainsNoneIgnoreCaseAppliesTheOutcomeOfEveryStepKind(string? value, string[] values, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchContainsNoneIgnoreCase(values),
                start.RejectContainsNoneIgnoreCase(values),
                start.BreakContainsNoneIgnoreCase(values),
                start.RequireContainsNoneIgnoreCase(values),
                start.EnsureContainsNoneIgnoreCase(values));

            FlowAssert.Kinds(expected,
                value.MatchContainsNoneIgnoreCase(values),
                value.RejectContainsNoneIgnoreCase(values),
                value.BreakContainsNoneIgnoreCase(values),
                value.RequireContainsNoneIgnoreCase(values),
                value.EnsureContainsNoneIgnoreCase(values));
        }

        [Theory]
        [InlineData("Gift CARD Crypto", new[] { "gift", "crypto" }, true)]
        [InlineData("gift card", new[] { "gift", "crypto" }, false)]
        [InlineData(null, new[] { "a" }, false)]
        public void ContainsAllInListIgnoreCaseAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchContainsAllInListIgnoreCase(list),
                start.RejectContainsAllInListIgnoreCase(list),
                start.BreakContainsAllInListIgnoreCase(list),
                start.RequireContainsAllInListIgnoreCase(list),
                start.EnsureContainsAllInListIgnoreCase(list));

            FlowAssert.Kinds(expected,
                value.MatchContainsAllInListIgnoreCase(list),
                value.RejectContainsAllInListIgnoreCase(list),
                value.BreakContainsAllInListIgnoreCase(list),
                value.RequireContainsAllInListIgnoreCase(list),
                value.EnsureContainsAllInListIgnoreCase(list));
        }

        [Theory]
        [InlineData("pay in cash", new[] { "GIFT", "crypto" }, true)]
        [InlineData("Pay in CRYPTO", new[] { "gift", "crypto" }, false)]
        [InlineData(null, new[] { "a" }, false)]
        public void ContainsNoneInListIgnoreCaseAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchContainsNoneInListIgnoreCase(list),
                start.RejectContainsNoneInListIgnoreCase(list),
                start.BreakContainsNoneInListIgnoreCase(list),
                start.RequireContainsNoneInListIgnoreCase(list),
                start.EnsureContainsNoneInListIgnoreCase(list));

            FlowAssert.Kinds(expected,
                value.MatchContainsNoneInListIgnoreCase(list),
                value.RejectContainsNoneInListIgnoreCase(list),
                value.BreakContainsNoneInListIgnoreCase(list),
                value.RequireContainsNoneInListIgnoreCase(list),
                value.EnsureContainsNoneInListIgnoreCase(list));
        }

        [Theory]
        [InlineData("payment to Jon Smyth today", "John Smith", 0.85, true)]
        [InlineData("payment to John Smith today", "John Smith", 1, true)]
        [InlineData("payment to Alice Cooper", "John Smith", 0.85, false)]
        [InlineData("John", "John Smith", 0.9, false)]
        [InlineData(null, "John Smith", 0.5, false)]
        [InlineData("John Smith", null, 0.5, false)]
        [InlineData("John Smith", "  ", 0.5, false)]
        public void ContainsFuzzyAppliesTheOutcomeOfEveryStepKind(string? value, string? term, double threshold, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchContainsFuzzy(term, threshold),
                start.RejectContainsFuzzy(term, threshold),
                start.BreakContainsFuzzy(term, threshold),
                start.RequireContainsFuzzy(term, threshold),
                start.EnsureContainsFuzzy(term, threshold));

            FlowAssert.Kinds(expected,
                value.MatchContainsFuzzy(term, threshold),
                value.RejectContainsFuzzy(term, threshold),
                value.BreakContainsFuzzy(term, threshold),
                value.RequireContainsFuzzy(term, threshold),
                value.EnsureContainsFuzzy(term, threshold));
        }

        [Theory]
        [InlineData("paid Jon Smyth", 0.85, new[] { "Alice Cooper", "John Smith" }, true)]
        [InlineData("paid Bob Marley", 0.85, new[] { "Alice Cooper", "John Smith" }, false)]
        [InlineData(null, 0.5, new[] { "John" }, false)]
        public void ContainsFuzzyAnyAppliesTheOutcomeOfEveryStepKind(string? value, double threshold, string[] terms, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchContainsFuzzyAny(threshold, terms),
                start.RejectContainsFuzzyAny(threshold, terms),
                start.BreakContainsFuzzyAny(threshold, terms),
                start.RequireContainsFuzzyAny(threshold, terms),
                start.EnsureContainsFuzzyAny(threshold, terms));

            FlowAssert.Kinds(expected,
                value.MatchContainsFuzzyAny(threshold, terms),
                value.RejectContainsFuzzyAny(threshold, terms),
                value.BreakContainsFuzzyAny(threshold, terms),
                value.RequireContainsFuzzyAny(threshold, terms),
                value.EnsureContainsFuzzyAny(threshold, terms));
        }

        [Theory]
        [InlineData("paid Jon Smyth", new[] { "Alice Cooper", "John Smith" }, 0.85, true)]
        [InlineData("paid Bob Marley", new[] { "Alice Cooper", "John Smith" }, 0.85, false)]
        [InlineData(null, new[] { "John" }, 0.5, false)]
        public void ContainsFuzzyAnyInListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, double threshold, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchContainsFuzzyAnyInList(list, threshold),
                start.RejectContainsFuzzyAnyInList(list, threshold),
                start.BreakContainsFuzzyAnyInList(list, threshold),
                start.RequireContainsFuzzyAnyInList(list, threshold),
                start.EnsureContainsFuzzyAnyInList(list, threshold));

            FlowAssert.Kinds(expected,
                value.MatchContainsFuzzyAnyInList(list, threshold),
                value.RejectContainsFuzzyAnyInList(list, threshold),
                value.BreakContainsFuzzyAnyInList(list, threshold),
                value.RequireContainsFuzzyAnyInList(list, threshold),
                value.EnsureContainsFuzzyAnyInList(list, threshold));
        }

        [Theory]
        [InlineData("kitten", new[] { "mitten", "zzzzzz" }, 1, true)]
        [InlineData("kitten", new[] { "sitting" }, 2, false)]
        [InlineData(null, new[] { "a" }, 5, false)]
        public void LevenshteinAtMostAnyInListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, int distance, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchLevenshteinAtMostAnyInList(list, distance),
                start.RejectLevenshteinAtMostAnyInList(list, distance),
                start.BreakLevenshteinAtMostAnyInList(list, distance),
                start.RequireLevenshteinAtMostAnyInList(list, distance),
                start.EnsureLevenshteinAtMostAnyInList(list, distance));

            FlowAssert.Kinds(expected,
                value.MatchLevenshteinAtMostAnyInList(list, distance),
                value.RejectLevenshteinAtMostAnyInList(list, distance),
                value.BreakLevenshteinAtMostAnyInList(list, distance),
                value.RequireLevenshteinAtMostAnyInList(list, distance),
                value.EnsureLevenshteinAtMostAnyInList(list, distance));
        }

        [Theory]
        [InlineData("kitten", new[] { "kitten", "zzzzzz" }, 1, true)]
        [InlineData("kitten", new[] { "sitting" }, 0.9, false)]
        [InlineData(null, new[] { "a" }, 0, false)]
        public void SimilarityAtLeastAnyInListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, double threshold, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchSimilarityAtLeastAnyInList(list, threshold),
                start.RejectSimilarityAtLeastAnyInList(list, threshold),
                start.BreakSimilarityAtLeastAnyInList(list, threshold),
                start.RequireSimilarityAtLeastAnyInList(list, threshold),
                start.EnsureSimilarityAtLeastAnyInList(list, threshold));

            FlowAssert.Kinds(expected,
                value.MatchSimilarityAtLeastAnyInList(list, threshold),
                value.RejectSimilarityAtLeastAnyInList(list, threshold),
                value.BreakSimilarityAtLeastAnyInList(list, threshold),
                value.RequireSimilarityAtLeastAnyInList(list, threshold),
                value.EnsureSimilarityAtLeastAnyInList(list, threshold));
        }

        [Theory]
        [InlineData("MARTHA", new[] { "ZZZZZZ", "MARHTA" }, 0.9, true)]
        [InlineData("MARTHA", new[] { "ZZZZZZ" }, 0.9, false)]
        [InlineData(null, new[] { "a" }, 0.5, false)]
        public void JaroWinklerAtLeastAnyInListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, double threshold, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchJaroWinklerAtLeastAnyInList(list, threshold),
                start.RejectJaroWinklerAtLeastAnyInList(list, threshold),
                start.BreakJaroWinklerAtLeastAnyInList(list, threshold),
                start.RequireJaroWinklerAtLeastAnyInList(list, threshold),
                start.EnsureJaroWinklerAtLeastAnyInList(list, threshold));

            FlowAssert.Kinds(expected,
                value.MatchJaroWinklerAtLeastAnyInList(list, threshold),
                value.RejectJaroWinklerAtLeastAnyInList(list, threshold),
                value.BreakJaroWinklerAtLeastAnyInList(list, threshold),
                value.RequireJaroWinklerAtLeastAnyInList(list, threshold),
                value.EnsureJaroWinklerAtLeastAnyInList(list, threshold));
        }

        [Theory]
        [InlineData("Jon Smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, true)]
        [InlineData("Smith John", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, true)]
        [InlineData("john smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, true)]
        [InlineData("Bob Marley", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, false)]
        [InlineData(null, new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, false)]
        [InlineData("John Smith", null, false)]
        public void SomehowInListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string>? list, bool expected)
        {
            list = list?.ToArray()!;
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchSomehowInList(list),
                start.RejectSomehowInList(list),
                start.BreakSomehowInList(list),
                start.RequireSomehowInList(list),
                start.EnsureSomehowInList(list));

            FlowAssert.Kinds(expected,
                value.MatchSomehowInList(list),
                value.RejectSomehowInList(list),
                value.BreakSomehowInList(list),
                value.RequireSomehowInList(list),
                value.EnsureSomehowInList(list));
        }

        [Theory]
        [InlineData("Jon Smyth", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, "levenshtein=2", true)]
        [InlineData("Jon Smyth", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, "levenshtein=1", false)]
        [InlineData("JOHN SMITH", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, "case,exact", false)]
        [InlineData("Dr John Smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, "notitles,exact", true)]
        [InlineData("Acme Trading Inc", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, "nosuffixes,exact", true)]
        [InlineData("Bob Marley", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, "nonsense", false)]
        public void SomehowInListWithAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, string? options, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchSomehowInListWith(list, options),
                start.RejectSomehowInListWith(list, options),
                start.BreakSomehowInListWith(list, options),
                start.RequireSomehowInListWith(list, options),
                start.EnsureSomehowInListWith(list, options));

            FlowAssert.Kinds(expected,
                value.MatchSomehowInListWith(list, options),
                value.RejectSomehowInListWith(list, options),
                value.BreakSomehowInListWith(list, options),
                value.RequireSomehowInListWith(list, options),
                value.EnsureSomehowInListWith(list, options));
        }

        [Theory]
        [InlineData("Jon Smith", "John Smith", true)]
        [InlineData("Bob", "John Smith", false)]
        [InlineData(null, "John Smith", false)]
        [InlineData("John Smith", null, false)]
        public void SomehowLikeAppliesTheOutcomeOfEveryStepKind(string? value, string? other, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchSomehowLike(other),
                start.RejectSomehowLike(other),
                start.BreakSomehowLike(other),
                start.RequireSomehowLike(other),
                start.EnsureSomehowLike(other));

            FlowAssert.Kinds(expected,
                value.MatchSomehowLike(other),
                value.RejectSomehowLike(other),
                value.BreakSomehowLike(other),
                value.RequireSomehowLike(other),
                value.EnsureSomehowLike(other));
        }

        [Theory]
        [InlineData("Jon Smyth", "John Smith", "lev=2", true)]
        [InlineData("Jon Smyth", "John Smith", "lev=1", false)]
        [InlineData("Jon Smith", "John Smith", "bad option", false)]
        public void SomehowLikeWithAppliesTheOutcomeOfEveryStepKind(string? value, string? other, string? options, bool expected)
        {
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchSomehowLikeWith(other, options),
                start.RejectSomehowLikeWith(other, options),
                start.BreakSomehowLikeWith(other, options),
                start.RequireSomehowLikeWith(other, options),
                start.EnsureSomehowLikeWith(other, options));

            FlowAssert.Kinds(expected,
                value.MatchSomehowLikeWith(other, options),
                value.RejectSomehowLikeWith(other, options),
                value.BreakSomehowLikeWith(other, options),
                value.RequireSomehowLikeWith(other, options),
                value.EnsureSomehowLikeWith(other, options));
        }

        [Theory]
        [InlineData("Payment to Jon Smith today", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, true)]
        [InlineData("Payment to Bob Marley", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, false)]
        [InlineData(null, new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, false)]
        public void ContainsSomehowInListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchContainsSomehowInList(list),
                start.RejectContainsSomehowInList(list),
                start.BreakContainsSomehowInList(list),
                start.RequireContainsSomehowInList(list),
                start.EnsureContainsSomehowInList(list));

            FlowAssert.Kinds(expected,
                value.MatchContainsSomehowInList(list),
                value.RejectContainsSomehowInList(list),
                value.BreakContainsSomehowInList(list),
                value.RequireContainsSomehowInList(list),
                value.EnsureContainsSomehowInList(list));
        }

        [Theory]
        [InlineData("Payment to Jon Smyth today", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, "lev=2", true)]
        [InlineData("Payment to Jon Smyth today", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, "lev=1", false)]
        [InlineData("Payment to Bob", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, "lev=2", false)]
        public void ContainsSomehowInListWithAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, string? options, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchContainsSomehowInListWith(list, options),
                start.RejectContainsSomehowInListWith(list, options),
                start.BreakContainsSomehowInListWith(list, options),
                start.RequireContainsSomehowInListWith(list, options),
                start.EnsureContainsSomehowInListWith(list, options));

            FlowAssert.Kinds(expected,
                value.MatchContainsSomehowInListWith(list, options),
                value.RejectContainsSomehowInListWith(list, options),
                value.BreakContainsSomehowInListWith(list, options),
                value.RequireContainsSomehowInListWith(list, options),
                value.EnsureContainsSomehowInListWith(list, options));
        }

        [Theory]
        [InlineData("JOHN   smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, true)]
        [InlineData("Jon Smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, false)]
        [InlineData("  john\tsmith  ", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, true)]
        [InlineData("J0hn Smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, false)]
        [InlineData("", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, false)]
        [InlineData("John Smith", new string[0], false)]
        public void NormalisedInListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchNormalisedInList(list),
                start.RejectNormalisedInList(list),
                start.BreakNormalisedInList(list),
                start.RequireNormalisedInList(list),
                start.EnsureNormalisedInList(list));

            FlowAssert.Kinds(expected,
                value.MatchNormalisedInList(list),
                value.RejectNormalisedInList(list),
                value.BreakNormalisedInList(list),
                value.RequireNormalisedInList(list),
                value.EnsureNormalisedInList(list));
        }

        [Theory]
        [InlineData("Jon Smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 1, true)]
        [InlineData("Jon Smyth", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 1, false)]
        [InlineData("Jon Smyth", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 2, true)]
        public void LevenshteinInListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, int maxDistance, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchLevenshteinInList(list, maxDistance),
                start.RejectLevenshteinInList(list, maxDistance),
                start.BreakLevenshteinInList(list, maxDistance),
                start.RequireLevenshteinInList(list, maxDistance),
                start.EnsureLevenshteinInList(list, maxDistance));

            FlowAssert.Kinds(expected,
                value.MatchLevenshteinInList(list, maxDistance),
                value.RejectLevenshteinInList(list, maxDistance),
                value.BreakLevenshteinInList(list, maxDistance),
                value.RequireLevenshteinInList(list, maxDistance),
                value.EnsureLevenshteinInList(list, maxDistance));
        }

        [Theory]
        [InlineData("Jhon Smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 1, true)]
        [InlineData("Jhon Smyth", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 1, false)]
        [InlineData("Jhon Smyth", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 2, true)]
        public void DamerauInListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, int maxDistance, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchDamerauInList(list, maxDistance),
                start.RejectDamerauInList(list, maxDistance),
                start.BreakDamerauInList(list, maxDistance),
                start.RequireDamerauInList(list, maxDistance),
                start.EnsureDamerauInList(list, maxDistance));

            FlowAssert.Kinds(expected,
                value.MatchDamerauInList(list, maxDistance),
                value.RejectDamerauInList(list, maxDistance),
                value.BreakDamerauInList(list, maxDistance),
                value.RequireDamerauInList(list, maxDistance),
                value.EnsureDamerauInList(list, maxDistance));
        }

        [Theory]
        [InlineData("Jon Smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 0.85, true)]
        [InlineData("Bob Marley", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 0.85, false)]
        [InlineData("John Smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 1, true)]
        [InlineData("Jon Smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 0.91, false)]
        [InlineData("", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 0.5, false)]
        public void SimilarInListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, double threshold, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchSimilarInList(list, threshold),
                start.RejectSimilarInList(list, threshold),
                start.BreakSimilarInList(list, threshold),
                start.RequireSimilarInList(list, threshold),
                start.EnsureSimilarInList(list, threshold));

            FlowAssert.Kinds(expected,
                value.MatchSimilarInList(list, threshold),
                value.RejectSimilarInList(list, threshold),
                value.BreakSimilarInList(list, threshold),
                value.RequireSimilarInList(list, threshold),
                value.EnsureSimilarInList(list, threshold));
        }

        [Theory]
        [InlineData("Jon Smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 0.95, true)]
        [InlineData("Bob Marley", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 0.95, false)]
        [InlineData("John Smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 1, true)]
        [InlineData("J Smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 0.99, false)]
        [InlineData("John Smith", new string[0], 0.5, false)]
        public void JaroWinklerInListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, double threshold, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchJaroWinklerInList(list, threshold),
                start.RejectJaroWinklerInList(list, threshold),
                start.BreakJaroWinklerInList(list, threshold),
                start.RequireJaroWinklerInList(list, threshold),
                start.EnsureJaroWinklerInList(list, threshold));

            FlowAssert.Kinds(expected,
                value.MatchJaroWinklerInList(list, threshold),
                value.RejectJaroWinklerInList(list, threshold),
                value.BreakJaroWinklerInList(list, threshold),
                value.RequireJaroWinklerInList(list, threshold),
                value.EnsureJaroWinklerInList(list, threshold));
        }

        [Theory]
        [InlineData("Jon Smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 0.8, true)]
        [InlineData("Jon Smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 0.9, false)]
        [InlineData("John Smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 1, true)]
        [InlineData("Bob Marley", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 0.5, false)]
        [InlineData("J", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 0.5, false)]
        public void DiceInListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, double threshold, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchDiceInList(list, threshold),
                start.RejectDiceInList(list, threshold),
                start.BreakDiceInList(list, threshold),
                start.RequireDiceInList(list, threshold),
                start.EnsureDiceInList(list, threshold));

            FlowAssert.Kinds(expected,
                value.MatchDiceInList(list, threshold),
                value.RejectDiceInList(list, threshold),
                value.BreakDiceInList(list, threshold),
                value.RequireDiceInList(list, threshold),
                value.EnsureDiceInList(list, threshold));
        }

        [Theory]
        [InlineData("Jon Smyth", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, true)]
        [InlineData("Bob Marley", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, false)]
        [InlineData("John Smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, true)]
        [InlineData("Smith John", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, false)]
        [InlineData("John", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, false)]
        public void SoundexInListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchSoundexInList(list),
                start.RejectSoundexInList(list),
                start.BreakSoundexInList(list),
                start.RequireSoundexInList(list),
                start.EnsureSoundexInList(list));

            FlowAssert.Kinds(expected,
                value.MatchSoundexInList(list),
                value.RejectSoundexInList(list),
                value.BreakSoundexInList(list),
                value.RequireSoundexInList(list),
                value.EnsureSoundexInList(list));
        }

        [Theory]
        [InlineData("Smith John", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 0.95, true)]
        [InlineData("Smith Bob", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 0.95, false)]
        [InlineData("John Smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 1, true)]
        [InlineData("Garcia Maria", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 1, true)]
        [InlineData("Bob Marley", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 0.95, false)]
        public void TokenSortInListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, double threshold, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchTokenSortInList(list, threshold),
                start.RejectTokenSortInList(list, threshold),
                start.BreakTokenSortInList(list, threshold),
                start.RequireTokenSortInList(list, threshold),
                start.EnsureTokenSortInList(list, threshold));

            FlowAssert.Kinds(expected,
                value.MatchTokenSortInList(list, threshold),
                value.RejectTokenSortInList(list, threshold),
                value.BreakTokenSortInList(list, threshold),
                value.RequireTokenSortInList(list, threshold),
                value.EnsureTokenSortInList(list, threshold));
        }

        [Theory]
        [InlineData("John", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 0.5, true)]
        [InlineData("John", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 0.6, false)]
        [InlineData("Smith John", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 1, true)]
        [InlineData("Bob", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 0.5, false)]
        public void TokenSetInListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, double threshold, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchTokenSetInList(list, threshold),
                start.RejectTokenSetInList(list, threshold),
                start.BreakTokenSetInList(list, threshold),
                start.RequireTokenSetInList(list, threshold),
                start.EnsureTokenSetInList(list, threshold));

            FlowAssert.Kinds(expected,
                value.MatchTokenSetInList(list, threshold),
                value.RejectTokenSetInList(list, threshold),
                value.BreakTokenSetInList(list, threshold),
                value.RequireTokenSetInList(list, threshold),
                value.EnsureTokenSetInList(list, threshold));
        }

        [Theory]
        [InlineData("J Smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, true)]
        [InlineData("J. Smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, true)]
        [InlineData("J S", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, false)]
        [InlineData("K Smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, false)]
        public void InitialsInListAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchInitialsInList(list),
                start.RejectInitialsInList(list),
                start.BreakInitialsInList(list),
                start.RequireInitialsInList(list),
                start.EnsureInitialsInList(list));

            FlowAssert.Kinds(expected,
                value.MatchInitialsInList(list),
                value.RejectInitialsInList(list),
                value.BreakInitialsInList(list),
                value.RequireInitialsInList(list),
                value.EnsureInitialsInList(list));
        }

        [Theory]
        [InlineData("Jon Smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 0.9, null, true)]
        [InlineData("Bob Marley", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 0.9, null, false)]
        [InlineData("Jon Smith", new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 0.89, "similarity", true)]
        [InlineData(null, new[] { "John Smith", "Maria Garcia", "Ahmed Al-Rashid", "Acme Trading Ltd" }, 0, null, false)]
        public void FuzzyScoreAtLeastAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, double threshold, string? options, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchFuzzyScoreAtLeast(list, threshold, options),
                start.RejectFuzzyScoreAtLeast(list, threshold, options),
                start.BreakFuzzyScoreAtLeast(list, threshold, options),
                start.RequireFuzzyScoreAtLeast(list, threshold, options),
                start.EnsureFuzzyScoreAtLeast(list, threshold, options));

            FlowAssert.Kinds(expected,
                value.MatchFuzzyScoreAtLeast(list, threshold, options),
                value.RejectFuzzyScoreAtLeast(list, threshold, options),
                value.BreakFuzzyScoreAtLeast(list, threshold, options),
                value.RequireFuzzyScoreAtLeast(list, threshold, options),
                value.EnsureFuzzyScoreAtLeast(list, threshold, options));
        }

        [Theory]
        [InlineData("John Smith", new[] { "John Smith", "john smith", "Bob" }, 2, "exact", true)]
        [InlineData("John Smith", new[] { "John Smith", "john smith", "Bob" }, 3, "exact", false)]
        [InlineData("John Smith", new[] { "Bob" }, 1, "exact", false)]
        [InlineData("John Smith", new[] { "Bob" }, 0, "exact", true)]
        public void FuzzyMatchCountAtLeastAppliesTheOutcomeOfEveryStepKind(string? value, IEnumerable<string> list, int count, string? options, bool expected)
        {
            list = list.ToArray();
            var start = value.Start();

            FlowAssert.Kinds(expected,
                start.MatchFuzzyMatchCountAtLeast(list, count, options),
                start.RejectFuzzyMatchCountAtLeast(list, count, options),
                start.BreakFuzzyMatchCountAtLeast(list, count, options),
                start.RequireFuzzyMatchCountAtLeast(list, count, options),
                start.EnsureFuzzyMatchCountAtLeast(list, count, options));

            FlowAssert.Kinds(expected,
                value.MatchFuzzyMatchCountAtLeast(list, count, options),
                value.RejectFuzzyMatchCountAtLeast(list, count, options),
                value.BreakFuzzyMatchCountAtLeast(list, count, options),
                value.RequireFuzzyMatchCountAtLeast(list, count, options),
                value.EnsureFuzzyMatchCountAtLeast(list, count, options));
        }
    }
}
