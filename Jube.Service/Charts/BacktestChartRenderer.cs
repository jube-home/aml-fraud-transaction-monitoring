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

// ReSharper disable NotAccessedPositionalProperty.Global

using Jube.Charting;
using Jube.Charting.Core.Models;
using Jube.Dto.Payload;

namespace Jube.Service.Charts
{
    public static class BacktestChartRenderer
    {
        public const int MaximumTags = 30;
        public const int MaximumTagNameLength = 60;

        public static string Funnel(BacktestChartRequest request)
        {
            Validate(request);

            return Chart.Funnel()
                .Title("From archive to firing")
                .Subtitle("Transactions read, kept by the filter and fired on by the rule")
                .Size(640, 260)
                .Theme(Theme(request))
                .Stage("Read", request.Scanned)
                .Stage("Kept by the filter", request.Evaluated)
                .Stage("Fired", request.Fired)
                .Render();
        }

        public static string Confusion(BacktestChartRequest request)
        {
            Validate(request);

            if (!request.ClassDefined)
            {
                throw new ArgumentException("A confusion matrix needs a definition of what is positive.");
            }

            return Chart.ConfusionMatrix()
                .Title("Confusion matrix")
                .Subtitle("What the rule did against what the transaction was")
                .XAxisTitle("Fired?")
                .YAxisTitle("Positive?")
                .Size(480, 360)
                .Theme(Theme(request))
                .Binary(request.TrueNegatives, request.FalsePositives, request.FalseNegatives, request.TruePositives,
                    "No", "Yes")
                .Render();
        }

        public static string Rates(BacktestChartRequest request)
        {
            Validate(request);

            var names = new List<string>();
            var values = new List<double>();

            void Add(string name, long numerator, long denominator)
            {
                names.Add(name);
                values.Add(denominator > 0 ? 100.0 * numerator / denominator : 0);
            }

            if (request.ClassDefined)
            {
                var precision = request.TruePositives + request.FalsePositives;
                var recall = request.TruePositives + request.FalseNegatives;
                var specificity = request.TrueNegatives + request.FalsePositives;

                Add("Precision", request.TruePositives, precision);
                Add("Recall", request.TruePositives, recall);

                var denominator = 2 * request.TruePositives + request.FalsePositives + request.FalseNegatives;

                Add("F1", 2 * request.TruePositives, denominator);
                Add("Specificity", request.TrueNegatives, specificity);
                Add("Positive rate", request.Positives, request.Evaluated);
            }

            Add("Fired rate", request.Fired, request.Evaluated);

            return Chart.Bar()
                .Title("Rates")
                .Subtitle("Precision, recall and the share of transactions fired on and positive")
                .Size(640, 300)
                .Theme(Theme(request))
                .Legend(LegendPosition.None)
                .Horizontal()
                .YAxisTitle("Percent")
                .ValueFormat(value => value.ToString("0.0") + "%")
                .Categories(names)
                .Series("Rate", values)
                .Render();
        }

        public static string Tags(BacktestChartRequest request)
        {
            Validate(request);

            var tags = request.Tags.OrderByDescending(tag => tag.Count).Take(MaximumTags).ToList();

            if (tags.Count == 0)
            {
                throw new ArgumentException("There are no tags to chart.");
            }

            return Chart.Bar()
                .Title("Tags on the evaluated transactions")
                .Size(640, Math.Max(220, 60 + tags.Count * 26))
                .Theme(Theme(request))
                .Legend(LegendPosition.None)
                .Horizontal()
                .YAxisTitle("Transactions")
                .Categories(tags.Select(tag => tag.Name.Length > MaximumTagNameLength
                    ? tag.Name[..MaximumTagNameLength]
                    : tag.Name))
                .Series("Transactions", tags.Select(tag => (double)tag.Count))
                .Render();
        }

        private static ThemeMode Theme(BacktestChartRequest request)
        {
            return request.Dark ? ThemeMode.Dark : ThemeMode.Light;
        }

        private static void Validate(BacktestChartRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var figures = new[]
            {
                request.Scanned, request.Evaluated, request.Fired, request.Positives, request.TruePositives,
                request.FalsePositives, request.FalseNegatives, request.TrueNegatives
            };

            if (figures.Any(figure => figure < 0) || request.Evaluated > request.Scanned ||
                request.Fired > request.Evaluated || request.Tags is null ||
                request.Tags.Count > 1000 || request.Tags.Any(tag => tag is null || tag.Name is null || tag.Count < 0))
            {
                throw new ArgumentException("The backtest figures are not consistent.");
            }
        }
    }
}
