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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit.Abstractions;

namespace Jube.Test.Security.QueryExhaustive
{
    public abstract class ChartPerformanceExhaustiveBase(DatabaseFixture fx, ITestOutputHelper output)
        : ChartExhaustiveBase(fx, output)
    {
        private readonly List<int> performanceRowIds = [];

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await using var db = Fx.GetDbContext();
            await SeedRowsAsync(db, World.A.TrialInstanceId, 0);
            await SeedRowsAsync(db, World.B.TrialInstanceId, 1);
            await SeedRowsAsync(db, World.Edge.TrialInstanceId, 2);
            await SeedNetworkAsync(db, World.A.PromotedId, 4);
            await SeedNetworkAsync(db, World.B.PromotedId, 2);
        }

        public override async Task DisposeAsync()
        {
            await using (var db = Fx.GetDbContext())
            {
                await db.GetTable<ExhaustiveSearchInstancePromotedTrialInstancePredictedActual>()
                    .Where(w => performanceRowIds.Contains(w.Id)).DeleteAsync();
            }

            await base.DisposeAsync();
        }

        private async Task SeedRowsAsync(DbContext db, int trialId, int variant)
        {
            (double predicted, double actual)[] rows =
            [
                (0.95, 1), (0.9, 1), (0.9, 0), (0.8, 1), (0.7, 0), (0.6, 1), (0.5, 0), (0.4, 0), (0.3, 1), (0.2, 0),
                (0.1, 0), (0.05, 0)
            ];

            if (variant == 2)
            {
                rows =
                [
                    (double.NaN, 1), (5, 1), (-5, 0), (0.5, 0), (0.5, 1), (1e300, 1), (-1e300, 0), (0, 0), (1, 1),
                    (double.PositiveInfinity, 1), (0.25, double.NaN)
                ];
            }

            foreach (var (predicted, actual) in rows)
            {
                performanceRowIds.Add(await db.InsertWithInt32IdentityAsync(
                    new ExhaustiveSearchInstancePromotedTrialInstancePredictedActual
                    {
                        ExhaustiveSearchInstanceTrialInstanceId = trialId,
                        Predicted = predicted,
                        Actual = actual,
                        Deleted = 0
                    }));
            }
        }

        private static Task SeedNetworkAsync(DbContext db, int promotedId, int inputs)
        {
            var weights = string.Join(",", Enumerable.Range(0, inputs).Select(i => (0.5 - i * 0.3).ToString("0.0#",
                CultureInfo.InvariantCulture)));

            var second = string.Join(",", Enumerable.Range(0, inputs).Select(i => (i * 0.4 - 0.2).ToString("0.0#",
                CultureInfo.InvariantCulture)));

            var sigmoid = "{\"$type\":\"SigmoidFunction\",\"alpha\":2}";

            var json = "{\"layers\":[{\"neurons\":[{\"weights\":[" + weights +
                       "],\"threshold\":0.1,\"activationFunction\":" +
                       sigmoid + "},{\"weights\":[" + second + "],\"threshold\":-0.1,\"activationFunction\":" +
                       sigmoid +
                       "}]},{\"neurons\":[{\"weights\":[0.8,-0.6],\"threshold\":0,\"activationFunction\":" + sigmoid +
                       "}]}]}";

            return db.GetTable<ExhaustiveSearchInstancePromotedTrialInstance>().Where(w => w.Id == promotedId)
                .Set(s => s.Json, json).UpdateAsync();
        }
    }
}