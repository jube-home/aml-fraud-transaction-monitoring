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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Jube.Service.Agent.AdvancedAnalyticsTools.Interfaces;
using Xunit;
using Jube.Service.Agent.AdvancedAnalyticsTools.Models;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Test.AdvancedAnalytics.Worker
{
    [Trait("Category", "Service")]
    public sealed class AdvancedAnalyticsTrainingToolConventionTests
    {
        private static readonly Type group = typeof(LogisticRegressionTrainingRunTools);

        private static AdvancedAnalyticsToolDescriptor Tool(string name)
        {
            return AdvancedAnalyticsToolCatalogue.Discover(group.Assembly).Single(d => d.ToolName == name);
        }

        [Theory]
        [InlineData("SubmitLogisticRegressionTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("GetTrainingRunStatus", AdvancedAnalyticsPermissions.Analytics)]
        [InlineData("CancelTrainingRun", AdvancedAnalyticsPermissions.Training)]
        [InlineData("GetTrainingRunResult", AdvancedAnalyticsPermissions.Analytics)]
        [InlineData("TrainLogisticRegression", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitMahalanobisAnomalyTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("GetMahalanobisAnomalyTrainingRunStatus", AdvancedAnalyticsPermissions.Analytics)]
        [InlineData("CancelMahalanobisAnomalyTrainingRun", AdvancedAnalyticsPermissions.Training)]
        [InlineData("GetMahalanobisAnomalyTrainingRunResult", AdvancedAnalyticsPermissions.Analytics)]
        [InlineData("SubmitLinearRegressionTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("GetLinearRegressionTrainingRunStatus", AdvancedAnalyticsPermissions.Analytics)]
        [InlineData("CancelLinearRegressionTrainingRun", AdvancedAnalyticsPermissions.Training)]
        [InlineData("GetLinearRegressionTrainingRunResult", AdvancedAnalyticsPermissions.Analytics)]
        [InlineData("SubmitPrincipalComponentTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("GetPrincipalComponentTrainingRunStatus", AdvancedAnalyticsPermissions.Analytics)]
        [InlineData("CancelPrincipalComponentTrainingRun", AdvancedAnalyticsPermissions.Training)]
        [InlineData("GetPrincipalComponentTrainingRunResult", AdvancedAnalyticsPermissions.Analytics)]
        [InlineData("SubmitGaussianMixtureDensityTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("GetGaussianMixtureDensityTrainingRunStatus", AdvancedAnalyticsPermissions.Analytics)]
        [InlineData("CancelGaussianMixtureDensityTrainingRun", AdvancedAnalyticsPermissions.Training)]
        [InlineData("GetGaussianMixtureDensityTrainingRunResult", AdvancedAnalyticsPermissions.Analytics)]
        [InlineData("SubmitNeuralNetworkClassifierTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitRandomForestClassifierTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitDecisionTreeClassifierTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitNaiveBayesClassifierTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitKNearestNeighboursClassifierTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitTreeAugmentedNaiveBayesClassifierTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitBaggingClassifierTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitAdaBoostClassifierTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitGradientBoostingClassifierTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitLinearSupportVectorClassifierTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitKernelSupportVectorClassifierTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitKNearestNeighboursRegressorTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitRandomForestRegressorTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitKernelRidgeRegressorTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitNeuralNetworkRegressorTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitGradientBoostingRegressorTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitDecisionTreeRegressorTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitIsolationForestAnomalyTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitOneClassSupportVectorAnomalyTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitBinarySplitClustererTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitMiniBatchKMeansClustererTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitKMedoidsClustererTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitBalancedKMeansClustererTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitGaussianMixtureClustererTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitKModesClustererTraining", AdvancedAnalyticsPermissions.Training)]
        [InlineData("SubmitMeanShiftClustererTraining", AdvancedAnalyticsPermissions.Training)]
        public void EachTrainingToolDeclaresItsPermissionAndIsDescribedInFull(string name, string permission)
        {
            var tool = Tool(name);

            tool.Permissions.Should().Equal(permission);
            tool.Description.Should().NotBeNullOrWhiteSpace();
            AdvancedAnalyticsToolCatalogue.Validate([tool]).Should().BeEmpty();

            foreach (var parameter in tool.Method.GetParameters()
                         .Where(p => p.ParameterType != typeof(CancellationToken)))
            {
                parameter.GetCustomAttribute<DescriptionAttribute>()!.Description.Length.Should().BeGreaterThan(20);
            }
        }

        [Fact]
        public void TheSubmitToolExplainsHowToPollAndTheSynchronousToolStatesItsCap()
        {
            Tool("SubmitLogisticRegressionTraining").Description.Should().Contain("GetTrainingRunStatus")
                .And.Contain("GetTrainingRunResult").And.Contain("run id");

            Tool("TrainLogisticRegression").Description.Should().Contain("2,000")
                .And.Contain("SubmitLogisticRegressionTraining");

            Tool("GetTrainingRunResult").Description.Should().Contain("warnings");
        }

        [Fact]
        [Trait("Covers", "SubmitLogisticRegressionTrainingAsync")]
        public async Task SubmitToolPassesEveryArgumentToTheRunsGatewayAsync()
        {
            var gateway = new RecordingRuns();
            var tools = new LogisticRegressionTrainingRunTools(gateway);
            var pointer = Guid.NewGuid();

            var view = await tools.SubmitLogisticRegressionTrainingAsync(4, "m", ["a", "b"], [1, 5], null, null,
                pointer, 1, 9, 0.1, 0.3, 2, LogisticClassWeighting.Balanced, LogisticMissingValuePolicy.Reject,
                false, false, 0.02, 5, 1, 0.4, 77, 1e-6, "key");

            view.RunId.Should().Be(11);
            var r = gateway.Submitted!;
            r.InstanceId.Should().Be(4);
            r.ModelName.Should().Be("m");
            r.VariableNames.Should().Equal("a", "b");
            r.ProcessingTypeIds.Should().Equal(1, 5);
            r.CachePointer.Should().Be(pointer);
            r.LabelColumn.Should().Be(1);
            r.Seed.Should().Be(9);
            r.ValidationFraction.Should().Be(0.1);
            r.TestFraction.Should().Be(0.3);
            r.RidgeLambda.Should().Be(2);
            r.ClassWeighting.Should().Be(LogisticClassWeighting.Balanced);
            r.MissingValuePolicy.Should().Be(LogisticMissingValuePolicy.Reject);
            r.Standardise.Should().BeFalse();
            r.Calibrate.Should().BeFalse();
            r.BaseRateOverride.Should().Be(0.02);
            r.CostFalseNegative.Should().Be(5);
            r.CostFalsePositive.Should().Be(1);
            r.DecisionThreshold.Should().Be(0.4);
            r.MaximumIterations.Should().Be(77);
            r.Tolerance.Should().Be(1e-6);
            r.CorrelationKey.Should().Be("key");
        }

        [Fact]
        [Trait("Covers", "GetTrainingRunStatusAsync")]
        public async Task StatusToolReadsTheRunStatusAsync()
        {
            var view = await new LogisticRegressionTrainingRunTools(new RecordingRuns()).GetTrainingRunStatusAsync(5);

            view.RunId.Should().Be(5);
            view.Status.Should().Be("Running");
        }

        [Fact]
        [Trait("Covers", "CancelTrainingRunAsync")]
        public async Task CancelToolCancelsTheRunAsync()
        {
            var gateway = new RecordingRuns();

            var view = await new LogisticRegressionTrainingRunTools(gateway).CancelTrainingRunAsync(6);

            gateway.Cancelled.Should().Be(6);
            view.Status.Should().Be("Cancelled");
        }

        [Fact]
        [Trait("Covers", "GetTrainingRunResultAsync")]
        public async Task ResultToolReadsTheResultAsync()
        {
            var view = await new LogisticRegressionTrainingRunTools(new RecordingRuns()).GetTrainingRunResultAsync(7);

            view.Run.RunId.Should().Be(7);
            view.Activated.Should().BeFalse();
        }

        private sealed class RecordingRuns : IAdvancedAnalyticsTrainingRuns
        {
            public TrainingRunSubmitRequest? Submitted { get; private set; }
            public int Cancelled { get; private set; }

            public Task<TrainingRunSubmissionView> SubmitAsync(TrainingRunSubmitRequest request,
                CancellationToken token = default)
            {
                Submitted = request;

                return Task.FromResult(new TrainingRunSubmissionView { RunId = 11, Status = "Pending" });
            }

            public Task<TrainingRunStatusView> GetStatusAsync(int runId, CancellationToken token = default)
            {
                return Task.FromResult(new TrainingRunStatusView { RunId = runId, Status = "Running" });
            }

            public Task<TrainingRunStatusView> CancelAsync(int runId, CancellationToken token = default)
            {
                Cancelled = runId;

                return Task.FromResult(new TrainingRunStatusView { RunId = runId, Status = "Cancelled" });
            }

            public Task<TrainingRunResultView> GetResultAsync(int runId, CancellationToken token = default)
            {
                return Task.FromResult(new TrainingRunResultView { Run = new TrainingRunStatusView { RunId = runId } });
            }
        }
    }
}