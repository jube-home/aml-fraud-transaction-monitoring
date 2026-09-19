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
using Jube.Data.Context;
using Jube.Data.Query;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Infrastructure.ModelScaffolding;
using LinqToDB;
using LinqToDB.Data;
using Xunit;
using Xunit.Abstractions;

namespace Jube.Test.Service.Query.ModelFieldReflection
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ModelFieldReflectionActiveTests(DatabaseFixture fx, ITestOutputHelper output)
    {
        private delegate Task<List<int>> ActiveIds(DbContext dbContext, ModelScaffold model);

        private delegate Task<int> SetActive(DbContext dbContext, List<int> ids, byte active);

        private static readonly (string Kind, ActiveIds Ids, SetActive Set)[] kinds =
        [
            ("RequestXPath",
                (db, m) => db.EntityAnalysisModelRequestXpath
                    .Where(w => w.EntityAnalysisModelId == m.ModelId && w.Active == 1).Select(w => w.Id).ToListAsync(),
                (db, ids, a) => db.EntityAnalysisModelRequestXpath.Where(w => ids.Contains(w.Id)).Set(w => w.Active, a)
                    .UpdateAsync()),
            ("InlineScript",
                (db, m) => db.EntityAnalysisModelInlineScript
                    .Where(w => w.EntityAnalysisModelId == m.ModelId && w.Active == 1).Select(w => w.Id).ToListAsync(),
                (db, ids, a) => db.EntityAnalysisModelInlineScript.Where(w => ids.Contains(w.Id)).Set(w => w.Active, a)
                    .UpdateAsync()),
            ("InlineFunction",
                (db, m) => db.EntityAnalysisModelInlineFunction
                    .Where(w => w.EntityAnalysisModelId == m.ModelId && w.Active == 1).Select(w => w.Id).ToListAsync(),
                (db, ids, a) => db.EntityAnalysisModelInlineFunction.Where(w => ids.Contains(w.Id))
                    .Set(w => w.Active, a).UpdateAsync()),
            ("Dictionary",
                (db, m) => db.EntityAnalysisModelDictionary
                    .Where(w => w.EntityAnalysisModelGuid == m.ModelGuid && w.Active == 1).Select(w => w.Id)
                    .ToListAsync(),
                (db, ids, a) => db.EntityAnalysisModelDictionary.Where(w => ids.Contains(w.Id)).Set(w => w.Active, a)
                    .UpdateAsync()),
            ("TtlCounter",
                (db, m) => db.EntityAnalysisModelTtlCounter
                    .Where(w => w.EntityAnalysisModelId == m.ModelId && w.Active == 1).Select(w => w.Id).ToListAsync(),
                (db, ids, a) => db.EntityAnalysisModelTtlCounter.Where(w => ids.Contains(w.Id)).Set(w => w.Active, a)
                    .UpdateAsync()),
            ("Sanction",
                (db, m) => db.EntityAnalysisModelSanction
                    .Where(w => w.EntityAnalysisModelId == m.ModelId && w.Active == 1).Select(w => w.Id).ToListAsync(),
                (db, ids, a) => db.EntityAnalysisModelSanction.Where(w => ids.Contains(w.Id)).Set(w => w.Active, a)
                    .UpdateAsync()),
            ("AbstractionRule",
                (db, m) => db.EntityAnalysisModelAbstractionRule
                    .Where(w => w.EntityAnalysisModelId == m.ModelId && w.Active == 1).Select(w => w.Id).ToListAsync(),
                (db, ids, a) => db.EntityAnalysisModelAbstractionRule.Where(w => ids.Contains(w.Id))
                    .Set(w => w.Active, a).UpdateAsync()),
            ("AbstractionCalculation",
                (db, m) => db.EntityAnalysisModelAbstractionCalculation
                    .Where(w => w.EntityAnalysisModelId == m.ModelId && w.Active == 1).Select(w => w.Id).ToListAsync(),
                (db, ids, a) => db.EntityAnalysisModelAbstractionCalculation.Where(w => ids.Contains(w.Id))
                    .Set(w => w.Active, a).UpdateAsync()),
            ("HttpAdaptation",
                (db, m) => db.EntityAnalysisModelHttpAdaptation
                    .Where(w => w.EntityAnalysisModelId == m.ModelId && w.Active == 1).Select(w => w.Id).ToListAsync(),
                (db, ids, a) => db.EntityAnalysisModelHttpAdaptation.Where(w => ids.Contains(w.Id))
                    .Set(w => w.Active, a).UpdateAsync()),
            ("ExhaustiveSearchInstance",
                (db, m) => db.ExhaustiveSearchInstance.Where(w => w.EntityAnalysisModelId == m.ModelId && w.Active == 1)
                    .Select(w => w.Id).ToListAsync(),
                (db, ids, a) => db.ExhaustiveSearchInstance.Where(w => ids.Contains(w.Id)).Set(w => w.Active, a)
                    .UpdateAsync()),
            ("Tag",
                (db, m) => db.EntityAnalysisModelTag.Where(w => w.EntityAnalysisModelId == m.ModelId && w.Active == 1)
                    .Select(w => w.Id).ToListAsync(),
                (db, ids, a) => db.EntityAnalysisModelTag.Where(w => ids.Contains(w.Id)).Set(w => w.Active, a)
                    .UpdateAsync()),
            ("List",
                (db, m) => db.EntityAnalysisModelList
                    .Where(w => w.EntityAnalysisModelGuid == m.ModelGuid && w.Active == 1).Select(w => w.Id)
                    .ToListAsync(),
                (db, ids, a) => db.EntityAnalysisModelList.Where(w => ids.Contains(w.Id)).Set(w => w.Active, a)
                    .UpdateAsync())
        ];

        private static readonly (string Table, bool ByGuid)[] tables =
        [
            ("EntityAnalysisModelRequestXpath", false), ("EntityAnalysisModelInlineScript", false),
            ("EntityAnalysisModelInlineFunction", false), ("EntityAnalysisModelDictionary", true),
            ("EntityAnalysisModelTtlCounter", false), ("EntityAnalysisModelSanction", false),
            ("EntityAnalysisModelAbstractionRule", false), ("EntityAnalysisModelAbstractionCalculation", false),
            ("EntityAnalysisModelHttpAdaptation", false), ("ExhaustiveSearchInstance", false),
            ("EntityAnalysisModelTag", false), ("EntityAnalysisModelList", true)
        ];

        private static async Task MakeLiveAsync(DbContext dbContext, ModelScaffold model)
        {
            foreach (var (table, byGuid) in tables)
            {
                var column = byGuid ? "EntityAnalysisModelGuid" : "EntityAnalysisModelId";
                var parameter = byGuid
                    ? new DataParameter("p", model.ModelGuid, DataType.Guid)
                    : new DataParameter("p", model.ModelId, DataType.Int32);
                await dbContext.ExecuteAsync($"UPDATE \"{table}\" SET \"Deleted\" = 0 WHERE \"{column}\" = @p",
                    parameter);
            }
        }

        private static async Task<List<string>> FieldNamesAsync(DbContext dbContext, ModelScaffold model,
            bool reporting = true)
        {
            var query = new GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery(dbContext,
                model.TenantRegistryId);
            var fields = await query.ExecuteAsync(model.ModelId, 5, reporting);
            return [.. fields.Select(s => s.Name).OrderBy(s => s, StringComparer.Ordinal)];
        }

        [Fact]
        public async Task EveryKindOfInactiveItemIsNotListedAndReactivationListsItAgainAsync()
        {
            await using var model = await ModelScaffold.CreateAsync(ModelScaffoldOptions.Isolated);
            await using var other = await ModelScaffold.CreateAsync(ModelScaffoldOptions.Isolated);
            await using var dbContext = fx.GetDbContext();
            await MakeLiveAsync(dbContext, model);
            await MakeLiveAsync(dbContext, other);
            var baseline = await FieldNamesAsync(dbContext, model);
            var otherBaseline = await FieldNamesAsync(dbContext, other);
            baseline.Should().NotBeEmpty();

            var changed = 0;
            foreach (var (kind, idsOf, set) in kinds)
            {
                var ids = await idsOf(dbContext, model);
                var rows = await set(dbContext, ids, 0);
                var inactive = await FieldNamesAsync(dbContext, model);
                output.WriteLine($"{kind}: {rows} row(s) deactivated, {baseline.Count} -> {inactive.Count} fields");

                if (rows > 0 && inactive.Count < baseline.Count)
                {
                    changed++;
                }

                inactive.Count.Should().BeLessThanOrEqualTo(baseline.Count, kind);
                (await set(dbContext, ids, 1)).Should().Be(rows, kind);
                (await FieldNamesAsync(dbContext, model)).Should().Equal(baseline, kind + " reactivated");
                (await FieldNamesAsync(dbContext, other)).Should().Equal(otherBaseline, kind + " other model");
            }

            changed.Should().BeGreaterThan(5,
                "the example model carries many kinds of item, each of which hides when inactive");
        }

        [Fact]
        public async Task DeactivatingEveryKindLeavesNoConfiguredItemListedAsync()
        {
            await using var model = await ModelScaffold.CreateAsync(ModelScaffoldOptions.Isolated);
            await using var dbContext = fx.GetDbContext();
            await MakeLiveAsync(dbContext, model);
            var baseline = await FieldNamesAsync(dbContext, model);

            foreach (var (_, idsOf, set) in kinds)
            {
                await set(dbContext, await idsOf(dbContext, model), 0);
            }

            var none = await FieldNamesAsync(dbContext, model);
            none.Count.Should().BeLessThan(baseline.Count);
            none.Should().BeSubsetOf(baseline);
        }

        [Fact]
        public async Task InactiveListsAreNotListedAndReactivationListsThemAgainAsync()
        {
            await using var model = await ModelScaffold.CreateAsync(ModelScaffoldOptions.Isolated);
            await using var dbContext = fx.GetDbContext();
            await MakeLiveAsync(dbContext, model);
            var (_, idsOf, set) = kinds.Single(k => k.Kind == "List");
            var ids = await idsOf(dbContext, model);
            ids.Should().NotBeEmpty();
            var baseline = await FieldNamesAsync(dbContext, model, false);
            baseline.Count(name => name.StartsWith("List.", StringComparison.Ordinal)).Should().BeGreaterThan(0);

            await set(dbContext, ids, 0);
            (await FieldNamesAsync(dbContext, model, false))
                .Count(name => name.StartsWith("List.", StringComparison.Ordinal))
                .Should().Be(0);

            await set(dbContext, ids, 1);
            (await FieldNamesAsync(dbContext, model, false)).Should().Equal(baseline);
        }
    }
}