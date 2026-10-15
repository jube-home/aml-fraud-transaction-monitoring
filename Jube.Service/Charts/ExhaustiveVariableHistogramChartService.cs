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
using Jube.Service.Query.ExhaustiveSearchInstanceVariable;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Charts
{
    public sealed class ExhaustiveVariableHistogramChartService
    {
        private readonly ExhaustiveSearchInstanceVariableService inner;

        private ExhaustiveVariableHistogramChartService(ExhaustiveSearchInstanceVariableService inner)
        {
            this.inner = inner;
        }

        public static Task<ExhaustiveVariableHistogramChartService> CreateAsync(DbContext dbContext, string? userName,
            ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<ExhaustiveVariableHistogramChartService> CreateAsync(DbContext dbContext,
            string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
            CancellationToken token = default)
        {
            return new ExhaustiveVariableHistogramChartService(await ExhaustiveSearchInstanceVariableService
                .CreateAsync(dbContext, userName, log,
                    stringLocalizerFactory, serviceChangeBus, auditLog, token).ConfigureAwait(false));
        }

        public async Task<string> GetSvgAsync(int exhaustiveSearchInstanceId, int exhaustiveSearchInstanceVariableId,
            CancellationToken token = default)
        {
            return ExhaustiveChartRenderer.VariableHistogram(
                (await inner.GetAsync(exhaustiveSearchInstanceId, token).ConfigureAwait(false)).FirstOrDefault(v =>
                    v.Id == exhaustiveSearchInstanceVariableId),
                id: "ex-vh-" + exhaustiveSearchInstanceId + "-" + exhaustiveSearchInstanceVariableId);
        }
    }
}