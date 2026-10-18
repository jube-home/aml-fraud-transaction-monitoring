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
    public sealed class StatisticsChartSeed
    {
        public const string MarkupName =
            "<script>alert(1)</script>\"><img src=x onerror=alert(1)>&amp;'</title><svg onload=alert(1)>]]>";

        public const string ControlName = "Qx\u0001Ctl\u0008\u001f\uFFFE\u2028<b>end";
        private readonly List<int> classIds = [];
        private readonly List<int> histogramIds = [];
        private readonly List<int> prescriptionHistogramIds = [];
        private readonly List<int> promotedVariableIds = [];
        private readonly List<int> trialVariableIds = [];
        private readonly List<int> variableIds = [];
        public int MarkupVariableId { get; private set; }
        public int ControlVariableId { get; private set; }

        public static async Task<StatisticsChartSeed> CreateAsync(DatabaseFixture fx, QueryExhaustiveWorld world)
        {
            var seed = new StatisticsChartSeed();
            await using var db = fx.GetDbContext();

            foreach (var set in new[] { world.A, world.B, world.Deleted })
            {
                await seed.SeedExistingAsync(db, set, false);
            }

            await seed.SeedExistingAsync(db, world.Edge, true);
            seed.MarkupVariableId = await seed.SeedHostileAsync(db, world.A, MarkupName, 91);
            seed.ControlVariableId = await seed.SeedHostileAsync(db, world.A, ControlName, 92);

            return seed;
        }

        public async Task DisposeAsync(DatabaseFixture fx)
        {
            await using var db = fx.GetDbContext();

            await db.GetTable<ExhaustiveSearchInstancePrescriptionHistogram>()
                .Where(w => prescriptionHistogramIds.Contains(w.Id)).DeleteAsync();
            await db.GetTable<ExhaustiveSearchInstanceVariableHistogramClass>().Where(w => classIds.Contains(w.Id))
                .DeleteAsync();
            await db.GetTable<ExhaustiveSearchInstancePromotedTrialInstanceVariable>()
                .Where(w => promotedVariableIds.Contains(w.Id)).DeleteAsync();
            await db.GetTable<ExhaustiveSearchInstanceTrialInstanceVariable>()
                .Where(w => trialVariableIds.Contains(w.Id)).DeleteAsync();
            await db.GetTable<ExhaustiveSearchInstanceVariableHistogram>().Where(w => histogramIds.Contains(w.Id))
                .DeleteAsync();
            await db.GetTable<ExhaustiveSearchInstanceVariable>().Where(w => variableIds.Contains(w.Id)).DeleteAsync();
        }

        private async Task SeedExistingAsync(DbContext db, Set set, bool nasty)
        {
            if (set.VariableId == 0)
            {
                return;
            }

            var promotedVariableId = await db.GetTable<ExhaustiveSearchInstanceTrialInstanceVariable>()
                .Where(w => w.ExhaustiveSearchInstanceVariableId == set.VariableId
                            && w.ExhaustiveSearchInstanceTrialInstanceId == set.TrialInstanceId)
                .SelectMany(t => db.GetTable<ExhaustiveSearchInstancePromotedTrialInstanceVariable>()
                        .Where(p => p.ExhaustiveSearchInstanceTrialInstanceVariableId == t.Id),
                    (t, p) => (int?)p.Id)
                .FirstOrDefaultAsync();

            if (nasty)
            {
                await AddClassAsync(db, set.VariableId, 0, (double.NaN, double.PositiveInfinity, int.MaxValue),
                    (1, 1e308, int.MaxValue));

                await AddClassAsync(db, set.VariableId, 1, (double.NaN, double.PositiveInfinity, 0),
                    (1, 1e308, int.MaxValue));

                if (promotedVariableId != null)
                {
                    await AddPrescriptionAsync(db, promotedVariableId.Value, (double.NegativeInfinity, double.NaN, -1),
                        (0, 1e-320, int.MaxValue), (1e-320, 1e308, 0));
                }

                return;
            }

            await AddClassAsync(db, set.VariableId, 0, (set.K, set.K + 1, 5), (set.K + 1, set.K + 2, 4));
            await AddClassAsync(db, set.VariableId, 1, (set.K, set.K + 1, 2), (set.K + 1, set.K + 2, 5));

            if (promotedVariableId != null)
            {
                await AddPrescriptionAsync(db, promotedVariableId.Value, (set.K, set.K + 1, 3),
                    (set.K + 1, set.K + 2, 6),
                    (set.K + 2, set.K + 3, 2));
            }
        }

        private async Task<int> SeedHostileAsync(DbContext db, Set set, string name, int sequence)
        {
            var variableId = await db.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstanceVariable
            {
                ExhaustiveSearchInstanceId = set.InstanceId, Name = name, Mean = 1, StandardDeviation = 2, Kurtosis = 3,
                Skewness = 4, Maximum = 5, Minimum = 0, Iqr = 6, NormalisationTypeId = 2, DistinctValues = 12,
                Correlation = 1, CorrelationAbsRank = sequence, VariableSequence = sequence, ProcessingTypeId = 1,
                Deleted = 0
            });
            variableIds.Add(variableId);

            var trialVariableId = await db.InsertWithInt32IdentityAsync(
                new ExhaustiveSearchInstanceTrialInstanceVariable
                {
                    ExhaustiveSearchInstanceVariableId = variableId,
                    ExhaustiveSearchInstanceTrialInstanceId = set.TrialInstanceId, Removed = 0,
                    VariableSequence = sequence
                });
            trialVariableIds.Add(trialVariableId);

            var promotedVariableId = await db.InsertWithInt32IdentityAsync(
                new ExhaustiveSearchInstancePromotedTrialInstanceVariable
                {
                    ExhaustiveSearchInstanceTrialInstanceVariableId = trialVariableId, Mean = 1, StandardDeviation = 2,
                    Maximum = 3, Minimum = 0, Iqr = 1
                });
            promotedVariableIds.Add(promotedVariableId);

            for (var bin = 1; bin <= 3; bin++)
            {
                histogramIds.Add(await db.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstanceVariableHistogram
                {
                    ExhaustiveSearchInstanceVariableId = variableId, BinSequence = bin, BinRangeStart = bin,
                    BinRangeEnd = bin + 1, Frequency = bin * 3
                }));
            }

            await AddClassAsync(db, variableId, 0, (1, 2, 4), (2, 3, 6), (3, 4, 8));
            await AddClassAsync(db, variableId, 1, (1, 2, 1), (2, 3, 2), (3, 4, 1));
            await AddPrescriptionAsync(db, promotedVariableId, (1, 2, 2), (2, 3, 5), (3, 4, 3));

            return variableId;
        }

        private async Task AddClassAsync(DbContext db, int variableId, int label,
            params (double Start, double End, int F)[] bins)
        {
            var sequence = 0;

            foreach (var (start, end, f) in bins)
            {
                classIds.Add(await db.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstanceVariableHistogramClass
                {
                    ExhaustiveSearchInstanceVariableId = variableId, ClassLabel = label, BinSequence = sequence++,
                    BinRangeStart = start, BinRangeEnd = end, Frequency = f
                }));
            }
        }

        private async Task AddPrescriptionAsync(DbContext db, int promotedVariableId,
            params (double Start, double End, int F)[] bins)
        {
            var sequence = 0;

            foreach (var (start, end, f) in bins)
            {
                prescriptionHistogramIds.Add(await db.InsertWithInt32IdentityAsync(
                    new ExhaustiveSearchInstancePrescriptionHistogram
                    {
                        ExhaustiveSearchInstancePromotedTrialInstanceVariableId = promotedVariableId,
                        BinIndex = sequence++, BinRangeStart = start, BinRangeEnd = end, Frequency = f
                    }));
            }
        }
    }
}