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

using System.ComponentModel;
using Jube.Engine.AdvancedAnalytics.Classification.Models;
using Jube.Engine.AdvancedAnalytics.Training;
using Jube.Service.Agent.AdvancedAnalyticsTools.Models;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools
{
    [AdvancedAnalyticsToolGroup("KernelSupportVectorClassifierTrainingRuns")]
    public class KernelSupportVectorClassifierTrainingRunTools(
        AdvancedAnalyticsKernelSupportVectorClassifierTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitKernelSupportVectorClassifierTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a nonlinear-kernel support vector classification training run for a background worker and " +
            "returns its run id straight away, before anything is trained. A support vector machine looks for the " +
            "single boundary between the two classes that leaves the widest possible margin of empty space on " +
            "either side of it, on the reasoning that the boundary furthest from every training row is the one " +
            "least likely to be fooled by a slightly different row at recall time. Fitted directly against the raw " +
            "predictors this boundary can only ever be a straight line or flat plane; a kernel changes that by " +
            "reinterpreting every comparison between two rows as a similarity computed in some reshaped, often far " +
            "higher-dimensional space, so that a boundary which is still perfectly straight in that reshaped space " +
            "corresponds to a curved, wrapped-around boundary back in the space of the original predictors, without " +
            "ever having to construct the reshaped space explicitly. Fitting is carried out by sequential minimal " +
            "optimisation, which repeatedly picks the pair of training rows whose current weights most violate the " +
            "optimality conditions and adjusts just that pair analytically, breaking what would otherwise be a very " +
            "large numerical optimisation problem into a long sequence of tiny two-row ones. The rows that end up " +
            "sitting on or inside the margin keep a nonzero weight and are called the support vectors; every other " +
            "training row is discarded once fitting finishes, since it contributed nothing to where the boundary " +
            "ended up. Because the fitted boundary lives in the reshaped space rather than the space of the original " +
            "predictors, there is no honest way to say how much any one predictor pushed a particular score up or " +
            "down, unlike a family that fits one weight per predictor; recalls from this family carry no such " +
            "breakdown. Because the notion of similarity between two rows depends directly on their raw numeric " +
            "scale, every predictor is z-scored to the training mean and standard deviation before the kernel ever " +
            "sees it. The background worker stores every kept support vector row, its weight and the kernel's own " +
            "parameters inside the model version, since recalling this kind of model means weighting the kernel " +
            "similarity of a new row against every one of those stored rows directly, in the same way that recalling " +
            "a k nearest neighbours model means searching its stored training rows; this makes the stored model grow " +
            "with however many training rows end up as support vectors rather than with a small fixed parameter " +
            "count, which is an expected characteristic of this family rather than a defect. The raw score a fitted " +
            "machine produces is a signed distance to the boundary in the reshaped space, not a probability, so it " +
            "is Platt-calibrated into a probability on the validation rows before it is reported; left uncalibrated " +
            "that raw score has no bounded, probability-like meaning at all. Give the predictor rows and the binary " +
            "outcome for each row inline, one array of numbers per case with NaN for a missing value, or a cache " +
            "pointer to a stored table whose last column is the label. To follow the " +
            "run, call GetAdvancedAnalyticsTrainingRunStatus with the returned run id until it reports Succeeded, " +
            "Failed or Cancelled, then call GetAdvancedAnalyticsTrainingRunResult. Training never makes the " +
            "resulting version live.")]
        public Task<TrainingRunSubmissionView> SubmitKernelSupportVectorClassifierTrainingAsync(
            [Description("Numeric identifier of the AskJooby Analytics instance the run and its model belong to.")]
            int instanceId,
            [Description(
                "Name of the model the trained version is filed under. An existing model of that name receives a new version; otherwise a model is created.")]
            string modelName,
            [Description("The name of each predictor column, unique and in column order.")]
            string[] variableNames,
            [Description(
                "Where each predictor comes from in the live pipeline, one number per variable name in the same order: 1 payload, 2 dictionary, 3 TTL counter, 4 sanction, 5 abstraction, 6 abstraction calculation, 7 or 99 other.")]
            int[] processingTypeIds,
            [Description(
                "Inline predictor rows, one array of numbers per case with NaN for a missing value. Every predictor is z-scored to the training mean and standard deviation before the kernel is applied. Every one of these rows that ends up as a support vector, after imputation and standardisation, is stored inside the trained model version, since scoring this kind of model means weighting the kernel similarity of the row being scored against every stored support vector. Leave empty when a cache pointer is given.")]
            double[][]? inputs = null,
            [Description(
                "Inline outcomes, one per row: 1 for the event of interest, otherwise 0. Both values must be present. Leave empty when a cache pointer is given.")]
            int[]? labels = null,
            [Description(
                "The complexity parameter C handed to the sequential minimal optimisation solver: how heavily a training row that falls on the wrong side of the margin, or inside it, is penalised in the objective the solver minimises. A small C accepts more such rows in exchange for a wider, smoother margin that is less sensitive to any one row; a large C insists on classifying the training rows correctly wherever possible, which narrows the margin and can end up fitting noise rather than genuine structure. There is no single correct value; it should be set by comparing held-out performance across a few values on a logarithmic scale, such as 0.1, 1, 10, 100. Must be greater than 0. Default 1.")]
            double complexity = 1.0,
            [Description(
                "Which kernel reshapes the notion of similarity between two standardised predictor rows before the maximum-margin boundary is fitted, which in turn decides what shape of boundary the machine can represent back in the original predictor space. Gaussian (the default, also called a radial basis function) treats two rows as more similar the closer together they sit in ordinary Euclidean space, with that similarity falling off smoothly and symmetrically in every direction as the rows move apart; it can bend the boundary around a cluster of essentially any shape and is a reasonable first choice whenever there is no prior reason to expect a particular algebraic form. Polynomial raises the standardised dot product of two rows to a fixed power (see degree), which lets the boundary curve following products and combinations of the original predictors up to that power, such as one predictor's effect changing depending on another's value; degree 1 recovers an ordinary flat boundary. Sigmoid passes the standardised dot product through a hyperbolic tangent, echoing the squashing function of a single hidden layer of a neural network, and can behave like a Gaussian kernel for some parameter ranges and poorly outside them, so it is included mainly for completeness rather than as a default choice. Laplacian is close in spirit to Gaussian but uses plain Euclidean distance rather than squared distance, so its similarity falls off more sharply than Gaussian for rows that are close together and more gently than Gaussian for rows that are far apart, which can suit data with occasional large outlying differences between rows. Default Gaussian.")]
            KernelSupportVectorKernel kernel = KernelSupportVectorKernel.Gaussian,
            [Description(
                "The gamma of the Gaussian, Sigmoid or Laplacian kernel, ignored entirely for the Polynomial kernel: how far the influence of a single training row reaches when the kernel measures similarity. A larger gamma treats rows as dissimilar unless they are very close together, letting the boundary bend sharply around individual rows at greater risk of overfitting; a smaller gamma treats rows further apart as still meaningfully similar, giving a smoother boundary that can underfit if taken too far. Zero (the default) is resolved automatically at training time to one over the number of predictor columns, a longstanding rule of thumb that scales sensibly as more predictors are added. Must not be negative. Default 0 (automatic).")]
            double gamma = 0d,
            [Description(
                "The degree of the Polynomial kernel, ignored entirely for every other kernel: the highest power the standardised dot product of two rows is raised to, which is also the highest order of combination among the original predictors the boundary can follow. Degree 1 is a plain dot product and gives an ordinary flat boundary; degree 2 lets the boundary follow pairwise products of predictors; higher degrees allow increasingly intricate boundaries at growing risk of overfitting and of the solver taking noticeably longer to converge. From 1 to 10. Default 3.")]
            int degree = 3,
            [Description(
                "Share of the rows held back to fit the Platt probability calibration, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of the rows held back purely to measure quality on rows the solver never fitted against, between 0 and 0.9. Together with the validation share it may not exceed 0.9. Default 0.2.")]
            double testFraction = 0.2,
            [Description(
                "Seed that makes the row split reproducible: the same seed and the same rows always produce the same training, validation and test split.")]
            int seed = 1,
            [Description(
                "Whether to fit a Platt calibration of the raw kernel decision function into a probability on the validation rows. The decision function a fitted machine produces is a signed distance to the separating boundary in the kernel's reshaped space, not a probability by construction, so leaving this off means the recalled value has no bounded, probability-like meaning at all; this should normally be left on. Default true.")]
            bool calibrate = true,
            [Description(
                "Pointer to a table in the Advanced Analytics cache holding the predictor columns and a label column. Leave empty when the rows are inline. The run fails if the pointer has expired by the time a worker starts it.")]
            Guid? cachePointer = null,
            [Description(
                "Zero based index of the label column in the cached table. Default is the last column; only used with a cache pointer.")]
            int labelColumn = -1,
            [Description("Optional opaque label kept with a newly created model, for finding it again.")]
            string? correlationKey = null,
            CancellationToken cancellationToken = default)
        {
            return SubmitAsync(instanceId, modelName, variableNames, processingTypeIds, inputs, labels, cachePointer,
                labelColumn, complexity, kernel, gamma, degree, validationFraction, testFraction, seed, calibrate,
                correlationKey, cancellationToken);
        }

        private async Task<TrainingRunSubmissionView> SubmitAsync(int instanceId, string modelName,
            string[] variableNames, int[] processingTypeIds, double[][]? inputs, int[]? labels, Guid? cachePointer,
            int labelColumn, double complexity, KernelSupportVectorKernel kernel, double gamma, int degree,
            double validationFraction, double testFraction, int seed, bool calibrate, string? correlationKey,
            CancellationToken cancellationToken)
        {
            var submission = await service.SubmitTrainingAsync(
                new AdvancedAnalyticsKernelSupportVectorClassifierTrainingDto
                {
                    AskJoobyAnalyticsInstanceId = instanceId,
                    ModelName = modelName,
                    CorrelationKey = correlationKey,
                    VariableNames = variableNames,
                    ProcessingTypeIds = processingTypeIds,
                    Input = cachePointer.HasValue
                        ? AdvancedAnalyticsTrainingInput.CachePointer(cachePointer.Value, labelColumn)
                        : null,
                    Inputs = inputs,
                    Labels = labels,
                    Complexity = complexity,
                    Kernel = kernel,
                    Gamma = gamma,
                    Degree = degree,
                    ValidationFraction = validationFraction,
                    TestFraction = testFraction,
                    Seed = seed,
                    Calibrate = calibrate
                }, cancellationToken).ConfigureAwait(false);

            return new TrainingRunSubmissionView
            {
                RunId = submission.RunId,
                Status = submission.Status.ToString(),
                HowToPoll = submission.HowToPoll
            };
        }
    }
}