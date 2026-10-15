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

namespace Jube.Service.Charts
{
    internal static class AdvancedAnalyticsNarrativeRules
    {
        private static string P(double value)
        {
            return value.ToString("0.0%", CultureInfo.InvariantCulture);
        }

        private static string N(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        public static IReadOnlyList<string> Holistic(AdvancedAnalyticsNarrativeFacts facts)
        {
            var paragraphs = new List<string>();

            if (facts.Variables.Count > 0)
            {
                paragraphs.Add("This is a " + facts.FriendlyKind + " model (" + (facts.ModelName ?? "model") +
                               " version " + facts.VersionNumber + "), trained on " + facts.Variables.Count +
                               " variable" + (facts.Variables.Count == 1 ? "" : "s") + " (" +
                               string.Join(", ", facts.Variables) + ") with seed " + facts.Seed + ".");
            }
            else
            {
                paragraphs.Add("This is a " + facts.FriendlyKind + " model (" + (facts.ModelName ?? "model") +
                               " version " + facts.VersionNumber + ").");
            }

            if (facts.SplitSizes is { } sizes && sizes.Train + sizes.Validation + sizes.Test > 0)
            {
                paragraphs.Add("It was fitted on " + sizes.Train.ToString("N0", CultureInfo.InvariantCulture) +
                               " training rows, with " +
                               sizes.Validation.ToString("N0", CultureInfo.InvariantCulture) +
                               " held back for validation and " +
                               sizes.Test.ToString("N0", CultureInfo.InvariantCulture) +
                               " never used for fitting or calibration, held back as an honest test set.");
            }

            if (facts.Quality is { } quality && quality.RocAuc is { } auc)
            {
                var band = ExhaustiveNarrativeRules.DiscriminationBand(auc);

                paragraphs.Add("On its held-out test rows, this model has a ROC AUC of " + N(auc) + " — " + band +
                               " discrimination between the classes.");
            }

            if (facts.Warnings.Count > 0)
            {
                paragraphs.Add((facts.Warnings.Count == 1
                                   ? "One warning was raised during training: "
                                   : facts.Warnings.Count + " warnings were raised during training: ") +
                               string.Join(" ", facts.Warnings.Select(w => w.Message)));
            }

            return paragraphs;
        }

        public static IReadOnlyList<string> ModelDiagram(AdvancedAnalyticsNarrativeFacts facts)
        {
            if (facts.Variables.Count == 0)
            {
                return [];
            }

            return
            [
                "The diagram shows this " + facts.FriendlyKind + " model's structure across its " +
                facts.Variables.Count + " input variable" + (facts.Variables.Count == 1 ? "" : "s") + "."
            ];
        }

        public static IReadOnlyList<string> Calibration(AdvancedAnalyticsNarrativeFacts facts)
        {
            if (!facts.HasCalibrationCurve || facts.Quality is not { } quality ||
                quality.Calibrated is not { } calibrated)
            {
                return [];
            }

            if (!calibrated)
            {
                return
                [
                    "Its scores are not calibrated: they are only meaningful for ranking one case against " +
                    "another, and should not be read as a probability or a percentage chance of being the " +
                    "positive class."
                ];
            }

            var paragraph = "Its scores are calibrated, so a score of 0.30 is meant to be read as roughly a " +
                            "30% chance of being the positive class, not just a ranking.";

            if (quality.BrierScore is { } brier)
            {
                paragraph += " Brier score " + N(brier) + " (lower is better) describes how close that promise " +
                             "is kept.";
            }

            return [paragraph];
        }

        public static IReadOnlyList<string> Roc(AdvancedAnalyticsNarrativeFacts facts)
        {
            if (facts.Quality is not { RocAuc: { } auc } quality || facts.RocPointCount < 2)
            {
                return [];
            }

            var band = ExhaustiveNarrativeRules.DiscriminationBand(auc);

            var paragraphs = new List<string>
            {
                "On " + (quality.Rows is { } rows ? rows.ToString("N0", CultureInfo.InvariantCulture) + " " : "") +
                "held-out test rows, this model has an AUC of " + N(auc) + " — by the common Hosmer-Lemeshow " +
                "convention, that is " + band + " discrimination between the positive and negative classes."
            };

            if (quality.Rows is { } r and > 0 && quality.Positives is { } positives)
            {
                var prevalence = positives / (double)r;

                if (prevalence is > 0 and < 0.05)
                {
                    paragraphs.Add("The positive class is rare in this sample (under 5%), so accuracy alone " +
                                   "would be misleading — look at precision, recall and the AUC instead.");
                }
            }

            if (quality.F1 is { } f1)
            {
                paragraphs.Add("At its default threshold, F1 is " + N(f1) +
                               (quality.Precision is { } precision && quality.Recall is { } recall
                                   ? " (precision " + P(precision) + ", recall " + P(recall) + ")."
                                   : "."));
            }

            return paragraphs;
        }
    }
}