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
using FluentValidation.Results;
using Jube.Data.Query.GetPostgresSchemaSnapshotQuery.Models;
using Jube.Data.Reporting.Check;
using Jube.Service.Repository.ExhaustiveSearchInstance;
using Xunit;

namespace Jube.Test.Reporting.Check
{
    [Trait("Category", "Unit")]
    public sealed class ExhaustiveFilterSqlSchemaCheckTests
    {
        private static readonly SqlSchemaSnapshot snapshot = new(new PostgresSchemaSnapshotRows(
            [
                new PostgresSchemaColumnRow("public", "Archive", "EntityAnalysisModelId", "int4", "N"),
                new PostgresSchemaColumnRow("public", "Archive", "Json", "jsonb", "U")
            ],
            [],
            [new PostgresSchemaFunctionRow("count", 0, 0, false, "a")],
            ["int4", "jsonb"]));

        [Fact]
        public void AFilterOverAColumnThatDoesNotExistIsRefusedAtSaveTime()
        {
            var results = new ValidationResult();

            ExhaustiveFilterSqlSchemaCheck.Append(results, "\"Nope\" = 1", snapshot);

            var failure = results.Errors.Should().ContainSingle().Subject;
            failure.PropertyName.Should().Be("FilterSql");
            failure.ErrorCode.Should().Be("FilterSqlSchema");
            failure.ErrorMessage.Should().Contain("\"Nope\"").And.Contain("(1:1)");
        }

        [Fact]
        public void AFilterOverTheArchiveColumnsIsAccepted()
        {
            var results = new ValidationResult();

            ExhaustiveFilterSqlSchemaCheck.Append(results, "\"EntityAnalysisModelId\" = @1 and \"Json\"->>'a' = 'x'",
                snapshot);

            results.Errors.Should().BeEmpty();
        }

        [Fact]
        public void AFilterAlreadyRefusedByTheStructuralRulesIsNotCheckedAgain()
        {
            var results = new ValidationResult([new ValidationFailure("FilterSql", "x")]);

            ExhaustiveFilterSqlSchemaCheck.Append(results, "\"Nope\" = 1", snapshot);

            results.Errors.Should().ContainSingle();
        }
    }
}
