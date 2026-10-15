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

using Jube.Charting.Charts.Models;
using Jube.Charting.Core;

namespace Jube.Charting.Charts
{
    public sealed class BulletChartBuilder : ChartBuilder<BulletChartBuilder>
    {
        private readonly List<BulletItem> items = [];
        private Func<double, string>? format;
        private bool thick;

        public BulletChartBuilder Item(string label, double value, double? target = null,
            IEnumerable<double>? thresholds = null)
        {
            items.Add(new BulletItem(label, value, target, thresholds?.ToArray()));

            return this;
        }

        public BulletChartBuilder Thick(bool thickness = true)
        {
            thick = thickness;

            return this;
        }

        public BulletChartBuilder ValueFormat(Func<double, string> formatter)
        {
            format = formatter;

            return this;
        }

        public BulletChart Build()
        {
            return new BulletChart(new BulletChartSpec
                { Options = Options, Items = [.. items], ValueFormat = format, Thick = thick });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}