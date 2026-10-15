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
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription;
using Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Charts
{
    public sealed class ExhaustivePrescriptionHistogramChartService
    {
        private readonly ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionService inner;

        private ExhaustivePrescriptionHistogramChartService(
            ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionService inner)
        {
            this.inner = inner;
        }

        public static Task<ExhaustivePrescriptionHistogramChartService> CreateAsync(DbContext dbContext,
            string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<ExhaustivePrescriptionHistogramChartService> CreateAsync(DbContext dbContext,
            string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
            CancellationToken token = default)
        {
            return new ExhaustivePrescriptionHistogramChartService(
                await ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionService.CreateAsync(dbContext,
                    userName, log, stringLocalizerFactory, serviceChangeBus, auditLog, token).ConfigureAwait(false));
        }

        public async Task<string> GetSvgAsync(int exhaustiveSearchInstancePromotedTrialInstanceId,
            int promotedTrialInstanceVariableId, CancellationToken token = default)
        {
            var (positive, negative) = await ResolveClassesAsync(exhaustiveSearchInstancePromotedTrialInstanceId,
                promotedTrialInstanceVariableId, token).ConfigureAwait(false);

            return ExhaustiveChartRenderer.PrescriptionHistogramByClass(positive, negative,
                id: "ex-ph-" + exhaustiveSearchInstancePromotedTrialInstanceId + "-" + promotedTrialInstanceVariableId);
        }

        public async Task<string> GetSummarySvgAsync(int exhaustiveSearchInstancePromotedTrialInstanceId,
            int promotedTrialInstanceVariableId, CancellationToken token = default)
        {
            var (positive, negative) = await ResolveClassesAsync(exhaustiveSearchInstancePromotedTrialInstanceId,
                promotedTrialInstanceVariableId, token).ConfigureAwait(false);

            return ExhaustiveChartRenderer.PrescriptionSummaryByClass(positive, negative,
                id: "ex-ps-" + exhaustiveSearchInstancePromotedTrialInstanceId + "-" + promotedTrialInstanceVariableId);
        }

        private async
            Task<(ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionDto? Positive,
                ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionDto? Negative)> ResolveClassesAsync(
                int exhaustiveSearchInstancePromotedTrialInstanceId, int promotedTrialInstanceVariableId,
                CancellationToken token)
        {
            var variables = (await inner.GetAsync(exhaustiveSearchInstancePromotedTrialInstanceId, token)
                .ConfigureAwait(false)).ToList();

            var selected = variables.FirstOrDefault(v =>
                v.PromotedTrialInstanceVariableId == promotedTrialInstanceVariableId);

            var positive = selected?.Positive == false
                ? variables.FirstOrDefault(v => v.Id == selected.Id && v.Positive == true)
                : selected;

            var negative = selected?.Positive == true
                ? variables.FirstOrDefault(v => v.Id == selected.Id && v.Positive == false)
                : selected?.Positive == false
                    ? selected
                    : null;

            return (positive, negative);
        }
    }
}