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
using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Models;

namespace Jube.Service.ModelDiagrams.Adapters
{
    internal sealed record CoefficientEntry(string Name, double Estimate)
    {
        public double? StandardError { get; init; }
        public double? Low { get; init; }
        public double? High { get; init; }
        public double? PValue { get; init; }
    }

    internal static class CoefficientMapping
    {
        public static CoefficientDiagram Build(IReadOnlyList<CoefficientEntry> entries, ModelDiagramOptions options,
            string title, string? subtitle, string estimateName, bool oddsRatioColumn, bool logScale,
            string description)
        {
            options.RequireWithin(entries.Count, "coefficients");
            var builder = Diagram.Coefficients().EstimateName(estimateName);

            if (logScale)
            {
                builder.LogScale();
            }
            else if (oddsRatioColumn)
            {
                builder.OddsRatios();
            }

            var spec = builder.Build().Spec;

            var items = entries.Select(e => new CoefficientItem(e.Name, e.Estimate)
            {
                StandardError = e.StandardError,
                Low = e.Low,
                High = e.High,
                PValue = e.PValue
            }).ToArray();

            return new CoefficientDiagram(spec with
            {
                Items = items,
                Options = options.ToChartOptions(title, subtitle, description)
            });
        }

        public static string Short(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}