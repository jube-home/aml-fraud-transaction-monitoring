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

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jube.Analytics.Categorical;
using Jube.Analytics.Datasets;
using Jube.Analytics.Datasets.Models;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Reporting;
using Jube.Data.Repository;
using Jube.Engine.Exhaustive.Algorithms.Models;

namespace Jube.Engine.Exhaustive.Storage
{
    internal sealed class DatasetTrainingPlan
    {
        internal SampleSource Source { get; init; }
        internal Dictionary<int, Variable> Variables { get; init; }
        internal string PositiveSql { get; init; }
        internal string PositiveSqlTokens { get; init; } = "[]";

        internal static async Task<DatasetTrainingPlan> LoadAsync(DbContext dbContext, int tenantRegistryId,
            int? csvId, int? jsonId, string filterSql, string filterTokens, CancellationToken token = default)
        {
            if (csvId.HasValue == jsonId.HasValue)
            {
                return null;
            }

            var reference = csvId.HasValue
                ? new DatasetReference(DatasetKind.Csv, csvId.Value)
                : new DatasetReference(DatasetKind.Json, jsonId.Value);

            var dataset = await new ExhaustiveInstanceDatasetRepository(dbContext, tenantRegistryId)
                .GetConfirmedAsync(reference, token).ConfigureAwait(false);

            if (dataset == null || string.IsNullOrWhiteSpace(dataset.ColumnsJson))
            {
                return null;
            }

            var columns = JsonSerializer.Deserialize<List<DatasetColumnDefinition>>(dataset.ColumnsJson);

            if (columns == null)
            {
                return null;
            }

            var variables = new Dictionary<int, Variable>();
            var sequence = 0;

            foreach (var column in columns
                         .Where(w => w.Include && !IsReferencedByFilter(w, filterSql))
                         .OrderBy(o => o.Index))
            {
                if (column.IsCategorical && !string.IsNullOrWhiteSpace(column.CategoricalEncoderJson))
                {
                    var encoder = CategoricalFieldEncoder.FromJson(column.CategoricalEncoderJson);
                    var path = DatasetRowJson.ValueJsonPath(column.Index);

                    for (var i = 0; i < encoder.Indicators.Count; i++)
                    {
                        variables.Add(sequence++, new Variable
                        {
                            Name = DatasetProcessingTypes.Prefix(column.ProcessingTypeId) + "." +
                                   encoder.Indicators[i].Name,
                            ProcessingTypeId = (byte)column.ProcessingTypeId,
                            ValueJsonPath = path,
                            CategoricalEncoder = encoder,
                            CategoricalIndicatorIndex = i
                        });
                    }

                    continue;
                }

                variables.Add(sequence++, new Variable
                {
                    Name = DatasetProcessingTypes.Prefix(column.ProcessingTypeId) + "." + column.Name,
                    ProcessingTypeId = (byte)column.ProcessingTypeId,
                    ValueJsonPath = DatasetRowJson.ValueJsonPath(column.Index)
                });
            }

            if (variables.Count == 0)
            {
                return null;
            }

            return new DatasetTrainingPlan
            {
                Source = SampleSource.Dataset(reference),
                Variables = variables,
                PositiveSql = string.IsNullOrWhiteSpace(filterSql) ? null : filterSql,
                PositiveSqlTokens = string.IsNullOrWhiteSpace(filterTokens) ? "[]" : filterTokens
            };
        }

        private static bool IsReferencedByFilter(DatasetColumnDefinition column, string filterSql)
        {
            return !string.IsNullOrWhiteSpace(filterSql) && filterSql.Contains("'" + column.StorageKey + "'");
        }
    }
}