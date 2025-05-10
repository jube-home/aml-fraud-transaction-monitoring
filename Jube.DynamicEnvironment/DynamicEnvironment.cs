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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using log4net;

namespace Jube.DynamicEnvironment;

public class DynamicEnvironment
{
    private readonly Dictionary<string, string> appSettings;
    private readonly ILog log;

    public DynamicEnvironment(ILog log)
    {
        this.log = log;
        try
        {
            this.log.Info("Dynamic Variables:  The default and hardcoded values are going to be added.");

            appSettings = new Dictionary<string, string>
            {
                { "ModelSynchronisationWait", "10000" },
                { "EnableNotification", "True" },
                { "EnableTtlCounter", "True" },
                { "ConnectionString", null },
                { "CacheConnectionString", null },
                { "ReportConnectionString", null },
                { "EnableSearchKeyCache", "True" },
                { "EnableCasesAutomation", "True" },
                { "CasesAutomationWait", "60000" },
                { "EnableEntityModel", "True" },
                { "ArchiverPersistThreads", "1   " },
                { "ModelInvokeAsynchronousThreads", "1" },
                { "BulkCopyThreshold", "100" },
                { "ActivationWatcherBulkCopyThreshold", "100" },
                { "ReprocessingThreads", "1" },
                { "ThreadPoolManualControl", "False" },
                { "MinThreadPoolThreads", "30" },
                { "MaxThreadPoolThreads", "1000" },
                { "MaximumModelInvokeAsyncQueue", "10000" },
                { "SMTPHost", null },
                { "SMTPPort", "587" },
                { "SMTPUser", null },
                { "SMTPPassword", null },
                { "SMTPFrom", null },
                { "ClickatellAPIKey", null },
                { "HttpAdaptationUrl", "https://localhost:5001" },
                { "HttpAdaptationTimeout", "1000" },
                { "HttpAdaptationValidateSsl", "False" },
                { "ReprocessingBulkLimit", "10000" },
                { "EnableMigration", "True" },
                { "EnableSanction", "True" },
                { "NegotiateAuthentication", "False" },
                { "EnableExhaustiveTraining", "True" },
                { "EnableCacheIndex", "True" },
                { "EnableReprocessing", "True" },
                { "UseMockDataExhaustive", "True" },
                { "SanctionLoaderWait", "60000" },
                { "EnableSanctionLoader", "False" },
                { "ActivationWatcherAllowPersist", "True" },
                { "ActivationWatcherPersistThreads", "1" },
                { "AMQP", "False" },
                { "AMQPUri", null },
                { "JWTValidAudience", "http://localhost:5001" },
                { "JWTValidIssuer", "http://localhost:5001" },
                { "JWTKey", null },
                { "PasswordHashingKey", null },
                { "EnablePublicInvokeController", "True" },
                { "EnableEngine", "True" },
                { "ExhaustiveTrialsLimit", "1000" },
                { "ExhaustiveMinVariableCount", "5" },
                { "ExhaustiveMaxVariableCount", "30" },
                { "ExhaustiveTrainingDataSamplePercentage", "0.6" },
                { "ExhaustiveCrossValidationDataSamplePercentage", "0.2" },
                { "ExhaustiveTestingDataSamplePercentage", "0.2" },
                { "ExhaustiveValidationTestingActivationThreshold", "0.5" },
                { "ExhaustiveTopologySinceImprovementLimit", "10" },
                { "ExhaustiveLayerDepthLimit", "4" },
                { "ExhaustiveLayerWidthLimitInputLayerFactor", "4" },
                { "ExhaustiveTopologyComplexityLimit", "10000" },
                { "ExhaustiveActivationFunctionExplorationEpochs", "3" },
                { "ExhaustiveTopologyExplorationEpochs", "3" },
                { "ExhaustiveTopologyFinalisationEpochs", "20" },
                { "ExhaustiveSimulationsCount", "100" },
                { "EnableCallback", "True" },
                { "CallbackTimeout", "3000" },
                { "StreamingActivationWatcher", "True" },
                { "WaitPollFromActivationWatcherTable", "5000" },
                { "WaitTtlCounterDecrement", "60000" },
                { "Redis", "True" },
                { "RedisConnectionString", "localhost" },
                { "StoreFullPayloadLatest", "True" },
                { "CachePruneServer", "True" },
                { "WaitCachePrune", "10000" },
                { "EnableSandbox", "False" },
                { "JubeCache", "True" }
            };

            foreach (DictionaryEntry environmentVariable in Environment.GetEnvironmentVariables())
            {
                if (appSettings.ContainsKey(Convert.ToString(environmentVariable.Key) ?? string.Empty))
                {
                    if (Convert.ToString(environmentVariable.Value) ==
                        appSettings[environmentVariable.Key.ToString() ?? string.Empty])
                    {
                        log.Info("Jube Environment Synchronisation Start: Environment variable " +
                                 environmentVariable.Key +
                                 " enjoys the same value in Application Settings so has not been overwritten.");
                    }
                    else
                    {
                        appSettings[environmentVariable.Key.ToString() ?? string.Empty] =
                            Convert.ToString(environmentVariable.Value);

                        log.Info("Jube Environment Synchronisation Start: Environment variable " +
                                 environmentVariable.Key + " has been updated " +
                                 " in Application Settings so has been overwritten.");
                    }
                }
                else
                {
                    log.Info("Jube Environment Synchronisation Start: Environment variable " +
                             environmentVariable.Key + " is not a valid Application Setting.");
                }
            }

            ValidateConnectionString();
            ValidateJwtKey();
            ValidatePasswordHashingKey();
        }
        catch (Exception ex)
        {
            log.Error(
                "Jube Environment Synchronisation Start: Error in starting Jube Environment Synchronisation as " +
                ex + ".");
            throw;
        }
    }

    private void ValidateConnectionString()
    {
        if (string.IsNullOrEmpty(appSettings["ConnectionString"]))
        {
            throw new Exception("Missing ConnectionString in Environment Variables.");
        }
    }

    private void ValidatePasswordHashingKey()
    {
        if (string.IsNullOrEmpty(appSettings["PasswordHashingKey"]))
        {
            throw new Exception("Missing PasswordHashingKey in Environment Variables.");
        }
    }

    private void ValidateJwtKey()
    {
        if (string.IsNullOrEmpty(appSettings["JWTKey"]))
        {
            throw new Exception("Missing JWTKey in Environment Variables.");
        }
    }

    public string AppSettings(string[] keys)
    {
        return keys.Select(AppSettings).FirstOrDefault(value => value != null);
    }

    public string AppSettings(string key)
    {
        string value = null;
        try
        {
            if (appSettings.TryGetValue(key, out var setting)) value = setting;
        }
        catch (Exception ex)
        {
            log.Error("Jube Environment Variable: Error fetching variable with key " + key + " with error " + ex);
        }

        return value;
    }
}