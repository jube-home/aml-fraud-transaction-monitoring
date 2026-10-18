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
using System.Threading;
using System.Threading.Tasks;
using Jube.Analytics.Categorical;
using Jube.Data.Context;
using Jube.Data.Reporting;
using Jube.Data.Repository;
using Jube.Engine.Exhaustive.Algorithms.Models;
using Jube.Engine.Exhaustive.Calibration;
using Jube.Engine.Exhaustive.Storage;
using Jube.Engine.Exhaustive.Utilities.QueryBuilder;
using Jube.Engine.Exhaustive.Utilities.QueryBuilder.Models;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Jube.Data.Query.GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery;

namespace Jube.Engine.Exhaustive.Utilities
{
    public static class Extraction
    {
        internal const int SampleLimit = 10000;

        public static async Task<Tuple<double[][], double[]>> GetClassDataAsync(DbContext dbContext,
            int entityAnalysisModelId,
            string filterSql,
            string filterTokens,
            Dictionary<int, Variable> variables,
            bool mockData,
            ILog log,
            string reportConnectionString = null,
            CancellationToken token = default)
        {
            using var postgres = new Postgres(reportConnectionString ?? dbContext.Connection.ConnectionString, log);

            var jsonList = await postgres.ExecuteReturnOnlyJsonFromArchiveSampleAsync(entityAnalysisModelId, filterSql,
                filterTokens, SampleLimit, mockData, token).ConfigureAwait(false);

            var dataList = BuildRows(variables, jsonList);
            var outputsList = new double[dataList.Length];
            Array.Fill(outputsList, 1d);

            return new Tuple<double[][], double[]>(dataList, outputsList);
        }

        public static async Task<IReadOnlyList<string>> GetClassJsonAsync(DbContext dbContext,
            int entityAnalysisModelId,
            string filterSql,
            string filterTokens,
            bool mockData,
            ILog log,
            string reportConnectionString = null,
            CancellationToken token = default)
        {
            using var postgres = new Postgres(reportConnectionString ?? dbContext.Connection.ConnectionString, log);

            var jsonList = await postgres.ExecuteReturnOnlyJsonFromArchiveSampleAsync(entityAnalysisModelId, filterSql,
                filterTokens, SampleLimit, mockData, token).ConfigureAwait(false);

            return jsonList as IReadOnlyList<string> ?? [.. jsonList];
        }

        public static async Task<Tuple<Dictionary<int, Variable>, double[][]>> GetSampleDataAsync(DbContext dbContext,
            int tenantRegistryId,
            int entityAnalysisModelId,
            bool mockData, CancellationToken token = default,
            CategoricalOptions categoricalOptions = null,
            Action<CategoricalPlan> onCategoricalPlan = null,
            IReadOnlyList<string> categoricalFitJson = null)
        {
            if (mockData)
            {
                var mockArchiveRepository = new MockArchiveRepository(dbContext);

                var jsonList =
                    await mockArchiveRepository
                        .GetJsonByEntityAnalysisModelIdRandomLimitAsync(entityAnalysisModelId, SampleLimit, token)
                        .ConfigureAwait(false);

                return await ProcessJsonAsync(dbContext, tenantRegistryId, entityAnalysisModelId, true, jsonList, token,
                    categoricalOptions, onCategoricalPlan, categoricalFitJson).ConfigureAwait(false);
            }
            else
            {
                var archiveRepository = new ArchiveRepository(dbContext);

                var jsonList =
                    await archiveRepository
                        .GetJsonByEntityAnalysisModelIdRandomLimitAsync(entityAnalysisModelId, SampleLimit, token)
                        .ConfigureAwait(false);

                return await ProcessJsonAsync(dbContext, tenantRegistryId, entityAnalysisModelId, false, jsonList,
                    token,
                    categoricalOptions, onCategoricalPlan, categoricalFitJson).ConfigureAwait(false);
            }
        }

        public static async Task<Tuple<Dictionary<int, Variable>, double[][]>> GetSampleDataAsync(DbContext dbContext,
            int tenantRegistryId,
            int entityAnalysisModelId,
            string filterJson,
            string filterTokens,
            bool mockData,
            ILog log,
            string reportConnectionString = null,
            CancellationToken token = default,
            CategoricalOptions categoricalOptions = null,
            Action<CategoricalPlan> onCategoricalPlan = null,
            IReadOnlyList<string> categoricalFitJson = null)
        {
            var filterJsonRule = JsonConvert.DeserializeObject<Rule>(filterJson);

            var filterRule = await ParserJsonToSqlEntityAnalysisModel
                .CreateAsync(filterJsonRule, dbContext, tenantRegistryId, entityAnalysisModelId, token)
                .ConfigureAwait(false);

            using var postgres = new Postgres(reportConnectionString ?? dbContext.Connection.ConnectionString, log);

            var jsonList = await postgres.ExecuteReturnOnlyJsonFromArchiveSampleAsync(entityAnalysisModelId,
                "NOT (" + filterRule.Sql + ")",
                filterTokens, SampleLimit, mockData, token).ConfigureAwait(false);

            return await ProcessJsonAsync(dbContext, tenantRegistryId, entityAnalysisModelId, mockData, jsonList, token,
                categoricalOptions, onCategoricalPlan, categoricalFitJson).ConfigureAwait(false);
        }

        internal static async Task<BaseRateResolution> ResolveArchiveBaseRateAsync(DbContext dbContext,
            int tenantRegistryId, int entityAnalysisModelId, string filterJson, string filterSql, string filterTokens,
            bool mockData, ILog log, string reportConnectionString = null,
            CancellationToken token = default)
        {
            try
            {
                var filterJsonRule = JsonConvert.DeserializeObject<Rule>(filterJson);

                var filterRule = await ParserJsonToSqlEntityAnalysisModel.CreateAsync(filterJsonRule, dbContext,
                    tenantRegistryId, entityAnalysisModelId, token).ConfigureAwait(false);

                var source = mockData
                    ? SampleSource.Mock(entityAnalysisModelId)
                    : SampleSource.Archive(entityAnalysisModelId);

                using var postgres = new Postgres(reportConnectionString ?? dbContext.Connection.ConnectionString, log);

                var positives = await postgres.ExecuteCountFromArchiveAsync(source, filterSql, filterTokens, token)
                    .ConfigureAwait(false);

                var negatives = await postgres.ExecuteCountFromArchiveAsync(source, "NOT (" + filterRule.Sql + ")",
                    filterTokens, token).ConfigureAwait(false);

                return BaseRateResolution.FromCounts(positives, negatives);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error(
                    $"Exhaustive Training: The population base rate for model {entityAnalysisModelId} could not be counted as {ex}. The probability will stay at the sample prior unless the instance sets a base rate.");

                return BaseRateResolution.Unavailable;
            }
        }

        internal static async Task<BaseRateResolution> ResolveDatasetBaseRateAsync(DbContext dbContext,
            DatasetTrainingPlan plan, ILog log, string reportConnectionString = null,
            CancellationToken token = default)
        {
            try
            {
                using var postgres = new Postgres(reportConnectionString ?? dbContext.Connection.ConnectionString, log);

                var positives = await postgres.ExecuteCountFromArchiveAsync(plan.Source, plan.PositiveSql,
                    plan.PositiveSqlTokens, token).ConfigureAwait(false);

                var negatives = await postgres.ExecuteCountFromArchiveAsync(plan.Source,
                    "NOT " + plan.PositiveSql, plan.PositiveSqlTokens, token).ConfigureAwait(false);

                return BaseRateResolution.FromCounts(positives, negatives);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error(
                    $"Exhaustive Training: The dataset base rate could not be counted as {ex}. The probability will stay at the sample prior unless the instance sets a base rate.");

                return BaseRateResolution.Unavailable;
            }
        }

        internal static async Task<Tuple<Dictionary<int, Variable>, double[][]>> GetDatasetSampleDataAsync(
            DbContext dbContext, DatasetTrainingPlan plan, bool wholeSample, ILog log,
            string reportConnectionString = null, CancellationToken token = default)
        {
            using var postgres = new Postgres(reportConnectionString ?? dbContext.Connection.ConnectionString, log);

            var jsonList = await postgres.ExecuteReturnOnlyJsonFromArchiveSampleAsync(plan.Source,
                wholeSample ? "true" : "NOT " + plan.PositiveSql,
                wholeSample ? "[]" : plan.PositiveSqlTokens, SampleLimit, token).ConfigureAwait(false);

            return new Tuple<Dictionary<int, Variable>, double[][]>(plan.Variables,
                BuildRows(plan.Variables, jsonList));
        }

        internal static async Task<Tuple<double[][], double[]>> GetDatasetClassDataAsync(DbContext dbContext,
            DatasetTrainingPlan plan, ILog log, string reportConnectionString = null,
            CancellationToken token = default)
        {
            using var postgres = new Postgres(reportConnectionString ?? dbContext.Connection.ConnectionString, log);

            var jsonList = await postgres.ExecuteReturnOnlyJsonFromArchiveSampleAsync(plan.Source,
                plan.PositiveSql, plan.PositiveSqlTokens, SampleLimit, token).ConfigureAwait(false);

            var dataList = BuildRows(plan.Variables, jsonList);
            var outputsList = new double[dataList.Length];
            Array.Fill(outputsList, 1d);

            return new Tuple<double[][], double[]>(dataList, outputsList);
        }

        internal static async Task<Tuple<Dictionary<int, Variable>, double[][]>> ProcessJsonAsync(DbContext dbContext,
            int tenantRegistryId, int entityAnalysisModelId,
            bool mockData, IEnumerable<string> jsonList, CancellationToken token = default,
            CategoricalOptions categoricalOptions = null, Action<CategoricalPlan> onCategoricalPlan = null,
            IReadOnlyList<string> categoricalFitJson = null)
        {
            Dictionary<int, Variable> variables;
            var rows = jsonList as IReadOnlyList<string> ?? [.. jsonList];

            if (!mockData)
            {
                var getModelFieldByEntityAnalysisModelIdParseTypeIdQuery =
                    new GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery(dbContext, tenantRegistryId);

                var fields = (await getModelFieldByEntityAnalysisModelIdParseTypeIdQuery
                    .ExecuteAsync(entityAnalysisModelId, 5, true, token).ConfigureAwait(false)).ToList();

                variables = BuildVariablesFromFields(fields);

                if (categoricalOptions is { Enabled: true })
                {
                    variables = CategoricalExpansion.Expand(fields, variables, rows, categoricalFitJson,
                        categoricalOptions,
                        out var plan);

                    onCategoricalPlan?.Invoke(plan);
                }
            }
            else
            {
                variables = BuildMockVariables();
            }

            return new Tuple<Dictionary<int, Variable>, double[][]>(variables, BuildRows(variables, rows));
        }

        internal static Dictionary<int, Variable> BuildVariablesFromFields(
            IEnumerable<GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQueryDto> fields)
        {
            var variables = new Dictionary<int, Variable>();
            var headerSequence = 0;

            foreach (var variable in from field in fields
                     where field.ProcessingTypeId is 3 or 5 or 7
                     select new Variable
                     {
                         Name = field.Name,
                         ProcessingTypeId = field.ProcessingTypeId,
                         ValueJsonPath = field.ValueJsonPath
                     })
            {
                variables.Add(headerSequence, variable);
                headerSequence += 1;
            }

            return variables;
        }

        internal static Dictionary<int, Variable> BuildMockVariables()
        {
            var variables = new Dictionary<int, Variable>();
            var headerSequence = 0;

            variables.Add(headerSequence, new Variable
            {
                Name = "Abstraction.IsChip",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.IsChip"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.IsSwipe",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.IsSwipe"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.IsManual",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.IsManual"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.CountTransactions1Day",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.CountTransactions1Day"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.Authenticated",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.Authenticated"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.CountTransactionsPINDecline1Day",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.CountTransactionsPINDecline1Day"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.CountTransactionsDeclined1Day",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.CountTransactionsDeclined1Day"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.CountUnsafeTerminals1Day",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.CountUnsafeTerminals1Day"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.CountInPerson1Day",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.CountInPerson1Day"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.CountInternet1Day",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.CountInternet1Day"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.ATM",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.ATM"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.CountATM1Day",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.CountATM1Day"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.CountOver30SEK1Day",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.CountOver30SEK1Day"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.InPerson",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.InPerson"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.TransactionAmt",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.TransactionAmt"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.SumTransactions1Day",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.SumTransactions1Day"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.SumATMTransactions1Day",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.SumATMTransactions1Day"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.Foreign",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.Foreign"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.DifferentCountryTransactions1Week",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.DifferentCountryTransactions1Week"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.DifferentMerchantTypes1Week",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.DifferentMerchantTypes1Week"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.DifferentDeclineReasons1Day",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.DifferentDeclineReasons1Day"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.DifferentCities1Week",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.DifferentCities1Week"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.CountSameMerchantUsedBefore1Week",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.CountSameMerchantUsedBefore1Week"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.HasBeenAbroad",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.HasBeenAbroad"
            });

            variables.Add(headerSequence += 1, new Variable
            {
                Name = "Abstraction.CashTransaction",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.CashTransaction"
            });

            variables.Add(headerSequence + 1, new Variable
            {
                Name = "Abstraction.HighRiskCountry",
                ProcessingTypeId = 5,
                ValueJsonPath = "$.abstraction.HighRiskCountry"
            });

            return variables;
        }

        internal static double[][] BuildRows(Dictionary<int, Variable> variables, IEnumerable<string> jsonList)
        {
            var rows = new List<double[]>();
            Dictionary<CategoricalFieldEncoder, int> slots = null;

            foreach (var json in jsonList)
            {
                var jObject = JObject.Parse(json);

                var row = new double[variables.Count];
                slots?.Clear();

                for (var i = 0; i < row.Length; i++)
                {
                    var encoder = variables[i].CategoricalEncoder;

                    if (encoder == null)
                    {
                        row[i] = ExtractValue(jObject, variables[i].ValueJsonPath);
                        continue;
                    }

                    slots ??= new Dictionary<CategoricalFieldEncoder, int>();

                    if (!slots.TryGetValue(encoder, out var slot))
                    {
                        slot = encoder.Resolve(CategoricalValue.FromToken(
                            CategoricalExpansion.SelectToken(jObject, variables[i].ValueJsonPath)));

                        slots[encoder] = slot;
                    }

                    row[i] = slot == variables[i].CategoricalIndicatorIndex ? 1d : 0d;
                }

                rows.Add(row);
            }

            return [.. rows];
        }

        internal static double ExtractValue(JObject jObject, string valueJsonPath)
        {
            double value = 0;

            try
            {
                var jToken = jObject.SelectToken(valueJsonPath);

                if (jToken != null)
                {
                    value = jToken.Value<double>();
                }
            }
            catch
            {
                value = 0;
            }

            return value;
        }
    }
}