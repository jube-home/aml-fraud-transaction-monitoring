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
using Jube.Charting.Scales;
using Jube.Charting.Exceptions;
using Jube.Charting.Core.Models;

namespace Jube.Charting.Charts
{
    public abstract class XyBuilderBase<TSelf> : ChartBuilder<TSelf> where TSelf : XyBuilderBase<TSelf>
    {
        protected List<PointSeries> SeriesList { get; } = [];
        protected XAxisKind XKind { get; set; } = XAxisKind.Linear;
        protected string[]? CategoryLabels { get; set; }
        protected AxisKind YKind { get; set; } = AxisKind.Linear;
        protected List<ReferenceLine> ReferenceList { get; } = [];
        protected (double Min, double Max)? XDomainFixed { get; set; }
        protected (double Min, double Max)? YDomainFixed { get; set; }
        protected bool ShowDataLabels { get; set; }
        protected List<PlotBand> BandList { get; } = [];
        protected List<Annotation> AnnotationList { get; } = [];
        protected Func<double, string>? XFormatter { get; set; }
        protected Func<double, string>? YFormatter { get; set; }
        protected Func<double, string>? Y2Formatter { get; set; }

        public TSelf Series(string name, IEnumerable<double> values, string? color = null)
        {
            var points = values.Select((v, i) => new DataPoint(i, v)).ToArray();
            SeriesList.Add(new PointSeries(name, points, color));

            return (TSelf)this;
        }

        public TSelf Series(string name, IEnumerable<double> xs, IEnumerable<double> ys, string? color = null)
        {
            var xa = xs.ToArray();
            var ya = ys.ToArray();

            if (xa.Length != ya.Length)
            {
                throw new ChartSpecificationException("X and Y values must have the same length.");
            }

            var points = new DataPoint[xa.Length];

            for (var i = 0; i < xa.Length; i++)
            {
                points[i] = new DataPoint(xa[i], ya[i]);
            }

            SeriesList.Add(new PointSeries(name, points, color));

            return (TSelf)this;
        }

        public TSelf Series(string name, IEnumerable<DataPoint> points, string? color = null)
        {
            SeriesList.Add(new PointSeries(name, [.. points], color));

            return (TSelf)this;
        }

        public TSelf Series(PointSeries series)
        {
            SeriesList.Add(series);

            return (TSelf)this;
        }

        public TSelf TimeSeries(string name, IEnumerable<DateTime> times, IEnumerable<double> values,
            string? color = null)
        {
            XKind = XAxisKind.Time;

            return Series(name, times.Select(TimeScale.ToSeconds), values, color);
        }

        public TSelf Categories(IEnumerable<string> categories)
        {
            CategoryLabels = [.. categories];
            XKind = XAxisKind.Category;

            return (TSelf)this;
        }

        public TSelf LogY()
        {
            YKind = AxisKind.Log;

            return (TSelf)this;
        }

        public TSelf LogX()
        {
            XKind = XAxisKind.Log;

            return (TSelf)this;
        }

        public TSelf XDomain(double min, double max)
        {
            XDomainFixed = (min, max);

            return (TSelf)this;
        }

        public TSelf YDomain(double min, double max)
        {
            YDomainFixed = (min, max);

            return (TSelf)this;
        }

        public TSelf Reference(double x1, double y1, double x2, double y2, string? label = null)
        {
            ReferenceList.Add(new ReferenceLine(x1, y1, x2, y2, label));

            return (TSelf)this;
        }

        public TSelf DataLabels(bool show = true)
        {
            ShowDataLabels = show;

            return (TSelf)this;
        }

        public TSelf XFormat(Func<double, string> format)
        {
            XFormatter = format;

            return (TSelf)this;
        }

        public TSelf YFormat(Func<double, string> format)
        {
            YFormatter = format;

            return (TSelf)this;
        }

        public TSelf SecondaryYFormat(Func<double, string> format)
        {
            Y2Formatter = format;

            return (TSelf)this;
        }

        public TSelf SecondaryYAxisTitle(string? title)
        {
            Options = Options with { SecondaryYAxisTitle = title };

            return (TSelf)this;
        }

        public TSelf OnSecondaryAxis()
        {
            if (SeriesList.Count == 0)
            {
                throw new ChartSpecificationException("Add a series before assigning it to the secondary axis.");
            }

            SeriesList[^1] = SeriesList[^1] with { SecondaryAxis = true };

            return (TSelf)this;
        }

        public TSelf HorizontalBand(double from, double to, string? label = null)
        {
            BandList.Add(new PlotBand(true, from, to, label));

            return (TSelf)this;
        }

        public TSelf VerticalBand(double from, double to, string? label = null)
        {
            BandList.Add(new PlotBand(false, from, to, label));

            return (TSelf)this;
        }

        public TSelf HorizontalLine(double y, string? label = null)
        {
            ReferenceList.Add(new ReferenceLine(double.NegativeInfinity, y, double.PositiveInfinity, y, label));

            return (TSelf)this;
        }

        public TSelf VerticalLine(double x, string? label = null)
        {
            ReferenceList.Add(new ReferenceLine(x, double.NegativeInfinity, x, double.PositiveInfinity, label));

            return (TSelf)this;
        }

        public TSelf Annotate(double x, double y, string text)
        {
            AnnotationList.Add(new Annotation(x, y, text));

            return (TSelf)this;
        }
    }
}