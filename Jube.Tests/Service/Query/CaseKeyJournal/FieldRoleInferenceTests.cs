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
using Jube.Dto.Options;
using Jube.Service.Query.CaseKeyJournal;
using Xunit;

namespace Jube.Test.Service.Query.CaseKeyJournal
{
    [Trait("Category", "Unit")]
    public sealed class FieldRoleInferenceTests
    {
        private static IReadOnlyCollection<object?> Strings(params string?[] values) => values;

        [Fact]
        public void AConfiguredRoleAlwaysWinsAndIsNotCalledInferred()
        {
            var (role, inferred) = FieldRoleInference.Infer((int)JournalFieldRole.Counterparty, "number", true,
                Strings("1", "2"));

            role.Should().Be((int)JournalFieldRole.Counterparty);
            inferred.Should().BeFalse();
        }

        [Theory]
        [InlineData("number", false, JournalFieldRole.Measure)]
        [InlineData("date", false, JournalFieldRole.Time)]
        [InlineData("boolean", false, JournalFieldRole.Category)]
        [InlineData("array", false, JournalFieldRole.Category)]
        [InlineData("number", true, JournalFieldRole.Identifier)]
        [InlineData("string", true, JournalFieldRole.Identifier)]
        public void TheTypeAndTheDrillFlagDecideWhenNothingIsConfigured(string type, bool drill,
            JournalFieldRole expected)
        {
            var (role, inferred) = FieldRoleInference.Infer(0, type, drill, Strings("a"));

            role.Should().Be((int)expected);
            inferred.Should().BeTrue();
        }

        [Fact]
        public void AFewRepeatedStringsAreACategoryAndManyDistinctOnesAreAnIdentifier()
        {
            var channels = Enumerable.Range(0, 40).Select(index => index % 3 == 0 ? "App" : "Web").ToArray();
            var accounts = Enumerable.Range(0, 40).Select(index => $"ACC-{index}").ToArray();

            FieldRoleInference.Infer(0, "string", false, Strings(channels)).RoleId
                .Should().Be((int)JournalFieldRole.Category);
            FieldRoleInference.Infer(0, "string", false, Strings(accounts)).RoleId
                .Should().Be((int)JournalFieldRole.Identifier);
        }

        [Fact]
        public void WithFewRowsALowNumberOfDistinctValuesIsStillACategory()
        {
            FieldRoleInference.Infer(0, "string", false, Strings("a", "b", "c")).RoleId
                .Should().Be((int)JournalFieldRole.Category);
        }

        [Fact]
        public void TooManyDistinctValuesIsNeverACategoryEvenWithFewRows()
        {
            var many = Enumerable.Range(0, FieldRoleInference.MaxCategoryValues + 1).Select(i => $"v{i}").ToArray();

            FieldRoleInference.Infer(0, "string", false, Strings(many)).RoleId
                .Should().Be((int)JournalFieldRole.Identifier);
        }

        [Fact]
        public void AColumnWithNoValuesIsLeftAsACategoryRatherThanFailing()
        {
            FieldRoleInference.Infer(0, "string", false, Strings(null, "")).RoleId
                .Should().Be((int)JournalFieldRole.Category);
        }
    }
}
