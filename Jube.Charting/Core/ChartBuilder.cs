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
using Jube.Charting.Interfaces;

namespace Jube.Charting.Core
{
    public abstract class ChartBuilder<TSelf> : IStyleModeChart where TSelf : ChartBuilder<TSelf>
    {
        protected ChartOptions Options { get; set; } = new();

        ISvgChart IStyleModeChart.WithStyleMode(ChartStyleMode mode)
        {
            return StyleMode(mode);
        }

        public abstract string Render();

        public TSelf Title(string? title)
        {
            Options = Options with { Title = title };

            return (TSelf)this;
        }

        public TSelf Subtitle(string? subtitle)
        {
            Options = Options with { Subtitle = subtitle };

            return (TSelf)this;
        }

        public TSelf XAxisTitle(string? title)
        {
            Options = Options with { XAxisTitle = title };

            return (TSelf)this;
        }

        public TSelf YAxisTitle(string? title)
        {
            Options = Options with { YAxisTitle = title };

            return (TSelf)this;
        }

        public TSelf Description(string? description)
        {
            Options = Options with { Description = description };

            return (TSelf)this;
        }

        public TSelf Id(string? id)
        {
            Options = Options with { Id = id };

            return (TSelf)this;
        }

        public TSelf Size(double width, double height)
        {
            Options = Options with { Width = width, Height = height };

            return (TSelf)this;
        }

        public TSelf FixedSize(double width, double height)
        {
            Options = Options with { Width = width, Height = height, Sizing = ChartSizing.Fixed };

            return (TSelf)this;
        }

        public TSelf Theme(ThemeMode mode)
        {
            Options = Options with { Theme = mode };

            return (TSelf)this;
        }

        public TSelf Legend(LegendPosition position)
        {
            Options = Options with { Legend = position };

            return (TSelf)this;
        }

        public TSelf LegendMaxRows(int rows)
        {
            Options = Options with { LegendMaxRows = rows };

            return (TSelf)this;
        }

        public TSelf TransparentBackground(bool transparent = true)
        {
            Options = Options with { TransparentBackground = transparent };

            return (TSelf)this;
        }

        public TSelf Interactive(bool interactive)
        {
            Options = Options with { Interactive = interactive };

            return (TSelf)this;
        }

        public TSelf StyleMode(ChartStyleMode mode)
        {
            Options = Options with { StyleMode = mode };

            return (TSelf)this;
        }

        public TSelf ExternalStyles()
        {
            return StyleMode(ChartStyleMode.External);
        }

        public TSelf WithOptions(ChartOptions options)
        {
            Options = options;

            return (TSelf)this;
        }

        public override string ToString()
        {
            return Render();
        }
    }
}