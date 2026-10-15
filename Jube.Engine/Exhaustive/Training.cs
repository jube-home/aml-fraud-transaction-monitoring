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
using System.Threading;
using System.Threading.Tasks;
using Accord.MachineLearning.VectorMachines;
using Accord.Statistics.Kernels;
using Jube.Data.Context;
using Jube.Data.Repository.Interfaces;
using Jube.Engine.Exhaustive.Algorithms.Models;
using Jube.Engine.Exhaustive.Anomaly;
using Jube.Engine.Exhaustive.Preparation;
using Jube.Engine.Exhaustive.Stages;
using Jube.Engine.Exhaustive.Stages.Models;
using Jube.Engine.Exhaustive.Statistics;
using Jube.Engine.Helpers;
using log4net;
using Jube.Data.Query.GetNextExhaustiveSearchInstanceQuery;

namespace Jube.Engine.Exhaustive
{
    public class Training(
        ILog log,
        DynamicEnvironment.DynamicEnvironment environment,
        JsonSerializationHelper jsonSerializationHelper)
    {
        internal Func<DbContext, CancellationToken, Task<GetNextExhaustiveSearchInstanceQueryDto>> fetchNext =
            (context, cancellationToken) =>
                new GetNextExhaustiveSearchInstanceQuery(context).ExecuteAsync(cancellationToken);

        internal TimeSpan pollDelay = TimeSpan.FromMilliseconds(60000);
        internal Random seeded = new(Environment.TickCount ^ Guid.NewGuid().GetHashCode());

        public async Task StartAsync(CancellationToken token = default)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var dbContext =
                        DataConnectionDbContext.GetResilientDbContextDataConnection(
                            environment.AppSettings("ConnectionString"), log);

                    if (log.IsInfoEnabled)
                    {
                        log.Info("Exhaustive Training: Opening a database connection.");
                    }

                    try
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                "Exhaustive Training: Opened a database connection. " +
                                "Will proceed to lookup a request for an Exhaustive Instance,  " +
                                "or in the absence of an Exhaustive Instance to create one.");
                        }

                        var exhaustiveSearchInstance = await fetchNext(dbContext, token).ConfigureAwait(false);

                        if (exhaustiveSearchInstance != null)
                        {
                            if (!await RunOneInstanceAsync(exhaustiveSearchInstance, dbContext, token)
                                    .ConfigureAwait(false))
                            {
                                if (log.IsInfoEnabled)
                                {
                                    log.Info(
                                        $"Exhaustive Training: ExhaustiveSearchInstanceId:{exhaustiveSearchInstance.Id} has exited in an uncompleted state.");
                                }
                                else
                                {
                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info(
                                            $"Exhaustive Training: ExhaustiveSearchInstanceId:{exhaustiveSearchInstance.Id} has exited in a completed state.");
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (log.IsInfoEnabled)
                            {
                                log.Info(
                                    "Exhaustive Training: Has not found anything that needs training.");
                            }
                        }

                        if (log.IsInfoEnabled)
                        {
                            log.Info("Exhaustive Training: Finished and closing database context.  Waiting.");
                        }

                        try
                        {
                            await dbContext.CloseAsync().ConfigureAwait(false);
                        }
                        finally
                        {
                            await dbContext.DisposeAsync().ConfigureAwait(false);
                        }

                        await Task.Delay(pollDelay, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        try
                        {
                            await dbContext.CloseAsync().ConfigureAwait(false);
                        }
                        finally
                        {
                            await dbContext.DisposeAsync().ConfigureAwait(false);
                        }

                        throw;
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            await dbContext.CloseAsync().ConfigureAwait(false);
                        }
                        finally
                        {
                            await dbContext.DisposeAsync().ConfigureAwait(false);
                        }

                        log.Error(
                            $"Exhaustive Training: Has experienced an error as {ex}  waiting to poll again.  Closing database context.");

                        await Task.Delay(pollDelay, token).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                log.Info($"Graceful Cancellation StartAsync: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                log.Error($"StartAsync: has produced an error {ex}");
            }
        }

        internal async Task<bool> RunOneInstanceAsync(GetNextExhaustiveSearchInstanceQueryDto exhaustiveSearchInstance,
            DbContext dbContext, CancellationToken token = default)
        {
            var run = new InstanceRun(exhaustiveSearchInstance, dbContext, token);

            if (!await new SampleStage(log, environment, jsonSerializationHelper).ExecuteAsync(run)
                    .ConfigureAwait(false))
            {
                return false;
            }

            if (!await new BaseStatisticsStage(log).ExecuteAsync(run).ConfigureAwait(false))
            {
                return false;
            }

            if (!await new AnomalyDetectionStage(log, environment).ExecuteAsync(run).ConfigureAwait(false))
            {
                return false;
            }

            if (!await new FilterStage(log, environment).ExecuteAsync(run).ConfigureAwait(false))
            {
                return false;
            }

            if (!await new ClassHistogramStage(log).ExecuteAsync(run).ConfigureAwait(false))
            {
                return false;
            }

            if (!await new PrepareStage(log, seeded).ExecuteAsync(run).ConfigureAwait(false))
            {
                return false;
            }

            var repositoryExhaustiveSearchInstance = run.RepositoryInstance;

            if (run.ClassCount > 0)
            {
                if (!await new BootstrapStage(log, seeded).ExecuteAsync(run).ConfigureAwait(false))
                {
                    return false;
                }

                if (!await new StoreStage(log).ExecuteAsync(run).ConfigureAwait(false))
                {
                    return false;
                }

                if (!await new CorrelationStage(log).ExecuteAsync(run).ConfigureAwait(false))
                {
                    return false;
                }

                if (!await new MulticollinearityStage(log).ExecuteAsync(run).ConfigureAwait(false))
                {
                    return false;
                }

                if (!await new SupervisedStage(log, environment, jsonSerializationHelper, seeded).ExecuteAsync(run)
                        .ConfigureAwait(false))
                {
                    return false;
                }
            }
            else
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Updated status to 17 for {exhaustiveSearchInstance.Id} " +
                        $"which denotes a zero class count.");
                }

                await repositoryExhaustiveSearchInstance.UpdateStatusAsync(exhaustiveSearchInstance.Id, 17, token)
                    .ConfigureAwait(false);
            }

            await repositoryExhaustiveSearchInstance.UpdateCompletedAsync(exhaustiveSearchInstance.Id, token)
                .ConfigureAwait(false);

            return true;
        }

        internal void DatasetSymmetry(double[][] inData, double[] inOutputs, out double[][] outData,
            out double[] outOutputs)
        {
            new DatasetBalancer(log, seeded).DatasetSymmetry(inData, inOutputs, out outData, out outOutputs);
        }

        internal void DatasetSymmetry(double[][] inData, double[] inOutputs, int targetPerClass,
            out double[][] outData, out double[] outOutputs)
        {
            new DatasetBalancer(log, seeded).DatasetSymmetry(inData, inOutputs, targetPerClass, out outData,
                out outOutputs);
        }

        internal double[] GetClassVariableByAnomaly(SupportVectorMachine<Gaussian> model,
            double[][] data,
            double probability)
        {
            return new AnomalyClassifier(log).GetClassVariableByAnomaly(model, data, probability);
        }

        internal double[][] NormaliseData(Dictionary<int, Variable> variables, double[][] data)
        {
            return new Normalisation(log).NormaliseData(variables, data);
        }

        internal static double[][] OnlyOutputWithClassificationForData(int classCount, double[] outputs,
            double[][] data)
        {
            return DatasetShaper.OnlyOutputWithClassificationForData(classCount, outputs, data);
        }

        internal static void Shuffle(Random r, double[][] existingData,
            double[] existingOutputs,
            out double[][] newData,
            out double[] outputs)
        {
            DatasetShaper.Shuffle(r, existingData, existingOutputs, out newData, out outputs);
        }

        internal Task CalculateStatisticsAsync(int exhaustiveSearchInstanceId, Dictionary<int, Variable> variables,
            double[][] data,
            IGenericRepository repositoryVariables,
            IGenericRepository repositoryHistogram,
            CancellationToken token
        )
        {
            return new StatisticsCalculator(log).CalculateStatisticsAsync(exhaustiveSearchInstanceId, variables, data,
                repositoryVariables, repositoryHistogram, token);
        }

        internal Task CalculateStatisticsAsync(int exhaustiveSearchInstanceId, Dictionary<int, Variable> variables,
            double[][] data,
            StatisticsTarget target,
            IGenericRepository repositoryVariables,
            IGenericRepository repositoryHistogram,
            CancellationToken token
        )
        {
            return new StatisticsCalculator(log).CalculateStatisticsAsync(exhaustiveSearchInstanceId, variables, data,
                target, repositoryVariables, repositoryHistogram, token);
        }
    }
}