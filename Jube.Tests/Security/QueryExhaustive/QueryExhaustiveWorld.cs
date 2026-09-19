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
using Jube.Test.Infrastructure;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Jube.Test.Security.QueryExhaustive.Models;

namespace Jube.Test.Security.QueryExhaustive;

public sealed class QueryExhaustiveWorld
{
    public const double DeletedMarker = 9.99;
    public const string DeletedVariableMarker = "QxDeletedVar";
    public const string RemovedVariableMarker = "QxRemovedVar";
    public const double KeyA = 7.31;
    public const double KeyB = 2.64;

    private readonly List<int> collinearityIds = [];
    private readonly List<int> histogramIds = [];
    private readonly List<int> modelIds = [];
    private readonly List<int> predictedActualIds = [];
    private readonly List<int> prescriptionIds = [];
    private readonly List<int> promotedIds = [];
    private readonly List<int> rocIds = [];
    private readonly List<int> searchInstanceIds = [];
    private readonly List<int> sensitivityIds = [];
    private readonly List<int> trialInstanceIds = [];
    private readonly List<int> trialVariableIds = [];
    private readonly List<int> variableIds = [];

    public Set A
    {
        get => field.Required();
        private set;
    }

    public Set B
    {
        get => field.Required();
        private set;
    }

    public Set Edge
    {
        get => field.Required();
        private set;
    }

    public Set Deleted
    {
        get => field.Required();
        private set;
    }

    public Set Empty
    {
        get => field.Required();
        private set;
    }

    public IEnumerable<Set> All => [A, B, Edge, Deleted, Empty];

    public static async Task<QueryExhaustiveWorld> CreateAsync(DatabaseFixture fx)
    {
        var world = new QueryExhaustiveWorld();
        await using var db = fx.GetDbContext();
        world.A = await world.SeedAsync(db, fx.Seed.UserWithPermission, "A", KeyA, Kind.Normal);
        world.B = await world.SeedAsync(db, fx.Seed.UserTenantB, "B", KeyB, Kind.Normal);
        world.Edge = await world.SeedAsync(db, fx.Seed.UserWithPermission, "Edge", 1, Kind.Nasty);
        world.Deleted = await world.SeedAsync(db, fx.Seed.UserWithPermission, "Del", KeyA, Kind.DeletedInstance);
        world.Empty = await world.SeedAsync(db, fx.Seed.UserWithPermission, "Empty", 1, Kind.Empty);
        return world;
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
        await db.GetTable<ExhaustiveSearchInstanceVariableMultiCollinearity>()
            .Where(w => collinearityIds.Contains(w.Id)).DeleteAsync();
        await db.GetTable<ExhaustiveSearchInstanceVariableHistogram>()
            .Where(w => histogramIds.Contains(w.Id)).DeleteAsync();
        await db.GetTable<ExhaustiveSearchInstancePromotedTrialInstanceRoc>()
            .Where(w => rocIds.Contains(w.Id)).DeleteAsync();
        await db.GetTable<ExhaustiveSearchInstancePromotedTrialInstancePredictedActual>()
            .Where(w => predictedActualIds.Contains(w.Id)).DeleteAsync();
        await db.GetTable<ExhaustiveSearchInstancePromotedTrialInstance>()
            .Where(w => promotedIds.Contains(w.Id)).DeleteAsync();
        await db.GetTable<ExhaustiveSearchInstanceVariable>().Where(w => variableIds.Contains(w.Id)).DeleteAsync();
        await db.GetTable<ExhaustiveSearchInstanceTrialInstance>()
            .Where(w => trialInstanceIds.Contains(w.Id)).DeleteAsync();
        foreach (var id in searchInstanceIds)
        {
            await db.GetTable<ExhaustiveSearchInstanceVersion>().Where(w => w.ExhaustiveSearchInstanceId == id)
                .DeleteAsync();
            await db.GetTable<ExhaustiveSearchInstance>().Where(w => w.Id == id).DeleteAsync();
        }

        foreach (var modelId in modelIds)
        {
            await db.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                .DeleteAsync();
            await db.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
        }
    }

    private static async Task<int> InsertAsync<T>(DbContext db, List<int> track, T poco) where T : notnull
    {
        var id = await db.InsertWithInt32IdentityAsync(poco);
        track.Add(id);
        return id;
    }

    private async Task<Set> SeedAsync(DbContext db, string user, string tag, double k, Kind kind)
    {
        var model = await new EntityAnalysisModelRepository(db, user).InsertAsync(new EntityAnalysisModel
        {
            Name = $"{DatabaseFixture.Prefix}Qx{tag}{Guid.NewGuid():N}"[..40],
            Guid = Guid.NewGuid(),
            Active = 1,
            Locked = 0,
            Deleted = 0
        });
        modelIds.Add(model.Id);

        var instance = await new ExhaustiveSearchInstanceRepository(db, user).InsertAsync(new ExhaustiveSearchInstance
        {
            Name = $"{DatabaseFixture.Prefix}Qx{tag}{Guid.NewGuid():N}"[..40],
            Guid = Guid.NewGuid(),
            EntityAnalysisModelId = model.Id,
            Active = 1,
            Locked = 0,
            Deleted = 0,
            StatusId = 0
        });
        searchInstanceIds.Add(instance.Id);

        if (kind == Kind.Empty)
        {
            return new Set(tag, k, instance.Id, 0, 0, 0, string.Empty);
        }

        var trial = await new ExhaustiveSearchInstanceTrialInstanceRepository(db).InsertAsync(
            new ExhaustiveSearchInstanceTrialInstance
            {
                ExhaustiveSearchInstanceId = instance.Id,
                CreatedDate = DateTime.UtcNow
            });
        trialInstanceIds.Add(trial.Id);

        var nasty = kind == Kind.Nasty;
        var promotedId = await InsertAsync(db, promotedIds, new ExhaustiveSearchInstancePromotedTrialInstance
        {
            ExhaustiveSearchInstanceTrialInstanceId = trial.Id,
            Active = 1,
            Score = nasty ? double.NaN : k,
            TopologyComplexity = 5,
            TruePositive = nasty ? int.MaxValue : 11,
            TrueNegative = nasty ? int.MaxValue : 22,
            FalsePositive = nasty ? int.MaxValue : 33,
            FalseNegative = nasty ? int.MaxValue : 44,
            Json = $"{{\"marker\":\"Qx{tag}Json\"}}",
            CreatedDate = DateTime.UtcNow
        });

        if (nasty)
        {
            await SeedNastyChildrenAsync(db, trial.Id, instance.Id);
        }
        else
        {
            await SeedNormalChildrenAsync(db, trial.Id, instance.Id, tag, k);
        }

        var variable = await db.GetTable<ExhaustiveSearchInstanceVariable>()
            .Where(w => w.ExhaustiveSearchInstanceId == instance.Id).OrderBy(o => o.Id)
            .Select(s => new { s.Id, s.Name })
            .FirstAsync();

        if (kind == Kind.DeletedInstance)
        {
            await db.GetTable<ExhaustiveSearchInstance>().Where(w => w.Id == instance.Id)
                .Set(s => s.Deleted, (byte)1).UpdateAsync();
        }

        return new Set(tag, k, instance.Id, trial.Id, promotedId, variable.Id, variable.Name);
    }

    private async Task SeedNormalChildrenAsync(DbContext db, int trialId, int instanceId, string tag, double k)
    {
        await InsertAsync(db, rocIds, new ExhaustiveSearchInstancePromotedTrialInstanceRoc
        {
            ExhaustiveSearchInstanceTrialInstanceId = trialId, Score = k, TruePositive = 10, TrueNegative = 30,
            FalsePositive = 20, FalseNegative = 40, Threshold = k, Deleted = 0
        });
        await InsertAsync(db, rocIds, new ExhaustiveSearchInstancePromotedTrialInstanceRoc
        {
            ExhaustiveSearchInstanceTrialInstanceId = trialId, Score = k + 1, TruePositive = 5, TrueNegative = 5,
            FalsePositive = 5, FalseNegative = 5, Threshold = k, Deleted = 0
        });
        await InsertAsync(db, rocIds, new ExhaustiveSearchInstancePromotedTrialInstanceRoc
        {
            ExhaustiveSearchInstanceTrialInstanceId = trialId, Score = DeletedMarker, TruePositive = 6,
            TrueNegative = 6, FalsePositive = 6, FalseNegative = 6, Threshold = k, Deleted = 1,
            DeletedDate = DateTime.UtcNow
        });

        for (var i = 0; i < 3; i++)
        {
            await InsertAsync(db, predictedActualIds, new ExhaustiveSearchInstancePromotedTrialInstancePredictedActual
            {
                ExhaustiveSearchInstanceTrialInstanceId = trialId, Predicted = 0, Actual = k + i, Deleted = 0
            });
        }

        await InsertAsync(db, predictedActualIds, new ExhaustiveSearchInstancePromotedTrialInstancePredictedActual
        {
            ExhaustiveSearchInstanceTrialInstanceId = trialId, Predicted = 0, Actual = DeletedMarker, Deleted = 1,
            DeletedDate = DateTime.UtcNow
        });

        var v1 = await InsertAsync(db, variableIds, Variable(instanceId, $"{DatabaseFixture.Prefix}Qx{tag}Var", k, 1));
        var v2 = await InsertAsync(db, variableIds,
            Variable(instanceId, $"<script>alert('{tag}')</script>{DatabaseFixture.Prefix}Qx{tag}Xss", k + 1, 2));
        var v3 = await InsertAsync(db, variableIds,
            Variable(instanceId, $"{DatabaseFixture.Prefix}{RemovedVariableMarker}{tag}", k + 2, 3));
        var v4 = Variable(instanceId, $"{DatabaseFixture.Prefix}{DeletedVariableMarker}{tag}", k + 3, 4);
        v4.Deleted = 1;
        v4.DeletedDate = DateTime.UtcNow;
        await InsertAsync(db, variableIds, v4);

        var tv1 = await InsertAsync(db, trialVariableIds, new ExhaustiveSearchInstanceTrialInstanceVariable
        {
            ExhaustiveSearchInstanceVariableId = v1, ExhaustiveSearchInstanceTrialInstanceId = trialId, Removed = 0,
            VariableSequence = 1
        });
        await InsertAsync(db, trialVariableIds, new ExhaustiveSearchInstanceTrialInstanceVariable
        {
            ExhaustiveSearchInstanceVariableId = v2, ExhaustiveSearchInstanceTrialInstanceId = trialId, Removed = 0,
            VariableSequence = 2
        });
        await InsertAsync(db, trialVariableIds, new ExhaustiveSearchInstanceTrialInstanceVariable
        {
            ExhaustiveSearchInstanceVariableId = v3, ExhaustiveSearchInstanceTrialInstanceId = trialId, Removed = 1,
            VariableSequence = 3
        });

        await InsertAsync(db, prescriptionIds, new ExhaustiveSearchInstancePromotedTrialInstanceVariable
        {
            ExhaustiveSearchInstanceTrialInstanceVariableId = tv1, Mean = k, StandardDeviation = k + 1,
            Maximum = k + 2, Minimum = k - 1
        });
        await InsertAsync(db, sensitivityIds, new ExhaustiveSearchInstancePromotedTrialInstanceSensitivity
        {
            ExhaustiveSearchInstanceTrialInstanceVariableId = tv1, Sensitivity = k
        });

        await InsertAsync(db, histogramIds, new ExhaustiveSearchInstanceVariableHistogram
        {
            ExhaustiveSearchInstanceVariableId = v1, BinSequence = 1, BinRangeStart = k, BinRangeEnd = k + 1,
            Frequency = 7
        });
        await InsertAsync(db, histogramIds, new ExhaustiveSearchInstanceVariableHistogram
        {
            ExhaustiveSearchInstanceVariableId = v1, BinSequence = 2, BinRangeStart = k + 1, BinRangeEnd = k + 2,
            Frequency = 9
        });
        await InsertAsync(db, histogramIds, new ExhaustiveSearchInstanceVariableHistogram
        {
            ExhaustiveSearchInstanceVariableId = v1, BinSequence = 3, BinRangeStart = DeletedMarker,
            BinRangeEnd = 10, Frequency = 1, Deleted = 1, DeletedDate = DateTime.UtcNow
        });

        await InsertAsync(db, collinearityIds, new ExhaustiveSearchInstanceVariableMultiCollinearity
        {
            ExhaustiveSearchInstanceVariableId = v1, TestExhaustiveSearchInstanceVariableId = v2, Correlation = k,
            CorrelationAbsRank = 1, Deleted = 0
        });
        await InsertAsync(db, collinearityIds, new ExhaustiveSearchInstanceVariableMultiCollinearity
        {
            ExhaustiveSearchInstanceVariableId = v1, TestExhaustiveSearchInstanceVariableId = v3,
            Correlation = DeletedMarker, CorrelationAbsRank = 2, Deleted = 1, DeletedDate = DateTime.UtcNow
        });
    }

    private async Task SeedNastyChildrenAsync(DbContext db, int trialId, int instanceId)
    {
        (int tp, int tn, int fp, int fn, double score)[] rocs =
        [
            (0, 0, 0, 0, double.NaN),
            (int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, double.PositiveInfinity),
            (5, 5, 5, 5, double.NegativeInfinity),
            (0, 0, 7, 0, 0),
            (7, 0, 0, 0, 0)
        ];
        foreach (var r in rocs)
        {
            await InsertAsync(db, rocIds, new ExhaustiveSearchInstancePromotedTrialInstanceRoc
            {
                ExhaustiveSearchInstanceTrialInstanceId = trialId, Score = r.score, TruePositive = r.tp,
                TrueNegative = r.tn, FalsePositive = r.fp, FalseNegative = r.fn, Threshold = r.score, Deleted = 0
            });
        }

        (double predicted, double actual)[] pairs =
        [
            (double.NaN, double.PositiveInfinity), (double.NegativeInfinity, 1), (1.7e308, -1.7e308), (0, 0),
            (double.NaN, double.NaN), (1e-320, 1e-320)
        ];
        foreach (var p in pairs)
        {
            await InsertAsync(db, predictedActualIds, new ExhaustiveSearchInstancePromotedTrialInstancePredictedActual
            {
                ExhaustiveSearchInstanceTrialInstanceId = trialId, Predicted = p.predicted, Actual = p.actual,
                Deleted = 0
            });
        }

        var nastyVariable = Variable(instanceId, $"{DatabaseFixture.Prefix}QxNasty", double.NaN, 1);
        nastyVariable.StandardDeviation = double.PositiveInfinity;
        nastyVariable.Maximum = double.NegativeInfinity;
        nastyVariable.Minimum = double.NaN;
        nastyVariable.Kurtosis = double.NaN;
        nastyVariable.Skewness = double.PositiveInfinity;
        nastyVariable.Iqr = double.NaN;
        nastyVariable.Correlation = double.NegativeInfinity;
        nastyVariable.DistinctValues = int.MaxValue;
        var v1 = await InsertAsync(db, variableIds, nastyVariable);
        var v2 = await InsertAsync(db, variableIds, Variable(instanceId, $"{DatabaseFixture.Prefix}QxZero", 0, 2));
        var tv1 = await InsertAsync(db, trialVariableIds, new ExhaustiveSearchInstanceTrialInstanceVariable
        {
            ExhaustiveSearchInstanceVariableId = v1, ExhaustiveSearchInstanceTrialInstanceId = trialId, Removed = 0,
            VariableSequence = 1
        });
        await InsertAsync(db, trialVariableIds, new ExhaustiveSearchInstanceTrialInstanceVariable
        {
            ExhaustiveSearchInstanceVariableId = v2, ExhaustiveSearchInstanceTrialInstanceId = trialId, Removed = 0,
            VariableSequence = 2
        });
        await InsertAsync(db, prescriptionIds, new ExhaustiveSearchInstancePromotedTrialInstanceVariable
        {
            ExhaustiveSearchInstanceTrialInstanceVariableId = tv1, Mean = double.NaN,
            StandardDeviation = double.PositiveInfinity, Maximum = double.NegativeInfinity, Minimum = double.NaN
        });
        await InsertAsync(db, sensitivityIds, new ExhaustiveSearchInstancePromotedTrialInstanceSensitivity
        {
            ExhaustiveSearchInstanceTrialInstanceVariableId = tv1, Sensitivity = double.NaN
        });
        await InsertAsync(db, histogramIds, new ExhaustiveSearchInstanceVariableHistogram
        {
            ExhaustiveSearchInstanceVariableId = v1, BinSequence = 1, BinRangeStart = double.NaN,
            BinRangeEnd = double.PositiveInfinity, Frequency = int.MaxValue
        });
        await InsertAsync(db, collinearityIds, new ExhaustiveSearchInstanceVariableMultiCollinearity
        {
            ExhaustiveSearchInstanceVariableId = v1, TestExhaustiveSearchInstanceVariableId = v2,
            Correlation = double.NaN, CorrelationAbsRank = 1, Deleted = 0
        });
        await InsertAsync(db, collinearityIds, new ExhaustiveSearchInstanceVariableMultiCollinearity
        {
            ExhaustiveSearchInstanceVariableId = v1, TestExhaustiveSearchInstanceVariableId = v1,
            Correlation = double.PositiveInfinity, CorrelationAbsRank = int.MaxValue, Deleted = 0
        });
    }

    private static ExhaustiveSearchInstanceVariable Variable(int instanceId, string name, double k, int sequence)
    {
        return new ExhaustiveSearchInstanceVariable
        {
            ExhaustiveSearchInstanceId = instanceId, Name = name, Mean = k, StandardDeviation = k + 1,
            Kurtosis = k + 2, Skewness = k + 3, Maximum = k + 4, Minimum = k - 1, Iqr = k + 5, NormalisationTypeId = 2,
            DistinctValues = 12, Correlation = k, CorrelationAbsRank = sequence, VariableSequence = sequence,
            ProcessingTypeId = 1, Deleted = 0
        };
    }
}