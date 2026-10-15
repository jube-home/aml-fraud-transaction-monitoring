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

using Jube.Charting.Core;
using Jube.Charting.More.Models;

namespace Jube.Charting.More
{
    public sealed class SurvivalChartBuilder : ChartBuilder<SurvivalChartBuilder>
    {
        private readonly List<SurvivalGroup> groups = [];
        private bool atRisk = true;
        private bool bands = true;
        private bool censor = true;
        private double confidence = 0.95;
        private bool cumulative;
        private bool logRank = true;
        private bool median = true;
        private ConfidenceMethod method = ConfidenceMethod.LogLog;
        private (double Min, double Max)? xDomain;
        private Func<double, string>? xFormat;

        public SurvivalChartBuilder Group(string name, IEnumerable<double> times, IEnumerable<bool> events,
            string? color = null)
        {
            groups.Add(new SurvivalGroup(name, [.. times], [.. events], color));

            return this;
        }

        public SurvivalChartBuilder Group(string name, IEnumerable<double> times, IEnumerable<int> events,
            string? color = null)
        {
            return Group(name, times, events.Select(e => e != 0), color);
        }

        public SurvivalChartBuilder Curve(string name, SurvivalCurve curve, string? color = null)
        {
            groups.Add(new SurvivalGroup(name, [], [], color, curve));

            return this;
        }

        public SurvivalChartBuilder ConfidenceLevel(double level,
            ConfidenceMethod confidenceMethod = ConfidenceMethod.LogLog)
        {
            confidence = level;
            method = confidenceMethod;

            return this;
        }

        public SurvivalChartBuilder Bands(bool show = true)
        {
            bands = show;

            return this;
        }

        public SurvivalChartBuilder CensorMarks(bool show = true)
        {
            censor = show;

            return this;
        }

        public SurvivalChartBuilder AtRiskTable(bool show = true)
        {
            atRisk = show;

            return this;
        }

        public SurvivalChartBuilder MedianMarkers(bool show = true)
        {
            median = show;

            return this;
        }

        public SurvivalChartBuilder LogRank(bool show = true)
        {
            logRank = show;

            return this;
        }

        public SurvivalChartBuilder Cumulative(bool enabled = true)
        {
            cumulative = enabled;

            return this;
        }

        public SurvivalChartBuilder XDomain(double min, double max)
        {
            xDomain = (min, max);

            return this;
        }

        public SurvivalChartBuilder XFormat(Func<double, string> format)
        {
            xFormat = format;

            return this;
        }

        public SurvivalChart Build()
        {
            return new SurvivalChart(new SurvivalChartSpec
            {
                Options = Options,
                Groups = [.. groups],
                Confidence = confidence,
                Method = method,
                Bands = bands,
                CensorMarks = censor,
                AtRisk = atRisk,
                MedianMarkers = median,
                LogRankTest = logRank,
                Cumulative = cumulative,
                XDomain = xDomain,
                XFormat = xFormat
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}