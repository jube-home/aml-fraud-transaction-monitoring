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

using Jube.Data.Repository.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Engine.AdvancedAnalytics.Anomaly;
using Jube.Engine.AdvancedAnalytics.Classification;
using Jube.Engine.AdvancedAnalytics.Clustering;
using Jube.Engine.AdvancedAnalytics.Density;
using Jube.Engine.AdvancedAnalytics.DimensionReduction;
using Jube.Engine.AdvancedAnalytics.Regression;
using log4net;
using Jube.Engine.AdvancedAnalytics.Training.Exceptions;
using Jube.Engine.AdvancedAnalytics.Anomaly.Models;
using Jube.Engine.AdvancedAnalytics.Regression.Models;
using Jube.Engine.AdvancedAnalytics.Classification.Models;
using Jube.Engine.AdvancedAnalytics.Clustering.Models;
using Jube.Engine.AdvancedAnalytics.Density.Models;
using Jube.Engine.AdvancedAnalytics.DimensionReduction.Models;
using Jube.Engine.AdvancedAnalytics.Training.Models;
using Jube.Data.Query.GetNextAdvancedAnalyticsTrainingRunQuery;
using Jube.Analytics;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Engine.AdvancedAnalytics.Training
{
    public sealed class AdvancedAnalyticsTrainingWorker(
        ILog log,
        AdvancedAnalyticsTrainingSettings settings,
        Func<DbContext> contextFactory,
        Func<DateTime> clock = null,
        string engineName = null,
        AdvancedAnalyticsTrainingInputResolver resolver = null)
    {
        public const string EngineStoppedError = "The engine stopped while the run was in progress.";
        public const string TimeoutError = "The run exceeded the maximum run duration and was cancelled.";
        public const string StoppedByRequestError = "The run was stopped on request.";
        public const string ShutdownError = "The engine was shutting down and the run was cancelled.";
        private readonly Func<DateTime> clock = clock ?? (() => DateTime.UtcNow);

        private readonly string engineName =
            engineName ?? $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";

        private readonly AdvancedAnalyticsTrainingInputResolver resolver =
            resolver ?? new AdvancedAnalyticsTrainingInputResolver();

        private readonly HashSet<Task> running = [];
        private readonly Lock runningLock = new();
        private readonly SemaphoreSlim slots = new(Math.Max(1, settings.MaxConcurrentRuns));
        private DateTime lastRecovery = DateTime.MinValue;

        public Func<double[][], int[], string[], LogisticRegressionOptions,
            CancellationToken, LogisticRegressionResult> Trainer { get; set; }

        public Func<double[][], string[], KMeansClustererOptions, CancellationToken, KMeansClustererResult>
            ClustererTrainer { get; set; }

        public Func<double[][], string[], BinarySplitClustererOptions, CancellationToken, BinarySplitClustererResult>
            BinarySplitClustererTrainer { get; set; }

        public Func<double[][], string[], MiniBatchKMeansClustererOptions, CancellationToken,
            MiniBatchKMeansClustererResult> MiniBatchKMeansClustererTrainer { get; set; }

        public Func<double[][], string[], KMedoidsClustererOptions, CancellationToken, KMedoidsClustererResult>
            KMedoidsClustererTrainer { get; set; }

        public Func<double[][], string[], BalancedKMeansClustererOptions, CancellationToken,
            BalancedKMeansClustererResult> BalancedKMeansClustererTrainer { get; set; }

        public Func<double[][], string[], GaussianMixtureClustererOptions, CancellationToken,
            GaussianMixtureClustererResult> GaussianMixtureClustererTrainer { get; set; }

        public Func<double[][], string[], KModesClustererOptions, CancellationToken, KModesClustererResult>
            KModesClustererTrainer { get; set; }

        public Func<double[][], string[], MeanShiftClustererOptions, CancellationToken, MeanShiftClustererResult>
            MeanShiftClustererTrainer { get; set; }

        public Func<double[][], int[], string[], MahalanobisAnomalyOptions, CancellationToken,
            MahalanobisAnomalyResult> MahalanobisTrainer { get; set; }

        public Func<double[][], int[], string[], IsolationForestAnomalyOptions, CancellationToken,
            IsolationForestAnomalyResult> IsolationForestTrainer { get; set; }

        public Func<double[][], int[], string[], OneClassSupportVectorAnomalyOptions, CancellationToken,
            OneClassSupportVectorAnomalyResult> OneClassSupportVectorAnomalyTrainer { get; set; }

        public Func<double[][], double[], string[],
                LinearRegressionOptions, CancellationToken,
                LinearRegressionResult>
            LinearRegressionTrainer { get; set; }

        public Func<double[][], double[], string[], DecisionTreeRegressorOptions, CancellationToken,
            DecisionTreeRegressorResult> DecisionTreeRegressorTrainer { get; set; }

        public Func<double[][], double[], string[], KNearestNeighboursRegressorOptions, CancellationToken,
                KNearestNeighboursRegressorResult>
            KNearestNeighboursRegressorTrainer { get; set; }

        public Func<double[][], double[], string[], RandomForestRegressorOptions, CancellationToken,
                RandomForestRegressorResult>
            RandomForestRegressorTrainer { get; set; }

        public Func<double[][], double[], string[], KernelRidgeRegressorOptions, CancellationToken,
                KernelRidgeRegressorResult>
            KernelRidgeRegressorTrainer { get; set; }

        public Func<double[][], double[], string[], NeuralNetworkRegressorOptions, CancellationToken,
                NeuralNetworkRegressorResult>
            NeuralNetworkRegressorTrainer { get; set; }

        public Func<double[][], double[], string[], GradientBoostingRegressorOptions, CancellationToken,
                GradientBoostingRegressorResult>
            GradientBoostingRegressorTrainer { get; set; }

        public Func<double[][], string[], PrincipalComponentOptions, CancellationToken, PrincipalComponentResult>
            PrincipalComponentTrainer { get; set; }

        public Func<double[][], string[], GaussianMixtureDensityOptions, CancellationToken,
            GaussianMixtureDensityResult> GaussianMixtureDensityTrainer { get; set; }

        public Func<double[][], int[], string[], NeuralNetworkClassifierOptions, CancellationToken,
            NeuralNetworkClassifierResult> NeuralNetworkClassifierTrainer { get; set; }

        public Func<double[][], int[], string[], RandomForestClassifierOptions, CancellationToken,
            RandomForestClassifierResult> RandomForestClassifierTrainer { get; set; }

        public Func<double[][], int[], string[], DecisionTreeClassifierOptions, CancellationToken,
            DecisionTreeClassifierResult> DecisionTreeClassifierTrainer { get; set; }

        public Func<double[][], int[], string[], NaiveBayesClassifierOptions, CancellationToken,
            NaiveBayesClassifierResult> NaiveBayesClassifierTrainer { get; set; }

        public Func<double[][], int[], string[], TreeAugmentedNaiveBayesClassifierOptions, CancellationToken,
            TreeAugmentedNaiveBayesClassifierResult> TreeAugmentedNaiveBayesClassifierTrainer { get; set; }

        public Func<double[][], int[], string[], KNearestNeighboursClassifierOptions, CancellationToken,
            KNearestNeighboursClassifierResult> KNearestNeighboursClassifierTrainer { get; set; }

        public Func<double[][], int[], string[], BaggingClassifierOptions, CancellationToken,
            BaggingClassifierResult> BaggingClassifierTrainer { get; set; }

        public Func<double[][], int[], string[], AdaBoostClassifierOptions, CancellationToken,
            AdaBoostClassifierResult> AdaBoostClassifierTrainer { get; set; }

        public Func<double[][], int[], string[], GradientBoostingClassifierOptions, CancellationToken,
            GradientBoostingClassifierResult> GradientBoostingClassifierTrainer { get; set; }

        public Func<double[][], int[], string[], LinearSupportVectorClassifierOptions, CancellationToken,
            LinearSupportVectorClassifierResult> LinearSupportVectorClassifierTrainer { get; set; }

        public Func<double[][], int[], string[], KernelSupportVectorClassifierOptions, CancellationToken,
            KernelSupportVectorClassifierResult> KernelSupportVectorClassifierTrainer { get; set; }

        public int RunningCount
        {
            get
            {
                lock (runningLock)
                {
                    return running.Count;
                }
            }
        }

        public async Task StartAsync(CancellationToken token = default)
        {
            var consecutiveErrors = 0;

            try
            {
                await RecoverStaleAsync(true, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                log.Error($"Advanced Analytics Training: stale run recovery at start failed as {ex}");
            }

            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await RunPassAsync(token).ConfigureAwait(false);
                        consecutiveErrors = 0;
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        consecutiveErrors++;

                        log.Error($"Advanced Analytics Training: pass failed as {ex}  backing off " +
                                  $"(consecutive errors {consecutiveErrors}).");
                    }

                    await Task.Delay(NextDelay(consecutiveErrors), token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                log.Info("Advanced Analytics Training: graceful cancellation of the poll loop.");
            }
            catch (Exception ex)
            {
                log.Error($"Advanced Analytics Training: StartAsync has produced an error {ex}");
            }
            finally
            {
                await DrainAsync().ConfigureAwait(false);
            }
        }

        internal TimeSpan NextDelay(int consecutiveErrors)
        {
            if (consecutiveErrors <= 0)
            {
                return settings.PollInterval;
            }

            var factor = Math.Pow(2, Math.Min(consecutiveErrors, 16));

            var milliseconds = Math.Min(settings.PollInterval.TotalMilliseconds * factor,
                settings.MaxBackoff.TotalMilliseconds);

            return TimeSpan.FromMilliseconds(Math.Max(milliseconds, settings.PollInterval.TotalMilliseconds));
        }

        public async Task<int> RunPassAsync(CancellationToken token = default)
        {
            await RecoverStaleAsync(false, token).ConfigureAwait(false);

            var started = 0;

            while (!token.IsCancellationRequested &&
                   await slots.WaitAsync(0, CancellationToken.None).ConfigureAwait(false))
            {
                GetNextAdvancedAnalyticsTrainingRunQueryDto next;

                try
                {
                    var claimContext = contextFactory();

                    try
                    {
                        next = await new GetNextAdvancedAnalyticsTrainingRunQuery(claimContext, engineName,
                            settings.MaxConcurrentRunsPerTenant, clock()).ExecuteAsync(token).ConfigureAwait(false);
                    }
                    finally
                    {
                        await claimContext.DisposeAsync(token).ConfigureAwait(false);
                    }
                }
                catch
                {
                    slots.Release();
                    throw;
                }

                if (next == null)
                {
                    slots.Release();
                    break;
                }

                started++;
                TrackRun(next, token);
            }

            return started;
        }

        public async Task RecoverStaleAsync(bool force, CancellationToken token = default)
        {
            var now = clock();

            if (!force && now - lastRecovery < settings.StaleRecoveryInterval)
            {
                return;
            }

            lastRecovery = now;

            var context = contextFactory();

            try
            {
                var recovered = await AdvancedAnalyticsTrainingRunRepository
                    .RecoverStaleAsync(context, now - settings.StaleThreshold, now, EngineStoppedError, token)
                    .ConfigureAwait(false);

                if (recovered.Count > 0)
                {
                    log.Warn($"Advanced Analytics Training: recovered stale runs [{string.Join(",", recovered)}] " +
                             "whose heartbeat had stopped; they were marked as stopped with the engine.");
                }
            }
            finally
            {
                await context.DisposeAsync(token).ConfigureAwait(false);
            }
        }

        public async Task WaitForIdleAsync(CancellationToken token = default)
        {
            while (true)
            {
                Task[] snapshot;

                lock (runningLock)
                {
                    snapshot = [.. running];
                }

                if (snapshot.Length == 0)
                {
                    return;
                }

                await Task.WhenAny(Task.WhenAll(snapshot), Task.Delay(20, token)).ConfigureAwait(false);
            }
        }

        private async Task DrainAsync()
        {
            try
            {
                using var grace = new CancellationTokenSource(settings.ShutdownGrace);
                await WaitForIdleAsync(grace.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                log.Warn("Advanced Analytics Training: runs were still finishing when the shutdown grace elapsed.");
            }
        }

        private void TrackRun(GetNextAdvancedAnalyticsTrainingRunQueryDto run, CancellationToken shutdown)
        {
            var self = new Task[1];
            var gate = new SemaphoreSlim(0, 1);

            self[0] = Task.Run(async () =>
            {
                try
                {
                    await gate.WaitAsync(shutdown).ConfigureAwait(false);
                    await RunClaimedAsync(run, shutdown).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    log.Error($"Advanced Analytics Training: run {run.Id} escaped its handler as {ex}");
                }
                finally
                {
                    lock (runningLock)
                    {
                        running.Remove(self[0]);
                    }

                    slots.Release();
                    gate.Dispose();
                }
            }, CancellationToken.None);

            lock (runningLock)
            {
                running.Add(self[0]);
            }

            gate.Release();
        }

        private async Task RunClaimedAsync(GetNextAdvancedAnalyticsTrainingRunQueryDto claimed,
            CancellationToken shutdown)
        {
            log.Info($"Advanced Analytics Training: {engineName} started run {claimed.Id}.");

            using var timeout = new CancellationTokenSource(settings.MaxRunDuration);
            using var userStop = new CancellationTokenSource();
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(shutdown, timeout.Token, userStop.Token);

            var context = contextFactory();
            AdvancedAnalyticsTrainingExecutor executor = null;
            KMeansClustererTrainingExecutor clustererExecutor = null;
            BinarySplitClustererTrainingExecutor binarySplitClustererExecutor = null;
            MiniBatchKMeansClustererTrainingExecutor miniBatchKMeansClustererExecutor = null;
            KMedoidsClustererTrainingExecutor kMedoidsClustererExecutor = null;
            BalancedKMeansClustererTrainingExecutor balancedKMeansClustererExecutor = null;
            GaussianMixtureClustererTrainingExecutor gaussianMixtureClustererExecutor = null;
            KModesClustererTrainingExecutor kModesClustererExecutor = null;
            MeanShiftClustererTrainingExecutor meanShiftClustererExecutor = null;
            MahalanobisAnomalyTrainingExecutor mahalanobisExecutor = null;
            AdvancedAnalyticsIsolationForestAnomalyTrainingExecutor isolationForestExecutor = null;
            AdvancedAnalyticsOneClassSupportVectorAnomalyTrainingExecutor oneClassSupportVectorAnomalyExecutor = null;
            AdvancedAnalyticsLinearRegressionTrainingExecutor regressorExecutor = null;
            AdvancedAnalyticsDecisionTreeRegressorTrainingExecutor decisionTreeRegressorExecutor = null;
            AdvancedAnalyticsKNearestNeighboursRegressorTrainingExecutor kNearestNeighboursRegressorExecutor = null;
            AdvancedAnalyticsRandomForestRegressorTrainingExecutor randomForestRegressorExecutor = null;
            AdvancedAnalyticsKernelRidgeRegressorTrainingExecutor kernelRidgeRegressorExecutor = null;
            AdvancedAnalyticsNeuralNetworkRegressorTrainingExecutor neuralNetworkRegressorExecutor = null;
            AdvancedAnalyticsGradientBoostingRegressorTrainingExecutor gradientBoostingRegressorExecutor = null;
            AdvancedAnalyticsPrincipalComponentTrainingExecutor principalComponentExecutor = null;
            AdvancedAnalyticsGaussianMixtureDensityTrainingExecutor gaussianMixtureDensityExecutor = null;
            AdvancedAnalyticsNeuralNetworkClassifierTrainingExecutor neuralNetworkClassifierExecutor = null;
            AdvancedAnalyticsRandomForestClassifierTrainingExecutor randomForestClassifierExecutor = null;
            AdvancedAnalyticsDecisionTreeClassifierTrainingExecutor decisionTreeClassifierExecutor = null;
            AdvancedAnalyticsNaiveBayesClassifierTrainingExecutor naiveBayesClassifierExecutor = null;
            AdvancedAnalyticsTreeAugmentedNaiveBayesClassifierTrainingExecutor
                treeAugmentedNaiveBayesClassifierExecutor = null;
            AdvancedAnalyticsKNearestNeighboursClassifierTrainingExecutor kNearestNeighboursClassifierExecutor = null;
            AdvancedAnalyticsBaggingClassifierTrainingExecutor baggingClassifierExecutor = null;
            AdvancedAnalyticsAdaBoostClassifierTrainingExecutor adaBoostClassifierExecutor = null;
            AdvancedAnalyticsGradientBoostingClassifierTrainingExecutor gradientBoostingClassifierExecutor = null;
            AdvancedAnalyticsLinearSupportVectorClassifierTrainingExecutor linearSupportVectorClassifierExecutor =
                null;
            AdvancedAnalyticsKernelSupportVectorClassifierTrainingExecutor kernelSupportVectorClassifierExecutor =
                null;

            var terminalNeeded = true;

            try
            {
                var userName = claimed.UserName ?? string.Empty;
                var runs = new AdvancedAnalyticsTrainingRunRepository(context, claimed.TenantRegistryId, userName);
                var run = await runs.GetByIdAsync(claimed.Id, CancellationToken.None).ConfigureAwait(false);

                if (run == null)
                {
                    log.Warn($"Advanced Analytics Training: run {claimed.Id} vanished after it was claimed.");
                    terminalNeeded = false;

                    return;
                }

                var monitor = MonitorAsync(claimed.Id, userStop, linked.Token);

                try
                {
                    string status;

                    switch (claimed.ModelKindId)
                    {
                        case AdvancedAnalyticsModelKind.KMeansClustererPipeline:
                        {
                            clustererExecutor = new KMeansClustererTrainingExecutor(context,
                                claimed.TenantRegistryId, userName, log);

                            if (ClustererTrainer != null)
                            {
                                clustererExecutor.Trainer = ClustererTrainer;
                            }

                            var arguments = AdvancedAnalyticsClustererTrainingArguments.FromJson(claimed.Arguments);

                            var result = await clustererExecutor.ExecuteAsync(run, arguments,
                                _ => Task.FromResult(arguments.Inputs ??
                                                     throw new AdvancedAnalyticsTrainingInputException(
                                                         "The run carries no inline rows to train on.")),
                                linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.BinarySplitClustererPipeline:
                        {
                            binarySplitClustererExecutor = new BinarySplitClustererTrainingExecutor(context,
                                claimed.TenantRegistryId, userName, log);

                            if (BinarySplitClustererTrainer != null)
                            {
                                binarySplitClustererExecutor.Trainer = BinarySplitClustererTrainer;
                            }

                            var arguments = BinarySplitClustererTrainingArguments.FromJson(claimed.Arguments);

                            var result = await binarySplitClustererExecutor.ExecuteAsync(run, arguments,
                                _ => Task.FromResult(arguments.Inputs ??
                                                     throw new AdvancedAnalyticsTrainingInputException(
                                                         "The run carries no inline rows to train on.")),
                                linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.MiniBatchKMeansClustererPipeline:
                        {
                            miniBatchKMeansClustererExecutor = new MiniBatchKMeansClustererTrainingExecutor(context,
                                claimed.TenantRegistryId, userName, log);

                            if (MiniBatchKMeansClustererTrainer != null)
                            {
                                miniBatchKMeansClustererExecutor.Trainer = MiniBatchKMeansClustererTrainer;
                            }

                            var arguments = MiniBatchKMeansClustererTrainingArguments.FromJson(claimed.Arguments);

                            var result = await miniBatchKMeansClustererExecutor.ExecuteAsync(run, arguments,
                                _ => Task.FromResult(arguments.Inputs ??
                                                     throw new AdvancedAnalyticsTrainingInputException(
                                                         "The run carries no inline rows to train on.")),
                                linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.KMedoidsClustererPipeline:
                        {
                            kMedoidsClustererExecutor = new KMedoidsClustererTrainingExecutor(context,
                                claimed.TenantRegistryId, userName, log);

                            if (KMedoidsClustererTrainer != null)
                            {
                                kMedoidsClustererExecutor.Trainer = KMedoidsClustererTrainer;
                            }

                            var arguments = KMedoidsClustererTrainingArguments.FromJson(claimed.Arguments);

                            var result = await kMedoidsClustererExecutor.ExecuteAsync(run, arguments,
                                _ => Task.FromResult(arguments.Inputs ??
                                                     throw new AdvancedAnalyticsTrainingInputException(
                                                         "The run carries no inline rows to train on.")),
                                linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.BalancedKMeansClustererPipeline:
                        {
                            balancedKMeansClustererExecutor = new BalancedKMeansClustererTrainingExecutor(context,
                                claimed.TenantRegistryId, userName, log);

                            if (BalancedKMeansClustererTrainer != null)
                            {
                                balancedKMeansClustererExecutor.Trainer = BalancedKMeansClustererTrainer;
                            }

                            var arguments = BalancedKMeansClustererTrainingArguments.FromJson(claimed.Arguments);

                            var result = await balancedKMeansClustererExecutor.ExecuteAsync(run, arguments,
                                _ => Task.FromResult(arguments.Inputs ??
                                                     throw new AdvancedAnalyticsTrainingInputException(
                                                         "The run carries no inline rows to train on.")),
                                linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.GaussianMixtureClustererPipeline:
                        {
                            gaussianMixtureClustererExecutor = new GaussianMixtureClustererTrainingExecutor(context,
                                claimed.TenantRegistryId, userName, log);

                            if (GaussianMixtureClustererTrainer != null)
                            {
                                gaussianMixtureClustererExecutor.Trainer = GaussianMixtureClustererTrainer;
                            }

                            var arguments = GaussianMixtureClustererTrainingArguments.FromJson(claimed.Arguments);

                            var result = await gaussianMixtureClustererExecutor.ExecuteAsync(run, arguments,
                                _ => Task.FromResult(arguments.Inputs ??
                                                     throw new AdvancedAnalyticsTrainingInputException(
                                                         "The run carries no inline rows to train on.")),
                                linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.KModesClustererPipeline:
                        {
                            kModesClustererExecutor = new KModesClustererTrainingExecutor(context,
                                claimed.TenantRegistryId, userName, log);

                            if (KModesClustererTrainer != null)
                            {
                                kModesClustererExecutor.Trainer = KModesClustererTrainer;
                            }

                            var arguments = KModesClustererTrainingArguments.FromJson(claimed.Arguments);

                            var result = await kModesClustererExecutor.ExecuteAsync(run, arguments,
                                _ => Task.FromResult(arguments.Inputs ??
                                                     throw new AdvancedAnalyticsTrainingInputException(
                                                         "The run carries no inline rows to train on.")),
                                linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.MeanShiftClustererPipeline:
                        {
                            meanShiftClustererExecutor = new MeanShiftClustererTrainingExecutor(context,
                                claimed.TenantRegistryId, userName, log);

                            if (MeanShiftClustererTrainer != null)
                            {
                                meanShiftClustererExecutor.Trainer = MeanShiftClustererTrainer;
                            }

                            var arguments = MeanShiftClustererTrainingArguments.FromJson(claimed.Arguments);

                            var result = await meanShiftClustererExecutor.ExecuteAsync(run, arguments,
                                _ => Task.FromResult(arguments.Inputs ??
                                                     throw new AdvancedAnalyticsTrainingInputException(
                                                         "The run carries no inline rows to train on.")),
                                linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.MahalanobisAnomalyPipeline:
                        {
                            mahalanobisExecutor = new MahalanobisAnomalyTrainingExecutor(context,
                                claimed.TenantRegistryId, userName, log);

                            if (MahalanobisTrainer != null)
                            {
                                mahalanobisExecutor.Trainer = MahalanobisTrainer;
                            }

                            var arguments = MahalanobisAnomalyTrainingArguments.FromJson(claimed.Arguments);

                            var inputs = arguments.Inputs ?? throw new AdvancedAnalyticsTrainingInputException(
                                "The run carries no inline rows to train on.");

                            var result = await mahalanobisExecutor.ExecuteAsync(run, arguments, inputs,
                                arguments.Labels, linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.IsolationForestAnomalyPipeline:
                        {
                            isolationForestExecutor = new AdvancedAnalyticsIsolationForestAnomalyTrainingExecutor(
                                context, claimed.TenantRegistryId, userName, log);

                            if (IsolationForestTrainer != null)
                            {
                                isolationForestExecutor.Trainer = IsolationForestTrainer;
                            }

                            var arguments = IsolationForestAnomalyTrainingArguments.FromJson(claimed.Arguments);

                            var inputs = arguments.Inputs ?? throw new AdvancedAnalyticsTrainingInputException(
                                "The run carries no inline rows to train on.");

                            var result = await isolationForestExecutor.ExecuteAsync(run, arguments, inputs,
                                arguments.Labels, linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.OneClassSupportVectorAnomalyPipeline:
                        {
                            oneClassSupportVectorAnomalyExecutor =
                                new AdvancedAnalyticsOneClassSupportVectorAnomalyTrainingExecutor(
                                    context, claimed.TenantRegistryId, userName, log);

                            if (OneClassSupportVectorAnomalyTrainer != null)
                            {
                                oneClassSupportVectorAnomalyExecutor.Trainer = OneClassSupportVectorAnomalyTrainer;
                            }

                            var arguments = OneClassSupportVectorAnomalyTrainingArguments.FromJson(claimed.Arguments);

                            var inputs = arguments.Inputs ?? throw new AdvancedAnalyticsTrainingInputException(
                                "The run carries no inline rows to train on.");

                            var result = await oneClassSupportVectorAnomalyExecutor.ExecuteAsync(run, arguments,
                                inputs, arguments.Labels, linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.PrincipalComponentPipeline:
                        {
                            principalComponentExecutor = new AdvancedAnalyticsPrincipalComponentTrainingExecutor(
                                context, claimed.TenantRegistryId, userName, log);

                            if (PrincipalComponentTrainer != null)
                            {
                                principalComponentExecutor.Trainer = PrincipalComponentTrainer;
                            }

                            var arguments = PrincipalComponentTrainingArguments.FromJson(claimed.Arguments);

                            var result = await principalComponentExecutor.ExecuteAsync(run, arguments,
                                _ => Task.FromResult(arguments.Inputs ??
                                                     throw new AdvancedAnalyticsTrainingInputException(
                                                         "The run carries no inline rows to train on.")),
                                linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.GaussianMixtureDensityPipeline:
                        {
                            gaussianMixtureDensityExecutor =
                                new AdvancedAnalyticsGaussianMixtureDensityTrainingExecutor(
                                    context, claimed.TenantRegistryId, userName, log);

                            if (GaussianMixtureDensityTrainer != null)
                            {
                                gaussianMixtureDensityExecutor.Trainer = GaussianMixtureDensityTrainer;
                            }

                            var arguments = GaussianMixtureDensityTrainingArguments.FromJson(claimed.Arguments);

                            var result = await gaussianMixtureDensityExecutor.ExecuteAsync(run, arguments,
                                _ => Task.FromResult(arguments.Inputs ??
                                                     throw new AdvancedAnalyticsTrainingInputException(
                                                         "The run carries no inline rows to train on.")),
                                linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.NeuralNetworkClassifierPipeline:
                        {
                            neuralNetworkClassifierExecutor =
                                new AdvancedAnalyticsNeuralNetworkClassifierTrainingExecutor(
                                    context, claimed.TenantRegistryId, userName, log);

                            if (NeuralNetworkClassifierTrainer != null)
                            {
                                neuralNetworkClassifierExecutor.Trainer = NeuralNetworkClassifierTrainer;
                            }

                            var arguments = NeuralNetworkClassifierTrainingArguments.FromJson(claimed.Arguments);

                            arguments.Input ??= claimed.InputPointer.HasValue
                                ? AdvancedAnalyticsTrainingInput.CachePointer(claimed.InputPointer.Value)
                                : new AdvancedAnalyticsTrainingInput();

                            var resolved = await resolver.ResolveAsync(new AdvancedAnalyticsTrainingInputContext
                            {
                                DbContext = context,
                                TenantRegistryId = claimed.TenantRegistryId,
                                UserName = userName,
                                Input = arguments.Input,
                                VariableCount = arguments.VariableNames?.Length ?? 0,
                                Settings = settings,
                                UtcNow = clock()
                            }, linked.Token).ConfigureAwait(false);

                            var result = await neuralNetworkClassifierExecutor.ExecuteAsync(run, arguments,
                                resolved.Inputs, resolved.Labels, linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.RandomForestClassifierPipeline:
                        {
                            randomForestClassifierExecutor =
                                new AdvancedAnalyticsRandomForestClassifierTrainingExecutor(
                                    context, claimed.TenantRegistryId, userName, log);

                            if (RandomForestClassifierTrainer != null)
                            {
                                randomForestClassifierExecutor.Trainer = RandomForestClassifierTrainer;
                            }

                            var arguments = RandomForestClassifierTrainingArguments.FromJson(claimed.Arguments);

                            arguments.Input ??= claimed.InputPointer.HasValue
                                ? AdvancedAnalyticsTrainingInput.CachePointer(claimed.InputPointer.Value)
                                : new AdvancedAnalyticsTrainingInput();

                            var result = await randomForestClassifierExecutor.ExecuteAsync(run, arguments,
                                async innerToken =>
                                {
                                    var resolved = await resolver.ResolveAsync(
                                        new AdvancedAnalyticsTrainingInputContext
                                        {
                                            DbContext = context,
                                            TenantRegistryId = claimed.TenantRegistryId,
                                            UserName = userName,
                                            Input = arguments.Input,
                                            VariableCount = arguments.VariableNames?.Length ?? 0,
                                            Settings = settings,
                                            UtcNow = clock()
                                        }, innerToken).ConfigureAwait(false);

                                    return (resolved.Inputs, resolved.Labels);
                                },
                                linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.DecisionTreeClassifierPipeline:
                        {
                            decisionTreeClassifierExecutor =
                                new AdvancedAnalyticsDecisionTreeClassifierTrainingExecutor(
                                    context, claimed.TenantRegistryId, userName, log);

                            if (DecisionTreeClassifierTrainer != null)
                            {
                                decisionTreeClassifierExecutor.Trainer = DecisionTreeClassifierTrainer;
                            }

                            var arguments = DecisionTreeClassifierTrainingArguments.FromJson(claimed.Arguments);

                            arguments.Input ??= claimed.InputPointer.HasValue
                                ? AdvancedAnalyticsTrainingInput.CachePointer(claimed.InputPointer.Value)
                                : new AdvancedAnalyticsTrainingInput();

                            var result = await decisionTreeClassifierExecutor.ExecuteAsync(run, arguments,
                                async innerToken =>
                                {
                                    var resolved = await resolver.ResolveAsync(
                                        new AdvancedAnalyticsTrainingInputContext
                                        {
                                            DbContext = context,
                                            TenantRegistryId = claimed.TenantRegistryId,
                                            UserName = userName,
                                            Input = arguments.Input,
                                            VariableCount = arguments.VariableNames?.Length ?? 0,
                                            Settings = settings,
                                            UtcNow = clock()
                                        }, innerToken).ConfigureAwait(false);

                                    return (resolved.Inputs, resolved.Labels);
                                },
                                linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.NaiveBayesClassifierPipeline:
                        {
                            naiveBayesClassifierExecutor = new AdvancedAnalyticsNaiveBayesClassifierTrainingExecutor(
                                context, claimed.TenantRegistryId, userName, log);

                            if (NaiveBayesClassifierTrainer != null)
                            {
                                naiveBayesClassifierExecutor.Trainer = NaiveBayesClassifierTrainer;
                            }

                            var arguments = NaiveBayesClassifierTrainingArguments.FromJson(claimed.Arguments);

                            arguments.Input ??= claimed.InputPointer.HasValue
                                ? AdvancedAnalyticsTrainingInput.CachePointer(claimed.InputPointer.Value)
                                : new AdvancedAnalyticsTrainingInput();

                            var resolved = await resolver.ResolveAsync(new AdvancedAnalyticsTrainingInputContext
                            {
                                DbContext = context,
                                TenantRegistryId = claimed.TenantRegistryId,
                                UserName = userName,
                                Input = arguments.Input,
                                VariableCount = arguments.VariableNames?.Length ?? 0,
                                Settings = settings,
                                UtcNow = clock()
                            }, linked.Token).ConfigureAwait(false);

                            var result = await naiveBayesClassifierExecutor.ExecuteAsync(run, arguments,
                                resolved.Inputs, resolved.Labels, linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.TreeAugmentedNaiveBayesClassifierPipeline:
                        {
                            treeAugmentedNaiveBayesClassifierExecutor =
                                new AdvancedAnalyticsTreeAugmentedNaiveBayesClassifierTrainingExecutor(context,
                                    claimed.TenantRegistryId, userName, log);

                            if (TreeAugmentedNaiveBayesClassifierTrainer != null)
                            {
                                treeAugmentedNaiveBayesClassifierExecutor.Trainer =
                                    TreeAugmentedNaiveBayesClassifierTrainer;
                            }

                            var arguments =
                                TreeAugmentedNaiveBayesClassifierTrainingArguments.FromJson(claimed.Arguments);

                            arguments.Input ??= claimed.InputPointer.HasValue
                                ? AdvancedAnalyticsTrainingInput.CachePointer(claimed.InputPointer.Value)
                                : new AdvancedAnalyticsTrainingInput();

                            var resolved = await resolver.ResolveAsync(new AdvancedAnalyticsTrainingInputContext
                            {
                                DbContext = context,
                                TenantRegistryId = claimed.TenantRegistryId,
                                UserName = userName,
                                Input = arguments.Input,
                                VariableCount = arguments.VariableNames?.Length ?? 0,
                                Settings = settings,
                                UtcNow = clock()
                            }, linked.Token).ConfigureAwait(false);

                            var result = await treeAugmentedNaiveBayesClassifierExecutor.ExecuteAsync(run, arguments,
                                resolved.Inputs, resolved.Labels, linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.BaggingClassifierPipeline:
                        {
                            baggingClassifierExecutor = new AdvancedAnalyticsBaggingClassifierTrainingExecutor(
                                context, claimed.TenantRegistryId, userName, log);

                            if (BaggingClassifierTrainer != null)
                            {
                                baggingClassifierExecutor.Trainer = BaggingClassifierTrainer;
                            }

                            var arguments = BaggingClassifierTrainingArguments.FromJson(claimed.Arguments);

                            arguments.Input ??= claimed.InputPointer.HasValue
                                ? AdvancedAnalyticsTrainingInput.CachePointer(claimed.InputPointer.Value)
                                : new AdvancedAnalyticsTrainingInput();

                            var result = await baggingClassifierExecutor.ExecuteAsync(run, arguments,
                                async innerToken =>
                                {
                                    var resolved = await resolver.ResolveAsync(
                                        new AdvancedAnalyticsTrainingInputContext
                                        {
                                            DbContext = context,
                                            TenantRegistryId = claimed.TenantRegistryId,
                                            UserName = userName,
                                            Input = arguments.Input,
                                            VariableCount = arguments.VariableNames?.Length ?? 0,
                                            Settings = settings,
                                            UtcNow = clock()
                                        }, innerToken).ConfigureAwait(false);

                                    return (resolved.Inputs, resolved.Labels);
                                },
                                linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.AdaBoostClassifierPipeline:
                        {
                            adaBoostClassifierExecutor = new AdvancedAnalyticsAdaBoostClassifierTrainingExecutor(
                                context, claimed.TenantRegistryId, userName, log);

                            if (AdaBoostClassifierTrainer != null)
                            {
                                adaBoostClassifierExecutor.Trainer = AdaBoostClassifierTrainer;
                            }

                            var arguments = AdaBoostClassifierTrainingArguments.FromJson(claimed.Arguments);

                            arguments.Input ??= claimed.InputPointer.HasValue
                                ? AdvancedAnalyticsTrainingInput.CachePointer(claimed.InputPointer.Value)
                                : new AdvancedAnalyticsTrainingInput();

                            var result = await adaBoostClassifierExecutor.ExecuteAsync(run, arguments,
                                async innerToken =>
                                {
                                    var resolved = await resolver.ResolveAsync(
                                        new AdvancedAnalyticsTrainingInputContext
                                        {
                                            DbContext = context,
                                            TenantRegistryId = claimed.TenantRegistryId,
                                            UserName = userName,
                                            Input = arguments.Input,
                                            VariableCount = arguments.VariableNames?.Length ?? 0,
                                            Settings = settings,
                                            UtcNow = clock()
                                        }, innerToken).ConfigureAwait(false);

                                    return (resolved.Inputs, resolved.Labels);
                                },
                                linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.GradientBoostingClassifierPipeline:
                        {
                            gradientBoostingClassifierExecutor =
                                new AdvancedAnalyticsGradientBoostingClassifierTrainingExecutor(context,
                                    claimed.TenantRegistryId, userName, log);

                            if (GradientBoostingClassifierTrainer != null)
                            {
                                gradientBoostingClassifierExecutor.Trainer = GradientBoostingClassifierTrainer;
                            }

                            var arguments = GradientBoostingClassifierTrainingArguments.FromJson(claimed.Arguments);

                            arguments.Input ??= claimed.InputPointer.HasValue
                                ? AdvancedAnalyticsTrainingInput.CachePointer(claimed.InputPointer.Value)
                                : new AdvancedAnalyticsTrainingInput();

                            var result = await gradientBoostingClassifierExecutor.ExecuteAsync(run, arguments,
                                async innerToken =>
                                {
                                    var resolved = await resolver.ResolveAsync(
                                        new AdvancedAnalyticsTrainingInputContext
                                        {
                                            DbContext = context,
                                            TenantRegistryId = claimed.TenantRegistryId,
                                            UserName = userName,
                                            Input = arguments.Input,
                                            VariableCount = arguments.VariableNames?.Length ?? 0,
                                            Settings = settings,
                                            UtcNow = clock()
                                        }, innerToken).ConfigureAwait(false);

                                    return (resolved.Inputs, resolved.Labels);
                                },
                                linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.LinearSupportVectorClassifierPipeline:
                        {
                            linearSupportVectorClassifierExecutor =
                                new AdvancedAnalyticsLinearSupportVectorClassifierTrainingExecutor(context,
                                    claimed.TenantRegistryId, userName, log);

                            if (LinearSupportVectorClassifierTrainer != null)
                            {
                                linearSupportVectorClassifierExecutor.Trainer = LinearSupportVectorClassifierTrainer;
                            }

                            var arguments = LinearSupportVectorClassifierTrainingArguments.FromJson(claimed.Arguments);

                            arguments.Input ??= claimed.InputPointer.HasValue
                                ? AdvancedAnalyticsTrainingInput.CachePointer(claimed.InputPointer.Value)
                                : new AdvancedAnalyticsTrainingInput();

                            var resolved = await resolver.ResolveAsync(new AdvancedAnalyticsTrainingInputContext
                            {
                                DbContext = context,
                                TenantRegistryId = claimed.TenantRegistryId,
                                UserName = userName,
                                Input = arguments.Input,
                                VariableCount = arguments.VariableNames?.Length ?? 0,
                                Settings = settings,
                                UtcNow = clock()
                            }, linked.Token).ConfigureAwait(false);

                            var result = await linearSupportVectorClassifierExecutor.ExecuteAsync(run, arguments,
                                resolved.Inputs, resolved.Labels, linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.KernelSupportVectorClassifierPipeline:
                        {
                            kernelSupportVectorClassifierExecutor =
                                new AdvancedAnalyticsKernelSupportVectorClassifierTrainingExecutor(context,
                                    claimed.TenantRegistryId, userName, log);

                            if (KernelSupportVectorClassifierTrainer != null)
                            {
                                kernelSupportVectorClassifierExecutor.Trainer = KernelSupportVectorClassifierTrainer;
                            }

                            var arguments = KernelSupportVectorClassifierTrainingArguments.FromJson(claimed.Arguments);

                            arguments.Input ??= claimed.InputPointer.HasValue
                                ? AdvancedAnalyticsTrainingInput.CachePointer(claimed.InputPointer.Value)
                                : new AdvancedAnalyticsTrainingInput();

                            var resolved = await resolver.ResolveAsync(new AdvancedAnalyticsTrainingInputContext
                            {
                                DbContext = context,
                                TenantRegistryId = claimed.TenantRegistryId,
                                UserName = userName,
                                Input = arguments.Input,
                                VariableCount = arguments.VariableNames?.Length ?? 0,
                                Settings = settings,
                                UtcNow = clock()
                            }, linked.Token).ConfigureAwait(false);

                            var result = await kernelSupportVectorClassifierExecutor.ExecuteAsync(run, arguments,
                                resolved.Inputs, resolved.Labels, linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.KNearestNeighboursClassifierPipeline:
                        {
                            kNearestNeighboursClassifierExecutor =
                                new AdvancedAnalyticsKNearestNeighboursClassifierTrainingExecutor(context,
                                    claimed.TenantRegistryId, userName, log);

                            if (KNearestNeighboursClassifierTrainer != null)
                            {
                                kNearestNeighboursClassifierExecutor.Trainer = KNearestNeighboursClassifierTrainer;
                            }

                            var arguments = KNearestNeighboursClassifierTrainingArguments.FromJson(claimed.Arguments);

                            arguments.Input ??= claimed.InputPointer.HasValue
                                ? AdvancedAnalyticsTrainingInput.CachePointer(claimed.InputPointer.Value)
                                : new AdvancedAnalyticsTrainingInput();

                            var resolved = await resolver.ResolveAsync(new AdvancedAnalyticsTrainingInputContext
                            {
                                DbContext = context,
                                TenantRegistryId = claimed.TenantRegistryId,
                                UserName = userName,
                                Input = arguments.Input,
                                VariableCount = arguments.VariableNames?.Length ?? 0,
                                Settings = settings,
                                UtcNow = clock()
                            }, linked.Token).ConfigureAwait(false);

                            var result = await kNearestNeighboursClassifierExecutor.ExecuteAsync(run, arguments,
                                resolved.Inputs, resolved.Labels, linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.MultipleLinearRegressionPipeline:
                        {
                            regressorExecutor = new AdvancedAnalyticsLinearRegressionTrainingExecutor(context,
                                claimed.TenantRegistryId, userName, log);

                            if (LinearRegressionTrainer != null)
                            {
                                regressorExecutor.Trainer = LinearRegressionTrainer;
                            }

                            var arguments =
                                AdvancedAnalyticsLinearRegressionTrainingArguments.FromJson(claimed.Arguments);

                            var result = await regressorExecutor.ExecuteAsync(run, arguments, linked.Token)
                                .ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.DecisionTreeRegressorPipeline:
                        {
                            decisionTreeRegressorExecutor = new AdvancedAnalyticsDecisionTreeRegressorTrainingExecutor(
                                context, claimed.TenantRegistryId, userName, log);

                            if (DecisionTreeRegressorTrainer != null)
                            {
                                decisionTreeRegressorExecutor.Trainer = DecisionTreeRegressorTrainer;
                            }

                            var arguments = DecisionTreeRegressorTrainingArguments.FromJson(claimed.Arguments);

                            var result = await decisionTreeRegressorExecutor.ExecuteAsync(run, arguments,
                                linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.KNearestNeighboursRegressorPipeline:
                        {
                            kNearestNeighboursRegressorExecutor =
                                new AdvancedAnalyticsKNearestNeighboursRegressorTrainingExecutor(context,
                                    claimed.TenantRegistryId, userName, log);

                            if (KNearestNeighboursRegressorTrainer != null)
                            {
                                kNearestNeighboursRegressorExecutor.Trainer = KNearestNeighboursRegressorTrainer;
                            }

                            var arguments = KNearestNeighboursRegressorTrainingArguments.FromJson(claimed.Arguments);

                            var result = await kNearestNeighboursRegressorExecutor.ExecuteAsync(run, arguments,
                                linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.RandomForestRegressorPipeline:
                        {
                            randomForestRegressorExecutor = new AdvancedAnalyticsRandomForestRegressorTrainingExecutor(
                                context, claimed.TenantRegistryId, userName, log);

                            if (RandomForestRegressorTrainer != null)
                            {
                                randomForestRegressorExecutor.Trainer = RandomForestRegressorTrainer;
                            }

                            var arguments = RandomForestRegressorTrainingArguments.FromJson(claimed.Arguments);

                            var result = await randomForestRegressorExecutor.ExecuteAsync(run, arguments,
                                _ => Task.FromResult((
                                    arguments.Inputs ?? throw new AdvancedAnalyticsTrainingInputException(
                                        "The run carries no inline rows to train on."),
                                    arguments.Outputs ?? throw new AdvancedAnalyticsTrainingInputException(
                                        "The run carries no outputs to train on."))),
                                linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.KernelRidgeRegressorPipeline:
                        {
                            kernelRidgeRegressorExecutor = new AdvancedAnalyticsKernelRidgeRegressorTrainingExecutor(
                                context, claimed.TenantRegistryId, userName, log);

                            if (KernelRidgeRegressorTrainer != null)
                            {
                                kernelRidgeRegressorExecutor.Trainer = KernelRidgeRegressorTrainer;
                            }

                            var arguments = KernelRidgeRegressorTrainingArguments.FromJson(claimed.Arguments);

                            var result = await kernelRidgeRegressorExecutor.ExecuteAsync(run, arguments, linked.Token)
                                .ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.NeuralNetworkRegressorPipeline:
                        {
                            neuralNetworkRegressorExecutor =
                                new AdvancedAnalyticsNeuralNetworkRegressorTrainingExecutor(
                                    context, claimed.TenantRegistryId, userName, log);

                            if (NeuralNetworkRegressorTrainer != null)
                            {
                                neuralNetworkRegressorExecutor.Trainer = NeuralNetworkRegressorTrainer;
                            }

                            var arguments = NeuralNetworkRegressorTrainingArguments.FromJson(claimed.Arguments);

                            var result = await neuralNetworkRegressorExecutor.ExecuteAsync(run, arguments,
                                linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        case AdvancedAnalyticsModelKind.GradientBoostingRegressorPipeline:
                        {
                            gradientBoostingRegressorExecutor =
                                new AdvancedAnalyticsGradientBoostingRegressorTrainingExecutor(context,
                                    claimed.TenantRegistryId, userName, log);

                            if (GradientBoostingRegressorTrainer != null)
                            {
                                gradientBoostingRegressorExecutor.Trainer = GradientBoostingRegressorTrainer;
                            }

                            var arguments = GradientBoostingRegressorTrainingArguments.FromJson(claimed.Arguments);

                            var result = await gradientBoostingRegressorExecutor.ExecuteAsync(run, arguments,
                                linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                        default:
                        {
                            executor = new AdvancedAnalyticsTrainingExecutor(context, claimed.TenantRegistryId,
                                userName, log);

                            if (Trainer != null)
                            {
                                executor.Trainer = Trainer;
                            }

                            var arguments = AdvancedAnalyticsTrainingArguments.FromJson(claimed.Arguments);

                            arguments.Input ??= claimed.InputPointer.HasValue
                                ? AdvancedAnalyticsTrainingInput.CachePointer(claimed.InputPointer.Value)
                                : new AdvancedAnalyticsTrainingInput();

                            var result = await executor.ExecuteAsync(run, arguments,
                                token => resolver.ResolveAsync(new AdvancedAnalyticsTrainingInputContext
                                {
                                    DbContext = context,
                                    TenantRegistryId = claimed.TenantRegistryId,
                                    UserName = userName,
                                    Input = arguments.Input,
                                    VariableCount = arguments.VariableNames?.Length ?? 0,
                                    Settings = settings,
                                    UtcNow = clock()
                                }, token), linked.Token).ConfigureAwait(false);

                            status = result.Status.ToString();
                            break;
                        }
                    }

                    terminalNeeded = false;
                    log.Info($"Advanced Analytics Training: run {claimed.Id} finished as {status}.");
                }
                catch (OperationCanceledException)
                {
                    var (status, error) = timeout.IsCancellationRequested && !shutdown.IsCancellationRequested
                        ? (AdvancedAnalyticsStatus.Failed, TimeoutError)
                        : userStop.IsCancellationRequested
                            ? (AdvancedAnalyticsStatus.Cancelled, StoppedByRequestError)
                            : (AdvancedAnalyticsStatus.Cancelled, ShutdownError);

                    await RecordTerminalAsync(executor, clustererExecutor, binarySplitClustererExecutor,
                        miniBatchKMeansClustererExecutor, kMedoidsClustererExecutor,
                        balancedKMeansClustererExecutor, gaussianMixtureClustererExecutor,
                        kModesClustererExecutor, meanShiftClustererExecutor, mahalanobisExecutor,
                        isolationForestExecutor, oneClassSupportVectorAnomalyExecutor, regressorExecutor,
                        decisionTreeRegressorExecutor,
                        kNearestNeighboursRegressorExecutor, randomForestRegressorExecutor,
                        kernelRidgeRegressorExecutor, neuralNetworkRegressorExecutor,
                        gradientBoostingRegressorExecutor,
                        principalComponentExecutor, gaussianMixtureDensityExecutor, neuralNetworkClassifierExecutor,
                        randomForestClassifierExecutor, decisionTreeClassifierExecutor,
                        naiveBayesClassifierExecutor, treeAugmentedNaiveBayesClassifierExecutor,
                        kNearestNeighboursClassifierExecutor, baggingClassifierExecutor,
                        adaBoostClassifierExecutor, gradientBoostingClassifierExecutor,
                        linearSupportVectorClassifierExecutor, kernelSupportVectorClassifierExecutor,
                        claimed.Id, status, error).ConfigureAwait(false);

                    terminalNeeded = false;
                    log.Info($"Advanced Analytics Training: run {claimed.Id} ended as {status}: {error}");
                }
                catch (Exception ex)
                {
                    log.Error($"Advanced Analytics Training: run {claimed.Id} failed as {ex}");

                    await RecordTerminalAsync(executor, clustererExecutor, binarySplitClustererExecutor,
                        miniBatchKMeansClustererExecutor, kMedoidsClustererExecutor,
                        balancedKMeansClustererExecutor, gaussianMixtureClustererExecutor,
                        kModesClustererExecutor, meanShiftClustererExecutor, mahalanobisExecutor,
                        isolationForestExecutor, oneClassSupportVectorAnomalyExecutor, regressorExecutor,
                        decisionTreeRegressorExecutor,
                        kNearestNeighboursRegressorExecutor, randomForestRegressorExecutor,
                        kernelRidgeRegressorExecutor, neuralNetworkRegressorExecutor,
                        gradientBoostingRegressorExecutor,
                        principalComponentExecutor, gaussianMixtureDensityExecutor, neuralNetworkClassifierExecutor,
                        randomForestClassifierExecutor, decisionTreeClassifierExecutor,
                        naiveBayesClassifierExecutor, treeAugmentedNaiveBayesClassifierExecutor,
                        kNearestNeighboursClassifierExecutor, baggingClassifierExecutor,
                        adaBoostClassifierExecutor, gradientBoostingClassifierExecutor,
                        linearSupportVectorClassifierExecutor, kernelSupportVectorClassifierExecutor,
                        claimed.Id, AdvancedAnalyticsStatus.Failed,
                        ex is AdvancedAnalyticsTrainingInputException
                            ? ex.Message
                            : "The training run failed unexpectedly.").ConfigureAwait(false);

                    terminalNeeded = false;
                }
                finally
                {
                    await userStop.CancelAsync().ConfigureAwait(false);
                    await monitor.ConfigureAwait(false);
                }
            }
            finally
            {
                if (terminalNeeded)
                {
                    log.Warn($"Advanced Analytics Training: run {claimed.Id} left without a terminal status.");
                }

                await context.DisposeAsync(linked.Token).ConfigureAwait(false);
            }
        }

        private static Task RecordTerminalAsync(AdvancedAnalyticsTrainingExecutor executor,
            KMeansClustererTrainingExecutor clustererExecutor,
            BinarySplitClustererTrainingExecutor binarySplitClustererExecutor,
            MiniBatchKMeansClustererTrainingExecutor miniBatchKMeansClustererExecutor,
            KMedoidsClustererTrainingExecutor kMedoidsClustererExecutor,
            BalancedKMeansClustererTrainingExecutor balancedKMeansClustererExecutor,
            GaussianMixtureClustererTrainingExecutor gaussianMixtureClustererExecutor,
            KModesClustererTrainingExecutor kModesClustererExecutor,
            MeanShiftClustererTrainingExecutor meanShiftClustererExecutor,
            MahalanobisAnomalyTrainingExecutor mahalanobisExecutor,
            AdvancedAnalyticsIsolationForestAnomalyTrainingExecutor isolationForestExecutor,
            AdvancedAnalyticsOneClassSupportVectorAnomalyTrainingExecutor oneClassSupportVectorAnomalyExecutor,
            AdvancedAnalyticsLinearRegressionTrainingExecutor regressorExecutor,
            AdvancedAnalyticsDecisionTreeRegressorTrainingExecutor decisionTreeRegressorExecutor,
            AdvancedAnalyticsKNearestNeighboursRegressorTrainingExecutor kNearestNeighboursRegressorExecutor,
            AdvancedAnalyticsRandomForestRegressorTrainingExecutor randomForestRegressorExecutor,
            AdvancedAnalyticsKernelRidgeRegressorTrainingExecutor kernelRidgeRegressorExecutor,
            AdvancedAnalyticsNeuralNetworkRegressorTrainingExecutor neuralNetworkRegressorExecutor,
            AdvancedAnalyticsGradientBoostingRegressorTrainingExecutor gradientBoostingRegressorExecutor,
            AdvancedAnalyticsPrincipalComponentTrainingExecutor principalComponentExecutor,
            AdvancedAnalyticsGaussianMixtureDensityTrainingExecutor gaussianMixtureDensityExecutor,
            AdvancedAnalyticsNeuralNetworkClassifierTrainingExecutor neuralNetworkClassifierExecutor,
            AdvancedAnalyticsRandomForestClassifierTrainingExecutor randomForestClassifierExecutor,
            AdvancedAnalyticsDecisionTreeClassifierTrainingExecutor decisionTreeClassifierExecutor,
            AdvancedAnalyticsNaiveBayesClassifierTrainingExecutor naiveBayesClassifierExecutor,
            AdvancedAnalyticsTreeAugmentedNaiveBayesClassifierTrainingExecutor
                treeAugmentedNaiveBayesClassifierExecutor,
            AdvancedAnalyticsKNearestNeighboursClassifierTrainingExecutor kNearestNeighboursClassifierExecutor,
            AdvancedAnalyticsBaggingClassifierTrainingExecutor baggingClassifierExecutor,
            AdvancedAnalyticsAdaBoostClassifierTrainingExecutor adaBoostClassifierExecutor,
            AdvancedAnalyticsGradientBoostingClassifierTrainingExecutor gradientBoostingClassifierExecutor,
            AdvancedAnalyticsLinearSupportVectorClassifierTrainingExecutor linearSupportVectorClassifierExecutor,
            AdvancedAnalyticsKernelSupportVectorClassifierTrainingExecutor kernelSupportVectorClassifierExecutor,
            int runId, AdvancedAnalyticsStatus status, string error)
        {
            if (clustererExecutor != null)
            {
                return clustererExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (binarySplitClustererExecutor != null)
            {
                return binarySplitClustererExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (miniBatchKMeansClustererExecutor != null)
            {
                return miniBatchKMeansClustererExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (kMedoidsClustererExecutor != null)
            {
                return kMedoidsClustererExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (balancedKMeansClustererExecutor != null)
            {
                return balancedKMeansClustererExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (gaussianMixtureClustererExecutor != null)
            {
                return gaussianMixtureClustererExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (kModesClustererExecutor != null)
            {
                return kModesClustererExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (meanShiftClustererExecutor != null)
            {
                return meanShiftClustererExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (mahalanobisExecutor != null)
            {
                return mahalanobisExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (isolationForestExecutor != null)
            {
                return isolationForestExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (oneClassSupportVectorAnomalyExecutor != null)
            {
                return oneClassSupportVectorAnomalyExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (regressorExecutor != null)
            {
                return regressorExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (decisionTreeRegressorExecutor != null)
            {
                return decisionTreeRegressorExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (kNearestNeighboursRegressorExecutor != null)
            {
                return kNearestNeighboursRegressorExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (randomForestRegressorExecutor != null)
            {
                return randomForestRegressorExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (kernelRidgeRegressorExecutor != null)
            {
                return kernelRidgeRegressorExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (neuralNetworkRegressorExecutor != null)
            {
                return neuralNetworkRegressorExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (gradientBoostingRegressorExecutor != null)
            {
                return gradientBoostingRegressorExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (principalComponentExecutor != null)
            {
                return principalComponentExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (gaussianMixtureDensityExecutor != null)
            {
                return gaussianMixtureDensityExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (neuralNetworkClassifierExecutor != null)
            {
                return neuralNetworkClassifierExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (randomForestClassifierExecutor != null)
            {
                return randomForestClassifierExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (decisionTreeClassifierExecutor != null)
            {
                return decisionTreeClassifierExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (naiveBayesClassifierExecutor != null)
            {
                return naiveBayesClassifierExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (treeAugmentedNaiveBayesClassifierExecutor != null)
            {
                return treeAugmentedNaiveBayesClassifierExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (kNearestNeighboursClassifierExecutor != null)
            {
                return kNearestNeighboursClassifierExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (baggingClassifierExecutor != null)
            {
                return baggingClassifierExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (adaBoostClassifierExecutor != null)
            {
                return adaBoostClassifierExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (gradientBoostingClassifierExecutor != null)
            {
                return gradientBoostingClassifierExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (linearSupportVectorClassifierExecutor != null)
            {
                return linearSupportVectorClassifierExecutor.RecordTerminalAsync(runId, status, error);
            }

            if (kernelSupportVectorClassifierExecutor != null)
            {
                return kernelSupportVectorClassifierExecutor.RecordTerminalAsync(runId, status, error);
            }

            return executor!.RecordTerminalAsync(runId, status, error);
        }

        private async Task MonitorAsync(int runId, CancellationTokenSource userStop, CancellationToken token)
        {
            var context = contextFactory();

            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(settings.HeartbeatInterval, token).ConfigureAwait(false);

                    try
                    {
                        var status = await AdvancedAnalyticsTrainingRunRepository
                            .HeartbeatAsync(context, runId, clock(), CancellationToken.None)
                            .ConfigureAwait(false);

                        if (status != AdvancedAnalyticsStatus.Running)
                        {
                            await userStop.CancelAsync().ConfigureAwait(false);

                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Advanced Analytics Training: heartbeat for run {runId} failed as {ex}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                await context.DisposeAsync(token).ConfigureAwait(false);
            }
        }
    }
}