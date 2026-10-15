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
using System.Diagnostics;
using System.Globalization;
using Accord.Neuro;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions;
using Jube.Engine.Exhaustive.Calibration;
using Jube.Engine.Exhaustive.Extensions;
using Jube.Engine.Exhaustive.Models;
using Jube.Engine.Helpers;
using Jube.HttpAdaptationProtocol;
using Jube.Test.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using BackgroundContext = Jube.Engine.BackgroundTasks.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;
using InvokeContext = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using Manager = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModelManager;
using Jube.Data.Repository.Models;
using Poco = Jube.Data.Poco;

namespace Jube.Test.Exhaustive
{
    [Trait("Category", "Unit")]
    public sealed class ExhaustiveCalibrationRecallPathsTests
    {
        private static readonly JsonSerializationHelper helper = new();

        private static ActivationNetwork Network()
        {
            var network = new ActivationNetwork(new SigmoidFunction(), 1, null, 1);
            var neuron = (ActivationNeuron)network.Layers[0].Neurons[0];
            neuron.Weights[0] = 1.5;
            neuron.Threshold = 0.2;

            return network;
        }

        private static ExhaustiveCalibration Calibration()
        {
            return ExhaustiveCalibration.FromColumns("platt", "logit", 1.9, -0.3, 1e-6, 0.5, 0.02,
                "PopulationCount");
        }

        private static ExhaustiveSearchInstance Model(ExhaustiveCalibration? calibration)
        {
            var model = new ExhaustiveSearchInstance
            {
                Id = 7,
                Name = "M",
                Guid = Guid.NewGuid(),
                TopologyNetwork = Network()
            };

            if (calibration != null)
            {
                model.Calibration = calibration;
            }

            model.NetworkVariablesInOrder.Add(new ExhaustiveSearchInstancePromotedTrialInstanceVariable
            {
                Name = "X",
                ProcessingTypeId = 1
            });

            return model;
        }

        private static BackgroundContext RecallContext(ExhaustiveSearchInstance model)
        {
            var manager = new Manager(new BackgroundContext());
            var entityModel = new EntityAnalysisModel();
            entityModel.Snapshot.ExhaustiveModels.Add(model);
            manager.Context.EntityAnalysisModels.ActiveEntityAnalysisModels.TryAdd(1, entityModel);

            var context = new BackgroundContext();
            context.Services.Log = new TestLog();
            context.Tasks.EntityAnalysisModelManager = manager;

            return context;
        }

        private static double? Stage(ExhaustiveSearchInstance model, double x)
        {
            var entityModel = new EntityAnalysisModel();
            entityModel.Snapshot.ExhaustiveModels.Add(model);

            var context = new InvokeContext
            {
                EntityAnalysisModel = entityModel,
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    Payload = new DictionaryNoBoxing<string>(),
                    Dictionary = new PooledDictionary<string, double>(),
                    TtlCounter = new PooledDictionary<string, double>(),
                    Sanction = new PooledDictionary<string, double>(),
                    Abstraction = new PooledDictionary<string, double>(),
                    AbstractionCalculation = new PooledDictionary<string, double>(),
                    ExhaustiveAdaptation = new PooledDictionary<string, double>(),
                    ExhaustiveAdaptationDetail = new PooledDictionary<string, Adaptation>(),
                    InvokeTaskPerformance = new InvokeTaskPerformance()
                },
                Log = TestLog.NoOp,
                Stopwatch = Stopwatch.StartNew(),
                LogSampled = true
            };

            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("X", x);

            context.ExecuteExhaustiveAdaptation();

            return context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation.TryGetValue("M", out var value)
                ? value
                : null;
        }

        [Theory]
        [InlineData(-2.0)]
        [InlineData(0.0)]
        [InlineData(0.37)]
        [InlineData(3.0)]
        public void ScoreRecallAndTheInPipelineStage_ReturnTheSameCalibratedValue(double x)
        {
            var model = Model(Calibration());

            var body = JObject.Parse("{\"X\": " + x.ToString("R", CultureInfo.InvariantCulture) +
                                     "}");

            var raw = Network().Compute([x])[0];
            var expected = Calibration().Apply(raw);

            var score = ExhaustiveExtensions.Score(model, body);
            var recall = RecallContext(model).RecallExhaustive(model.Guid, body);
            var stage = Stage(model, x);

            score.Should().Be(expected);
            recall.Should().Be(expected);
            stage.Should().Be(expected);
            expected.Should().NotBe(raw);
        }

        [Theory]
        [InlineData(-2.0)]
        [InlineData(0.37)]
        [InlineData(3.0)]
        public void AModelWithoutACalibrator_BehavesByteForByteAsBefore(double x)
        {
            var model = Model(null);

            var body = JObject.Parse("{\"X\": " + x.ToString("R", CultureInfo.InvariantCulture) +
                                     "}");

            var raw = Network().Compute([x])[0];

            BitConverter.DoubleToInt64Bits(ExhaustiveExtensions.Score(model, body)).Should()
                .Be(BitConverter.DoubleToInt64Bits(raw));

            BitConverter.DoubleToInt64Bits(RecallContext(model).RecallExhaustive(model.Guid, body)).Should()
                .Be(BitConverter.DoubleToInt64Bits(raw));

            BitConverter.DoubleToInt64Bits(Stage(model, x)!.Value).Should().Be(BitConverter.DoubleToInt64Bits(raw));

            BitConverter.DoubleToInt64Bits(ExhaustiveExtensions.Score(model.TopologyNetwork,
                model.NetworkVariablesInOrder, body)).Should().Be(BitConverter.DoubleToInt64Bits(raw));
        }

        [Fact]
        public void AnInvalidCalibrator_DoesNotServeRawScoresOnAnyPath()
        {
            var model = Model(ExhaustiveCalibration.Invalid("corrupt"));
            var body = JObject.Parse("{\"X\": 0.5}");
            var log = new TestLog();
            var context = RecallContext(model);
            context.Services.Log = log;

            var act = () => ExhaustiveExtensions.Score(model, body);
            act.Should().Throw<InvalidOperationException>();
            context.RecallExhaustive(model.Guid, body).Should().Be(0);
            log.Entries.Should().Contain(e => e.Level == "ERROR" && e.Message.Contains("invalid"));
            Stage(model, 0.5).Should().BeNull();
        }

        private static Poco.ExhaustiveSearchInstance Record()
        {
            return new Poco.ExhaustiveSearchInstance { Id = 12, Guid = Guid.NewGuid(), Name = "Fraud Model" };
        }

        private static Poco.ExhaustiveSearchInstancePromotedTrialInstance Row(
            Action<Poco.ExhaustiveSearchInstancePromotedTrialInstance>? edit = null)
        {
            var row = new Poco.ExhaustiveSearchInstancePromotedTrialInstance
            {
                Json = JsonConvert.SerializeObject(Network(), helper.TopologyNetworkJsonSerializerSettings)
            };

            edit?.Invoke(row);

            return row;
        }

        [Fact]
        public void Sync_BuildsTheCalibrationFromThePromotedTrialRow()
        {
            var model = SyncExhaustiveSearchInstancesExtensions.BuildExhaustiveModel(Record(), Row(r =>
            {
                r.CalibrationMethodId = ExhaustiveCalibrationMethod.Platt;
                r.CalibrationInputId = ExhaustiveCalibrationInput.Logit;
                r.CalibrationSlope = 1.9;
                r.CalibrationIntercept = -0.3;
                r.CalibrationEpsilon = 1e-6;
                r.CalibrationPrior = 0.5;
                r.BaseRate = 0.02;
                r.BaseRateSourceId = ExhaustiveBaseRateSource.PopulationCount;
            }), [], helper.DeserializeTopologyNetworkJsonSerializerSettings);

            model.Calibration.IsCalibrated.Should().BeTrue();
            model.Calibration.Apply(0.7).Should().Be(Calibration().Apply(0.7));
            model.TopologyNetwork.Should().NotBeNull();
        }

        [Fact]
        public void Sync_ForAModelPromotedBeforeCalibration_LoadsTheIdentity()
        {
            var model = SyncExhaustiveSearchInstancesExtensions.BuildExhaustiveModel(Record(), Row(), [],
                helper.DeserializeTopologyNetworkJsonSerializerSettings);

            model.Calibration.IsCalibrated.Should().BeFalse();
            model.Calibration.IsInvalid.Should().BeFalse();
            model.Calibration.Status.Should().Be("NotCalibrated");
        }

        [Fact]
        public void Sync_ForASkippedCalibration_LoadsTheIdentityAndKeepsScoringRaw()
        {
            var model = SyncExhaustiveSearchInstancesExtensions.BuildExhaustiveModel(Record(), Row(r =>
            {
                r.CalibrationMethodId = ExhaustiveCalibrationMethod.None;
                r.CalibrationStatusId = ExhaustiveCalibrationStatus.Skipped;
                r.CalibrationReason = "single class";
            }), [], helper.DeserializeTopologyNetworkJsonSerializerSettings);

            model.Calibration.IsCalibrated.Should().BeFalse();
            model.Calibration.Apply(0.3).Should().Be(0.3);
        }

        [Fact]
        public void Sync_ForCorruptColumns_RegistersTheModelLogsAnErrorAndMarksItInvalid()
        {
            var model = SyncExhaustiveSearchInstancesExtensions.BuildExhaustiveModel(Record(), Row(r =>
            {
                r.CalibrationMethodId = ExhaustiveCalibrationMethod.Platt;
                r.CalibrationInputId = ExhaustiveCalibrationInput.Logit;
                r.CalibrationSlope = -1;
                r.CalibrationIntercept = 0;
                r.CalibrationEpsilon = 1e-6;
            }), [], helper.DeserializeTopologyNetworkJsonSerializerSettings);

            var log = new TestLog();

            SyncExhaustiveSearchInstancesExtensions.LogIncompleteModel(log, model);

            model.TopologyNetwork.Should().NotBeNull();
            model.Calibration.IsInvalid.Should().BeTrue();
            log.Entries.Should().Contain(e => e.Level == "ERROR" && e.Message.Contains("stored calibrator"));
        }
    }
}