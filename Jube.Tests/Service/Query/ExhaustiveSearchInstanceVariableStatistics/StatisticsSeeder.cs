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
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;

namespace Jube.Test.Service.Query.ExhaustiveSearchInstanceVariableStatistics
{
    internal sealed class StatisticsSeeder(DatabaseFixture fx)
    {
        private readonly List<int> anomalyHistogramIds = [];
        private readonly List<int> anomalyIds = [];
        private readonly List<int> classHistogramIds = [];
        private readonly List<int> classificationHistogramIds = [];
        private readonly List<int> classificationIds = [];
        private readonly List<int> histogramIds = [];
        private readonly List<int> modelIds = [];
        private readonly List<int> prescriptionHistogramIds = [];
        private readonly List<int> promotedIds = [];
        private readonly List<int> promotedVariableIds = [];
        private readonly List<int> searchInstanceIds = [];
        private readonly List<int> trialInstanceIds = [];
        private readonly List<int> trialVariableIds = [];
        private readonly List<int> variableIds = [];

        public async Task DisposeAsync()
        {
            await using var db = fx.GetDbContext();

            await db.GetTable<ExhaustiveSearchInstancePrescriptionHistogram>()
                .Where(w => prescriptionHistogramIds.Contains(w.Id)).DeleteAsync();
            await db.GetTable<ExhaustiveSearchInstancePromotedTrialInstanceVariable>()
                .Where(w => promotedVariableIds.Contains(w.Id)).DeleteAsync();

            await db.GetTable<Data.Poco.ExhaustiveSearchInstanceTrialInstanceVariable>()
                .Where(w => trialVariableIds.Contains(w.Id)).DeleteAsync();

            await db.GetTable<ExhaustiveSearchInstancePromotedTrialInstance>()
                .Where(w => promotedIds.Contains(w.Id)).DeleteAsync();
            await db.GetTable<ExhaustiveSearchInstanceTrialInstance>()
                .Where(w => trialInstanceIds.Contains(w.Id)).DeleteAsync();
            await db.GetTable<ExhaustiveSearchInstanceVariableHistogramClass>()
                .Where(w => classHistogramIds.Contains(w.Id)).DeleteAsync();
            await db.GetTable<ExhaustiveSearchInstanceVariableHistogramClassification>()
                .Where(w => classificationHistogramIds.Contains(w.Id)).DeleteAsync();
            await db.GetTable<ExhaustiveSearchInstanceVariableHistogramAnomaly>()
                .Where(w => anomalyHistogramIds.Contains(w.Id)).DeleteAsync();
            await db.GetTable<ExhaustiveSearchInstanceVariableClassification>()
                .Where(w => classificationIds.Contains(w.Id)).DeleteAsync();
            await db.GetTable<ExhaustiveSearchInstanceVariableAnomaly>()
                .Where(w => anomalyIds.Contains(w.Id)).DeleteAsync();
            await db.GetTable<ExhaustiveSearchInstanceVariableHistogram>()
                .Where(w => histogramIds.Contains(w.Id)).DeleteAsync();

            await db.GetTable<Data.Poco.ExhaustiveSearchInstanceVariable>().Where(w => variableIds.Contains(w.Id))
                .DeleteAsync();

            foreach (var id in searchInstanceIds)
            {
                await db.GetTable<ExhaustiveSearchInstanceVersion>().Where(w => w.ExhaustiveSearchInstanceId == id)
                    .DeleteAsync();

                await db.GetTable<Data.Poco.ExhaustiveSearchInstance>().Where(w => w.Id == id).DeleteAsync();
            }

            foreach (var modelId in modelIds)
            {
                await db.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                    .DeleteAsync();

                await db.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        public async Task<Instance> InstanceAsync(DbContext db, string user, bool withPromotedTrial = true,
            byte deleted = 0)
        {
            var model = await new EntityAnalysisModelRepository(db, user).InsertAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}St{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(), Active = 1,
                Locked = 0, Deleted = 0
            });
            modelIds.Add(model.Id);

            var instance = await new ExhaustiveSearchInstanceRepository(db, user).InsertAsync(
                new Data.Poco.ExhaustiveSearchInstance
                {
                    Name = $"{DatabaseFixture.Prefix}St{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(),
                    EntityAnalysisModelId = model.Id, Active = 1, Locked = 0, Deleted = deleted, StatusId = 0
                });
            searchInstanceIds.Add(instance.Id);

            if (!withPromotedTrial)
            {
                return new Instance(instance.Id, 0);
            }

            var trial = await new ExhaustiveSearchInstanceTrialInstanceRepository(db).InsertAsync(
                new ExhaustiveSearchInstanceTrialInstance
                {
                    ExhaustiveSearchInstanceId = instance.Id, CreatedDate = DateTime.UtcNow
                });
            trialInstanceIds.Add(trial.Id);

            promotedIds.Add(await db.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstancePromotedTrialInstance
            {
                ExhaustiveSearchInstanceTrialInstanceId = trial.Id, Active = 1, Score = 0.9, Json = "{}",
                CreatedDate = DateTime.UtcNow
            }));

            return new Instance(instance.Id, trial.Id);
        }

        public async Task<int> VariableAsync(DbContext db, Instance instance, string name, byte deleted = 0)
        {
            var id = await db.InsertWithInt32IdentityAsync(new Data.Poco.ExhaustiveSearchInstanceVariable
            {
                ExhaustiveSearchInstanceId = instance.Id, Name = name, Mean = 5, StandardDeviation = 2, Minimum = 0,
                Maximum = 10, Iqr = 3, Deleted = deleted
            });
            variableIds.Add(id);

            return id;
        }

        public async Task PopulationAsync(DbContext db, int variableId, params (double Start, double End, int F)[] bins)
        {
            var sequence = 0;

            foreach (var (start, end, f) in bins)
            {
                histogramIds.Add(await db.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstanceVariableHistogram
                {
                    ExhaustiveSearchInstanceVariableId = variableId, BinSequence = sequence++, BinRangeStart = start,
                    BinRangeEnd = end, Frequency = f
                }));
            }
        }

        public async Task ClassAsync(DbContext db, int variableId, int label, byte deleted,
            params (double Start, double End, int F)[] bins)
        {
            var sequence = 0;

            foreach (var (start, end, f) in bins)
            {
                classHistogramIds.Add(await db.InsertWithInt32IdentityAsync(
                    new ExhaustiveSearchInstanceVariableHistogramClass
                    {
                        ExhaustiveSearchInstanceVariableId = variableId, ClassLabel = label, BinSequence = sequence++,
                        BinRangeStart = start, BinRangeEnd = end, Frequency = f, Deleted = deleted
                    }));
            }
        }

        public async Task ClassificationAsync(DbContext db, int variableId, double mean,
            params (double Start, double End, int F)[] bins)
        {
            var id = await db.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstanceVariableClassification
            {
                ExhaustiveSearchInstanceVariableId = variableId, Mean = mean, StandardDeviation = 1, Minimum = 6,
                Maximum = 10, Iqr = 1.5
            });
            classificationIds.Add(id);
            var sequence = 0;

            foreach (var (start, end, f) in bins)
            {
                classificationHistogramIds.Add(await db.InsertWithInt32IdentityAsync(
                    new ExhaustiveSearchInstanceVariableHistogramClassification
                    {
                        ExhaustiveSearchInstanceVariableClassificationId = id, BinSequence = sequence++,
                        BinRangeStart = start, BinRangeEnd = end, Frequency = f
                    }));
            }
        }

        public async Task AnomalyAsync(DbContext db, int variableId, double mean,
            params (double Start, double End, int F)[] bins)
        {
            var id = await db.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstanceVariableAnomaly
            {
                ExhaustiveSearchInstanceVariableId = variableId, Mean = mean, StandardDeviation = 1, Minimum = 5,
                Maximum = 10, Iqr = 1
            });
            anomalyIds.Add(id);
            var sequence = 0;

            foreach (var (start, end, f) in bins)
            {
                anomalyHistogramIds.Add(await db.InsertWithInt32IdentityAsync(
                    new ExhaustiveSearchInstanceVariableHistogramAnomaly
                    {
                        ExhaustiveSearchInstanceVariableAnomalyId = id, BinSequence = sequence++,
                        BinRangeStart = start, BinRangeEnd = end, Frequency = f
                    }));
            }
        }

        public async Task PrescriptionAsync(DbContext db, Instance instance, int variableId, double mean,
            params (double Start, double End, int F)[] bins)
        {
            var trialVariableId = await db.InsertWithInt32IdentityAsync(
                new Data.Poco.ExhaustiveSearchInstanceTrialInstanceVariable
                {
                    ExhaustiveSearchInstanceVariableId = variableId,
                    ExhaustiveSearchInstanceTrialInstanceId = instance.TrialInstanceId, Removed = 0,
                    VariableSequence = 1
                });
            trialVariableIds.Add(trialVariableId);

            var promotedVariableId = await db.InsertWithInt32IdentityAsync(
                new ExhaustiveSearchInstancePromotedTrialInstanceVariable
                {
                    ExhaustiveSearchInstanceTrialInstanceVariableId = trialVariableId, Mean = mean,
                    StandardDeviation = 1.5, Minimum = 4, Maximum = 12, Iqr = 2
                });
            promotedVariableIds.Add(promotedVariableId);
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

        public async Task<(Instance Instance, int VariableId)> FullAsync(DbContext db, string user, string name)
        {
            var instance = await InstanceAsync(db, user);
            var variableId = await VariableAsync(db, instance, name);
            await PopulationAsync(db, variableId, (0, 5, 60), (5, 10, 40));
            await ClassAsync(db, variableId, 0, 0, (0, 5, 55), (5, 10, 25));
            await ClassAsync(db, variableId, 1, 0, (0, 5, 5), (5, 10, 15));
            await ClassificationAsync(db, variableId, 8, (6, 8, 3), (8, 10, 9));
            await AnomalyAsync(db, variableId, 7.5, (5, 7.5, 4), (7.5, 10, 12));
            await PrescriptionAsync(db, instance, variableId, 7, (4, 6, 1), (6, 8, 6), (8, 10, 9), (10, 12, 4));

            return (instance, variableId);
        }

        public sealed record Instance(int Id, int TrialInstanceId);
    }
}