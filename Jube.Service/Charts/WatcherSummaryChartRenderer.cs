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
using Jube.Charting.Exceptions;
using Jube.Dto.Payload;

namespace Jube.Service.Charts
{
    public static class WatcherSummaryChartRenderer
    {
        public const int MaximumRules = 100;
        public const int MaximumNameLength = 60;

        public static string Render(WatcherSummaryChartRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.Rules is null || request.Rules.Count is < 1 or > MaximumRules ||
                request.Rules.Any(r => r is null || r.Name is null || r.Frequency < 0 || !double.IsFinite(r.Aggregate)))
            {
                throw new ArgumentException("The summary needs between 1 and " + MaximumRules +
                                            " activation rules with finite figures.");
            }

            var names = request.Rules.Select(r =>
                r.Name.Length > MaximumNameLength ? r.Name[..MaximumNameLength] : r.Name);

            try
            {
                return Chart.Combo()
                    .Title("Activations by rule")
                    .Subtitle("How often each activation rule fired, and the response elevation it added")
                    .XAxisTitle("Activation rule")
                    .YAxisTitle("Activations")
                    .SecondaryYAxisTitle("Aggregate response elevation")
                    .Size(900, 320)
                    .Legend(LegendPosition.Top)
                    .Theme(request.Dark ? ThemeMode.Dark : ThemeMode.Light)
                    .Categories(names)
                    .Columns("Activations", request.Rules.Select(r => (double)r.Frequency),
                        string.IsNullOrWhiteSpace(request.ColumnColor) ? null : request.ColumnColor)
                    .Line("Aggregate response elevation", request.Rules.Select(r => r.Aggregate),
                        string.IsNullOrWhiteSpace(request.LineColor) ? null : request.LineColor, true)
                    .Render();
            }
            catch (ChartSpecificationException e)
            {
                throw new ArgumentException(e.Message, e);
            }
        }
    }
}
