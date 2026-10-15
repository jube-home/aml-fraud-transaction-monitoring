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
using Jube.Engine.AdvancedAnalytics.Regression.Models;
using Jube.Service.Agent.AdvancedAnalyticsTools.Models;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun;
using Jube.Service.Repository.AdvancedAnalyticsTrainingRun.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools
{
    [AdvancedAnalyticsToolGroup("KernelRidgeRegressorTrainingRuns")]
    public class KernelRidgeRegressorTrainingRunTools(
        AdvancedAnalyticsKernelRidgeRegressorTrainingService service)
    {
        [AdvancedAnalyticsTool("SubmitKernelRidgeRegressorTraining")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [Description(
            "Queues a kernel ridge regression training run for a background worker and returns its run id straight " +
            "away, before anything is trained. Ordinary ridge regression shrinks an otherwise ordinary linear fit by " +
            "adding a penalty on the size of its coefficients, which trades a little bias for a more stable fit when " +
            "predictors are noisy, correlated or numerous relative to the number of rows; kernel ridge regression " +
            "keeps exactly that shrinkage idea but solves it after first reinterpreting every pairwise comparison " +
            "between two standardised training rows as a similarity computed by the chosen kernel, so the fitted " +
            "surface bends back in the space of the original predictors in whatever way the kernel allows, while the " +
            "underlying mathematics stays a single closed-form linear solve rather than an iterative search. " +
            "Concretely, fitting builds the square matrix of kernel similarities between every pair of training rows, " +
            "adds the ridge penalty to its diagonal, and solves once for one weight per training row by Cholesky " +
            "decomposition, falling back to a general linear solve on the rare system that is not positive definite. " +
            "Every training row keeps a nonzero weight, unlike a support vector machine, which discards most rows " +
            "during fitting; there is no sparsity mechanism here, so the background worker stores every standardised " +
            "training row together with its fitted weight and the kernel's own parameters inside the model version, " +
            "since recalling this kind of model means summing the kernel similarity of a new row against every one " +
            "of those stored rows, weighted by its fitted coefficient, plus the training mean. Because a kernel's " +
            "notion of similarity between two rows depends directly on their raw numeric scale, every predictor is " +
            "z-scored to the training mean and standard deviation before the kernel ever sees it. The value recalled " +
            "is the fitted value itself, on the target's own scale; regression targets are never calibrated the way " +
            "a classifier's score is. Give the predictor rows and the continuous outcome for each row inline, one " +
            "array of numbers per case with NaN for a missing value. To follow the run, call " +
            "GetAdvancedAnalyticsTrainingRunStatus with the returned run id until it reports Succeeded, Failed or " +
            "Cancelled, then call GetAdvancedAnalyticsTrainingRunResult. Training never makes the resulting version " +
            "live.")]
        public Task<TrainingRunSubmissionView> SubmitKernelRidgeRegressorTrainingAsync(
            [Description("Numeric identifier of the Advanced Analytics instance the run and its model belong to.")]
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
                "Predictor rows, one array of numbers per case with NaN for a missing value. Every predictor is z-scored to the training mean and standard deviation before the kernel is applied. Every one of these rows, after imputation and standardisation, is kept as part of the kernel expansion and stored inside the trained model version, since scoring this kind of model means weighting the kernel similarity of the row being scored against every stored training row.")]
            double[][] inputs,
            [Description(
                "The continuous outcome for each row, in the same order as the rows. Must all be finite numbers.")]
            double[] outputs,
            [Description(
                "The complexity behind the ridge penalty: the penalty added to the diagonal of the kernel similarity matrix before it is solved is one over this value. A small complexity applies a heavy penalty, shrinking the fit strongly towards the training mean and tolerating a system that would otherwise be poorly conditioned; a large complexity applies a light penalty, letting the fit follow the training rows more closely at growing risk of fitting noise rather than genuine structure. There is no single correct value; it should be set by comparing held-out performance across a few values on a logarithmic scale, such as 0.1, 1, 10, 100. Must be greater than 0. Default 1.")]
            double complexity = 1.0,
            [Description(
                "Which kernel reshapes the notion of similarity between two standardised predictor rows before the ridge-regularised system is solved, which in turn decides what shape of surface the fitted model can represent back in the original predictor space. Gaussian (the default, also called a radial basis function) treats two rows as more similar the closer together they sit in ordinary Euclidean space, with that similarity falling off smoothly and symmetrically in every direction as the rows move apart; it can bend the fitted surface around structure of essentially any shape and is a reasonable first choice whenever there is no prior reason to expect a particular algebraic form. Polynomial raises the standardised dot product of two rows to a fixed power (see degree), which lets the fitted surface curve following products and combinations of the original predictors up to that power, such as one predictor's effect on the outcome changing depending on another's value; degree 1 recovers an ordinary flat surface. Sigmoid passes the standardised dot product through a hyperbolic tangent, echoing the squashing function of a single hidden layer of a neural network, and can behave usefully for some parameter ranges and poorly outside them, so it is included mainly for completeness rather than as a default choice. Laplacian is close in spirit to Gaussian but uses plain Euclidean distance rather than squared distance, so its similarity falls off more sharply than Gaussian for rows that are close together and more gently than Gaussian for rows that are far apart, which can suit data with occasional large outlying differences between rows. Linear reduces the kernel to a plain standardised dot product with no reshaping at all, so the fitted surface is an ordinary flat plane, the same shape an ordinary linear regression would fit, solved instead through the ridge-regularised kernel system; it is a useful baseline for comparing whether a nonlinear kernel is earning its added complexity. Default Gaussian.")]
            KernelRidgeRegressorKernel kernel = KernelRidgeRegressorKernel.Gaussian,
            [Description(
                "The gamma of the Gaussian, Sigmoid or Laplacian kernel, ignored entirely for the Polynomial and Linear kernels: how far the influence of a single training row reaches when the kernel measures similarity. A larger gamma treats rows as dissimilar unless they are very close together, letting the fitted surface bend sharply around individual rows at greater risk of overfitting; a smaller gamma treats rows further apart as still meaningfully similar, giving a smoother surface that can underfit if taken too far. Zero (the default) is resolved automatically at training time to one over the number of predictor columns, a longstanding rule of thumb that scales sensibly as more predictors are added. Must not be negative. Default 0 (automatic).")]
            double gamma = 0d,
            [Description(
                "The degree of the Polynomial kernel, ignored entirely for every other kernel: the highest power the standardised dot product of two rows is raised to, which is also the highest order of combination among the original predictors the fitted surface can follow. Degree 1 is a plain dot product and gives an ordinary flat surface; degree 2 lets the surface follow pairwise products of predictors; higher degrees allow increasingly intricate surfaces at growing risk of overfitting. From 1 to 10. Default 3.")]
            int degree = 3,
            [Description(
                "Share of the rows held back purely to measure quality of the fit on rows not used to solve the ridge system, between 0 and 0.9. Default 0.2.")]
            double validationFraction = 0.2,
            [Description(
                "Share of the rows held back purely to measure quality on rows never seen during fitting, between 0 and 0.9. Together with the validation share it may not exceed 0.9. Default 0.2.")]
            double testFraction = 0.2,
            [Description(
                "Seed that makes the row split reproducible: the same seed and the same rows always produce the same training, validation and test split.")]
            int seed = 1,
            [Description("Optional opaque label kept with a newly created model, for finding it again.")]
            string? correlationKey = null,
            CancellationToken cancellationToken = default)
        {
            return SubmitAsync(instanceId, modelName, variableNames, processingTypeIds, inputs, outputs, complexity,
                kernel, gamma, degree, validationFraction, testFraction, seed, correlationKey, cancellationToken);
        }

        private async Task<TrainingRunSubmissionView> SubmitAsync(int instanceId, string modelName,
            string[] variableNames, int[] processingTypeIds, double[][] inputs, double[] outputs, double complexity,
            KernelRidgeRegressorKernel kernel, double gamma, int degree, double validationFraction, double testFraction,
            int seed, string? correlationKey, CancellationToken cancellationToken)
        {
            var submission = await service.SubmitTrainingAsync(
                new AdvancedAnalyticsKernelRidgeRegressorTrainingDto
                {
                    AdvancedAnalyticsInstanceId = instanceId,
                    ModelName = modelName,
                    CorrelationKey = correlationKey,
                    VariableNames = variableNames,
                    ProcessingTypeIds = processingTypeIds,
                    Inputs = inputs,
                    Outputs = outputs,
                    Complexity = complexity,
                    Kernel = kernel,
                    Gamma = gamma,
                    Degree = degree,
                    ValidationFraction = validationFraction,
                    TestFraction = testFraction,
                    Seed = seed
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