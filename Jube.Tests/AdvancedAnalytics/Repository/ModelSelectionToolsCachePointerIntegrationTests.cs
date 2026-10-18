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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.AskJooby.DataCache.Models;
using Jube.Data.Repository;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Xunit;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;
using Jube.Analytics.Datasets;

namespace Jube.Test.AdvancedAnalytics.Repository
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ModelSelectionToolsCachePointerIntegrationTests(DatabaseFixture fx)
        : AdvancedAnalyticsRepositoryTestBase(fx)
    {
        private static readonly string[] variableNames = ["a", "b", "c"];

        private sealed class SingleTableCache(CachedTable table) : IToolResultCache
        {
            public Task<string> StoreAsync(CachedTable stored, string? label = null, TimeSpan? ttl = null,
                CancellationToken token = default)
            {
                throw new NotSupportedException("This fake only serves a table already given to it.");
            }

            public Task<CachedTable?> GetAsync(string pointerKey, CancellationToken token = default)
            {
                return Task.FromResult(pointerKey == "tbl_demo" ? table : null);
            }
        }

        [Fact]
        public async Task CompareModelsResolvesARealCachedTableAndReturnsARealComparisonAsync()
        {
            const int rows = 300;
            var random = new Random(11);
            var flat = new double[rows * 4];

            for (var r = 0; r < rows; r++)
            {
                var a = random.NextDouble() * 10;
                flat[r * 4] = a;
                flat[r * 4 + 1] = random.NextDouble() * 10;
                flat[r * 4 + 2] = random.NextDouble() * 10;
                flat[r * 4 + 3] = a > 5 ? 1 : 0;
            }

            var repository = new AdvancedAnalyticsCacheRepository(Db, TenantA, "ZzTestUser");
            var put = await repository.PutAsync(AdvancedAnalyticsPacking.Pack(flat), rows, 4);

            var tools = new ModelSelectionTools(
                new AdvancedAnalyticsTrainingDataPointerReader(Fixture.ConnectionString, TenantA, "ZzTestUser",
                    TestLog.NoOp));

            var result = await tools.CompareModelsAsync(variableNames,
                families: [EvaluationFamily.LogisticRegression, EvaluationFamily.DecisionTree], folds: 3,
                cachePointer: put.Guid);

            result.Ranking.Should().HaveCount(2);
            result.Ranking.Select(r => r.Rank).Should().Equal(1, 2);
            result.Recommendation.Should().NotBeEmpty();
        }

        [Fact]
        public async Task
            PutModelComparisonDataTurnsADataPointerIntoARealCachePointerThatCompareModelsCanThenReadAsync()
        {
            const int rows = 300;
            var random = new Random(13);

            var table = new CachedTable(
                [
                    new CachedColumn("Dependent", "double"), new CachedColumn("a", "double"),
                    new CachedColumn("b", "double"), new CachedColumn("c", "double")
                ],
                [
                    .. Enumerable.Range(0, rows).Select(_ =>
                    {
                        var a = random.NextDouble() * 10;

                        return new Dictionary<string, object?>
                        {
                            ["a"] = a,
                            ["b"] = random.NextDouble() * 10,
                            ["c"] = random.NextDouble() * 10,
                            ["Dependent"] = a > 5 ? 1d : 0d
                        };
                    })
                ]);

            var writer = new AdvancedAnalyticsTrainingDataPointerWriter(Fixture.ConnectionString, TenantA,
                "ZzTestUser", TestLog.NoOp, new SingleTableCache(table));

            var prepared = await writer.PutAsync("tbl_demo", variableNames, "Dependent", CancellationToken.None);

            prepared.Rows.Should().Be(rows);
            prepared.LabelColumn.Should().Be(variableNames.Length);

            var tools = new ModelSelectionTools(
                new AdvancedAnalyticsTrainingDataPointerReader(Fixture.ConnectionString, TenantA, "ZzTestUser",
                    TestLog.NoOp));

            var result = await tools.CompareModelsAsync(variableNames,
                families: [EvaluationFamily.LogisticRegression, EvaluationFamily.DecisionTree], folds: 3,
                cachePointer: prepared.CachePointer, labelColumn: prepared.LabelColumn);

            result.Ranking.Should().HaveCount(2);
            result.Ranking.Select(r => r.Rank).Should().Equal(1, 2);
        }
    }
}