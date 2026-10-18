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
using Jube.Charting.Charts.Models;
using Jube.Charting.Core;

namespace Jube.Charting.Charts
{
    public sealed class CandlestickChartBuilder : ChartBuilder<CandlestickChartBuilder>
    {
        private readonly List<Candle> candles = [];
        private Func<double, string>? format;
        private CandleStyle style;

        public CandlestickChartBuilder Candle(string label, double open, double high, double low, double close)
        {
            candles.Add(new Candle(label, open, high, low, close));

            return this;
        }

        public CandlestickChartBuilder Candle(DateTime time, double open, double high, double low, double close)
        {
            return Candle(time.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), open, high, low,
                close);
        }

        public CandlestickChartBuilder Ohlc(bool enabled = true)
        {
            style = enabled ? CandleStyle.Ohlc : CandleStyle.Candlestick;

            return this;
        }

        public CandlestickChartBuilder ValueFormat(Func<double, string> formatter)
        {
            format = formatter;

            return this;
        }

        public CandlestickChart Build()
        {
            return new CandlestickChart(new CandlestickChartSpec
            {
                Options = Options,
                Candles = [.. candles],
                Style = style,
                ValueFormat = format
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}