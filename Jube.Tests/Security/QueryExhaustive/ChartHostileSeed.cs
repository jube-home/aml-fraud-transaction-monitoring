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
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Security.QueryExhaustive.Models;
using LinqToDB;

namespace Jube.Test.Security.QueryExhaustive
{
    public sealed class ChartHostileSeed
    {
        public const string MarkupName =
            "<script>alert(1)</script>\"><img src=x onerror=alert(1)>&amp;'</title><svg onload=alert(1)>]]>";

        public const string ControlName = "Qx\u0001Ctl\u0008\u001f\uFFFE\u2028<b>end";
        private readonly List<int> histogramIds = [];
        private readonly List<int> prescriptionIds = [];
        private readonly List<int> sensitivityIds = [];
        private readonly List<int> trialVariableIds = [];
        private readonly List<int> variableIds = [];
        public int MarkupVariableId { get; private set; }
        public int ControlVariableId { get; private set; }

        public static async Task<ChartHostileSeed> CreateAsync(DatabaseFixture fx, Set set)
        {
            var seed = new ChartHostileSeed();
            await using var db = fx.GetDbContext();
            seed.MarkupVariableId = await seed.SeedAsync(db, set, MarkupName, 91);
            seed.ControlVariableId = await seed.SeedAsync(db, set, ControlName, 92);

            return seed;
        }

        public async Task DisposeAsync(DatabaseFixture fx)
        {
            await using var db = fx.GetDbContext();

            await db.GetTable<ExhaustiveSearchInstancePromotedTrialInstanceSensitivity>()
                .Where(w => sensitivityIds.Contains(w.Id)).DeleteAsync();
            await db.GetTable<ExhaustiveSearchInstancePromotedTrialInstanceVariable>()
                .Where(w => prescriptionIds.Contains(w.Id)).DeleteAsync();
            await db.GetTable<ExhaustiveSearchInstanceTrialInstanceVariable>()
                .Where(w => trialVariableIds.Contains(w.Id)).DeleteAsync();
            await db.GetTable<ExhaustiveSearchInstanceVariableHistogram>()
                .Where(w => histogramIds.Contains(w.Id)).DeleteAsync();
            await db.GetTable<ExhaustiveSearchInstanceVariable>().Where(w => variableIds.Contains(w.Id)).DeleteAsync();
        }

        private async Task<int> SeedAsync(DbContext db, Set set, string name, int sequence)
        {
            var variableId = await db.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstanceVariable
            {
                ExhaustiveSearchInstanceId = set.InstanceId, Name = name, Mean = 1, StandardDeviation = 2,
                Kurtosis = 3, Skewness = 4, Maximum = 5, Minimum = 0, Iqr = 6, NormalisationTypeId = 2,
                DistinctValues = 12, Correlation = 1, CorrelationAbsRank = sequence, VariableSequence = sequence,
                ProcessingTypeId = 1, Deleted = 0
            });
            variableIds.Add(variableId);

            var trialVariableId = await db.InsertWithInt32IdentityAsync(
                new ExhaustiveSearchInstanceTrialInstanceVariable
                {
                    ExhaustiveSearchInstanceVariableId = variableId,
                    ExhaustiveSearchInstanceTrialInstanceId = set.TrialInstanceId,
                    Removed = 0, VariableSequence = sequence
                });
            trialVariableIds.Add(trialVariableId);

            prescriptionIds.Add(await db.InsertWithInt32IdentityAsync(
                new ExhaustiveSearchInstancePromotedTrialInstanceVariable
                {
                    ExhaustiveSearchInstanceTrialInstanceVariableId = trialVariableId, Mean = 1, StandardDeviation = 2,
                    Maximum = 3, Minimum = 0
                }));

            sensitivityIds.Add(await db.InsertWithInt32IdentityAsync(
                new ExhaustiveSearchInstancePromotedTrialInstanceSensitivity
                {
                    ExhaustiveSearchInstanceTrialInstanceVariableId = trialVariableId, Sensitivity = 50 + sequence
                }));

            for (var bin = 1; bin <= 3; bin++)
            {
                histogramIds.Add(await db.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstanceVariableHistogram
                {
                    ExhaustiveSearchInstanceVariableId = variableId, BinSequence = bin, BinRangeStart = bin,
                    BinRangeEnd = bin + 1, Frequency = bin * 3
                }));
            }

            return variableId;
        }
    }
}