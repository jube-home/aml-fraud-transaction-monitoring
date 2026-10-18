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

using Jube.Data.Context;
using Jube.Dto.Query.ExhaustiveSearchInstanceVariableStatistics;
using Jube.Service.Charts.Models;
using Jube.Service.Query.ExhaustiveSearchInstanceVariableStatistics;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Charts
{
    public sealed class ExhaustiveStatisticsChartService
    {
        public const string ClassesView = "classes";
        public const string MonteCarloView = "montecarlo";
        public const string SummaryView = "summary";
        private readonly ExhaustiveSearchInstanceVariableStatisticsService inner;

        private ExhaustiveStatisticsChartService(ExhaustiveSearchInstanceVariableStatisticsService inner)
        {
            this.inner = inner;
        }

        public static Task<ExhaustiveStatisticsChartService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<ExhaustiveStatisticsChartService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            return new ExhaustiveStatisticsChartService(await ExhaustiveSearchInstanceVariableStatisticsService
                .CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, auditLog, token)
                .ConfigureAwait(false));
        }

        public async Task<string> GetVariableClassesSvgAsync(int exhaustiveSearchInstanceId,
            int exhaustiveSearchInstanceVariableId, bool counts, bool large, CancellationToken token = default)
        {
            return ExhaustiveStatisticsChartRenderer.VariableClasses(
                await GetOneAsync(exhaustiveSearchInstanceId, exhaustiveSearchInstanceVariableId, token)
                    .ConfigureAwait(false), counts, large,
                id: Id("vc", exhaustiveSearchInstanceId, exhaustiveSearchInstanceVariableId, large));
        }

        public async Task<string> GetMonteCarloSvgAsync(int exhaustiveSearchInstanceId,
            int exhaustiveSearchInstanceVariableId, bool counts, bool large, CancellationToken token = default)
        {
            return ExhaustiveStatisticsChartRenderer.MonteCarlo(
                await GetOneAsync(exhaustiveSearchInstanceId, exhaustiveSearchInstanceVariableId, token)
                    .ConfigureAwait(false), counts, large,
                id: Id("mc", exhaustiveSearchInstanceId, exhaustiveSearchInstanceVariableId, large));
        }

        public async Task<string> GetMonteCarloSummarySvgAsync(int exhaustiveSearchInstanceId,
            int exhaustiveSearchInstanceVariableId, bool large, CancellationToken token = default)
        {
            return ExhaustiveStatisticsChartRenderer.MonteCarloSummary(
                await GetOneAsync(exhaustiveSearchInstanceId, exhaustiveSearchInstanceVariableId, token)
                    .ConfigureAwait(false), large,
                id: Id("ms", exhaustiveSearchInstanceId, exhaustiveSearchInstanceVariableId, large));
        }

        public async Task<List<ExhaustiveStatisticsChartGridItem>> GetGridAsync(int exhaustiveSearchInstanceId,
            string view, bool counts, CancellationToken token = default)
        {
            var variables = await inner.GetAsync(exhaustiveSearchInstanceId, null, token).ConfigureAwait(false);

            return variables.Select(variable => new ExhaustiveStatisticsChartGridItem
            {
                VariableId = variable.Id,
                Svg = view switch
                {
                    MonteCarloView => ExhaustiveStatisticsChartRenderer.MonteCarlo(variable, counts,
                        id: Id("mc", exhaustiveSearchInstanceId, variable.Id, false)),
                    SummaryView => ExhaustiveStatisticsChartRenderer.MonteCarloSummary(variable,
                        id: Id("ms", exhaustiveSearchInstanceId, variable.Id, false)),
                    _ => ExhaustiveStatisticsChartRenderer.VariableClasses(variable, counts,
                        id: Id("vc", exhaustiveSearchInstanceId, variable.Id, false))
                }
            }).ToList();
        }

        private async Task<ExhaustiveSearchInstanceVariableStatisticsDto?> GetOneAsync(int exhaustiveSearchInstanceId,
            int exhaustiveSearchInstanceVariableId, CancellationToken token)
        {
            return (await inner.GetAsync(exhaustiveSearchInstanceId, exhaustiveSearchInstanceVariableId, token)
                .ConfigureAwait(false)).FirstOrDefault();
        }

        private static string Id(string kind, int exhaustiveSearchInstanceId, int variableId, bool large)
        {
            return "ex-" + kind + (large ? "l-" : "-") + exhaustiveSearchInstanceId + "-" + variableId;
        }
    }
}