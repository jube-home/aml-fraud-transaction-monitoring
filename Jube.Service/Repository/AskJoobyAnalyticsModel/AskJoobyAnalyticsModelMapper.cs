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

using Jube.Data.Poco;
using Jube.Engine.AdvancedAnalytics.Anomaly;
using Jube.Engine.AdvancedAnalytics.Anomaly.Models;
using Jube.Engine.AdvancedAnalytics.Classification;
using Jube.Engine.AdvancedAnalytics.Clustering;
using Jube.Engine.AdvancedAnalytics.Density;
using Jube.Engine.AdvancedAnalytics.DimensionReduction;
using Jube.Engine.AdvancedAnalytics.Evidence;
using Jube.Engine.AdvancedAnalytics.Interfaces;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Service.Repository.AskJoobyAnalyticsInstance;
using Jube.Service.Repository.AskJoobyAnalyticsModel.Models;
using Newtonsoft.Json;

namespace Jube.Service.Repository.AskJoobyAnalyticsModel
{
    internal static class AskJoobyAnalyticsModelMapper
    {
        public static IAdvancedAnalyticsEvidence? ReadEvidence(string? metricsJson)
        {
            if (string.IsNullOrWhiteSpace(metricsJson))
            {
                return null;
            }

            var kind = AdvancedAnalyticsEvidenceJson.PeekKind(metricsJson);

            try
            {
                return kind switch
                {
                    LogisticRegressionRecall.ModelKind => ReadLogisticEvidence(metricsJson),
                    MahalanobisAnomalyRecall.ModelKind => ReadMahalanobisAnomalyEvidence(metricsJson),
                    MultipleLinearRegressionRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<
                                Jube.Engine.AdvancedAnalytics.Regression.Models.LinearRegressionEvidenceDetails>>(
                                metricsJson),
                    DecisionTreeRegressorRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<DecisionTreeRegressorEvidenceDetails>>(
                                metricsJson),
                    KNearestNeighboursRegressorRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<KNearestNeighboursRegressorEvidenceDetails>>(
                                metricsJson),
                    RandomForestRegressorRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<
                                Jube.Engine.AdvancedAnalytics.Regression.Models.RandomForestRegressorEvidenceDetails>>(
                                metricsJson),
                    KernelRidgeRegressorRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<KernelRidgeRegressorEvidenceDetails>>(metricsJson),
                    NeuralNetworkRegressorRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<NeuralNetworkRegressorEvidenceDetails>>(
                                metricsJson),
                    GradientBoostingRegressorRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<GradientBoostingRegressorEvidenceDetails>>(
                                metricsJson),
                    KMeansClustererRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<KMeansEvidenceDetails>>(metricsJson),
                    PrincipalComponentRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<PrincipalComponentEvidenceDetails>>(metricsJson),
                    GaussianMixtureDensityRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<GaussianMixtureDensityEvidenceDetails>>(
                                metricsJson),
                    NeuralNetworkClassifierRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<NeuralNetworkEvidenceDetails>>(metricsJson),
                    RandomForestClassifierRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<RandomForestEvidenceDetails>>(metricsJson),
                    DecisionTreeClassifierRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<DecisionTreeEvidenceDetails>>(metricsJson),
                    NaiveBayesClassifierRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<
                                Jube.Engine.AdvancedAnalytics.Classification.Models.NaiveBayesEvidenceDetails>>(
                                metricsJson),
                    KNearestNeighboursClassifierRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<KNearestNeighboursEvidenceDetails>>(metricsJson),
                    TreeAugmentedNaiveBayesClassifierRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<
                                Jube.Engine.AdvancedAnalytics.Classification.Models
                                .TreeAugmentedNaiveBayesEvidenceDetails>>(metricsJson),
                    BaggingClassifierRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<BaggingEvidenceDetails>>(metricsJson),
                    AdaBoostClassifierRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<AdaBoostEvidenceDetails>>(metricsJson),
                    GradientBoostingClassifierRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<GradientBoostingEvidenceDetails>>(metricsJson),
                    LinearSupportVectorClassifierRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<
                                Jube.Engine.AdvancedAnalytics.Classification.Models
                                .LinearSupportVectorEvidenceDetails>>(metricsJson),
                    KernelSupportVectorClassifierRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<KernelSupportVectorEvidenceDetails>>(metricsJson),
                    IsolationForestAnomalyRecall.ModelKind => ReadIsolationForestAnomalyEvidence(metricsJson),
                    OneClassSupportVectorAnomalyRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<OneClassSupportVectorEvidenceDetails>>(
                                metricsJson),
                    BinarySplitClustererRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<BinarySplitEvidenceDetails>>(metricsJson),
                    MiniBatchKMeansClustererRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<MiniBatchKMeansEvidenceDetails>>(metricsJson),
                    KMedoidsClustererRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<KMedoidsEvidenceDetails>>(metricsJson),
                    BalancedKMeansClustererRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<BalancedKMeansEvidenceDetails>>(metricsJson),
                    GaussianMixtureClustererRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<GaussianMixtureEvidenceDetails>>(metricsJson),
                    KModesClustererRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<KModesEvidenceDetails>>(metricsJson),
                    MeanShiftClustererRecall.ModelKind =>
                        AdvancedAnalyticsEvidenceJson
                            .Deserialise<AdvancedAnalyticsEvidence<MeanShiftEvidenceDetails>>(metricsJson),
                    _ => null
                };
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static Models.AdvancedAnalyticsLogisticEvidence? ReadLogisticEvidence(string metricsJson)
        {
            var evidence = AdvancedAnalyticsEvidenceJson.Deserialise<Models.AdvancedAnalyticsLogisticEvidence>(
                metricsJson);

            return evidence?.SplitSizes == null || (object?)evidence.Calibration == null
                                                || (object?)evidence.Fit == null || (object?)evidence.Priors == null
                ? null
                : evidence;
        }

        private static AdvancedAnalyticsEvidence<MahalanobisEvidenceDetails>? ReadMahalanobisAnomalyEvidence(
            string metricsJson)
        {
            var evidence =
                AdvancedAnalyticsEvidenceJson.Deserialise<AdvancedAnalyticsEvidence<MahalanobisEvidenceDetails>>(
                    metricsJson);

            return evidence?.SplitSizes == null || ReferenceEquals(evidence.Details, null)
                                                || (object?)evidence.Details.Calibration == null
                ? null
                : evidence;
        }

        private static AdvancedAnalyticsEvidence<IsolationForestEvidenceDetails>? ReadIsolationForestAnomalyEvidence(
            string metricsJson)
        {
            var evidence =
                AdvancedAnalyticsEvidenceJson.Deserialise<AdvancedAnalyticsEvidence<IsolationForestEvidenceDetails>>(
                    metricsJson);

            return evidence?.SplitSizes == null || ReferenceEquals(evidence.Details, null)
                                                || (object?)evidence.Details.Calibration == null
                ? null
                : evidence;
        }

        public static AskJoobyAnalyticsModelVersionDto ToDto(AskJoobyAnalyticsTrainedModelVersion version,
            int? activeVersionId, bool includeEvidence)
        {
            var evidence = ReadEvidence(version.MetricsJson);
            var logistic = evidence as Models.AdvancedAnalyticsLogisticEvidence;
            var test = logistic?.TestMetrics;

            var warningCodes = evidence switch
            {
                Models.AdvancedAnalyticsLogisticEvidence l => l.Warnings.Select(w => w.Code).ToArray(),
                _ => []
            };

            return new AskJoobyAnalyticsModelVersionDto
            {
                Id = version.Id,
                Guid = version.Guid,
                TrainedModelId = version.AskJoobyAnalyticsTrainedModelId,
                VersionNumber = version.VersionNumber,
                IsActive = activeVersionId == version.Id,
                ProducingRunId = version.AdvancedAnalyticsTrainingRunId,
                CreatedUser = version.CreatedUser,
                CreatedDate = AskJoobyAnalyticsInstanceMapper.ToOffset(version.CreatedDate),
                TestRows = test?.Rows,
                TestRocAuc = test?.RocAuc,
                TestAveragePrecision = test?.AveragePrecision,
                TestLogLoss = test?.LogLoss,
                TestBrierScore = test?.BrierScore,
                TestExpectedCalibrationError = test?.ExpectedCalibrationError,
                WarningCodes = warningCodes,
                Evidence = includeEvidence ? evidence : null,
                TransformedVariableCount = TransformedVariableCountLabel(version.VariablesJson)
            };
        }

        internal static string? TransformedVariableCountLabel(string? variablesJson)
        {
            if (string.IsNullOrWhiteSpace(variablesJson))
            {
                return null;
            }

            List<AdvancedAnalyticsVariableRow>? rows;

            try
            {
                rows = AdvancedAnalyticsEvidenceJson.Deserialise<List<AdvancedAnalyticsVariableRow>>(variablesJson);
            }
            catch (JsonException)
            {
                return null;
            }

            if (rows == null || rows.Count == 0)
            {
                return null;
            }

            var transformed = rows.Count(r => r.TransformationId != VariableTransformationKind.None);

            return $"{transformed} of {rows.Count} variables transformed";
        }

        public static AskJoobyAnalyticsTrainedModelDto ToDto(AskJoobyAnalyticsTrainedModel model,
            AskJoobyAnalyticsTrainedModelVersion? activeVersion,
            IEnumerable<AskJoobyAnalyticsTrainedModelVersion>? versions)
        {
            return new AskJoobyAnalyticsTrainedModelDto
            {
                Id = model.Id,
                Guid = model.Guid,
                AskJoobyAnalyticsInstanceId = model.AskJoobyAnalyticsInstanceId,
                Name = model.Name,
                Kind = model.KindId,
                Source = model.SourceId,
                Active = model.Active == 1,
                ActiveVersionId = model.ActiveVersionId,
                ActiveVersionNumber = activeVersion?.VersionNumber,
                CorrelationKey = model.CorrelationKey,
                CreatedUser = model.CreatedUser,
                CreatedDate = AskJoobyAnalyticsInstanceMapper.ToOffset(model.CreatedDate),
                UpdatedDate = AskJoobyAnalyticsInstanceMapper.ToOffset(model.UpdatedDate),
                Version = model.Version,
                Versions = versions?.Select(v => ToDto(v, model.ActiveVersionId, false)).ToList()
            };
        }

        public static AdvancedAnalyticsPromotionEvidenceDto ToEvidence(AskJoobyAnalyticsTrainedModel model,
            AskJoobyAnalyticsTrainedModelVersion version, Models.AdvancedAnalyticsLogisticEvidence evidence,
            int? allocatedEntityAnalysisModelId)
        {
            return new AdvancedAnalyticsPromotionEvidenceDto
            {
                VersionId = version.Id,
                VersionNumber = version.VersionNumber,
                TrainedModelId = model.Id,
                ModelName = model.Name,
                IsActiveVersion = model.ActiveVersionId == version.Id,
                InstanceAllocated = allocatedEntityAnalysisModelId.HasValue,
                AllocatedEntityAnalysisModelId = allocatedEntityAnalysisModelId,
                ProducingRunId = version.AdvancedAnalyticsTrainingRunId,
                Seed = evidence.Seed,
                HasHeldOutTestSet = evidence.TestMetrics != null,
                SplitSizes = evidence.SplitSizes,
                TestMetrics = evidence.TestMetrics,
                TrainMetrics = evidence.TrainMetrics,
                Calibration = evidence.Calibration,
                Fit = evidence.Fit,
                Priors = evidence.Priors,
                Coefficients = evidence.Coefficients,
                Warnings = evidence.Warnings
            };
        }

        private static AdvancedAnalyticsGenericPromotionEvidenceDto ToGenericEvidence(
            AskJoobyAnalyticsTrainedModel model, AskJoobyAnalyticsTrainedModelVersion version,
            IAdvancedAnalyticsEvidence evidence, int? allocatedEntityAnalysisModelId)
        {
            return new AdvancedAnalyticsGenericPromotionEvidenceDto
            {
                VersionId = version.Id,
                VersionNumber = version.VersionNumber,
                TrainedModelId = model.Id,
                ModelName = model.Name,
                IsActiveVersion = model.ActiveVersionId == version.Id,
                InstanceAllocated = allocatedEntityAnalysisModelId.HasValue,
                AllocatedEntityAnalysisModelId = allocatedEntityAnalysisModelId,
                ProducingRunId = version.AdvancedAnalyticsTrainingRunId,
                Kind = evidence.Kind,
                Seed = evidence.Seed,
                SplitSizes = evidence.CommonSplitSizes,
                Warnings = evidence.CommonWarnings.ToArray()
            };
        }

        public static object ToPromotionEvidence(AskJoobyAnalyticsTrainedModel model,
            AskJoobyAnalyticsTrainedModelVersion version, IAdvancedAnalyticsEvidence evidence,
            int? allocatedEntityAnalysisModelId)
        {
            return evidence switch
            {
                Models.AdvancedAnalyticsLogisticEvidence logistic => ToEvidence(model, version, logistic,
                    allocatedEntityAnalysisModelId),
                _ => ToGenericEvidence(model, version, evidence, allocatedEntityAnalysisModelId)
            };
        }
    }
}