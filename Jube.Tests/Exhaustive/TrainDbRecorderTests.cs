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
using Jube.Engine.Exhaustive.Algorithms;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Npgsql;
using Xunit;

namespace Jube.Test.Exhaustive
{
    [Collection("Database")]
    public class TrainDbRecorderTests(DatabaseFixture fx)
    {
        private async Task<(int InstanceId, int ModelId)> NewInstanceAsync(byte statusId = 0)
        {
            await using var db = fx.GetDbContext();

            var model = await new EntityAnalysisModelRepository(db, fx.Seed.UserWithPermission).InsertAsync(
                new EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Rc{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0
                });

            var instance = await new ExhaustiveSearchInstanceRepository(db, fx.Seed.UserWithPermission).InsertAsync(
                new ExhaustiveSearchInstance
                {
                    Name = $"{DatabaseFixture.Prefix}Rc{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    EntityAnalysisModelId = model.Id,
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    StatusId = statusId
                });

            return (instance.Id, model.Id);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(16, false)]
        [InlineData(18, true)]
        [InlineData(19, true)]
        public async Task IsStopRequested_IsTrueOnlyForStoppingAndStoppedStatusesAsync(byte status, bool expected)
        {
            var (instanceId, _) = await NewInstanceAsync(status);
            await using var db = fx.GetDbContext();
            var recorder = new DbTrialRecorder(db, new ExhaustiveSearchInstanceRepository(db), instanceId);

            (await recorder.IsStopRequestedAsync()).Should().Be(expected);
        }

        [Fact]
        public async Task InstanceUpdates_PersistScoreModelsAndModelsSinceBestAsync()
        {
            var (instanceId, _) = await NewInstanceAsync();
            await using var db = fx.GetDbContext();
            var recorder = new DbTrialRecorder(db, new ExhaustiveSearchInstanceRepository(db), instanceId);

            await recorder.UpdateBestScoreAsync(0.75, 12);
            await recorder.UpdateModelsAsync(4);
            await recorder.UpdateModelsSinceBestAsync(3);

            var row = await db.ExhaustiveSearchInstance.FirstAsync(w => w.Id == instanceId);
            row.Score.Should().Be(0.75);
            row.TopologyComplexity.Should().Be(12);
            row.Models.Should().Be(4);
            row.ModelsSinceBest.Should().Be(3);
        }

        [Fact]
        public async Task Recorder_InsertsEveryTrialArtefactAndReturnsTheGeneratedIdsAsync()
        {
            var (instanceId, _) = await NewInstanceAsync();
            await using var db = fx.GetDbContext();
            var recorder = new DbTrialRecorder(db, new ExhaustiveSearchInstanceRepository(db), instanceId);

            var trial = await recorder.InsertTrialInstanceAsync(new ExhaustiveSearchInstanceTrialInstance
            {
                ExhaustiveSearchInstanceId = instanceId,
                CreatedDate = DateTime.UtcNow
            });
            trial.Id.Should().BeGreaterThan(0);

            var topology = await recorder.InsertTopologyTrialAsync(
                new ExhaustiveSearchInstanceTrialInstanceTopologyTrial
                {
                    ExhaustiveSearchInstanceTrialInstanceId = trial.Id,
                    Layer = 1,
                    Neurons = 3,
                    Score = 0.5,
                    Finalisation = 0,
                    TopologyTrials = 1,
                    TrialsSinceImprovement = 0
                });
            topology.Id.Should().BeGreaterThan(0);

            var activation = new ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial
            {
                ExhaustiveSearchInstanceTrialInstanceId = trial.Id,
                ActivationFunctionId = 1
            };

            var firstId = (await recorder.InsertActivationFunctionTrialAsync(activation)).Id;
            activation.ActivationFunctionId = 2;
            var secondId = (await recorder.InsertActivationFunctionTrialAsync(activation)).Id;
            secondId.Should().NotBe(firstId, "an existing row is never overwritten");

            await recorder.InsertRocAsync(new ExhaustiveSearchInstancePromotedTrialInstanceRoc
            {
                ExhaustiveSearchInstanceTrialInstanceId = trial.Id,
                Threshold = 0.05,
                TruePositive = 1,
                FalsePositive = 2,
                TrueNegative = 3,
                FalseNegative = 4,
                Score = 0.1
            });

            await recorder.InsertPredictedActualAsync(new ExhaustiveSearchInstancePromotedTrialInstancePredictedActual
            {
                ExhaustiveSearchInstanceTrialInstanceId = trial.Id,
                Predicted = 0.3,
                Actual = 1
            });

            var promoted = await recorder.InsertPromotedAsync(new ExhaustiveSearchInstancePromotedTrialInstance
            {
                ExhaustiveSearchInstanceTrialInstanceId = trial.Id,
                Active = 1,
                Score = 0.9,
                TopologyComplexity = 4,
                Json = "{}",
                TruePositive = 1,
                TrueNegative = 1,
                FalsePositive = 0,
                FalseNegative = 0
            });
            promoted.Id.Should().BeGreaterThan(0);

            (await db.GetTable<ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial>()
                .Where(w => w.ExhaustiveSearchInstanceTrialInstanceId == trial.Id)
                .Select(w => w.ActivationFunctionId).ToListAsync()).Should().BeEquivalentTo(new int?[] { 1, 2 });

            (await db.GetTable<ExhaustiveSearchInstancePromotedTrialInstanceRoc>()
                .CountAsync(w => w.ExhaustiveSearchInstanceTrialInstanceId == trial.Id)).Should().Be(1);
            (await db.GetTable<ExhaustiveSearchInstancePromotedTrialInstancePredictedActual>()
                .CountAsync(w => w.ExhaustiveSearchInstanceTrialInstanceId == trial.Id)).Should().Be(1);
        }

        [Fact]
        public async Task TrialVariables_InsertThenMarkRemovedAsync()
        {
            var (instanceId, _) = await NewInstanceAsync();
            await using var db = fx.GetDbContext();
            var recorder = new DbTrialRecorder(db, new ExhaustiveSearchInstanceRepository(db), instanceId);

            var variable = await db.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstanceVariable
            {
                ExhaustiveSearchInstanceId = instanceId,
                Name = "a",
                VariableSequence = 0
            });

            var trial = await recorder.InsertTrialInstanceAsync(new ExhaustiveSearchInstanceTrialInstance
            {
                ExhaustiveSearchInstanceId = instanceId,
                CreatedDate = DateTime.UtcNow
            });

            await recorder.InsertTrialVariableAsync(new ExhaustiveSearchInstanceTrialInstanceVariable
            {
                ExhaustiveSearchInstanceTrialInstanceId = trial.Id,
                ExhaustiveSearchInstanceVariableId = variable,
                VariableSequence = 0
            });

            await recorder.MarkTrialVariableRemovedAsync(variable, trial.Id);

            var rows = await db.GetTable<ExhaustiveSearchInstanceTrialInstanceVariable>()
                .Where(w => w.ExhaustiveSearchInstanceTrialInstanceId == trial.Id).ToListAsync();

            rows.Should().ContainSingle().Which.Removed.Should().Be(1);
        }

        [Fact]
        public async Task MarkTrialVariableRemoved_ForAnUnknownPair_ThrowsAsync()
        {
            var (instanceId, _) = await NewInstanceAsync();
            await using var db = fx.GetDbContext();
            var recorder = new DbTrialRecorder(db, new ExhaustiveSearchInstanceRepository(db), instanceId);

            var act = async () => await recorder.MarkTrialVariableRemovedAsync(-1, -1);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task DeleteTrialVariables_SoftDeletesTheRowsOfThatTrialOnlyAsync()
        {
            var (instanceId, _) = await NewInstanceAsync();
            await using var db = fx.GetDbContext();
            var recorder = new DbTrialRecorder(db, new ExhaustiveSearchInstanceRepository(db), instanceId);

            var variable = await db.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstanceVariable
            {
                ExhaustiveSearchInstanceId = instanceId,
                Name = "a",
                VariableSequence = 0
            });

            var trial = await recorder.InsertTrialInstanceAsync(new ExhaustiveSearchInstanceTrialInstance
            {
                ExhaustiveSearchInstanceId = instanceId,
                CreatedDate = DateTime.UtcNow
            });
            var otherTrial = await recorder.InsertTrialInstanceAsync(new ExhaustiveSearchInstanceTrialInstance
            {
                ExhaustiveSearchInstanceId = instanceId,
                CreatedDate = DateTime.UtcNow
            });

            var row = await recorder.InsertTrialVariableAsync(new ExhaustiveSearchInstanceTrialInstanceVariable
            {
                ExhaustiveSearchInstanceTrialInstanceId = trial.Id,
                ExhaustiveSearchInstanceVariableId = variable,
                VariableSequence = 0
            });
            var otherRow = await recorder.InsertTrialVariableAsync(new ExhaustiveSearchInstanceTrialInstanceVariable
            {
                ExhaustiveSearchInstanceTrialInstanceId = otherTrial.Id,
                ExhaustiveSearchInstanceVariableId = variable,
                VariableSequence = 0
            });

            await recorder.DeleteTrialVariablesAsync(int.MaxValue);

            (await db.GetTable<ExhaustiveSearchInstanceTrialInstanceVariable>().FirstAsync(w => w.Id == row.Id))
                .Deleted.Should().NotBe(1, "no trial instance has that id");

            await recorder.DeleteTrialVariablesAsync(trial.Id);

            (await db.GetTable<ExhaustiveSearchInstanceTrialInstanceVariable>().FirstAsync(w => w.Id == row.Id))
                .Deleted.Should().Be(1);
            (await db.GetTable<ExhaustiveSearchInstanceTrialInstanceVariable>().FirstAsync(w => w.Id == otherRow.Id))
                .Deleted.Should().NotBe(1);
        }

        [Fact]
        public async Task PromotedVariableHistogram_IsInsertedAgainstThePromotedVariableAsync()
        {
            var (instanceId, _) = await NewInstanceAsync();
            await using var db = fx.GetDbContext();
            var recorder = new DbTrialRecorder(db, new ExhaustiveSearchInstanceRepository(db), instanceId);

            var trial = await recorder.InsertTrialInstanceAsync(new ExhaustiveSearchInstanceTrialInstance
            {
                ExhaustiveSearchInstanceId = instanceId,
                CreatedDate = DateTime.UtcNow
            });

            await recorder.InsertPromotedAsync(new ExhaustiveSearchInstancePromotedTrialInstance
            {
                ExhaustiveSearchInstanceTrialInstanceId = trial.Id,
                Active = 1,
                Score = 0.9,
                TopologyComplexity = 4,
                Json = "{}"
            });

            var variable = await recorder.InsertPromotedVariableAsync(
                new ExhaustiveSearchInstancePromotedTrialInstanceVariable
                {
                    Mean = 1.5,
                    StandardDeviation = 0.5
                });

            await recorder.InsertPromotedVariableHistogramAsync(
                new ExhaustiveSearchInstancePrescriptionHistogram
                {
                    ExhaustiveSearchInstancePromotedTrialInstanceVariableId = variable.Id,
                    BinIndex = 3,
                    BinRangeStart = 0.25,
                    BinRangeEnd = 0.5,
                    Frequency = 17
                });

            var rows = await db.GetTable<ExhaustiveSearchInstancePrescriptionHistogram>()
                .Where(w => w.ExhaustiveSearchInstancePromotedTrialInstanceVariableId == variable.Id)
                .ToListAsync();

            var row = rows.Should().ContainSingle().Subject;
            row.BinIndex.Should().Be(3);
            row.BinRangeStart.Should().Be(0.25);
            row.BinRangeEnd.Should().Be(0.5);
            row.Frequency.Should().Be(17);
        }

        [Fact]
        public async Task PromotedVariableHistogram_ForAnUnknownPromotedVariable_ViolatesTheForeignKeyAsync()
        {
            var (instanceId, _) = await NewInstanceAsync();
            await using var db = fx.GetDbContext();
            var recorder = new DbTrialRecorder(db, new ExhaustiveSearchInstanceRepository(db), instanceId);

            var act = async () => await recorder.InsertPromotedVariableHistogramAsync(
                new ExhaustiveSearchInstancePrescriptionHistogram
                {
                    ExhaustiveSearchInstancePromotedTrialInstanceVariableId = -1,
                    BinIndex = 0,
                    Frequency = 1
                });

            await act.Should().ThrowAsync<PostgresException>();
        }
    }
}