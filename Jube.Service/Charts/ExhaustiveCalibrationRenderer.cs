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

using System.Globalization;
using Jube.Charting;
using Jube.Charting.Core.Models;
using Jube.Charting.Performance.Models;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceQuery;
using Jube.Engine.Exhaustive.Calibration;

namespace Jube.Service.Charts
{
    public static class ExhaustiveCalibrationRenderer
    {
        public const string NotCalibratedHistorical = "Not calibrated (promoted before calibration was introduced)";
        public const string RawSeriesName = "Raw network output";
        public const string CalibratedSeriesName = "Calibrated probability";

        public static ExhaustiveCalibration ToCalibration(
            ExhaustiveSearchInstancePromotedTrialInstanceQueryDto? promoted)
        {
            return promoted == null
                ? ExhaustiveCalibration.Identity
                : ExhaustiveCalibration.FromColumns(promoted.CalibrationMethod?.ToLowerInvariant(),
                    promoted.CalibrationInput?.ToLowerInvariant(),
                    promoted.CalibrationSlope, promoted.CalibrationIntercept, promoted.CalibrationEpsilon,
                    promoted.CalibrationPrior, promoted.BaseRate, promoted.BaseRateSource,
                    promoted.CalibrationParametersJson);
        }

        public static ExhaustiveSearchInstancePromotedTrialInstanceQueryDto? Active(
            IEnumerable<ExhaustiveSearchInstancePromotedTrialInstanceQueryDto> promoted)
        {
            return promoted.Where(p => p.Active).OrderByDescending(p => p.Id).FirstOrDefault();
        }

        public static List<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto> Calibrate(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto> rows,
            ExhaustiveCalibration calibration)
        {
            if (calibration.IsInvalid || !calibration.IsCalibrated)
            {
                return rows.ToList();
            }

            return rows.Select(r =>
            {
                var predicted = calibration.ApplyAtSamplePrior(r.Predicted);

                return new ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto
                {
                    Predicted = predicted,
                    Actual = r.Actual,
                    Error = r.Actual - predicted,
                    Value = r.Value
                };
            }).ToList();
        }

        public static string Caption(ExhaustiveSearchInstancePromotedTrialInstanceQueryDto? promoted)
        {
            if (promoted == null)
            {
                return NotCalibratedHistorical;
            }

            var calibration = ToCalibration(promoted);

            if (calibration.IsInvalid)
            {
                return "The stored calibrator is invalid and the model does not score: " + calibration.InvalidReason;
            }

            if (!calibration.IsCalibrated)
            {
                return string.IsNullOrEmpty(promoted.CalibrationStatus)
                    ? NotCalibratedHistorical
                    : "Not calibrated: " + promoted.CalibrationReason;
            }

            var ci = CultureInfo.InvariantCulture;

            var method = calibration.Method switch
            {
                ExhaustiveCalibration.MethodPlatt => "Platt scaling on the validation split (slope "
                                                     + promoted.CalibrationSlope!.Value.ToString("0.###", ci) +
                                                     ", intercept "
                                                     + promoted.CalibrationIntercept!.Value.ToString("0.###", ci) + ", "
                                                     + (promoted.CalibrationSampleSize ?? 0) + " rows)",
                ExhaustiveCalibration.MethodIsotonic => "Isotonic regression on the validation split ("
                                                        + (promoted.CalibrationSampleSize ?? 0) + " rows)",
                ExhaustiveCalibration.MethodBeta => "Beta calibration on the validation split ("
                                                    + (promoted.CalibrationSampleSize ?? 0) + " rows)",
                _ => "Calibrated on the validation split (" + (promoted.CalibrationSampleSize ?? 0) + " rows)"
            };

            var text = method + ". Curves are the test split at the sample "
                              + "prior " + (promoted.CalibrationPrior ?? 0.5).ToString("0.###", ci) + ".";

            text += promoted.BaseRate.HasValue
                ? " Recall applies the base rate " + promoted.BaseRate.Value.ToString("0.####", ci)
                                                   + " (" + promoted.BaseRateSource + ")."
                : " No base rate shift is applied (" + promoted.BaseRateSource + ").";

            text += promoted.CalibrationStatus == ExhaustiveCalibration.StatusCalibratedLowSample
                ? " Low sample warning: " + promoted.CalibrationReason
                : string.Empty;

            return text;
        }

        public static string PanelCaption(ExhaustiveSearchInstancePromotedTrialInstanceQueryDto? promoted)
        {
            var calibration = ToCalibration(promoted);

            if (calibration.IsInvalid || !calibration.IsCalibrated)
            {
                return Caption(promoted);
            }

            var ci = CultureInfo.InvariantCulture;

            return "Calibrated at the sample prior " + (promoted!.CalibrationPrior ?? 0.5).ToString("0.###", ci)
                                                     + (promoted.BaseRate.HasValue
                                                         ? ", recall base rate " +
                                                           promoted.BaseRate.Value.ToString("0.####", ci)
                                                         : string.Empty);
        }

        public static string PanelDescription(ExhaustiveSearchInstancePromotedTrialInstanceQueryDto? promoted)
        {
            var calibration = ToCalibration(promoted);

            if (calibration.IsInvalid || !calibration.IsCalibrated)
            {
                return Caption(promoted);
            }

            return Caption(promoted) + " Thresholds now apply to calibrated probabilities: at a low base rate few "
                                     + "rows exceed 0.5, so consider the minimum-risk threshold from the threshold sweep.";
        }

        public static IReadOnlyList<MetricTile> CalibrationTiles(
            ExhaustiveSearchInstancePromotedTrialInstanceQueryDto? promoted)
        {
            var tiles = new List<MetricTile>();

            if (promoted == null || !ToCalibration(promoted).IsCalibrated)
            {
                return tiles;
            }

            var ci = CultureInfo.InvariantCulture;
            var quality = CalibrationQuality.FromJson(promoted.CalibrationQualityJson);

            void Add(string name, double? value, string? detail, string description)
            {
                if (value.HasValue && double.IsFinite(value.Value))
                {
                    tiles.Add(new MetricTile(name, value.Value, detail, description));
                }
            }

            Add("Calibrator slope", promoted.CalibrationSlope,
                "Platt, " + (promoted.CalibrationSampleSize ?? 0) + " validation rows",
                "Slope of the Platt calibrator fitted on the validation split. Above 1 the raw output was under-confident, below 1 over-confident.");

            Add("Calibrator intercept", promoted.CalibrationIntercept, promoted.CalibrationInput + " input",
                "Intercept of the Platt calibrator, on the " + promoted.CalibrationInput + " of the network output.");

            if (quality?.Test != null)
            {
                Add("Raw ECE", quality.Test.Ece.Raw, "calibrated " + Text(quality.Test.Ece.Calibrated, ci),
                    "Expected calibration error of the uncalibrated network output on the test rows.");
                Add("Raw Brier score", quality.Test.Brier.Raw, "calibrated " + Text(quality.Test.Brier.Calibrated, ci),
                    "Brier score of the uncalibrated network output on the test rows.");
                Add("Raw log loss", quality.Test.LogLoss.Raw, "calibrated " + Text(quality.Test.LogLoss.Calibrated, ci),
                    "Log loss of the uncalibrated network output on the test rows.");
            }

            Add("Training prior", promoted.TrainingPrior, null,
                "Positive class proportion of the training split the network learned from.");

            Add("Base rate", promoted.BaseRate, promoted.BaseRateSource,
                "Deployment base rate the recalled probability is shifted to, and where it came from.");

            if (quality?.TestAtBaseRate != null)
            {
                Add("Brier at base rate", quality.TestAtBaseRate.Brier.Calibrated,
                    "raw " + Text(quality.TestAtBaseRate.Brier.Raw, ci),
                    "Test rows reweighted to the base rate: Brier score of the recalled probability versus the raw output.");
                Add("Log loss at base rate", quality.TestAtBaseRate.LogLoss.Calibrated,
                    "raw " + Text(quality.TestAtBaseRate.LogLoss.Raw, ci),
                    "Test rows reweighted to the base rate: log loss of the recalled probability versus the raw output.");
            }

            return tiles;
        }

        private static string Text(double? value, CultureInfo ci)
        {
            return value.HasValue ? value.Value.ToString("0.###", ci) : "n/a";
        }

        public static string Comparison(
            IReadOnlyList<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto> rows,
            ExhaustiveSearchInstancePromotedTrialInstanceQueryDto? promoted,
            ChartStyleMode styleMode = ChartStyleMode.External, string? id = null)
        {
            const string title = "Calibration: raw versus calibrated";

            if (rows.Count == 0)
            {
                return ExhaustiveChartRenderer.Empty(title, "Mean predicted probability",
                    "Observed fraction of positives", styleMode: styleMode, id: id);
            }

            var calibration = ToCalibration(promoted);
            var actual = rows.Select(r => r.Actual >= 0.5).ToArray();
            var raw = rows.Select(r => Math.Min(Math.Max(r.Predicted, 0d), 1d)).ToArray();

            var builder = Chart.Calibration()
                .Title(title)
                .Subtitle(Caption(promoted))
                .Description(Caption(promoted))
                .Legend(LegendPosition.Bottom)
                .FromProbabilities(calibration.IsCalibrated ? RawSeriesName : "Raw network output (not calibrated)",
                    actual, raw);

            if (calibration.IsCalibrated)
            {
                builder.FromProbabilities(CalibratedSeriesName, actual,
                    rows.Select(r => calibration.ApplyAtSamplePrior(r.Predicted)).ToArray());
            }

            return builder.StyleMode(styleMode).Id(id).Render();
        }
    }
}