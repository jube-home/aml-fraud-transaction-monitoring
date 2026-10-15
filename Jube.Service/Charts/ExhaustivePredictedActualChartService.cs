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
using Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual;
using Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceQuery;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Charts
{
    public sealed class ExhaustivePredictedActualChartService
    {
        private readonly ExhaustiveSearchInstancePromotedTrialInstancePredictedActualService inner;
        private readonly ExhaustiveSearchInstancePromotedTrialInstanceQueryService promoted;

        private ExhaustivePredictedActualChartService(
            ExhaustiveSearchInstancePromotedTrialInstancePredictedActualService inner,
            ExhaustiveSearchInstancePromotedTrialInstanceQueryService promoted)
        {
            this.inner = inner;
            this.promoted = promoted;
        }

        public static Task<ExhaustivePredictedActualChartService> CreateAsync(DbContext dbContext, string? userName,
            ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<ExhaustivePredictedActualChartService> CreateAsync(DbContext dbContext,
            string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
            CancellationToken token = default)
        {
            return new ExhaustivePredictedActualChartService(
                await ExhaustiveSearchInstancePromotedTrialInstancePredictedActualService.CreateAsync(dbContext,
                    userName, log,
                    stringLocalizerFactory, serviceChangeBus, auditLog, token).ConfigureAwait(false),
                await ExhaustiveSearchInstancePromotedTrialInstanceQueryService.CreateAsync(dbContext,
                    userName, log,
                    stringLocalizerFactory, serviceChangeBus, auditLog, token).ConfigureAwait(false));
        }

        public async Task<string> GetSvgAsync(int exhaustiveSearchInstanceId, CancellationToken token = default)
        {
            var rows = await inner.GetAsync(exhaustiveSearchInstanceId, token: token).ConfigureAwait(false);

            var calibration = ExhaustiveCalibrationRenderer.ToCalibration(ExhaustiveCalibrationRenderer.Active(
                await promoted.GetAsync(exhaustiveSearchInstanceId, token).ConfigureAwait(false)));

            return ExhaustiveChartRenderer.PredictedActual(
                ExhaustiveCalibrationRenderer.Calibrate(rows, calibration),
                id: "ex-pa-" + exhaustiveSearchInstanceId, calibrated: calibration.IsCalibrated);
        }
    }
}