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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.Repository
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class DictionaryEvalExpressionRepositoryTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<int> createdIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            if (createdIds.Count == 0)
            {
                return;
            }

            await using var dbContext = fx.GetDbContext();
            await dbContext.DictionaryEvalExpression.Where(w => createdIds.Contains(w.Id)).DeleteAsync();
        }

        private async Task<int> SeedAsync(string name, string expression, int resultTypeId, byte? compiled = null,
            string? compileError = null)
        {
            await using var dbContext = fx.GetDbContext();

            var id = await dbContext.InsertWithInt32IdentityAsync(new DictionaryEvalExpression
            {
                Name = $"{DatabaseFixture.Prefix}{name}{Guid.NewGuid():N}",
                Expression = expression,
                ResultTypeId = resultTypeId,
                Compiled = compiled,
                CompileError = compileError
            });

            createdIds.Add(id);
            return id;
        }

        [Fact]
        public async Task GetAsyncReturnsASeededRowAsync()
        {
            var id = await SeedAsync("IsCorporateEmail", "!value.EndsWith(\"gmail.com\")", 5);

            await using var dbContext = fx.GetDbContext();
            var repository = new DictionaryEvalExpressionRepository(dbContext);

            var rows = (await repository.GetAsync()).ToList();

            rows.Should().Contain(r => r.Id == id && r.Expression == "!value.EndsWith(\"gmail.com\")" &&
                                       r.ResultTypeId == 5);
        }

        [Fact]
        public async Task GetAsyncReturnsMultipleSeededRowsAsync()
        {
            var firstId = await SeedAsync("First", "value.Length > 0", 5);
            var secondId = await SeedAsync("Second", "value.ToUpper()", 1);

            await using var dbContext = fx.GetDbContext();
            var repository = new DictionaryEvalExpressionRepository(dbContext);

            var rows = (await repository.GetAsync()).Select(r => r.Id).ToList();

            rows.Should().Contain(firstId);
            rows.Should().Contain(secondId);
        }

        [Fact]
        public async Task UpdateCompileStatusAsyncMarksARowAsSuccessfullyCompiledAsync()
        {
            var id = await SeedAsync("Pending", "value.Length > 0", 5);

            await using var dbContext = fx.GetDbContext();
            var repository = new DictionaryEvalExpressionRepository(dbContext);

            await repository.UpdateCompileStatusAsync(id, true, null);

            var row = (await repository.GetAsync()).Single(r => r.Id == id);
            row.Compiled.Should().Be(1);
            row.CompileError.Should().BeNull();
        }

        [Fact]
        public async Task UpdateCompileStatusAsyncRecordsAFailureAndItsErrorMessageAsync()
        {
            var id = await SeedAsync("Bad", "value.", 1);

            await using var dbContext = fx.GetDbContext();
            var repository = new DictionaryEvalExpressionRepository(dbContext);

            await repository.UpdateCompileStatusAsync(id, false, "unexpected token at end of expression");

            var row = (await repository.GetAsync()).Single(r => r.Id == id);
            row.Compiled.Should().Be(0);
            row.CompileError.Should().Be("unexpected token at end of expression");
        }

        [Fact]
        public async Task UpdateCompileStatusAsyncOverwritesAPreviousCompileErrorOnceFixedAsync()
        {
            var id = await SeedAsync("FixedLater", "value.Length > 0", 5, 0, "some earlier failure");

            await using var dbContext = fx.GetDbContext();
            var repository = new DictionaryEvalExpressionRepository(dbContext);

            await repository.UpdateCompileStatusAsync(id, true, null);

            var row = (await repository.GetAsync()).Single(r => r.Id == id);
            row.Compiled.Should().Be(1);
            row.CompileError.Should().BeNull();
        }

        [Fact]
        public async Task UpdateCompileStatusAsyncOnlyAffectsTheTargetedRowAsync()
        {
            var untouchedId = await SeedAsync("Untouched", "value.Length > 0", 5, 1);
            var targetId = await SeedAsync("Target", "value.Length > 0", 5);

            await using var dbContext = fx.GetDbContext();
            var repository = new DictionaryEvalExpressionRepository(dbContext);

            await repository.UpdateCompileStatusAsync(targetId, false, "boom");

            var rows = (await repository.GetAsync()).ToList();
            rows.Single(r => r.Id == untouchedId).Compiled.Should().Be(1);
            rows.Single(r => r.Id == untouchedId).CompileError.Should().BeNull();
            rows.Single(r => r.Id == targetId).Compiled.Should().Be(0);
            rows.Single(r => r.Id == targetId).CompileError.Should().Be("boom");
        }
    }
}