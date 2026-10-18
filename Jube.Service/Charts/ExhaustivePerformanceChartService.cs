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

using Jube.Charting.Core.Models;
using Jube.Data.Context;
using Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual;
using Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceQuery;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Charts
{
    public enum ExhaustivePerformanceChart
    {
        ScoreDistribution,
        Roc,
        PrecisionRecall,
        Calibration,
        Gains,
        ValueCaptured,
        Lift,
        Ks,
        ThresholdSweep,
        Confusion,
        Measures
    }

    public sealed class ExhaustivePerformanceChartService
    {
        private readonly ExhaustiveSearchInstancePromotedTrialInstancePredictedActualService inner;
        private readonly ExhaustiveSearchInstancePromotedTrialInstanceQueryService promoted;

        private ExhaustivePerformanceChartService(
            ExhaustiveSearchInstancePromotedTrialInstancePredictedActualService inner,
            ExhaustiveSearchInstancePromotedTrialInstanceQueryService promoted)
        {
            this.inner = inner;
            this.promoted = promoted;
        }

        public static Task<ExhaustivePerformanceChartService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<ExhaustivePerformanceChartService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            return new ExhaustivePerformanceChartService(
                await ExhaustiveSearchInstancePromotedTrialInstancePredictedActualService
                    .CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, auditLog, token)
                    .ConfigureAwait(false),
                await ExhaustiveSearchInstancePromotedTrialInstanceQueryService
                    .CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, auditLog, token)
                    .ConfigureAwait(false));
        }

        public async Task<string> GetSvgAsync(int exhaustiveSearchInstanceId, ExhaustivePerformanceChart chart,
            double? threshold = null, double? falseNegativeCost = null, double? falsePositiveCost = null,
            int? exhaustiveSearchInstanceVariableId = null, string? variableName = null,
            CancellationToken token = default)
        {
            var rawRows = await inner.GetAsync(exhaustiveSearchInstanceId, exhaustiveSearchInstanceVariableId, token)
                .ConfigureAwait(false);

            var active = ExhaustiveCalibrationRenderer.Active(
                await promoted.GetAsync(exhaustiveSearchInstanceId, token).ConfigureAwait(false));

            var calibration = ExhaustiveCalibrationRenderer.ToCalibration(active);
            var rows = ExhaustiveCalibrationRenderer.Calibrate(rawRows, calibration);
            var t = ExhaustivePerformanceRenderer.SafeThreshold(threshold);
            var id = "ex-" + Prefix(chart) + "-" + exhaustiveSearchInstanceId;

            return chart switch
            {
                ExhaustivePerformanceChart.ScoreDistribution =>
                    ExhaustivePerformanceRenderer.ScoreDistribution(rows, t, id: id, variableName: variableName),
                ExhaustivePerformanceChart.Roc => ExhaustivePerformanceRenderer.Roc(rows, id: id,
                    variableName: variableName),
                ExhaustivePerformanceChart.PrecisionRecall =>
                    ExhaustivePerformanceRenderer.PrecisionRecall(rows, id: id),
                ExhaustivePerformanceChart.Calibration => calibration.IsCalibrated
                    ? ExhaustiveCalibrationRenderer.Comparison(rawRows, active, id: id)
                    : ExhaustivePerformanceRenderer.Calibration(rows, id: id,
                        note: active is null ? null : ExhaustiveCalibrationRenderer.Caption(active)),
                ExhaustivePerformanceChart.Gains => ExhaustivePerformanceRenderer.Gains(rows, id: id,
                    variableName: variableName),
                ExhaustivePerformanceChart.ValueCaptured => ExhaustivePerformanceRenderer.ValueCaptured(rows, id: id,
                    variableName: variableName),
                ExhaustivePerformanceChart.Lift => ExhaustivePerformanceRenderer.Lift(rows, id: id,
                    variableName: variableName),
                ExhaustivePerformanceChart.Ks => ExhaustivePerformanceRenderer.Ks(rows, t, id: id,
                    variableName: variableName),
                ExhaustivePerformanceChart.ThresholdSweep => ExhaustivePerformanceRenderer.ThresholdSweep(rows, t,
                    falseNegativeCost, falsePositiveCost, id: id),
                ExhaustivePerformanceChart.Confusion => ExhaustivePerformanceRenderer.Confusion(rows, t, id: id),
                ExhaustivePerformanceChart.Measures => ExhaustivePerformanceRenderer.MeasuresPanel(rows, t,
                    ChartStyleMode.External, id,
                    ExhaustiveCalibrationRenderer.CalibrationTiles(active).Count == 0
                        ? null
                        : ExhaustiveCalibrationRenderer.CalibrationTiles(active),
                    active is null ? null : ExhaustiveCalibrationRenderer.PanelCaption(active),
                    active is null ? null : ExhaustiveCalibrationRenderer.PanelDescription(active)),
                _ => throw new ArgumentOutOfRangeException(nameof(chart), chart, null)
            };
        }

        private static string Prefix(ExhaustivePerformanceChart chart)
        {
            return chart switch
            {
                ExhaustivePerformanceChart.ScoreDistribution => "pd",
                ExhaustivePerformanceChart.Roc => "pr",
                ExhaustivePerformanceChart.PrecisionRecall => "pp",
                ExhaustivePerformanceChart.Calibration => "pc",
                ExhaustivePerformanceChart.Gains => "pg",
                ExhaustivePerformanceChart.ValueCaptured => "pv",
                ExhaustivePerformanceChart.Lift => "pl",
                ExhaustivePerformanceChart.Ks => "pk",
                ExhaustivePerformanceChart.ThresholdSweep => "pt",
                ExhaustivePerformanceChart.Confusion => "pm",
                _ => "pe"
            };
        }
    }
}